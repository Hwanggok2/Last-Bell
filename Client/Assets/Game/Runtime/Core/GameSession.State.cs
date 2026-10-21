using System;
using System.Collections.Generic;

namespace LastBell
{
    public sealed partial class GameSession
    {
        private void Initialize(GameSaveData saved)
        {
            catalog.ValidateOrThrow();
            if (catalog.combat.hpUnitsPerPoint != 1000) throw new InvalidOperationException("Core requires milli-HP.");
            state = saved == null ? new GameSaveData
            {
                selectedCharacterId = catalog.defaultCharacterId,
                gold = catalog.economy.initialGold,
                fragments = catalog.economy.initialFragments
            } : CloneSave(saved);
            foreach (var definition in catalog.characters)
                if (!state.characters.Exists(x => x.id == definition.id))
                    state.characters.Add(new CharacterProgressData { id = definition.id });
            MigrateSave();
            if(state.schemaVersion==3)UpgradeIntegerStatsSave();
            ValidateSave();
            if (saved == null) { BeginRun(); return; }
            if (state.phase == SessionPhase.ExchangeConfirmation)
            {
                // Quotes are never executed on load; a fresh button press must confirm a current quote.
                if (!IsQuoteCurrent()) { state.exchange = null; state.phase = SessionPhase.Lobby; }
            }
            if (state.phase == SessionPhase.Combat)
            {
                state.run.attackHitAtMs = -1;
                state.run.dodgeStartMs = -1000000;
                if (!IsEnemyStunned) ScheduleCurrentCue();
                paused = true;
                LastMessage = "계속하면 온전한 공격 예고부터 재개합니다.";
            }
            if (state.phase == SessionPhase.TutorialAttack) state.run.attackHitAtMs = -1;
            IsDirty = true;
        }

