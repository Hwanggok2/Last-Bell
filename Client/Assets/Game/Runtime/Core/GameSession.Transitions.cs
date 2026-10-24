using System;

namespace LastBell
{
    public sealed partial class GameSession
    {
        private TransitionView GetTransition()
        {
            var t = state.phase == SessionPhase.Transition ? state.run?.transition : null;
            return t == null ? null : new TransitionView
            {
                Kind = t.kind, FromStage = t.fromStage, ToStage = t.toStage,
                ElapsedSeconds = (float)(t.elapsedMs / 1000), DurationSeconds = t.durationMs / 1000f,
                IsRepeat = t.isRepeat
            };
        }

        private StoryView GetStory()
        {
            var pending = state.phase == SessionPhase.StoryDialogue ? state.run?.story : null;
            if (pending == null) return null;
            var scene = catalog.FindStory(pending.sceneId,pending.legacy);
            if(pending.reunion)
            {
                var companion=catalog.FindCompanion(scene.companionId);
                return new StoryView{Text=companion.returnLine,Speaker=companion.displayName,PortraitId=companion.id,LineIndex=0,LineCount=1,SceneId=scene.id,CompanionId=companion.id};
            }
            var line = scene.lines[pending.lineIndex];
            var response = Array.Find(line.responses, x => x.characterId == state.run.characterId);
            return new StoryView
            {
                Text = response == null ? line.text : response.text,
                Speaker = line.speaker == "player" || line.speaker == "선택한 종지기" ? catalog.GetCharacter(state.run.characterId).displayName : line.speaker,
                PortraitId = line.portraitId == "player" ? state.run.characterId : line.portraitId,
                CompanionId=scene.companionId,AwaitingCompanionChoice=pending.awaitingCompanionChoice,
                CanInviteCompanion=pending.awaitingCompanionChoice&&state.run.companions.Count<CompanionSlots,
                LineIndex = pending.lineIndex, LineCount = scene.lines.Length, SceneId = scene.id
            };
        }

        private void StartTransition(TransitionKind kind, int fromStage, int toStage, int durationMs, bool repeat = false)
        {
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            CancelCompanionWindups();
            state.phase = SessionPhase.Transition;
            state.run.comboPractice=ComboPracticeMode.None;
            state.run.attackHitAtMs = -1;
            state.run.enemyHitAtMs = -1;
            state.run.transition = new TransitionData
            {
                kind = kind, fromStage = fromStage, toStage = toStage,
                durationMs = durationMs, isRepeat = repeat
            };
        }

        private void BeginTravel(int fromStage, int toStage)
        {
            // The new enemy is prepared once, at the start of travel. No cue or input
            // is active until CombatReady. Visuals can retain the defeated actor separately.
            PrepareStage(toStage);
            if (state.phase != SessionPhase.MvpComplete && state.phase != SessionPhase.RunEnd)
            {
                // Crossing the gate covers 1020px instead of 1920px. Keep running speed unchanged.
                int duration=fromStage==10&&toStage==11?(int)Math.Round(catalog.transitions.travelMs*1020.0/1920.0):catalog.transitions.travelMs;
                StartTransition(TransitionKind.Travel, fromStage, toStage, duration);
                Emit(SessionEventKind.StagePrepared);
            }
        }

        private void CombatReady()
        {
            var run = state.run;
            run.transition = null;
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            run.attackHitAtMs = -1;
            run.attackReadyAtMs = run.clockMs;
            run.dodgeStartMs = -1000000;
            PrepareCompanionCombat();
            if(run.stage==5&&!state.comboPracticeCompleted&&!state.comboPracticeSkipped)
            {
                state.phase=SessionPhase.ComboHint;run.enemyHitAtMs=-1;
                return;
            }
            state.phase = SessionPhase.Combat;
            ScheduleCurrentCue();
            Emit(SessionEventKind.StageStarted);
        }

        private string BossIntroKey() { return state.run.cycle + ":" + catalog.GetStage(state.run.stage).enemyId; }

