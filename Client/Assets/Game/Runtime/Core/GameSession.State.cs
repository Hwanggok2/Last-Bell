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
            if (state.schemaVersion != 1 || state.gold < 0 || state.fragments < 0 ||
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
            }
            bool active = state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialDodge ||
                state.phase == SessionPhase.TutorialAttack || state.phase == SessionPhase.RewardChoice;
            if (active && (state.run == null || state.run.playerHpMilli <= 0 || state.run.clockMs < 0 ||
                double.IsNaN(state.run.clockMs) || double.IsInfinity(state.run.clockMs) ||
                state.run.characterId != state.selectedCharacterId || !catalog.TryGetStage(state.run.stage, out _)))
                throw new InvalidOperationException("Invalid active run.");
            if (state.phase == SessionPhase.RewardChoice &&
                (state.run.offer == null || state.run.offer.relicIds.Length != catalog.relicRules.choicesPerBoss ||
                 state.run.offer.offeredLevels.Length != state.run.offer.relicIds.Length || !state.run.stageRewardApplied))
                throw new InvalidOperationException("Invalid pending reward.");
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
                attack = attack * (1 + bonus("attackPercent")),
                maxHpMilli = (long)Rounded(hp * (1 + bonus("maxHpPercent"))) * 1000,
                critChance = Math.Min(Number(c.critChanceCap), Number(g.critBase) + Number(g.critPerEffectiveLevel) * luck + raw("critChanceBonus")),
                goldMultiplier = 1 + Math.Min(Number(c.goldLuckBonusCap), Number(g.goldBonusPerEffectiveLevel) * luck),
                stunDamageMultiplier = Math.Min(Number(c.stunDamageMultiplierCap), Number(g.resonanceBase) + Number(g.resonancePerEffectiveLevel) * resonance + raw("stunDamageBonus")),
                normalStagger = c.normalStagger * (1 + bonus("normalStaggerPercent")),
                perfectStagger = c.perfectStagger * (1 + bonus("perfectStaggerPercent")),
                damageReduction = bonus("damageReduction")
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
                case "down": return DodgeDirection.Down;
                case "forward": return DodgeDirection.Forward;
                default: throw new InvalidOperationException("Unknown dodge direction.");
            }
        }
    }
}
