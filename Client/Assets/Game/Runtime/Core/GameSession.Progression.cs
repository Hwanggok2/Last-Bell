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
            state.run = new RunStateData
            {
                characterId = character.id,
                runId = ++state.runSequence,
                cycle = character.cycle,
                stage = character.checkpoint + 1,
                statLevels = (int[])character.statLevels.Clone(),
                equippedRelicIds = (string[])character.equippedRelicIds.Clone(),
                appliedRelicLevels = Array.ConvertAll(character.equippedRelicIds, FindRelicLevel)
            };
            state.run.combatStats = CalculateStats(character.id, state.run.statLevels, state.run.equippedRelicIds, state.run.appliedRelicLevels);
            state.run.playerHpMilli = state.run.combatStats.maxHpMilli;
            PrepareStage(character.checkpoint + 1);
            if (state.phase != SessionPhase.MvpComplete && state.tutorialStep < 2)
            {
                state.phase = state.tutorialStep == 0 ? SessionPhase.TutorialDodge : SessionPhase.TutorialAttack;
                state.run.enemyHitAtMs = -1;
            }
            else if (state.phase != SessionPhase.MvpComplete)
                StartTransition(TransitionKind.Arrival, state.run.stage, state.run.stage, catalog.transitions.arrivalMs);
            if (state.phase != SessionPhase.MvpComplete) Emit(SessionEventKind.StagePrepared);
            Publish();
            return true;
        }

        private void PrepareStage(int stageNumber)
        {
            var run = state.run;
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            if (run.cycle != 0 || !catalog.TryGetStage(stageNumber, out StageDefinition stage))
            {
                state.phase = SessionPhase.MvpComplete;
                run.enemyHitAtMs = -1;
                run.attackHitAtMs = -1;
                run.transition = null;
                LastMessage = "1~10층 MVP와 11층 경계 전투를 마쳤습니다. 이후 콘텐츠는 제작 중입니다.";
                Emit(SessionEventKind.MvpCompleted);
                return;
            }
            run.stage = stageNumber;
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
            run.stageRewardApplied = false;
            run.offer = null;
            run.story = null;
        }

        private bool EndRun()
        {
            if (state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.StoryDialogue ||
                (state.phase == SessionPhase.Transition && state.run?.offer != null)) return Reject("보상을 먼저 마치세요.");
            if (state.tutorialStep < 2) return Reject("회피와 공격 연습을 먼저 마치세요.");
            if (state.phase == SessionPhase.Lobby || state.phase == SessionPhase.ExchangeConfirmation) return false;
            state.phase = SessionPhase.Lobby;
            state.exchange = null;
            paused = false;
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            if (state.run != null) { state.run.attackHitAtMs = -1; state.run.enemyHitAtMs = -1; state.run.transition = null; }
            Publish();
            return true;
        }

        private void DefeatPlayer()
        {
            state.run.playerHpMilli = 0;
            state.run.attackHitAtMs = -1;
            state.run.enemyHitAtMs = -1;
            StartTransition(TransitionKind.Death, state.run.stage, CurrentCharacter.checkpoint + 1, catalog.transitions.deathMs);
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
            if (stage.kind == "boss")
            {
                CreateRelicOffer();
                PrepareStory();
            }
            StartTransition(TransitionKind.EnemyDefeat, run.stage, run.stage + 1, catalog.transitions.enemyDefeatMs);
            Emit(SessionEventKind.EnemyDefeated);
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
            run.offer = new RelicOfferData
            {
                rewardId = run.runId + ":" + run.cycle + ":" + run.stage,
                stage = run.stage,
                relicIds = ids,
                offeredLevels = Array.ConvertAll(ids, FindRelicLevel),
                maxGold = catalog.GetMaxRelicGold(run.stage)
            };
        }

        private bool CommitRelicChoice(string id)
        {
            if (paused || state.phase != SessionPhase.RewardChoice || state.run?.offer == null) return false;
            var run = state.run;
            var offer = run.offer;
            int index = Array.IndexOf(offer.relicIds, id);
            if (index < 0 || run.lastAppliedRewardId == offer.rewardId) return false;
            int level = FindRelicLevel(id);
            if (level != offer.offeredLevels[index]) return Reject("보상 상태가 달라졌습니다. 저장 상태를 확인하세요.");
            if (level == 0) state.relics.Add(new OwnedRelicData { id = id, level = 1 });
            else if (level < catalog.relicRules.maxLevel) state.relics.Find(x => x.id == id).level++;
            else { state.gold = checked(state.gold + offer.maxGold); run.earnedGold=checked(run.earnedGold+offer.maxGold); state.walletVersion++; }
            long oldMax = run.combatStats.maxHpMilli;
            run.appliedRelicLevels = Array.ConvertAll(run.equippedRelicIds, FindRelicLevel);
            run.combatStats = CalculateStats(run.characterId, run.statLevels, run.equippedRelicIds, run.appliedRelicLevels);
            run.playerHpMilli = Math.Min(run.combatStats.maxHpMilli, run.playerHpMilli + Math.Max(0, run.combatStats.maxHpMilli - oldMax));
            if (run.stage % 10 == 0)
            {
                CurrentCharacter.checkpoint = run.stage;
                run.playerHpMilli = run.combatStats.maxHpMilli;
            }
            run.lastAppliedRewardId = offer.rewardId;
            run.offer = null;
            BeginTravel(run.stage, run.stage + 1);
            Emit(SessionEventKind.RelicChosen, relicId: id, eventStage: offer.stage);
            Publish();
            return true;
        }
    }
}
