using System;
using System.Collections.Generic;

namespace LastBell
{
    public enum SessionPhase { Lobby, TutorialDodge, TutorialAttack, Combat, RewardChoice, ExchangeConfirmation, MvpComplete, Transition, StoryDialogue, DeathResult, Prologue, ComboHint, RunEnd, RewardAnnouncement }
    public enum TransitionKind { EnemyDefeat, Travel, Arrival, BossIntro, Death }
    public enum DodgeDirection { Up, Back, Down, Forward }
    public enum SessionEventKind { AttackStarted, DodgeStarted, Dodged, PerfectDodge, PlayerDamaged, EnemyDamaged, EnemyStunned, EnemyDefeated, PlayerDefeated, StageStarted, RelicChosen, MvpCompleted, StagePrepared, EnemyCueStarted, EnemyReleased, ParryStarted, Parried, PerfectParry, SkillShieldAbsorbed, CompanionAttackStarted, CompanionDamaged, PlayerRecovered, CompanionUnlocked, CompanionJoined, ServicePurchased, ComboPracticeChanged, ComboPracticeCompleted, PlayerSkillStarted, PlayerSkillContact }

    [Serializable]
    public sealed class GameSaveData
    {
        public int schemaVersion = 9;
        public bool adventureNewAccount = true, comboPracticeCompleted, comboPracticeSkipped;
        public int companionSlots = 1;
        public List<CompanionProgressData> companions = new List<CompanionProgressData>();
        public bool prologueSeen, comboHintSeen;
        public long gold;
        public int fragments;
        public int unlockedSlots = 1; // Read-only schema <=7 migration input.
        public int storageCapacity = 3;
        public int highestClearedStage;
        public long walletVersion;
        public int tutorialStep;
        public int runSequence;
        public int randomState = 17328719;
        public string selectedCharacterId;
        public SessionPhase phase = SessionPhase.Lobby;
        public List<CharacterProgressData> characters = new List<CharacterProgressData>();
        public List<OwnedRelicData> relics = new List<OwnedRelicData>();
        public List<string> unlockedStoryIds = new List<string>();
        public RunStateData run;
        public ExchangeQuoteData exchange;

        internal GameSaveData Copy()
        {
            var copy = (GameSaveData)MemberwiseClone();
            copy.characters = characters == null ? new List<CharacterProgressData>() : characters.ConvertAll(x => x.Copy());
            copy.relics = relics == null ? new List<OwnedRelicData>() : relics.ConvertAll(x => x.Copy());
            copy.unlockedStoryIds = unlockedStoryIds == null ? new List<string>() : new List<string>(unlockedStoryIds);
            copy.companions = companions == null ? new List<CompanionProgressData>() : companions.ConvertAll(x=>x.Copy());
            copy.run = run?.Copy();
            copy.exchange = exchange?.Copy();
            copy.NormalizeOptionalRecords();
            return copy;
        }

        // JsonUtility may materialize a null inline class as an all-default object.
        // Collapse ONLY that exact empty shape. Any identity, elapsed time, candidate,
        // or other nondefault value is retained for GameSession's strict validation.
        // Phase validation still rejects a missing record when that phase requires it.
        internal void NormalizeOptionalRecords()
        {
            if (run == null) return;
            var offer = run.offer;
            if (offer != null && !offer.legacyMidbossReward && string.IsNullOrEmpty(offer.rewardId) &&
                offer.stage == 0 && offer.maxGold == 0 &&
                (offer.relicIds == null || offer.relicIds.Length == 0) &&
                (offer.offeredLevels == null || offer.offeredLevels.Length == 0)) run.offer = null;
            var story = run.story;
            if (story != null && string.IsNullOrEmpty(story.sceneId) && string.IsNullOrEmpty(story.rewardId) &&
                story.lineIndex == 0&&!story.legacy&&!story.reunion&&!story.awaitingCompanionChoice) run.story = null;
            var transition = run.transition;
            if (transition != null && transition.kind == TransitionKind.EnemyDefeat &&
                transition.fromStage == 0 && transition.toStage == 0 && transition.elapsedMs == 0 &&
                transition.durationMs == 0 && !transition.isRepeat) run.transition = null;
            if (run.IsEmptyInlineRecord) run = null;
        }
    }

