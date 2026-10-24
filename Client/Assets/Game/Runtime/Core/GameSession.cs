using System;
using System.Collections.Generic;

namespace LastBell
{
    // Pure C# rule owner. Views issue commands and read these properties.
    // ExportSave returns a detached DTO; JsonUtility/file IO belong to Bootstrap.
    public sealed partial class GameSession
    {
        private readonly GameCatalog catalog;
        private GameSaveData state;
        private bool paused;
        private const double TimeEpsilonMs = 0.0001;

        public event Action Changed;
        public event Action SaveRequested;
        public event Action<SessionEvent> EventRaised;

        public GameSession(GameCatalog catalog, GameSaveData saved = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Initialize(saved);
            // Retain the old save field for compatibility, but hit counts are retired.
            if(state.run!=null)state.run.hitCombo=0;
        }

        public GameCatalog Catalog => catalog;
        public SessionPhase Phase => state.phase;
        public bool IsPaused => paused;
        public bool IsDirty { get; private set; }
        public string LastMessage { get; private set; } = "";
        public string CurrentCharacterId => state.selectedCharacterId;
        public long Gold => state.gold;
        public long ExchangeRate => catalog.GetExchangeRate(state.highestClearedStage);
        public long PendingMaxRelicGold => state.run?.offer?.maxGold ?? 0;
        public int Fragments => state.fragments;
        public int StorageCapacity => state.storageCapacity;
        public int OwnedRelicCount => state.relics.Count;
        public int NextStorageExpansionCost => StorageCapacity >= catalog.relicRules.maxOwned ? 0 :
            catalog.economy.storageExpansionCosts[StorageCapacity - catalog.relicRules.initialCapacity];
        public int UnlockedSlots => StorageCapacity; // Legacy probe API; capacity now limits ownership.
        public int Stage => state.run == null || Phase == SessionPhase.Lobby || Phase == SessionPhase.ExchangeConfirmation ? CheckpointStartStage : state.run.stage;
        public int Cycle => CurrentCharacter.cycle;
        public int CheckpointStartStage => CurrentCharacter.checkpoint >= 10 && catalog.mvpLastStage >= 11 ? 11 : 1;
        public int TutorialStep => state.tutorialStep;
        public double ClockMs => state.run == null ? 0 : state.run.clockMs;
        public float DodgeRecoverySeconds => state.run == null ? 0 : (float)Math.Max(0,(state.run.dodgeStartMs+catalog.combat.dodgeActiveMs+catalog.combat.dodgeRecoveryMs-ClockMs)/1000);
        public float AttackRecoverySeconds => state.run == null ? 0 : (float)Math.Max(0,(Math.Max(state.run.attackReadyAtMs,CanPerfectCounter?0:state.run.dodgeStartMs+catalog.combat.dodgeActiveMs)-ClockMs)/1000);
        public float PlayerHp => state.run == null ? 0 : state.run.playerHpMilli / 1000f;
        public float PlayerMaxHp => state.run == null ? 0 : state.run.combatStats.maxHpMilli / 1000f;
        public float EnemyHp => state.run == null ? 0 : state.run.enemyHpMilli / 1000f;
        public float EnemyMaxHp => state.run == null ? 0 : state.run.enemyMaxHpMilli / 1000f;
        public long PlayerMaxHpMilli => state.run?.combatStats?.maxHpMilli ?? 0;
        public long EnemyMaxHpMilli => state.run?.enemyMaxHpMilli ?? 0;
        public float Stagger => state.run == null ? 0 : (float)state.run.stagger;
        public int HitCombo => state.run?.hitCombo ?? 0;
        public int PerfectStreak => state.run?.perfectStreak ?? 0;
        public double AttackSpeed => state.run?.attackSpeed > 0 ? state.run.attackSpeed : 1;
        public long DefeatedGold => state.run?.defeatedGold ?? 0;
        public bool IsEnemyStunned => state.run != null && state.run.stunUntilMs > ClockMs;
        public bool IsStunReady => state.run != null && !IsEnemyStunned && state.run.stagger + TimeEpsilonMs >= catalog.combat.staggerThreshold;
        public bool IsStaggerImmune => state.run != null && state.run.immunityUntilMs > ClockMs;
        public float StunSecondsRemaining => IsEnemyStunned ? (float)((state.run.stunUntilMs - ClockMs) / 1000) : 0;
        public float StunDurationSeconds => state.run==null?0:(catalog.GetStunDurationMs(catalog.GetStage(Stage).kind)+state.run.stunExtensionMs)/1000f;
        public EnemyIntentView EnemyIntent => GetEnemyIntent();
        public bool IsEnemyImpactPending => IsPendingEnemyImpact(state.run);
        public TransitionView Transition => GetTransition();
        public StoryView Story => GetStory();
        public RunResultView Result => (state.run==null||!state.run.ended||Phase==SessionPhase.Lobby||Phase==SessionPhase.ExchangeConfirmation||Phase==SessionPhase.Prologue)?null:new RunResultView {
            Cleared=state.run.cleared,Retired=!state.run.cleared&&state.run.playerHpMilli>0,DefeatedStage=state.run.stage,RetryStage=CheckpointStartStage,Gold=state.run.earnedGold,Fragments=state.run.earnedFragments,SinceUpdate=state.run.earningsSinceUpdate
        };
        public IReadOnlyList<string> RelicChoices => Array.AsReadOnly(state.run?.offer?.relicIds ?? new string[0]);
        public IReadOnlyList<string> OwnedRelicIds => Array.AsReadOnly(AllOwnedRelicIds());
        public IReadOnlyList<string> EquippedRelicIds => OwnedRelicIds;
        public ExchangeQuoteData PendingExchange => CloneQuote(state.exchange);
        public bool CanConfirmExchange => state.exchange != null && state.fragments >= state.exchange.fragments;
        public CombatStatsData PlayerStats => CloneStats(state.run == null || Phase == SessionPhase.Lobby || Phase == SessionPhase.ExchangeConfirmation ? BuildLobbyStats() : state.run.combatStats);