        private void ValidateSave()
        {
            if (state.schemaVersion != 4 || state.gold < 0 || state.fragments < 0 ||
                state.unlockedSlots < 1 || state.unlockedSlots > catalog.relicRules.maxEquipped ||
                state.tutorialStep < 0 || state.tutorialStep > 2 || CurrentCharacter == null ||
                !Enum.IsDefined(typeof(SessionPhase), state.phase))
                throw new InvalidOperationException("Invalid save header.");
            var relicIds = new HashSet<string>();
            foreach (var item in state.relics)
            {
                catalog.GetRelic(item.id);
                if (!relicIds.Add(item.id) || item.level < 1 || item.level > catalog.relicRules.maxLevel)
                    throw new InvalidOperationException("Invalid owned relic.");
            }
            var characters = new HashSet<string>();
            foreach (var character in state.characters)
            {
                catalog.GetCharacter(character.id);
                if (!characters.Add(character.id) || character.statLevels.Length != 4 ||
                    character.checkpoint < 0 || character.checkpoint % 10 != 0 || character.cycle != 0)
                    throw new InvalidOperationException("Invalid character progress.");
                foreach (int level in character.statLevels)
                    if (level < 0 || level > catalog.growth.maxLevel) throw new InvalidOperationException("Invalid stat level.");
                var equipped = new HashSet<string>();
                foreach (string id in character.equippedRelicIds)
                    if (!equipped.Add(id) || FindRelicLevel(id) == 0) throw new InvalidOperationException("Invalid equipment.");
                if (equipped.Count > state.unlockedSlots) throw new InvalidOperationException("Equipment exceeds slots.");
                var seen = new HashSet<string>();
                foreach (string id in character.seenStoryIds)
                    if (catalog.FindStory(id) == null || !seen.Add(id)) throw new InvalidOperationException("Invalid seen story.");
            }
            bool active = state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialDodge ||
                state.phase == SessionPhase.TutorialAttack || state.phase == SessionPhase.RewardChoice ||
                state.phase == SessionPhase.Transition || state.phase == SessionPhase.StoryDialogue || state.phase == SessionPhase.DeathResult;
            bool death = state.phase == SessionPhase.DeathResult || state.phase == SessionPhase.Transition && state.run?.transition?.kind == TransitionKind.Death;
            if (active && (state.run == null || (death ? state.run.playerHpMilli != 0 : state.run.playerHpMilli <= 0) || state.run.clockMs < 0 ||
                double.IsNaN(state.run.clockMs) || double.IsInfinity(state.run.clockMs) ||
                state.run.characterId != state.selectedCharacterId || !catalog.TryGetStage(state.run.stage, out _)))
                throw new InvalidOperationException("Invalid active run.");
            if(active && (state.run.combatStats==null || state.run.combatStats.maxHp<=0 || state.run.combatStats.attack<=0 || state.run.earnedGold<0 || state.run.earnedFragments<0))
                throw new InvalidOperationException("Invalid integer combat stats or run earnings.");
            if ((state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.StoryDialogue) &&
                (state.run.offer == null || state.run.offer.relicIds.Length != catalog.relicRules.choicesPerBoss ||
                 state.run.offer.offeredLevels.Length != state.run.offer.relicIds.Length || !state.run.stageRewardApplied))
                throw new InvalidOperationException("Invalid pending reward.");
            if (state.run?.offer != null)
            {
                var offer = state.run.offer;
                if (!state.run.stageRewardApplied || offer.stage != state.run.stage ||
                    offer.rewardId != state.run.runId + ":" + state.run.cycle + ":" + state.run.stage ||
                    offer.rewardId == state.run.lastAppliedRewardId ||
                    (catalog.GetStage(offer.stage).kind != "boss" && !(offer.legacyMidbossReward && catalog.GetStage(offer.stage).kind == "miniboss")) ||
                    offer.relicIds.Length != 3 || offer.offeredLevels.Length != 3)
                    throw new InvalidOperationException("Invalid reward identity.");
                var candidates = new HashSet<string>();
                for (int i = 0; i < offer.relicIds.Length; i++)
                {
                    catalog.GetRelic(offer.relicIds[i]);
                    if (!candidates.Add(offer.relicIds[i]) || offer.offeredLevels[i] != FindRelicLevel(offer.relicIds[i]))
                        throw new InvalidOperationException("Invalid reward candidates.");
                }
            }
            if (state.run?.story != null)
            {
                var story = state.run.story;
                var scene = catalog.FindStory(story.sceneId);
                if (scene == null || scene.stage != state.run.stage || state.run.offer == null ||
                    story.rewardId != state.run.offer.rewardId || story.lineIndex < 0 || story.lineIndex >= scene.lines.Length ||
                    CurrentCharacter.seenStoryIds.Contains(scene.id) ||
                    (state.phase != SessionPhase.StoryDialogue && !(state.phase == SessionPhase.Transition && state.run.transition?.kind == TransitionKind.EnemyDefeat)))
                    throw new InvalidOperationException("Invalid pending story.");
            }
            else if (state.phase == SessionPhase.StoryDialogue) throw new InvalidOperationException("Missing pending story.");
            if (state.phase == SessionPhase.Transition)
            {
                var t = state.run.transition;
                if (t == null || !Enum.IsDefined(typeof(TransitionKind), t.kind) || t.durationMs <= 0 ||
                    double.IsNaN(t.elapsedMs) || double.IsInfinity(t.elapsedMs) || t.elapsedMs < 0 || t.elapsedMs > t.durationMs ||
                    t.fromStage < 1 || t.fromStage > catalog.boundaryStage || t.toStage < 1 || t.toStage > catalog.boundaryStage + 1 ||
                    state.run.enemyHitAtMs != -1 || state.run.attackHitAtMs != -1)
                    throw new InvalidOperationException("Invalid pending transition.");
                bool defeated = t.kind == TransitionKind.EnemyDefeat;
                if ((defeated && (!state.run.stageRewardApplied || state.run.enemyHpMilli != 0 || state.run.stage != t.fromStage)) ||
                    (!death && !defeated && (state.run.stage != t.toStage || state.run.stageRewardApplied)) ||
                    (t.kind == TransitionKind.BossIntro && catalog.GetStage(state.run.stage).kind == "normal"))
                    throw new InvalidOperationException("Transition/run mismatch.");
            }
            else if (state.run?.transition != null) throw new InvalidOperationException("Transition outside transition phase.");
        }

