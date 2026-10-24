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
                fragments = catalog.economy.initialFragments,
                storageCapacity = catalog.relicRules.initialCapacity
            } : CloneSave(saved);
            foreach (var definition in catalog.characters)
                if (!state.characters.Exists(x => x.id == definition.id))
                    state.characters.Add(new CharacterProgressData { id = definition.id });
            MigrateSave();
            if(state.schemaVersion==3)UpgradeIntegerStatsSave();
            if(state.schemaVersion==4){state.prologueSeen=true;state.schemaVersion=5;}
            if(state.schemaVersion==5)
            {
                if(state.phase==SessionPhase.MvpComplete||state.run!=null&&state.run.stage>10){state.phase=SessionPhase.Lobby;state.run=null;state.exchange=null;}
                if(state.run!=null){state.run.randomSequence=new string[0];state.run.sequenceCursor=0;state.run.attackSpeed=1;}
                state.schemaVersion=6;
            }
            MigrateReferenceSave();
            if (state.schemaVersion == 7)
            {
                if (state.unlockedSlots < 1 || state.unlockedSlots > 3)
                    throw new InvalidOperationException("Invalid legacy equipment capacity.");
                // Preserve old paid expansions and every owned relic without charging again.
                state.storageCapacity = Math.Max(catalog.relicRules.initialCapacity + state.unlockedSlots - 1, state.relics.Count);
                state.schemaVersion = 8;
            }
            MigrateAdventureSave();
            MigrateIndependentSkillSave();
            ValidateSave();
            // A balance update can lower the allowed combo length or change the enemy pool.
            // Preserve still-valid unresolved attacks and restart their complete warnings.
            if(state.run!=null&&catalog.TryGetStage(state.run.stage,out StageDefinition resumeStage))
            {
                var run=state.run;int maxCount=Array.FindLastIndex(resumeStage.sequenceWeights,x=>x>0)+1;
                bool changed=run.randomSequence.Length-run.sequenceCursor>maxCount;
                for(int i=run.sequenceCursor;i<run.randomSequence.Length;i++)
                    changed|=Array.IndexOf(resumeStage.randomPatterns,run.randomSequence[i])<0;
                if(changed)
                {
                    var pending=new List<string>();
                    for(int i=run.sequenceCursor;i<run.randomSequence.Length&&pending.Count<maxCount;i++)
                        if(Array.IndexOf(resumeStage.randomPatterns,run.randomSequence[i])>=0)pending.Add(run.randomSequence[i]);
                    run.randomSequence=pending.ToArray();run.sequenceCursor=0;
                }
            }
            // The previous prototype reset checkpoints on every load. Recover the
            // Stage 11 unlock from each character's cleared-stage record, not the shared wallet progress.
            foreach(var character in state.characters)
                character.checkpoint=character.checkpoint>=10||character.highestStage>=10?10:0;
            RefundMovedReservations();
            if(state.run!=null&&state.run.stage>catalog.mvpLastStage)
            {
                state.phase=SessionPhase.Lobby;state.run=null;state.exchange=null;
                LastMessage=$"프로토타입은 {catalog.mvpLastStage}스테이지까지입니다. 새 도전은 {CheckpointStartStage}스테이지부터 시작합니다.";
            }
            if (saved == null) { state.phase=SessionPhase.Prologue; IsDirty=true; return; }
            if (state.phase == SessionPhase.ExchangeConfirmation)
            {
                // Quotes are never executed on load; a fresh button press must confirm a current quote.
                if (!IsQuoteCurrent()) { state.exchange = null; state.phase = SessionPhase.Lobby; }
            }
            if (state.phase == SessionPhase.Combat)
            {
                state.run.attackHitAtMs = -1;
                state.run.pendingSkillAttack = false;
                state.run.dodgeStartMs = -1000000;
                if(GuidedPractice)
                {
                    if(state.run.comboPractice==ComboPracticeMode.Recovery){ClearCombatQueue();state.run.enemyHitAtMs=-1;}
                    else StartGuidedPractice();
                    paused=true;IsDirty=true;return;
                }
                if (!IsEnemyStunned) ScheduleCurrentCue();
                paused = true;
                LastMessage = "계속하면 온전한 공격 예고부터 재개합니다.";
            }
            if (state.phase == SessionPhase.TutorialAttack) state.run.attackHitAtMs = -1;
            IsDirty = true;
        }

        private void ValidateSave()
        {
            if (state.schemaVersion != 9 || state.gold < 0 || state.fragments < 0 ||
                state.storageCapacity < catalog.relicRules.initialCapacity || state.storageCapacity > catalog.relicRules.maxOwned || state.relics.Count > state.storageCapacity ||
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
                if (!characters.Add(character.id) || character.statLevels.Length != 5 ||
                    character.checkpoint < 0 || character.checkpoint % 10 != 0 || character.cycle != 0)
                    throw new InvalidOperationException("Invalid character progress.");
                foreach (int level in character.statLevels)
                    if (level < 0 || level > catalog.growth.maxLevel) throw new InvalidOperationException("Invalid stat level.");
                var equipped = new HashSet<string>();
                foreach (string id in character.equippedRelicIds)
                    if (!equipped.Add(id) || FindRelicLevel(id) == 0) throw new InvalidOperationException("Invalid equipment.");
                var seen = new HashSet<string>();
                foreach (string id in character.seenStoryIds)
                    if (catalog.FindStory(id) == null || !seen.Add(id)) throw new InvalidOperationException("Invalid seen story.");
            }
            bool active = state.phase == SessionPhase.MvpComplete || state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialDodge ||
                state.phase == SessionPhase.TutorialAttack || state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.RunEnd || state.phase == SessionPhase.RewardAnnouncement ||
                state.phase == SessionPhase.Transition || state.phase == SessionPhase.StoryDialogue || state.phase == SessionPhase.DeathResult ||
                state.phase == SessionPhase.ComboHint;
            bool death = state.phase == SessionPhase.Transition && state.run?.transition?.kind == TransitionKind.Death;
            if (active && (state.run == null || (death ? state.run.playerHpMilli != 0 : !state.run.ended && state.run.playerHpMilli <= 0) || state.run.clockMs < 0 ||
                double.IsNaN(state.run.clockMs) || double.IsInfinity(state.run.clockMs) ||
                state.run.characterId != state.selectedCharacterId || !catalog.TryGetStage(state.run.stage, out _)))
                throw new InvalidOperationException("Invalid active run.");
            if(active && (state.run.combatStats==null || state.run.combatStats.maxHp<=0 || state.run.combatStats.attack<=0 || state.run.earnedGold<0 || state.run.earnedFragments<0))
                throw new InvalidOperationException("Invalid integer combat stats or run earnings.");
            if ((state.phase == SessionPhase.RewardChoice) &&
                (state.run.offer == null || state.run.offer.relicIds.Length != catalog.relicRules.choicesPerBoss ||
                 state.run.offer.offeredLevels.Length != state.run.offer.relicIds.Length || !state.run.ended))
                throw new InvalidOperationException("Invalid pending reward.");
            if (state.run != null)
            {
                var r=state.run;
                ValidateSkillSave(r);
                if(r.sequenceCursor<0||r.sequenceCursor>r.randomSequence.Length||r.randomSequence.Length>3||
                    Array.Exists(r.randomSequence,x=>GameCatalog.RequiredDodge(x)==null)||r.perfectStreak<0||r.hitCombo<0||
                    r.stunExtensionMs<0||r.stunExtensionMs>catalog.combat.stunExtensionCapMs||r.defeatedGold<0||
                    double.IsNaN(r.attackSpeed)||double.IsInfinity(r.attackSpeed)||r.attackSpeed<=0)
                    throw new InvalidOperationException("Invalid combat sequence or feedback state.");
            }
            if (state.run?.offer != null)
            {
                var offer = state.run.offer;
                if (!state.run.ended ||
                    offer.rewardId != state.run.runId + ":" + state.run.cycle + ":" + offer.stage ||
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
                var scene = catalog.FindStory(story.sceneId,story.legacy);
                if (scene == null || scene.stage != state.run.stage || !state.run.stageRewardApplied ||
                    story.rewardId != state.run.runId+":"+state.run.cycle+":"+state.run.stage || story.lineIndex < 0 || story.lineIndex >= scene.lines.Length ||
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
            if(state.phase==SessionPhase.ComboHint&&(state.run.stage!=5||state.comboPracticeCompleted||state.comboPracticeSkipped||state.run.enemyHitAtMs!=-1))
                throw new InvalidOperationException("Invalid combo hint state.");
            ValidateSettlement();
            ValidateAdventureSave();
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
            return LobbyStatsFor(c,c.statLevels);
        }
        private CombatStatsData LobbyStatsFor(CharacterProgressData c,int[] levels)
        {
            var owned=AllOwnedRelicIds();
            return CalculateStats(c.id,levels,owned,Array.ConvertAll(owned,FindRelicLevel),c.weaponRank,c.reservations.Exists(x=>x.serviceId=="travel_blessing"),c.reservations.Exists(x=>x.serviceId=="herbal_tea"),CompanionUnlocked("tori"));
        }

        private string[] AllOwnedRelicIds() => Array.ConvertAll(Array.FindAll(catalog.relics, r => FindRelicLevel(r.id) > 0), r => r.id);

        private CombatStatsData CalculateStats(string characterId, int[] levels, string[] equipment, int[] relicLevels,int weaponRank=0,bool blessing=false,bool tea=false,bool guide=false)
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
            // Whole percentage points: luck always improves gold, even between crit steps.
            int luckStar = stars[2] - 1, resonanceStar = stars[3] - 1;
            int critGrowth = levels[2] * g.luckCritPerTenLevels[luckStar] / 10 + stars[2] * breaks[2];
            int goldGrowth = levels[2] * g.luckGoldPerLevel[luckStar] + stars[2] * breaks[2];
            int resonanceGrowth = g.resonancePerLevel[resonanceStar] * (levels[3] + g.effectiveMilestoneLevels * breaks[3]);
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
            int recovery=CalculateRecovery(stars[4],levels.Length>4?levels[4]:0);
            return new CombatStatsData
            {
                baseAttack=Rounded(attack),recovery=recovery+(tea?Rounded(recovery*catalog.adventure.teaRecoveryPercent/100d):0),
                attack = Rounded(attack * (1 + bonus("attackPercent")+(weaponRank>0?catalog.adventure.weaponAttackPercents[weaponRank-1]/100d:0))),
                maxHp = Rounded(hp * (1 + bonus("maxHpPercent")+(blessing?catalog.adventure.blessingHpPercent/100d:0))),
                critChancePercent = Math.Min(Rounded(Number(c.critChanceCap)*100), Rounded(Number(g.critBase)*100) + critGrowth + Rounded(raw("critChanceBonus")*100)),
                goldBonusPercent = Math.Min(Rounded(Number(c.goldLuckBonusCap)*100), goldGrowth+(guide?catalog.adventure.guideGoldPercent:0)),
                stunDamagePercent = Math.Min(Rounded(Number(c.stunDamageMultiplierCap)*100), Rounded(Number(g.resonanceBase)*100) + resonanceGrowth + Rounded(raw("stunDamageBonus")*100)),
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