        public void Tick(float deltaSeconds) { AdvanceTime(deltaSeconds); }
        // Call once after collecting the inputs for a displayed frame. Unlike Tick's
        // exact-time simulation, this exposes a full cue before resolving its impact.
        public void TickInputFrame(float deltaSeconds) { AdvanceTime(deltaSeconds, true); }
        public bool Attack() { return BeginAttack(); }
        public bool UseSkill() { return BeginSkill(); }
        public bool Dodge(DodgeDirection direction) { return BeginDodge(direction); }
        public bool StartRun() { return BeginRun(); }
        public bool RetryRun()
        {
            if(state.phase!=SessionPhase.DeathResult&&state.phase!=SessionPhase.MvpComplete)return false;
            state.phase=SessionPhase.Lobby;
            return BeginRun();
        }
        public bool ReturnToLobby() { return EndRun(); }
        public bool ChooseRelic(string id, bool expandStorage = false) { return CommitRelicChoice(id, expandStorage, false); }
        public bool ConvertRelicRewardToGold(string id) { return CommitRelicChoice(id, false, true); }
        public bool NeedsRelicStorage(string id) => FindRelicLevel(id) == 0 && OwnedRelicCount >= StorageCapacity;
        public bool AdvanceStory() { return AdvanceStoryLine(false); }
        public bool SkipStory() { return AdvanceStoryLine(true); }
        public bool Upgrade(int statIndex) { return UpgradeStat(statIndex); }
        public bool ConfirmExchange() { return CommitExchange(); }
        public bool CancelExchange() { return DismissExchange(); }
        public bool EquipRelic(string id) { return Reject("보유한 유물은 모두 자동 적용됩니다."); }
        public bool UnequipRelic(string id) { return Reject("보유한 유물은 모두 자동 적용됩니다."); }
        public bool SelectCharacter(string id) { return ChangeCharacter(id); }
        public bool BuyNextSlot() { return PurchaseSlot(); }
        public bool ExpandRelicStorage() { return PurchaseSlot(); }
        public void SetPaused(bool value)
        {
            if (paused && !value && IsPendingEnemyImpact(state.run)) pendingImpactNeedsDisplay = true;
            paused = value;
            Changed?.Invoke();
        }
        public void MarkSaved() { IsDirty = false; }
        public GameSaveData ExportSave() { return CloneSave(practiceBackupState??state); }
        public int GetStatLevel(int index) { return index >= 0 && index < 5 ? CurrentCharacter.statLevels[index] : 0; }
        public int GetRelicLevel(string id) { return FindRelicLevel(id); }
        public long GetUpgradeCost(int index) { return StatUpgradeCost(index); }

        private CharacterProgressData CurrentCharacter => state.characters.Find(x => x.id == state.selectedCharacterId);

        private void Publish(bool requestSave = true)
        {
            if(practiceBackupState!=null){Changed?.Invoke();return;}
            IsDirty = true;
            Changed?.Invoke();
            if (requestSave) SaveRequested?.Invoke();
        }

        private bool Reject(string message)
        {
            LastMessage = message;
            Changed?.Invoke();
            return false;
        }

        private void Emit(SessionEventKind kind, long damage = 0, bool critical = false, DodgeDirection direction = DodgeDirection.Up, string relicId = null, int? eventStage = null, double staggerBefore = 0, double staggerGained = 0, bool perfectCounter = false, string patternId = null, long goldReward = 0, string skillId = null, long absorbedMilli = 0, string companionId = null, long recoveryMilli = 0, string serviceId = null)
        {
            EventRaised?.Invoke(new SessionEvent { ServiceId = serviceId, CompanionId = companionId, RecoveryMilli = recoveryMilli, SkillId = skillId, AbsorbedMilli = absorbedMilli, GoldReward = goldReward, TimeMs = ClockMs, PerfectCounter = perfectCounter, PatternId = patternId ?? state.run?.patternId, Kind = kind, DamageMilli = damage, Critical = critical, Direction = direction, Stage = eventStage ?? state.run?.stage ?? Stage, RelicId = relicId, StaggerBefore = staggerBefore, StaggerGained = staggerGained });
        }
    }
}