        private void AdvanceTransition(float seconds)
        {
            double remaining = (double)seconds * 1000;
            bool checkpoint = false;
            // Carry time between visual phases only; never spend leftover time in a
            // newly opened cue, dialogue, reward selection or lobby.
            while (state.phase == SessionPhase.Transition)
            {
                var t = state.run.transition;
                double step = Math.Min(remaining, Math.Max(0, t.durationMs - t.elapsedMs));
                t.elapsedMs += step;
                remaining -= step;
                if (t.elapsedMs + TimeEpsilonMs < t.durationMs) break;
                checkpoint = true;
                state.run.transition = null;
                switch (t.kind)
                {
                    case TransitionKind.EnemyDefeat:
                        if (state.run.story != null) state.phase = SessionPhase.StoryDialogue;
                        else BeginTravel(t.fromStage, t.toStage);
                        break;
                    case TransitionKind.Travel:
                        StartTransition(TransitionKind.Arrival, t.fromStage, t.toStage, catalog.transitions.arrivalMs);
                        break;
                    case TransitionKind.Arrival:
                        string kind = catalog.GetStage(state.run.stage).kind;
                        if (kind == "normal") CombatReady();
                        else
                        {
                            bool repeat = CurrentCharacter.seenBossIntros.Contains(BossIntroKey());
                            int duration = kind == "boss"
                                ? (repeat ? catalog.transitions.repeatBossIntroMs : catalog.transitions.bossIntroMs)
                                : (repeat ? catalog.transitions.repeatMinibossIntroMs : catalog.transitions.minibossIntroMs);
                            StartTransition(TransitionKind.BossIntro, t.fromStage, t.toStage, duration, repeat);
                        }
                        break;
                    case TransitionKind.BossIntro:
                        string key = BossIntroKey();
                        if (!CurrentCharacter.seenBossIntros.Contains(key)) CurrentCharacter.seenBossIntros.Add(key);
                        CombatReady();
                        break;
                    case TransitionKind.Death:
                        BeginSettlement(false);
                        paused = false;
                        break;
                }
                // Saves at each boundary include all prior reward/story mutations.
                Publish();
                if (remaining <= TimeEpsilonMs) break;
            }
            if (!checkpoint || state.phase == SessionPhase.Transition) Publish(false);
        }

        private void PrepareStory()
        {
            var scene = catalog.FindStory(state.run.stage);
            if (scene == null) return;
            if (!state.unlockedStoryIds.Contains(scene.id)) state.unlockedStoryIds.Add(scene.id);
            if (!CurrentCharacter.seenStoryIds.Contains(scene.id))
                state.run.story = new StoryStateData { sceneId = scene.id, rewardId = state.run.runId+":"+state.run.cycle+":"+state.run.stage, reunion=!string.IsNullOrEmpty(scene.companionId)&&CompanionUnlocked(scene.companionId) };
        }

        private bool AdvanceStoryLine(bool skip)
        {
            if (paused || state.phase != SessionPhase.StoryDialogue || state.run.story == null) return false;
            var story = state.run.story;
            var scene = catalog.FindStory(story.sceneId,story.legacy);
            if(story.awaitingCompanionChoice)return ChooseCompanionJoin(false);
            if (!skip && !story.reunion && story.lineIndex + 1 < scene.lines.Length) story.lineIndex++;
            else
            {
                bool first=UnlockCompanion(scene.companionId);
                bool vacant=state.run.companions.Count<CompanionSlots;
                if(first&&vacant&&scene.companionId=="tori"&&state.adventureNewAccount&&CurrentCharacter.companionIds.Count==0)JoinCurrentCompanion("tori");
                else if(first){story.lineIndex=scene.lines.Length-1;story.awaitingCompanionChoice=true;Publish();return true;}
                FinishCompanionStory(scene);
            }
            Publish();
            return true;
        }
        void JoinCurrentCompanion(string id)
        {
            if(state.run.companions.Exists(x=>x.id==id)||state.run.companions.Count>=CompanionSlots)return;
            CurrentCharacter.companionIds.Add(id);SnapshotCompanion(id);Emit(SessionEventKind.CompanionJoined,companionId:id);
        }
        public bool ChooseCompanionJoin(bool join)
        {
            var story=state.run?.story;
            if(paused||Phase!=SessionPhase.StoryDialogue||story==null||!story.awaitingCompanionChoice)return false;
            var scene=catalog.FindStory(story.sceneId,story.legacy);
            if(join){if(state.run.companions.Count>=CompanionSlots)return false;JoinCurrentCompanion(scene.companionId);}
            FinishCompanionStory(scene);Publish();return true;
        }
        void FinishCompanionStory(StoryDefinition scene)
        {
            if(!CurrentCharacter.seenStoryIds.Contains(scene.id))CurrentCharacter.seenStoryIds.Add(scene.id);
            state.run.story=null;
            if(state.run.stage==10)StartTransition(TransitionKind.EnemyDefeat,10,11,catalog.transitions.enemyDefeatMs);
            else BeginTravel(state.run.stage,state.run.stage+1);
        }
    }
}