        private void MigrateSave()
        {
            if (state.schemaVersion == 2) { UpgradeEconomySave(); return; }
            if (state.schemaVersion != 1) return;
            // Preserve rewards already promised by the old MVP, including its one
            // pending miniboss choice. New kills follow the boss-only rule.
            if (state.phase == SessionPhase.RewardChoice && state.run?.offer != null)
            {
                if (catalog.TryGetStage(state.run.stage, out StageDefinition stage))
                {
                    state.run.offer.legacyMidbossReward = stage.kind == "miniboss";
                    if (stage.kind == "boss")
                    {
                        PrepareStory();
                        if (state.run.story != null) state.phase = SessionPhase.StoryDialogue;
                    }
                }
            }
            state.schemaVersion = 2;
            UpgradeEconomySave();
        }

        private void UpgradeEconomySave()
        {
            // Existing numeric JSON gold fields deserialize losslessly into long.
            // Keep wallets, stat levels and promised relic rewards; only quotes expire.
            foreach (var c in state.characters)
                state.highestClearedStage = Math.Max(state.highestClearedStage, c.checkpoint);
            state.exchange = null;
            if (state.phase == SessionPhase.ExchangeConfirmation) state.phase = SessionPhase.Lobby;
            state.schemaVersion = 3;
        }

        private void UpgradeIntegerStatsSave()
        {
            var run=state.run;
            if(run!=null && !string.IsNullOrEmpty(run.characterId) && run.statLevels.Length==4)
            {
                run.combatStats=CalculateStats(run.characterId,run.statLevels,run.equippedRelicIds,run.appliedRelicLevels);
                // Round live HP to whole points, preserve death and all already-paid rewards.
                run.playerHpMilli=run.playerHpMilli<=0?0:Math.Min(run.combatStats.maxHpMilli,Math.Max(1000,Milli(Math.Round(run.playerHpMilli/1000d,MidpointRounding.AwayFromZero))));
                run.enemyHpMilli=run.enemyHpMilli<=0?0:Math.Max(1000,Milli(Math.Round(run.enemyHpMilli/1000d,MidpointRounding.AwayFromZero)));
                run.stats=null;
                // Old saves have no per-run ledger: explicitly label the recoverable portion.
                run.earnedGold=0;run.earnedFragments=0;run.earningsSinceUpdate=true;
            }
            state.schemaVersion=4;
        }

        private static double Number(float number) { return (double)(decimal)number; }
        private static long Milli(double hp) { return checked((long)Math.Round(hp * 1000, MidpointRounding.AwayFromZero)); }
        private static int Rounded(double value) { return checked((int)Math.Round(value, MidpointRounding.AwayFromZero)); }
        private static GameSaveData CloneSave(GameSaveData value) { return value.Copy(); }
        private static CombatStatsData CloneStats(CombatStatsData value) { return value?.Copy(); }
        private static ExchangeQuoteData CloneQuote(ExchangeQuoteData value) { return value?.Copy(); }

        private int FindRelicLevel(string id)
        {
            var relic = state.relics.Find(x => x.id == id);
            return relic == null ? 0 : relic.level;
        }

        private CombatStatsData BuildLobbyStats()
        {
            var c = CurrentCharacter;
            var levels = Array.ConvertAll(c.equippedRelicIds, FindRelicLevel);
            return CalculateStats(c.id, c.statLevels, c.equippedRelicIds, levels);
        }

