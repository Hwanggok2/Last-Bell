using System;
using System.Collections.Generic;

namespace LastBell
{
    // Configuration only. Deserialize with JsonUtility.FromJson<GameCatalog>, then ValidateOrThrow.
    // Treat these fields as read-only during play; GameSession owns all mutable state.
    [Serializable]
    public sealed partial class GameCatalog
    {
        public int schemaVersion;
        public string dataVersion;
        public string sourcePlanningVersion;
        public string generatedNotice;
        public string sourceSha256;
        public string defaultCharacterId;
        public int mvpLastStage;
        public int boundaryStage;
        public CombatConfig combat;
        public GrowthConfig growth;
        public EconomyConfig economy;
        public RelicRules relicRules;
        public TransitionConfig transitions;
        public OnboardingConfig onboarding;
        public StoryDefinition[] stories;
        public StoryDefinition[] plannedStories, legacyStories;
        public AdventureConfig adventure;
        public CompanionDefinition[] companions;
        public CharacterDefinition[] characters;
        public SkillDefinition[] skills;
        public EnemyDefinition[] enemies;
        public StageDefinition[] stages;
        public RelicDefinition[] relics;

        [NonSerialized] private Dictionary<string, CharacterDefinition> characterIndex;
        [NonSerialized] private Dictionary<string, EnemyDefinition> enemyIndex;
        [NonSerialized] private Dictionary<int, StageDefinition> stageIndex;
        [NonSerialized] private Dictionary<string, RelicDefinition> relicIndex;

