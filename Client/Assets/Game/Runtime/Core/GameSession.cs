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
        public int UnlockedSlots => state.unlockedSlots;
        public int Stage => state.run == null || Phase == SessionPhase.Lobby || Phase == SessionPhase.ExchangeConfirmation ? CheckpointStartStage : state.run.stage;
        public int Cycle => CurrentCharacter.cycle;
        public int CheckpointStartStage => CurrentCharacter.checkpoint + 1;
        public int TutorialStep => state.tutorialStep;
        public double ClockMs => state.run == null ? 0 : state.run.clockMs;
        public float DodgeRecoverySeconds => state.run == null ? 0 : (float)Math.Max(0,(state.run.dodgeStartMs+catalog.combat.dodgeActiveMs+catalog.combat.dodgeRecoveryMs-ClockMs)/1000);
        public float AttackRecoverySeconds => state.run == null||CanParry ? 0 : IsParryWindow?DodgeRecoverySeconds:(float)Math.Max(0,(Math.Max(state.run.attackReadyAtMs,CanPerfectCounter?0:state.run.dodgeStartMs+catalog.combat.dodgeActiveMs)-ClockMs)/1000);
        public float PlayerHp => state.run == null ? 0 : state.run.playerHpMilli / 1000f;
        public float PlayerMaxHp => state.run == null ? 0 : state.run.combatStats.maxHpMilli / 1000f;
        public float EnemyHp => state.run == null ? 0 : state.run.enemyHpMilli / 1000f;
        public float EnemyMaxHp => state.run == null ? 0 : state.run.enemyMaxHpMilli / 1000f;
        public float Stagger => state.run == null ? 0 : (float)state.run.stagger;
        public bool IsEnemyStunned => state.run != null && state.run.stunUntilMs > ClockMs;
        public bool IsStaggerImmune => state.run != null && state.run.immunityUntilMs > ClockMs;
        public float StunSecondsRemaining => IsEnemyStunned ? (float)((state.run.stunUntilMs - ClockMs) / 1000) : 0;
        public EnemyIntentView EnemyIntent => GetEnemyIntent();
        public bool IsEnemyImpactPending => IsPendingEnemyImpact(state.run);
        public TransitionView Transition => GetTransition();
        public StoryView Story => GetStory();
        public RunResultView Result => state.phase!=SessionPhase.DeathResult?null:new RunResultView {
            DefeatedStage=state.run.stage,RetryStage=CheckpointStartStage,Gold=state.run.earnedGold,Fragments=state.run.earnedFragments,SinceUpdate=state.run.earningsSinceUpdate
        };
        public IReadOnlyList<string> RelicChoices => Array.AsReadOnly(state.run?.offer?.relicIds ?? new string[0]);
        public IReadOnlyList<string> EquippedRelicIds => Array.AsReadOnly(CurrentCharacter.equippedRelicIds);
        public ExchangeQuoteData PendingExchange => CloneQuote(state.exchange);
        public bool CanConfirmExchange => state.exchange != null && state.fragments >= state.exchange.fragments;
        public CombatStatsData PlayerStats => CloneStats(state.run == null || Phase == SessionPhase.Lobby || Phase == SessionPhase.ExchangeConfirmation ? BuildLobbyStats() : state.run.combatStats);

        public void Tick(float deltaSeconds) { AdvanceTime(deltaSeconds); }
        // Call once after collecting the inputs for a displayed frame. Unlike Tick's
        // exact-time simulation, this exposes a full cue before resolving its impact.
        public void TickInputFrame(float deltaSeconds) { AdvanceTime(deltaSeconds, true); }
        public bool Attack() { return BeginAttack(); }
        public bool Dodge(DodgeDirection direction) { return BeginDodge(direction); }
        public bool StartRun() { return BeginRun(); }
        public bool RetryRun()
        {
            if(state.phase!=SessionPhase.DeathResult)return false;
            state.phase=SessionPhase.Lobby;
            return BeginRun();
        }
        public bool ReturnToLobby() { return EndRun(); }
        public bool ChooseRelic(string id) { return CommitRelicChoice(id); }
        public bool AdvanceStory() { return AdvanceStoryLine(false); }
        public bool SkipStory() { return AdvanceStoryLine(true); }
        public bool Upgrade(int statIndex) { return UpgradeStat(statIndex); }
        public bool ConfirmExchange() { return CommitExchange(); }
        public bool CancelExchange() { return DismissExchange(); }
        public bool EquipRelic(string id) { return ChangeEquipment(id, true); }
        public bool UnequipRelic(string id) { return ChangeEquipment(id, false); }
        public bool SelectCharacter(string id) { return ChangeCharacter(id); }
        public bool BuyNextSlot() { return PurchaseSlot(); }
        public void SetPaused(bool value)
        {
            if (paused && !value && IsPendingEnemyImpact(state.run)) pendingImpactNeedsDisplay = true;
            paused = value;
            Changed?.Invoke();
        }
        public void MarkSaved() { IsDirty = false; }
        public GameSaveData ExportSave() { return CloneSave(state); }
        public int GetStatLevel(int index) { return index >= 0 && index < 4 ? CurrentCharacter.statLevels[index] : 0; }
        public int GetRelicLevel(string id) { return FindRelicLevel(id); }
        public long GetUpgradeCost(int index) { return StatUpgradeCost(index); }

        private CharacterProgressData CurrentCharacter => state.characters.Find(x => x.id == state.selectedCharacterId);

        private void Publish(bool requestSave = true)
        {
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

        private void Emit(SessionEventKind kind, long damage = 0, bool critical = false, DodgeDirection direction = DodgeDirection.Up, string relicId = null, int? eventStage = null, double staggerBefore = 0, double staggerGained = 0, bool perfectCounter = false, string patternId = null)
        {
            EventRaised?.Invoke(new SessionEvent { TimeMs = ClockMs, PerfectCounter = perfectCounter, PatternId = patternId ?? state.run?.patternId, Kind = kind, DamageMilli = damage, Critical = critical, Direction = direction, Stage = eventStage ?? state.run?.stage ?? Stage, RelicId = relicId, StaggerBefore = staggerBefore, StaggerGained = staggerGained });
        }
    }
}