        private CombatStatsData CalculateStats(string characterId, int[] levels, string[] equipment, int[] relicLevels)
        {
            var g = catalog.growth;
            var c = catalog.combat;
            int[] stars = catalog.GetCharacter(characterId).stars;
            var q = new double[4];
            var breaks = new int[4];
            for (int i = 0; i < 4; i++)
            {
                q[i] = Number(g.starScale[stars[i] - 1]);
                breaks[i] = Math.Max(0, (levels[i] - 1) / 10);
            }
            double attack = (Number(g.attackBase) + Number(g.attackLinearPerLevel) * q[0] * levels[0]) *
                Math.Pow(Number(g.attackBreakBase) + Number(g.attackBreakPerStar) * (stars[0] - 1), breaks[0]);
            int hp = Rounded((Number(g.hpBase) + Number(g.hpLinearPerLevel) * q[1] * levels[1]) *
                Math.Pow(Number(g.hpBreakBase) + Number(g.hpBreakPerStar) * (stars[1] - 1), breaks[1]));
            double luck = q[2] * (levels[2] + g.effectiveMilestoneLevels * breaks[2]);
            double resonance = q[3] * (levels[3] + g.effectiveMilestoneLevels * breaks[3]);
            var totals = new Dictionary<string, double>();
            var caps = new Dictionary<string, double>();
            for (int i = 0; i < equipment.Length; i++)
            {
                var relic = catalog.GetRelic(equipment[i]);
                if (!totals.ContainsKey(relic.effect)) totals[relic.effect] = 0;
                totals[relic.effect] += Number(relic.EffectAtLevel(relicLevels[i]));
                caps[relic.effect] = Number(relic.cap);
            }
            Func<string, double> raw = key => totals.TryGetValue(key, out double value) ? value : 0;
            Func<string, double> bonus = key => Math.Min(raw(key), caps.TryGetValue(key, out double cap) ? cap : 0);
            return new CombatStatsData
            {
                attack = Rounded(attack * (1 + bonus("attackPercent"))),
                maxHp = Rounded(hp * (1 + bonus("maxHpPercent"))),
                critChancePercent = Rounded((Math.Min(Number(c.critChanceCap), Number(g.critBase) + Number(g.critPerEffectiveLevel) * luck + raw("critChanceBonus"))) * 100),
                goldBonusPercent = Rounded((Math.Min(Number(c.goldLuckBonusCap), Number(g.goldBonusPerEffectiveLevel) * luck)) * 100),
                stunDamagePercent = Rounded((Math.Min(Number(c.stunDamageMultiplierCap), Number(g.resonanceBase) + Number(g.resonancePerEffectiveLevel) * resonance + raw("stunDamageBonus"))) * 100),
                normalStagger = Rounded((c.normalStagger * (1 + bonus("normalStaggerPercent")))),
                perfectStagger = Rounded((c.perfectStagger * (1 + bonus("perfectStaggerPercent")))),
                damageReductionPercent = Rounded((bonus("damageReduction")) * 100)
            };
        }

        private uint NextRandom()
        {
            uint value = unchecked((uint)state.randomState);
            if (value == 0) value = 17328719;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state.randomState = unchecked((int)value);
            return value;
        }

        private int RandomIndex(int count)
        {
            uint limit = uint.MaxValue - uint.MaxValue % (uint)count;
            uint value;
            do { value = NextRandom(); } while (value >= limit);
            return (int)(value % (uint)count);
        }

        private static DodgeDirection ParseDirection(string direction)
        {
            switch (direction)
            {
                case "up": return DodgeDirection.Up;
                case "back": return DodgeDirection.Back;
                case "down": case "parry": return DodgeDirection.Down;
                case "forward": return DodgeDirection.Forward;
                default: throw new InvalidOperationException("Unknown dodge direction.");
            }
        }
    }
}