    [Serializable]
    public sealed class CharacterProgressData
    {
        public string id;
        public int[] statLevels = new int[5];
        public List<string> companionIds = new List<string>();
        public List<ServiceReservationData> reservations = new List<ServiceReservationData>();
        public int weaponRank;
        public string[] equippedRelicIds = new string[0];
        public int checkpoint;
        public int cycle;
        public int highestStage;
        public List<string> seenStoryIds = new List<string>();
        public List<string> seenBossIntros = new List<string>();
        internal CharacterProgressData Copy()
        {
            var copy = (CharacterProgressData)MemberwiseClone();
            copy.statLevels = statLevels == null ? new int[5] : (int[])statLevels.Clone();
            copy.companionIds = companionIds == null ? new List<string>() : new List<string>(companionIds);
            copy.reservations = reservations == null ? new List<ServiceReservationData>() : reservations.ConvertAll(x=>x.Copy());
            copy.equippedRelicIds = equippedRelicIds == null ? new string[0] : (string[])equippedRelicIds.Clone();
            copy.seenStoryIds = seenStoryIds == null ? new List<string>() : new List<string>(seenStoryIds);
            copy.seenBossIntros = seenBossIntros == null ? new List<string>() : new List<string>(seenBossIntros);
            return copy;
        }
    }