        public void ValidateOrThrow()
        {
            if(onboarding==null||onboarding.prologueDurationMs<1000||onboarding.prologueParagraphs==null||onboarding.prologueParagraphs.Length==0||
                Array.Exists(onboarding.prologueParagraphs,string.IsNullOrWhiteSpace)||string.IsNullOrWhiteSpace(onboarding.comboHint))
                throw new InvalidOperationException("Invalid onboarding content.");
            string[] errors = Validate();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors));
            characterIndex = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
            enemyIndex = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
            stageIndex = new Dictionary<int, StageDefinition>();
            relicIndex = new Dictionary<string, RelicDefinition>(StringComparer.Ordinal);
            foreach (var value in characters) characterIndex.Add(value.id, value);
            foreach (var value in enemies) enemyIndex.Add(value.id, value);
            foreach (var value in stages) stageIndex.Add(value.number, value);
            foreach (var value in relics) relicIndex.Add(value.id, value);
        }

        private void EnsureIndex() { if (stageIndex == null) ValidateOrThrow(); }
        public CharacterDefinition GetCharacter(string id) { EnsureIndex(); return characterIndex[id]; }
        public SkillDefinition FindSkill(string characterId) { return skills == null ? null : Array.Find(skills, x => x != null && x.characterId == characterId); }
        public EnemyDefinition GetEnemy(string id) { EnsureIndex(); return enemyIndex[id]; }
        public StageDefinition GetStage(int number) { EnsureIndex(); return stageIndex[number]; }
        public RelicDefinition GetRelic(string id) { EnsureIndex(); return relicIndex[id]; }
        public StoryDefinition FindStory(int stage) { return Array.Find(stories, x => x.stage == stage) ?? Array.Find(plannedStories ?? new StoryDefinition[0], x=>x.stage==stage); }
        public StoryDefinition FindStory(string id) { return FindStory(id, false); }
        public StoryDefinition FindStory(string id, bool legacy) { return legacy ? Array.Find(legacyStories ?? new StoryDefinition[0],x=>x.id==id) : Array.Find(stories,x=>x.id==id) ?? Array.Find(plannedStories ?? new StoryDefinition[0],x=>x.id==id) ?? Array.Find(legacyStories ?? new StoryDefinition[0],x=>x.id==id); }
        public bool TryGetStage(int number, out StageDefinition stage) { EnsureIndex(); return stageIndex.TryGetValue(number, out stage); }

        // targetLevel is the level being purchased: 1..50. Index 0 is the initial level, not a purchase.
        public long GetUpgradeCost(int targetLevel)
        {
            EnsureIndex();
            if (targetLevel <= growth.minLevel || targetLevel > growth.maxLevel) throw new ArgumentOutOfRangeException(nameof(targetLevel));
            return economy.upgradeCosts[targetLevel];
        }

        public long GetBaseGold(StageDefinition stage)
        {
            EnsureIndex();
            int multiplier = stage.kind == "boss" ? economy.bossGoldMultiplier : stage.kind == "miniboss" ? economy.minibossGoldMultiplier : 1;
            return checked(economy.normalGoldPerStage[stage.number] * multiplier);
        }

        public long GetExchangeRate(int highestClearedStage)
        {
            EnsureIndex();
            int tier = 0;
            for (int i = 1; i < economy.exchangeTierUnlockBosses.Length; i++)
                if (highestClearedStage >= economy.exchangeTierUnlockBosses[i]) tier = i;
            return economy.exchangeGoldPerUnlockTier[tier];
        }

        public int GetFragmentReward(StageDefinition stage)
        {
            EnsureIndex();
            return stage.kind == "boss" ? economy.bossFragments : stage.kind == "miniboss" ? economy.minibossFragments : 0;
        }

        public long GetMaxRelicGold(int stageNumber)
        {
            EnsureIndex();
            int zone = (stageNumber - 1) / 10;
            if (stageNumber < 1 || zone >= economy.maxRelicGoldPerZone.Length) throw new ArgumentOutOfRangeException(nameof(stageNumber));
            return economy.maxRelicGoldPerZone[zone];
        }

        public int GetStunDurationMs(string kind)
        {
            EnsureIndex();
            return kind == "boss" ? combat.bossStunMs : kind == "miniboss" ? combat.minibossStunMs : combat.normalStunMs;
        }

        public string[] Validate()
        {
            var errors = new List<string>();
            Require(errors, schemaVersion == 2, "schemaVersion: expected 2");
            Require(errors, !string.IsNullOrWhiteSpace(dataVersion), "dataVersion: required");
            Require(errors, combat != null && growth != null && economy != null && relicRules != null, "combat/growth/economy/relicRules: required");
            var characterIds = CheckIds(errors, characters, x => x.id, "characters");
            var enemyIds = CheckIds(errors, enemies, x => x.id, "enemies");
            CheckIds(errors, relics, x => x.id, "relics");
            Require(errors, defaultCharacterId != null && characterIds.Contains(defaultCharacterId), "defaultCharacterId: unknown character");
            if (combat == null || growth == null || economy == null || relicRules == null) return errors.ToArray();
            Require(errors, transitions != null, "transitions: required");
            if (transitions != null)
                Require(errors, transitions.enemyDefeatMs > 0 && transitions.travelMs > 0 && transitions.arrivalMs > 0 && transitions.deathMs > 0 &&
                    transitions.minibossIntroMs > 0 && transitions.bossIntroMs > 0 && transitions.repeatMinibossIntroMs > 0 && transitions.repeatBossIntroMs > 0,
                    "transitions: durations must be positive");
            ValidateAdventure(errors, characterIds);
            Require(errors, growth.minLevel == 0 && growth.maxLevel == 50, "growth: MVP levels must be 0..50");
            Require(errors, growth.breakthroughLevels != null && growth.breakthroughLevels.Length == 4, "growth.breakthroughLevels: four milestones required");
            if (growth.breakthroughLevels != null) for (int i = 0; i < growth.breakthroughLevels.Length; i++)
                Require(errors, growth.breakthroughLevels[i] == 11 + 10 * i, "growth.breakthroughLevels: expected 11/21/31/41");
            Require(errors, growth.starScale != null && growth.starScale.Length == 5, "growth.starScale: five values required");
            if (growth.starScale != null) for (int i = 0; i < growth.starScale.Length; i++)
                Require(errors, Positive(growth.starScale[i]) && (i == 0 || growth.starScale[i] > growth.starScale[i - 1]), "growth.starScale: must be finite, positive and increasing");
            Require(errors, growth.effectiveMilestoneLevels > 0 && Positive(growth.attackBase) && Positive(growth.attackLinearPerLevel) && Positive(growth.attackBreakBase) && Positive(growth.attackBreakPerStar) && Positive(growth.hpBase) && Positive(growth.hpLinearPerLevel) && Positive(growth.hpBreakBase) && Positive(growth.hpBreakPerStar), "growth: invalid attack/HP coefficients");
            Require(errors, Positive(growth.critBase) && growth.critBase <= combat.critChanceCap && Positive(growth.resonanceBase) && growth.resonanceBase <= combat.stunDamageMultiplierCap, "growth: invalid luck/resonance bases");
            Require(errors, PositiveArray(growth.luckCritPerTenLevels,5) && PositiveArray(growth.luckGoldPerLevel,5) && PositiveArray(growth.resonancePerLevel,5), "growth: five integer star growth values required");
            Require(errors, economy.upgradeCosts != null && economy.upgradeCosts.Length == growth.maxLevel + 1, "economy.upgradeCosts: expected 51 entries indexed by target level");
            if (economy.upgradeCosts != null)
                for (int i = 0; i < economy.upgradeCosts.Length; i++) Require(errors, i == 0 ? economy.upgradeCosts[i] == 0 : economy.upgradeCosts[i] > economy.upgradeCosts[i - 1], "economy.upgradeCosts[" + i + "]: invalid cost");
            Require(errors, PositiveArray(economy.exchangeGoldPerUnlockTier, 5) && economy.exchangeGoldPerUnlockTier[0] == 100, "economy.exchangeGoldPerUnlockTier: five positive rates starting100");
            Require(errors, economy.exchangeTierUnlockBosses != null && economy.exchangeTierUnlockBosses.Length == 5, "economy.exchangeTierUnlockBosses: five thresholds required");
            if (economy.exchangeTierUnlockBosses != null) for (int i=0;i<economy.exchangeTierUnlockBosses.Length;i++)
                Require(errors,economy.exchangeTierUnlockBosses[i]==i*10,"economy.exchangeTierUnlockBosses: expected0/10/20/30/40");
            if(economy.exchangeGoldPerUnlockTier!=null) for(int i=1;i<economy.exchangeGoldPerUnlockTier.Length;i++)
                Require(errors,economy.exchangeGoldPerUnlockTier[i]>economy.exchangeGoldPerUnlockTier[i-1],"economy.exchangeGoldPerUnlockTier: must increase");
            Require(errors, economy.initialGold >= 0 && economy.initialFragments >= 0 && economy.minibossFragments == 0 && economy.bossFragments == 4, "economy: invalid initial wallet or boss-only 0/4 rewards");
            Require(errors, economy.minibossGoldMultiplier > 0 && economy.bossGoldMultiplier > 0 && economy.ngGoldMultiplier == 1f, "economy: invalid gold multipliers");
            Require(errors, economy.normalGoldPerStage != null && economy.normalGoldPerStage.Length == 51 && economy.normalGoldPerStage[0] == 0, "economy.normalGoldPerStage: expected51 entries, index0=0");
            if(economy.normalGoldPerStage!=null) for(int i=1;i<economy.normalGoldPerStage.Length;i++)
                Require(errors,economy.normalGoldPerStage[i]>economy.normalGoldPerStage[i-1] && economy.normalGoldPerStage[i]<=long.MaxValue/Math.Max(1,Math.Max(economy.bossGoldMultiplier,economy.minibossGoldMultiplier)),"economy.normalGoldPerStage: increasing Int64 rewards required");
            Require(errors, PositiveArray(economy.maxRelicGoldPerZone, 5), "economy.maxRelicGoldPerZone: expected five positive zones");
            Require(errors, PositiveArray(economy.storageExpansionCosts, 7) && Array.TrueForAll(new[]{0,1,2,3,4,5,6}, i => economy.storageExpansionCosts[i] == new[]{4,8,12,18,26,36,48}[i]), "economy.storageExpansionCosts: invalid expansion costs");
            Require(errors, relicRules.maxLevel == 5 && relicRules.initialCapacity == 3 && relicRules.maxOwned == 10 && relicRules.choicesPerBoss == 3 && relicRules.includeMaxInPool, "relicRules: expected automatic relics, 3..10 storage capacity");
            Require(errors, relics != null && relics.Length == 10, "relics: expected ten definitions");
            Require(errors, combat.dodgeActiveMs > 0 && combat.dodgeRecoveryMs >= 0 && combat.perfectWindowMs > 0 && combat.perfectWindowMs <= combat.dodgeActiveMs, "combat: invalid dodge timing");
            Require(errors, combat.singleTelegraphMs%3==0 && combat.fastTelegraphMs%3==0, "combat: three equal warning beats required");
            Require(errors, combat.comboMinGapMs >= combat.dodgeActiveMs + combat.dodgeRecoveryMs && combat.singleTelegraphMs > 0 && combat.fastTelegraphMs > combat.attackFlightMs && combat.attackFlightMs > 0 && combat.singleTelegraphMs >= combat.fastTelegraphMs && combat.perfectCounterMs > 0, "combat: invalid cue/combo timing");
            Require(errors, combat.comboDefaultGapMs >= combat.comboMinGapMs && combat.maxVisibleThreats > 0, "combat: invalid minimum hit gap/threat visibility");
            Require(errors, combat.attackWindupMs > 0 && combat.attackWindupMs < combat.attackIntervalMs && combat.hpUnitsPerPoint == 1000, "combat: invalid attack timing or milli-HP scale");
            Require(errors, combat.normalStagger > 0 && combat.perfectStagger > 0 && combat.staggerThreshold > 0 && combat.damageStaggerPenalty > 0 && combat.normalStunMs > 0 && combat.minibossStunMs > 0 && combat.bossStunMs > 0 && combat.postStunImmunityMs > 0, "combat: invalid stagger/stun settings");
            Require(errors, Positive(combat.critDamageMultiplier) && Positive(combat.critChanceCap) && combat.critChanceCap <= 1 && Positive(combat.goldLuckBonusCap) && combat.goldLuckBonusCap <= 2 && Positive(combat.stunDamageMultiplierCap), "combat: invalid caps");
            if (characters != null) foreach (var c in characters)
            {
                if (c == null) continue;
                Require(errors, !string.IsNullOrWhiteSpace(c.displayName) && !string.IsNullOrWhiteSpace(c.artId), "characters." + c.id + ": displayName/artId required");
                if (c.id == defaultCharacterId) Require(errors, c.availableInMvp, "defaultCharacterId: must be available in MVP");
                Require(errors, c.stars != null && c.stars.Length == 5, "characters." + c.id + ".stars: attack/hp/luck/resonance/recovery required");
                if (c.stars != null) foreach (int star in c.stars) Require(errors, star >= 1 && star <= 5, "characters." + c.id + ".stars: expected 1..5");
            }
            if (skills != null)
            {
                CheckIds(errors, skills, x => x.id, "skills");
                var skillCharacters = new HashSet<string>();
                foreach (var skill in skills)
                {
                    if (skill == null) continue;
                    Require(errors, characterIds.Contains(skill.characterId ?? "") && skillCharacters.Add(skill.characterId), "skills: unknown or duplicate character");
                    Require(errors, !string.IsNullOrWhiteSpace(skill.displayName) && !string.IsNullOrWhiteSpace(skill.description), "skills: name/description required");
                    Require(errors, skill.cooldownMs == 20000 && skill.contactDelayMs == 200 && skill.damagePercent > 0 && skill.damagePercent <= 1000 &&
                        skill.shieldMaxHpPercent >= 0 && skill.shieldMaxHpPercent <= 100 && skill.shieldDurationMs >= 0 &&
                        (skill.shieldMaxHpPercent > 0) == (skill.shieldDurationMs > 0) && skill.staggerGain >= 0 && skill.staggerGain <= combat.staggerThreshold,
                        "skills: invalid cooldown or effect");
                }
                if (characters != null) foreach (var character in characters)
                    if (character != null && character.availableInMvp) Require(errors, skillCharacters.Contains(character.id), "skills: available character needs a skill");
            }
            if (relics != null) foreach (var r in relics)
            {
                if (r == null) continue;
                Require(errors, !string.IsNullOrWhiteSpace(r.displayName) && IsEffect(r.effect) && Positive(r.cap), "relics." + r.id + ": invalid name/effect/cap");
                Require(errors, r.values != null && r.values.Length == relicRules.maxLevel, "relics." + r.id + ".values: expected five total effects");
                if (r.values != null) for (int i = 0; i < r.values.Length; i++)
                    Require(errors, Positive(r.values[i]) && r.values[i] <= r.cap && (i == 0 || r.values[i] > r.values[i - 1]), "relics." + r.id + ".values: must increase and be finite within cap");
            }
            if (enemies != null) foreach (var enemy in enemies)
                if (enemy != null) Require(errors, !string.IsNullOrWhiteSpace(enemy.displayName) && !string.IsNullOrWhiteSpace(enemy.artId), "enemies." + enemy.id + ": displayName/artId required");
            var stageNumbers = new HashSet<int>();
            var playableEnemies = new HashSet<string>();
            var playableArt = new HashSet<string>();
            Require(errors, stages != null && stages.Length == boundaryStage, "stages: expected contiguous 1..boundaryStage");
            Require(errors, mvpLastStage == 20 && boundaryStage == 21, "stages: prototype boundary must be 20 -> 21");
            if (stages != null) foreach (var s in stages)
            {
                if (s == null) { errors.Add("stages: null entry"); continue; }
                string path = "stages." + s.number;
                Require(errors, s.number >= 1 && s.number <= boundaryStage && stageNumbers.Add(s.number), path + ": invalid/duplicate number");
                Require(errors, s.enemyId != null && enemyIds.Contains(s.enemyId), path + ".enemyId: unknown enemy");
                if(s.number<=mvpLastStage&&s.enemyId!=null)
                {
                    Require(errors,playableEnemies.Add(s.enemyId),path+": playable stages must use distinct enemies");
                    var enemy=enemies==null?null:Array.Find(enemies,x=>x!=null&&x.id==s.enemyId);
                    if(enemy!=null)Require(errors,playableArt.Add(enemy.artId),path+": playable stages must use distinct enemy art");
                }
                string kind = s.number % 10 == 0 ? "boss" : s.number % 5 == 0 ? "miniboss" : "normal";
                Require(errors, s.kind == kind, path + ".kind: inconsistent with floor");
                bool dashOnly = s.number == 11 && s.randomPatterns != null && s.randomPatterns.Length == 1 && s.randomPatterns[0] == "frontal_dash";
                Require(errors, s.randomPatterns != null && (s.randomPatterns.Length >= 2 || dashOnly) && Array.TrueForAll(s.randomPatterns, x => RequiredDodge(x) != null), path + ": invalid random pool");
                Require(errors, s.sequenceWeights != null && s.sequenceWeights.Length == 3 && Array.TrueForAll(s.sequenceWeights,x=>x>=0) && s.sequenceWeights[0]+s.sequenceWeights[1]+s.sequenceWeights[2]==100, path + ": invalid sequence weights");
                Require(errors, s.maxHp > 0 && s.damage > 0, path + ": HP and damage must be positive");
                Require(errors, string.IsNullOrEmpty(s.teachDodge) || IsDirection(s.teachDodge), path + ".teachDodge: invalid direction");
                Require(errors, s.patternSteps != null && s.patternSteps.Length > 0, path + ".patternSteps: required");
                if (s.patternSteps == null) continue;
                if (s.number >= 1 && s.number <= 3)
                {
                    string direction = new[] { "up", "back", "parry", "forward" }[s.number - 1];
                    Require(errors, s.teachDodge == direction && s.patternSteps.Length > 0 && s.patternSteps[0] != null && s.patternSteps[0].requiredDodge == direction, path + ": first attack must introduce its tutorial direction");
                }
                for (int i = 0; i < s.patternSteps.Length; i++)
                {
                    var p = s.patternSteps[i];
                    if (p == null) { errors.Add(path + ".patternSteps[" + i + "]: null"); continue; }
                    string expected = RequiredDodge(p.danger);
                    Require(errors, expected != null && p.requiredDodge == expected, path + ".patternSteps[" + i + "]: danger/dodge mismatch");
                    Require(errors,p.comboStep>=0&&p.comboStep<=combat.maxVisibleThreats&&(p.telegraphMs==combat.singleTelegraphMs||p.telegraphMs==combat.fastTelegraphMs),path+": invalid cue");
                    Require(errors,p.intervalMs>=combat.comboMinGapMs&&(p.comboStep>1||p.intervalMs>=p.telegraphMs),path+": invalid hit interval");
                    Require(errors,p.comboStep<=1||(i>0&&s.patternSteps[i-1]!=null&&p.telegraphMs==s.patternSteps[i-1].telegraphMs),path+": combo tempo mismatch");
                    Require(errors, p.comboStep <= 1 || (i > 0 && s.patternSteps[i - 1] != null && s.patternSteps[i - 1].comboStep == p.comboStep - 1), path + ".patternSteps[" + i + "]: broken combo order");
                    Require(errors, p.comboStep != 1 || (i + 1 < s.patternSteps.Length && s.patternSteps[i + 1] != null && s.patternSteps[i + 1].comboStep == 2), path + ".patternSteps[" + i + "]: combo start needs a followup");
                }
            }
            return errors.ToArray();
        }

        public static bool IsDirection(string direction) { return direction == "up" || direction == "back" || direction == "parry"; }
        public static string RequiredDodge(string danger)
        {
            switch (danger) { case "low_sweep": return "up"; case "high_sweep": return "parry"; case "thrust": case "frontal_dash": return "back"; default: return null; }
        }
        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Positive(float value) { return IsFinite(value) && value > 0; }
        private static bool PositiveArray(int[] values, int count)
        {
            if (values == null || values.Length != count) return false;
            foreach (int value in values) if (value <= 0) return false;
            return true;
        }
        private static bool IsEffect(string effect)
        {
            return effect == "attackPercent" || effect == "maxHpPercent" || effect == "normalStaggerPercent" || effect == "perfectStaggerPercent" || effect == "stunDamageBonus" || effect == "damageReduction" || effect == "critChanceBonus";
        }
        private static bool PositiveArray(long[] values,int count)
        {
            if(values==null||values.Length!=count)return false;
            foreach(long value in values)if(value<=0)return false;
            return true;
        }
        private static void Require(List<string> errors, bool condition, string message) { if (!condition) errors.Add(message); }
        private static HashSet<string> CheckIds<T>(List<string> errors, T[] values, Func<T, string> id, string path) where T : class
        {
            var found = new HashSet<string>(StringComparer.Ordinal);
            if (values == null || values.Length == 0) { errors.Add(path + ": required array"); return found; }
            foreach (T value in values)
            {
                if (value == null) { errors.Add(path + ": null entry"); continue; }
                string key = id(value);
                Require(errors, !string.IsNullOrWhiteSpace(key) && found.Add(key), path + ": empty/duplicate ID " + key);
            }
            return found;
        }
    }

    [Serializable] public sealed class TransitionConfig
    {
        public int enemyDefeatMs, travelMs, arrivalMs, deathMs;
        public int minibossIntroMs, bossIntroMs, repeatMinibossIntroMs, repeatBossIntroMs;
    }
    [Serializable] public sealed class StoryDefinition
    {
        public string id, bossId, kind, companionId;
        public int stage;
        public StoryLineDefinition[] lines;
    }
    [Serializable] public sealed class StoryLineDefinition
    {
        public string speaker, text, portraitId;
        public StoryResponseDefinition[] responses;
    }
    [Serializable] public sealed class StoryResponseDefinition { public string characterId, text; }
    [Serializable] public sealed class CombatConfig
    {
        public int stunExtensionMs, stunExtensionCapMs, perfectStreakMaxSteps;
        public float stunAttackSpeedMultiplier, minibossStaggerMultiplier, bossStaggerMultiplier, perfectStreakStep;
        public int dodgeActiveMs, dodgeRecoveryMs, perfectWindowMs, comboMinGapMs;
        public int normalStagger, perfectStagger, staggerThreshold, damageStaggerPenalty;
        public int attackIntervalMs, attackWindupMs, normalStunMs, minibossStunMs, bossStunMs, postStunImmunityMs;
        public int singleTelegraphMs, fastTelegraphMs, attackFlightMs, fastAttackFlightMs, comboDefaultGapMs, perfectCounterMs, maxVisibleThreats, hpUnitsPerPoint;
        public float critDamageMultiplier, critChanceCap, goldLuckBonusCap, stunDamageMultiplierCap;
    }
    [Serializable] public sealed class GrowthConfig
    {
        public int minLevel, maxLevel, effectiveMilestoneLevels;
        public int[] breakthroughLevels;
        public float[] starScale;
        public float attackBase, attackLinearPerLevel, attackBreakBase, attackBreakPerStar;
        public float hpBase, hpLinearPerLevel, hpBreakBase, hpBreakPerStar;
        public float critBase, resonanceBase;
        public int[] luckCritPerTenLevels, luckGoldPerLevel, resonancePerLevel;
    }
    [Serializable] public sealed class EconomyConfig
    {
        public long initialGold;
        public int initialFragments, minibossFragments, bossFragments;
        public int minibossGoldMultiplier, bossGoldMultiplier;
        public long[] normalGoldPerStage, maxRelicGoldPerZone, upgradeCosts, exchangeGoldPerUnlockTier;
        public int[] storageExpansionCosts, exchangeTierUnlockBosses;
        public float ngGoldMultiplier;
    }
    [Serializable] public sealed class RelicRules
    {
        public int maxLevel, initialCapacity, maxOwned, choicesPerBoss;
        public bool includeMaxInPool;
    }
    [Serializable] public sealed class SkillDefinition
    {
        public string id, characterId, displayName, description;
        public int cooldownMs, contactDelayMs, damagePercent, shieldMaxHpPercent, shieldDurationMs, staggerGain;
    }

    [Serializable] public sealed class CharacterDefinition
    {
        public string id, displayName, artId;
        // Fixed order: attack, hp, luck, resonance. These are aptitude stars, not purchasable levels.
        public int[] stars;
        public bool availableInMvp;
    }
    [Serializable] public sealed class EnemyDefinition { public string id, displayName, artId; }
    [Serializable] public sealed class StageDefinition
    {
        public int number, maxHp, damage;
        public string enemyId, kind, teachDodge;
        public PatternStep[] patternSteps;
        public string[] randomPatterns;
        public int[] sequenceWeights;
    }
    [Serializable] public sealed class PatternStep
    {
        public string danger, requiredDodge;
        // Milliseconds between the PREVIOUS hit and this hit (also across cycle wrap).
        // On battle start, first hit occurs after its telegraphMs, not intervalMs.
        public int telegraphMs, intervalMs;
        // 0 = independent single, 1 = combo overview/start, 2+ = ordered combo followups.
        public int comboStep;
    }
    [Serializable] public sealed class RelicDefinition
    {
        public string id, displayName, effect, effectLabel;
        public float cap;
        // Total effect at Lv.1..5; use values[level - 1], never sum levels.
        public float[] values;
        public float EffectAtLevel(int level)
        {
            if (level == 0) return 0f;
            if (values == null || level < 0 || level > values.Length) throw new ArgumentOutOfRangeException(nameof(level));
            return values[level - 1];
        }
    }
}
