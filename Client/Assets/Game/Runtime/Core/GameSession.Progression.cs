using System;

namespace LastBell
{
    public sealed partial class GameSession
    {
        private bool BeginRun()
        {
            if (state.phase != SessionPhase.Lobby) return Reject("로비에서 출전할 수 있습니다.");
            var character = CurrentCharacter;
            if (!catalog.GetCharacter(character.id).availableInMvp) return Reject("아직 제작 중인 캐릭터입니다.");
            state.exchange = null;
            paused = false;
            LastMessage = "";
            RefundMovedReservations();
            state.run = new RunStateData
            {
                characterId = character.id,
                skillRulesVersion = 1,
                runId = ++state.runSequence,
                cycle = character.cycle,
                stage = CheckpointStartStage,
                statLevels = (int[])character.statLevels.Clone(),
                equippedRelicIds = AllOwnedRelicIds(),
                appliedRelicLevels = Array.ConvertAll(AllOwnedRelicIds(), FindRelicLevel)
            };
            var run=state.run;run.weaponRank=character.weaponRank;run.blessing=IsServiceReserved("travel_blessing");run.guideApplied=CompanionUnlocked("tori");
            bool tea=IsServiceReserved("herbal_tea");
            run.combatStats = CalculateStats(character.id,run.statLevels,run.equippedRelicIds,run.appliedRelicLevels,run.weaponRank,run.blessing,tea,run.guideApplied);
            run.teaRecovery=tea?Rounded(CalculateRecovery(catalog.GetCharacter(character.id).stars[4],run.statLevels[4])*catalog.adventure.teaRecoveryPercent/100d):0;
            character.reservations.Clear();
            foreach(string id in character.companionIds)SnapshotCompanion(id);
            state.run.playerHpMilli = state.run.combatStats.maxHpMilli;
            PrepareStage(state.run.stage);
            ContinueOpening();
            if (state.phase != SessionPhase.MvpComplete) Emit(SessionEventKind.StagePrepared);
            Publish();
            return true;
        }

        private void ContinueOpening()
        {
            if (state.phase != SessionPhase.MvpComplete && state.tutorialStep < 2)
            {
                state.phase = state.tutorialStep == 0 ? SessionPhase.TutorialDodge : SessionPhase.TutorialAttack;
                state.run.enemyHitAtMs = -1;
            }
            else if (state.phase != SessionPhase.MvpComplete)
                StartTransition(TransitionKind.Arrival, state.run.stage, state.run.stage, catalog.transitions.arrivalMs);
        }

        private void PrepareStage(int stageNumber)
        {
            var run = state.run;
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            if (run.cycle != 0 || stageNumber > catalog.mvpLastStage || !catalog.TryGetStage(stageNumber, out StageDefinition stage))
            {
                run.cleared=true;run.ended=true;
                state.phase = SessionPhase.RunEnd;
                run.enemyHitAtMs = -1;
                run.attackHitAtMs = -1;
                run.transition = null;
                paused = false;
                run.hitCombo=run.perfectStreak=0;
                LastMessage = catalog.mvpLastStage+"스테이지를 모두 클리어했습니다!";
                Emit(SessionEventKind.MvpCompleted);
                return;
            }
            run.stage = stageNumber;
            run.comboPractice=ComboPracticeMode.None;
            run.enemyMaxHpMilli = (long)stage.maxHp * 1000;
            run.enemyHpMilli = run.enemyMaxHpMilli;
            run.stagger = 0;
            run.stunUntilMs = -1;
            run.immunityUntilMs = -1;
            run.attackHitAtMs = -1;
            run.enemyHitAtMs = -1;
            run.attackReadyAtMs = run.clockMs;
            run.dodgeStartMs = -1000000;
            run.patternStep = 0;
            run.patternIndex = 0;
            run.randomSequence=new string[0];run.sequenceCursor=0;
            run.previousPattern=run.penultimatePattern=null;
            run.perfectStreak=run.stunExtensionMs=0;run.defeatedGold=0;run.attackSpeed=1;
            run.stageRewardApplied = false;
            run.offer = null;
            run.story = null;
        }

        private bool EndRun()
        {
            if(practiceBackupState!=null)return EndComboPractice();
            if (state.phase == SessionPhase.RunEnd || state.phase == SessionPhase.RewardAnnouncement || state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.StoryDialogue ||
                (state.phase == SessionPhase.Transition && state.run?.offer != null)) return Reject("보상을 먼저 마치세요.");
            if (state.tutorialStep < 2) return Reject("회피와 공격 연습을 먼저 마치세요.");
            if (state.phase == SessionPhase.Lobby || state.phase == SessionPhase.ExchangeConfirmation) return false;
            if(state.run!=null&&!state.run.ended)
            { BeginSettlement(false);Publish();return true; }
            state.phase = SessionPhase.Lobby;
            state.exchange = null;
            paused = false;
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            if (state.run != null) { state.run.hitCombo=state.run.perfectStreak=0; state.run.attackHitAtMs = -1; state.run.enemyHitAtMs = -1; state.run.transition = null; }
            Publish();
            return true;
        }

