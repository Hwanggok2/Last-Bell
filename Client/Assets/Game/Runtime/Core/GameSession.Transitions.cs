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
            var scene = catalog.FindStory(pending.sceneId);
            var line = scene.lines[pending.lineIndex];
            var response = Array.Find(line.responses, x => x.characterId == state.run.characterId);
            return new StoryView
            {
                Text = response == null ? line.text : response.text,
                Speaker = line.speaker == "선택한 종지기" ? catalog.GetCharacter(state.run.characterId).displayName : line.speaker,
                LineIndex = pending.lineIndex, LineCount = scene.lines.Length, SceneId = scene.id
            };
        }

        private void StartTransition(TransitionKind kind, int fromStage, int toStage, int durationMs, bool repeat = false)
        {
            ClearPendingEnemyImpact();
            ClearCombatQueue();
            state.phase = SessionPhase.Transition;
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
            if (state.phase != SessionPhase.MvpComplete)
            {
                StartTransition(TransitionKind.Travel, fromStage, toStage, catalog.transitions.travelMs);
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
                        else if (state.run.offer != null) state.phase = SessionPhase.RewardChoice;
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
                        state.phase = SessionPhase.DeathResult;
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
                state.run.story = new StoryStateData { sceneId = scene.id, rewardId = state.run.offer.rewardId };
        }

        private bool AdvanceStoryLine(bool skip)
        {
            if (paused || state.phase != SessionPhase.StoryDialogue || state.run.story == null) return false;
            var story = state.run.story;
            var scene = catalog.FindStory(story.sceneId);
            if (!skip && story.lineIndex + 1 < scene.lines.Length) story.lineIndex++;
            else
            {
                if (!CurrentCharacter.seenStoryIds.Contains(scene.id)) CurrentCharacter.seenStoryIds.Add(scene.id);
                state.run.story = null;
                state.phase = SessionPhase.RewardChoice;
            }
            Publish();
            return true;
        }
    }
}
