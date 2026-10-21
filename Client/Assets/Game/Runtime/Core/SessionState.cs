using System;
using System.Collections.Generic;

namespace LastBell
{
    public enum SessionPhase { Lobby, TutorialDodge, TutorialAttack, Combat, RewardChoice, ExchangeConfirmation, MvpComplete, Transition, StoryDialogue, DeathResult }
    public enum TransitionKind { EnemyDefeat, Travel, Arrival, BossIntro, Death }
    public enum DodgeDirection { Up, Back, Down, Forward }
    public enum SessionEventKind { AttackStarted, DodgeStarted, Dodged, PerfectDodge, PlayerDamaged, EnemyDamaged, EnemyStunned, EnemyDefeated, PlayerDefeated, StageStarted, RelicChosen, MvpCompleted, StagePrepared, EnemyCueStarted, EnemyReleased, ParryStarted, Parried, PerfectParry }

    [Serializable]
    public sealed class GameSaveData
    {
        public int schemaVersion = 4;
        public long gold;
        public int fragments;
        public int unlockedSlots = 1;
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
                story.lineIndex == 0) run.story = null;
            var transition = run.transition;
            if (transition != null && transition.kind == TransitionKind.EnemyDefeat &&
                transition.fromStage == 0 && transition.toStage == 0 && transition.elapsedMs == 0 &&
                transition.durationMs == 0 && !transition.isRepeat) run.transition = null;
        }
    }

    [Serializable]
    public sealed class CharacterProgressData
    {
        public string id;
        public int[] statLevels = new int[4];
        public string[] equippedRelicIds = new string[0];
        public int checkpoint;
        public int cycle;
        public int highestStage;
        public List<string> seenStoryIds = new List<string>();
        public List<string> seenBossIntros = new List<string>();
        internal CharacterProgressData Copy()
        {
            var copy = (CharacterProgressData)MemberwiseClone();
            copy.statLevels = statLevels == null ? new int[4] : (int[])statLevels.Clone();
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
        internal LegacyCombatStatsData Copy() { return (LegacyCombatStatsData)MemberwiseClone(); }
    }

    // Final stats are integer values. Ratios are derived from the saved integer percent.
    [Serializable]
    public sealed class CombatStatsData
    {
        public int attack, maxHp, critChancePercent, goldBonusPercent, stunDamagePercent;
        public int normalStagger, perfectStagger, damageReductionPercent;
        public long maxHpMilli => (long)maxHp * 1000;
        public double critChance => critChancePercent / 100d;
        public double goldMultiplier => 1 + goldBonusPercent / 100d;
        public double stunDamageMultiplier => stunDamagePercent / 100d;
        public double damageReduction => damageReductionPercent / 100d;
        internal CombatStatsData Copy() { return (CombatStatsData)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class RunStateData
    {
        public string characterId;
        public int runId;
        public int stage;
        public int cycle;
        public int[] statLevels = new int[4];
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
        public double enemyHitAtMs = -1;
        public double telegraphStartMs;
        public string patternId;
        public int patternIndex;
        public int patternStep;
        public double stagger;
        public double stunUntilMs = -1;
        public double immunityUntilMs = -1;
        public bool stageRewardApplied;
        public string lastAppliedRewardId;
        public RelicOfferData offer;
        public TransitionData transition;
        public StoryStateData story;
        internal RunStateData Copy()
        {
            var copy = (RunStateData)MemberwiseClone();
            copy.statLevels = (int[])statLevels.Clone();
            copy.equippedRelicIds = (string[])equippedRelicIds.Clone();
            copy.appliedRelicLevels = (int[])appliedRelicLevels.Clone();
            copy.stats = stats?.Copy();
            copy.combatStats = combatStats?.Copy();
            copy.offer = offer?.Copy();
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
        public string Text { get; internal set; }
        public string Speaker { get; internal set; }
        public int LineIndex { get; internal set; }
        public int LineCount { get; internal set; }
        public string SceneId { get; internal set; }
    }

    public sealed class RunResultView
    {
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
        public SessionEventKind Kind { get; internal set; }
        public DodgeDirection Direction { get; internal set; }
        public long DamageMilli { get; internal set; }
        public bool Critical { get; internal set; }
        public int Stage { get; internal set; }
        public string RelicId { get; internal set; }
        public double TimeMs {get;internal set;}
        public string PatternId {get;internal set;}
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