        private void DefeatPlayer()
        {
            state.run.playerHpMilli = 0;
            state.run.story=null;
            state.run.hitCombo=state.run.perfectStreak=0;
            state.run.attackHitAtMs = -1;
            state.run.enemyHitAtMs = -1;
            StartTransition(TransitionKind.Death, state.run.stage, CheckpointStartStage, catalog.transitions.deathMs);
            paused = false;
            LastMessage = "쓰러졌습니다.";
            Emit(SessionEventKind.PlayerDefeated);
        }

        private void DefeatEnemy()
        {
            var run = state.run;
            if (run.stageRewardApplied) return;
            run.stageRewardApplied = true;
            run.enemyHpMilli = 0;
            run.enemyHitAtMs = -1;
            run.attackHitAtMs = -1;
            var stage = catalog.GetStage(run.stage);
            long gold = checked((long)decimal.Floor(catalog.GetBaseGold(stage) * (decimal)run.combatStats.goldMultiplier));
            state.gold = checked(state.gold + gold);
            int fragments=catalog.GetFragmentReward(stage);
            state.fragments = checked(state.fragments + fragments);
            run.earnedGold=checked(run.earnedGold+gold);
            run.earnedFragments=checked(run.earnedFragments+fragments);
            state.walletVersion++;
            state.highestClearedStage = Math.Max(state.highestClearedStage, run.stage);
            CurrentCharacter.highestStage = Math.Max(CurrentCharacter.highestStage, run.stage);
            if (run.stage >= 10) CurrentCharacter.checkpoint = 10;
            if (stage.kind == "boss")
            {
                CreateRelicOffer();
            }
            if(run.playerHpMilli>0)
            {
                long heal=Math.Min(Milli(run.combatStats.recovery),run.combatStats.maxHpMilli-run.playerHpMilli);
                run.playerHpMilli+=heal;if(heal>0)Emit(SessionEventKind.PlayerRecovered,recoveryMilli:heal);
            }
            PrepareStory();
            StartTransition(TransitionKind.EnemyDefeat, run.stage, run.stage + 1, catalog.transitions.enemyDefeatMs);
            run.defeatedGold=gold;
            if(run.stage==10 && run.story!=null){run.transition=null;state.phase=SessionPhase.StoryDialogue;}
            Emit(SessionEventKind.EnemyDefeated,goldReward:gold);
        }

        private void CreateRelicOffer()
        {
            var run = state.run;
            string[] candidates = Array.ConvertAll(catalog.relics, x => x.id);
            int count = catalog.relicRules.choicesPerBoss;
            for (int i = 0; i < count; i++)
            {
                int chosen = i + RandomIndex(candidates.Length - i);
                string old = candidates[i];
                candidates[i] = candidates[chosen];
                candidates[chosen] = old;
            }
            var ids = new string[count];
            Array.Copy(candidates, ids, count);
            run.pendingRewards.Add(new RelicOfferData
            {
                rewardId = run.runId + ":" + run.cycle + ":" + run.stage,
                stage = run.stage,
                relicIds = ids,
                offeredLevels = Array.ConvertAll(ids, FindRelicLevel),
                maxGold = catalog.GetMaxRelicGold(run.stage)
            });
        }

        private bool CommitRelicChoice(string id, bool expandStorage, bool convertToGold)
        {
            if (paused || state.phase != SessionPhase.RewardChoice || state.run?.offer == null) return false;
            var run = state.run;
            var offer = run.offer;
            int index = Array.IndexOf(offer.relicIds, id);
            if (index < 0 || run.lastAppliedRewardId == offer.rewardId) return false;
            int level = FindRelicLevel(id);
            if (level != offer.offeredLevels[index]) return Reject("보상 상태가 달라졌습니다. 저장 상태를 확인하세요.");
            bool full = level == 0 && OwnedRelicCount >= StorageCapacity;
            if (convertToGold && !full) return false;
            if (full && !convertToGold)
            {
                int cost = NextStorageExpansionCost;
                if (!expandStorage) return Reject("보관함이 가득 찼습니다. 확장 비용을 확인하세요.");
                if (cost <= 0 || state.fragments < cost) return Reject("종 파편이 부족합니다. 다른 유물을 고르거나 골드로 받으세요.");
                state.fragments -= cost;
                state.walletVersion++;
                state.storageCapacity++;
            }
            if (convertToGold) { state.gold = checked(state.gold + offer.maxGold); run.earnedGold = checked(run.earnedGold + offer.maxGold); state.walletVersion++; }
            else if (level == 0) state.relics.Add(new OwnedRelicData { id = id, level = 1 });
            else if (level < catalog.relicRules.maxLevel) state.relics.Find(x => x.id == id).level++;
            else { state.gold = checked(state.gold + offer.maxGold); run.earnedGold=checked(run.earnedGold+offer.maxGold); state.walletVersion++; }
            run.lastAppliedRewardId = offer.rewardId;
            run.claimedRewards.Add(offer.rewardId);
            run.pendingRewards.RemoveAll(x=>x.rewardId==offer.rewardId);
            run.offer = null;
            ShowNextSettlement();
            Emit(SessionEventKind.RelicChosen, relicId: id, eventStage: offer.stage);
            Publish();
            return true;
        }
    }
}
