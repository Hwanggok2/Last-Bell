using System;
using System.Collections.Generic;

namespace LastBell
{
    public enum SessionPhase { Lobby, TutorialDodge, TutorialAttack, Combat, RewardChoice, ExchangeConfirmation, MvpComplete }
    public enum DodgeDirection { Up, Back, Down, Forward }
    public enum SessionEventKind { AttackStarted, DodgeStarted, Dodged, PerfectDodge, PlayerDamaged, EnemyDamaged, EnemyStunned, EnemyDefeated, PlayerDefeated, StageStarted, RelicChosen, MvpCompleted }

    [Serializable]
    public sealed class GameSaveData
    {
        public int schemaVersion = 1;
        public int gold;
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
        public RunStateData run;
        public ExchangeQuoteData exchange;

        internal GameSaveData Copy()
        {
            var copy = (GameSaveData)MemberwiseClone();
            copy.characters = characters == null ? new List<CharacterProgressData>() : characters.ConvertAll(x => x.Copy());
            copy.relics = relics == null ? new List<OwnedRelicData>() : relics.ConvertAll(x => x.Copy());
            copy.run = run?.Copy();
            copy.exchange = exchange?.Copy();
            return copy;
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
        internal CharacterProgressData Copy()
        {
            var copy = (CharacterProgressData)MemberwiseClone();
            copy.statLevels = statLevels == null ? new int[4] : (int[])statLevels.Clone();
            copy.equippedRelicIds = equippedRelicIds == null ? new string[0] : (string[])equippedRelicIds.Clone();
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
    public sealed class CombatStatsData
    {
        public double attack;
        public long maxHpMilli;
        public double critChance;
        public double goldMultiplier;
        public double stunDamageMultiplier;
        public double normalStagger;
        public double perfectStagger;
        public double damageReduction;
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
        public CombatStatsData stats = new CombatStatsData();
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
        internal RunStateData Copy()
        {
            var copy = (RunStateData)MemberwiseClone();
            copy.statLevels = (int[])statLevels.Clone();
            copy.equippedRelicIds = (string[])equippedRelicIds.Clone();
            copy.appliedRelicLevels = (int[])appliedRelicLevels.Clone();
            copy.stats = stats.Copy();
            copy.offer = offer?.Copy();
            return copy;
        }
    }

    [Serializable]
    public sealed class RelicOfferData
    {
        public string rewardId;
        public int stage;
        public string[] relicIds = new string[0];
        public int[] offeredLevels = new int[0];
        public int maxGold;
        internal RelicOfferData Copy()
        {
            var copy = (RelicOfferData)MemberwiseClone();
            copy.relicIds = (string[])relicIds.Clone();
            copy.offeredLevels = (int[])offeredLevels.Clone();
            return copy;
        }
    }

    [Serializable]
    public sealed class ExchangeQuoteData
    {
        public string characterId;
        public int statIndex;
        public int level;
        public int upgradeCost;
        public int fragments;
        public int gold;
        public int exchangeRate;
        public int walletGold;
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
    }

    public sealed class EnemyIntentView
    {
        public bool Visible { get; internal set; }
        public DodgeDirection Direction { get; internal set; }
        public DodgeDirection NextDirection { get; internal set; }
        public bool HasNext { get; internal set; }
        public float Progress { get; internal set; }
        public float SecondsToHit { get; internal set; }
        public string PatternId { get; internal set; }
    }
}