    [Serializable]
    public sealed class OwnedRelicData
    {
        public string id;
        public int level;
        internal OwnedRelicData Copy() { return (OwnedRelicData)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class LegacyCombatStatsData
    {
        public double attack;
        public long maxHpMilli;
        public double critChance;
        public double goldMultiplier;
        public double stunDamageMultiplier;
        public double normalStagger;
        public double perfectStagger;
        public double damageReduction;
        internal bool IsEmpty => attack == 0 && maxHpMilli == 0 && critChance == 0 && goldMultiplier == 0 &&
            stunDamageMultiplier == 0 && normalStagger == 0 && perfectStagger == 0 && damageReduction == 0;
        internal LegacyCombatStatsData Copy() { return (LegacyCombatStatsData)MemberwiseClone(); }
    }

    // Final stats are integer values. Ratios are derived from the saved integer percent.
    [Serializable]
    public sealed class CombatStatsData
    {
        public int attack, maxHp, critChancePercent, goldBonusPercent, stunDamagePercent;
        public int normalStagger, perfectStagger, damageReductionPercent;
        public int baseAttack, recovery;
        public long maxHpMilli => (long)maxHp * 1000;
        public double critChance => critChancePercent / 100d;
        public double goldMultiplier => 1 + goldBonusPercent / 100d;
        public double stunDamageMultiplier => stunDamagePercent / 100d;
        public double damageReduction => damageReductionPercent / 100d;
        internal bool IsEmpty => attack == 0 && maxHp == 0 && critChancePercent == 0 && goldBonusPercent == 0 &&
            stunDamagePercent == 0 && normalStagger == 0 && perfectStagger == 0 && damageReductionPercent == 0 && baseAttack == 0 && recovery == 0;
        internal CombatStatsData Copy() { return (CombatStatsData)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class RunStateData
    {
        public string characterId;
        public int runId;
        public int stage;
        public int cycle;
        public int[] statLevels = new int[5];
        public List<RunCompanionData> companions = new List<RunCompanionData>();
        public int weaponRank, teaRecovery;
        public bool blessing, guideApplied;
        public ComboPracticeMode comboPractice;
        public double practiceRecoveryUntilMs;
        public string[] equippedRelicIds = new string[0];
        public int[] appliedRelicLevels = new int[0];
        // Read-only migration input for schema1..3; new runs use combatStats.
        public LegacyCombatStatsData stats;
        public CombatStatsData combatStats = new CombatStatsData();
        public long earnedGold;
        public int earnedFragments;
        public bool earningsSinceUpdate;
        public long playerHpMilli;
        public long enemyHpMilli;
        public long enemyMaxHpMilli;
        public double clockMs;
        public double dodgeStartMs = -1000000;
        public DodgeDirection dodgeDirection;
        public double attackReadyAtMs;
        public double attackHitAtMs = -1;
        // Optional schema8 fields: old saves start ready. Pending contact is never replayed on restore.
        public bool pendingSkillAttack;
        public int skillRulesVersion;
        public bool skillContactPending;
        public double skillContactRemainingMs;
        public double skillCooldownRemainingMs, skillShieldRemainingMs;
        public long skillShieldMilli;
        public double enemyHitAtMs = -1;
        public double telegraphStartMs;
        public string patternId;
        public int patternIndex;
        public int patternRandomState, perfectStreak, hitCombo, stunExtensionMs;
        public string[] randomSequence = new string[0];
        public int sequenceCursor;
        public string previousPattern, penultimatePattern;
        public bool forceTwoHitSequence;
        public double attackSpeed = 1;
        public long defeatedGold;
        public int patternStep;
        public double stagger;
        public double stunUntilMs = -1;
        public double immunityUntilMs = -1;
        public bool stageRewardApplied;
        public string lastAppliedRewardId;
        public RelicOfferData offer;
        public List<RelicOfferData> pendingRewards = new List<RelicOfferData>();
        public List<string> claimedRewards = new List<string>();
        public bool ended, cleared;
        public TransitionData transition;
        public StoryStateData story;
        // Unity can materialize null inline classes with zero values or field initializers.
        // Never discard a partial run: identity, money, timing and nested data all count.
        internal bool IsEmptyInlineRecord => string.IsNullOrEmpty(characterId) && runId == 0 && stage == 0 && cycle == 0 &&
            (statLevels == null || (statLevels.Length == 0 || statLevels.Length == 4 || statLevels.Length == 5) && Array.TrueForAll(statLevels, x => x == 0)) &&
            (companions == null || companions.Count == 0) && weaponRank == 0 && teaRecovery == 0 && !blessing && !guideApplied &&
            comboPractice == ComboPracticeMode.None && practiceRecoveryUntilMs == 0 &&
            (equippedRelicIds == null || equippedRelicIds.Length == 0) && (appliedRelicLevels == null || appliedRelicLevels.Length == 0) &&
            (stats == null || stats.IsEmpty) && (combatStats == null || combatStats.IsEmpty) &&
            earnedGold == 0 && earnedFragments == 0 && !earningsSinceUpdate && playerHpMilli == 0 && enemyHpMilli == 0 && enemyMaxHpMilli == 0 && clockMs == 0 &&
            (dodgeStartMs == 0 || dodgeStartMs == -1000000) && dodgeDirection == default(DodgeDirection) && attackReadyAtMs == 0 &&
            (attackHitAtMs == 0 || attackHitAtMs == -1) && !pendingSkillAttack && skillRulesVersion == 0 && !skillContactPending && skillContactRemainingMs == 0 && skillCooldownRemainingMs == 0 && skillShieldRemainingMs == 0 && skillShieldMilli == 0 &&
            (enemyHitAtMs == 0 || enemyHitAtMs == -1) && telegraphStartMs == 0 && string.IsNullOrEmpty(patternId) && patternIndex == 0 &&
            patternRandomState == 0 && perfectStreak == 0 && hitCombo == 0 && stunExtensionMs == 0 && (randomSequence == null || randomSequence.Length == 0) &&
            sequenceCursor == 0 && string.IsNullOrEmpty(previousPattern) && string.IsNullOrEmpty(penultimatePattern) && !forceTwoHitSequence &&
            (attackSpeed == 0 || attackSpeed == 1) && defeatedGold == 0 && patternStep == 0 && stagger == 0 &&
            (stunUntilMs == 0 || stunUntilMs == -1) && (immunityUntilMs == 0 || immunityUntilMs == -1) && !stageRewardApplied && string.IsNullOrEmpty(lastAppliedRewardId) &&
            offer == null && (pendingRewards == null || pendingRewards.Count == 0) && (claimedRewards == null || claimedRewards.Count == 0) &&
            !ended && !cleared && transition == null && story == null;
        internal RunStateData Copy()
        {
            var copy = (RunStateData)MemberwiseClone();
            copy.statLevels = (int[])statLevels.Clone();
            copy.equippedRelicIds = (string[])equippedRelicIds.Clone();
            copy.appliedRelicLevels = (int[])appliedRelicLevels.Clone();
            copy.randomSequence = randomSequence == null ? new string[0] : (string[])randomSequence.Clone();
            copy.stats = stats?.Copy();
            copy.companions = companions == null ? new List<RunCompanionData>() : companions.ConvertAll(x=>x.Copy());
            copy.combatStats = combatStats?.Copy();
            copy.offer = offer?.Copy();
            copy.pendingRewards = pendingRewards == null ? new List<RelicOfferData>() : pendingRewards.ConvertAll(x=>x.Copy());
            copy.claimedRewards = claimedRewards == null ? new List<string>() : new List<string>(claimedRewards);
            copy.transition = transition?.Copy();
            copy.story = story?.Copy();
            return copy;
        }
    }

    [Serializable]
    public sealed class TransitionData
    {
        public TransitionKind kind;
        public int fromStage, toStage;
        public double elapsedMs;
        public int durationMs;
        public bool isRepeat;
        internal TransitionData Copy() { return (TransitionData)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class StoryStateData
    {
        public string sceneId, rewardId;
        public int lineIndex;
        public bool legacy, reunion, awaitingCompanionChoice;
        internal StoryStateData Copy() { return (StoryStateData)MemberwiseClone(); }
    }

    public sealed class TransitionView
    {
        public TransitionKind Kind { get; internal set; }
        public int FromStage { get; internal set; }
        public int ToStage { get; internal set; }
        public float ElapsedSeconds { get; internal set; }
        public float DurationSeconds { get; internal set; }
        public bool IsRepeat { get; internal set; }
        public float Progress => DurationSeconds <= 0 ? 1 : Math.Min(1, ElapsedSeconds / DurationSeconds);
    }

    public sealed class StoryView
    {
        public string CompanionId { get; internal set; }
        public bool CanInviteCompanion { get; internal set; }
        public bool AwaitingCompanionChoice { get; internal set; }
        public string PortraitId { get; internal set; }
        public string Text { get; internal set; }
        public string Speaker { get; internal set; }
        public int LineIndex { get; internal set; }
        public int LineCount { get; internal set; }
        public string SceneId { get; internal set; }
    }

    public sealed class RunResultView
    {
        public bool Cleared { get; internal set; }
        public bool Retired { get; internal set; }
        public int DefeatedStage { get; internal set; }
        public int RetryStage { get; internal set; }
        public long Gold { get; internal set; }
        public int Fragments { get; internal set; }
        public bool SinceUpdate { get; internal set; }
    }

    [Serializable]
    public sealed class RelicOfferData
    {
        public bool legacyMidbossReward;
        public string rewardId;
        public int stage;
        public string[] relicIds = new string[0];
        public int[] offeredLevels = new int[0];
        public long maxGold;
        internal RelicOfferData Copy()
        {
            var copy = (RelicOfferData)MemberwiseClone();
            copy.relicIds = relicIds == null ? new string[0] : (string[])relicIds.Clone();
            copy.offeredLevels = offeredLevels == null ? new int[0] : (int[])offeredLevels.Clone();
            return copy;
        }
    }

    [Serializable]
    public sealed class ExchangeQuoteData
    {
        public bool purchaseOnly;
        public string characterId;
        public int statIndex;
        public int level;
        public long upgradeCost;
        public int fragments;
        public long gold;
        public long exchangeRate;
        public long walletGold;
        public int walletFragments;
        public long walletVersion;
        internal ExchangeQuoteData Copy() { return (ExchangeQuoteData)MemberwiseClone(); }
    }

    public sealed class SessionEvent
    {
        public string CompanionId {get;internal set;}
        public string ServiceId {get;internal set;}
        public long RecoveryMilli {get;internal set;}
        public SessionEventKind Kind { get; internal set; }
        public DodgeDirection Direction { get; internal set; }
        public long DamageMilli { get; internal set; }
        public long GoldReward { get; internal set; }
        public bool Critical { get; internal set; }
        public int Stage { get; internal set; }
        public string RelicId { get; internal set; }
        public double TimeMs {get;internal set;}
        public string PatternId {get;internal set;}
        public string SkillId {get;internal set;}
        public long AbsorbedMilli {get;internal set;}
        public bool PerfectCounter {get;internal set;}
        public double StaggerBefore { get; internal set; }
        public double StaggerGained { get; internal set; }
    }

    public sealed class EnemyIntentView
    {
        public bool Visible { get; internal set; }
        public DodgeDirection Direction { get; internal set; }
        public DodgeDirection NextDirection { get; internal set; }
        public bool HasNext { get; internal set; }
        public float Progress { get; internal set; }
        public float SecondsToHit { get; internal set; }
        public float TelegraphMs { get; internal set; }
        public string PatternId { get; internal set; }
    }
}
