using System;

namespace LastBell
{
    public sealed partial class GameSession
    {
        private PatternStep CurrentStep => catalog.GetStage(state.run.stage).patternSteps[state.run.patternStep];

        private void ScheduleCurrentCue()
        {
            var run = state.run;
            var step = CurrentStep;
            run.patternId = step.danger;
            run.telegraphStartMs = run.clockMs;
            run.enemyHitAtMs = run.clockMs + step.telegraphMs;
        }

        private void ScheduleNextHit()
        {
            var run = state.run;
            var steps = catalog.GetStage(run.stage).patternSteps;
            run.patternStep = (run.patternStep + 1) % steps.Length;
            if (run.patternStep == 0) run.patternIndex++;
            var step = CurrentStep;
            run.patternId = step.danger;
            run.enemyHitAtMs = run.clockMs + step.intervalMs;
            run.telegraphStartMs = run.enemyHitAtMs - step.telegraphMs;
        }

        private EnemyIntentView GetEnemyIntent()
        {
            var view = new EnemyIntentView();
            if (state.run == null || (state.phase != SessionPhase.Combat && state.phase != SessionPhase.TutorialDodge))
                return view;
            var run = state.run;
            if (state.phase == SessionPhase.TutorialDodge)
                return new EnemyIntentView { Visible = true, Direction = DodgeDirection.Up, Progress = 1, PatternId = "low_sweep" };
            var steps = catalog.GetStage(run.stage).patternSteps;
            var step = CurrentStep;
            var next = steps[(run.patternStep + 1) % steps.Length];
            view.Visible = !IsEnemyStunned && run.enemyHitAtMs >= 0 && run.clockMs + TimeEpsilonMs >= run.telegraphStartMs;
            view.Direction = ParseDirection(step.requiredDodge);
            view.PatternId = step.danger;
            view.SecondsToHit = run.enemyHitAtMs < 0 ? 0 : (float)Math.Max(0, (run.enemyHitAtMs - run.clockMs) / 1000);
            view.Progress = (float)Math.Max(0, Math.Min(1, (run.clockMs - run.telegraphStartMs) / step.telegraphMs));
            view.HasNext = view.Visible && step.comboStep > 0 && next.comboStep == step.comboStep + 1;
            view.NextDirection = ParseDirection(next.requiredDodge);
            return view;
        }

        private bool BeginDodge(DodgeDirection direction)
        {
            if (paused || !Enum.IsDefined(typeof(DodgeDirection), direction)) return false;
            var run = state.run;
            if (state.phase == SessionPhase.TutorialDodge)
            {
                if (direction != DodgeDirection.Up) return Reject("▲를 눌러 피해!");
                run.dodgeStartMs = run.clockMs;
                run.dodgeDirection = direction;
                run.stagger = catalog.combat.normalStagger;
                state.tutorialStep = 1;
                state.phase = SessionPhase.TutorialAttack;
                LastMessage = "검을 눌러 공격해!";
                Emit(SessionEventKind.DodgeStarted, direction: direction);
                Emit(SessionEventKind.Dodged, direction: direction);
                Publish();
                return true;
            }
            if (state.phase != SessionPhase.Combat ||
                run.clockMs - run.dodgeStartMs + TimeEpsilonMs < catalog.combat.dodgeActiveMs + catalog.combat.dodgeRecoveryMs)
                return false;
            run.dodgeStartMs = run.clockMs;
            run.dodgeDirection = direction;
            run.attackHitAtMs = -1; // The already-spent attack interval is intentionally not refunded.
            Emit(SessionEventKind.DodgeStarted, direction: direction);
            Publish();
            return true;
        }

        private bool BeginAttack()
        {
            if (paused || (state.phase != SessionPhase.Combat && state.phase != SessionPhase.TutorialAttack)) return false;
            var run = state.run;
            if (run.attackHitAtMs >= 0 || run.clockMs + TimeEpsilonMs < run.attackReadyAtMs ||
                (state.phase == SessionPhase.Combat && run.clockMs - run.dodgeStartMs + TimeEpsilonMs < catalog.combat.dodgeActiveMs))
                return false;
            run.attackReadyAtMs = run.clockMs + catalog.combat.attackIntervalMs;
            run.attackHitAtMs = run.clockMs + catalog.combat.attackWindupMs;
            Emit(SessionEventKind.AttackStarted);
            Publish();
            return true;
        }

        private void AdvanceTime(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (paused || (state.phase != SessionPhase.Combat && state.phase != SessionPhase.TutorialAttack)) return;
            var run = state.run;
            double target = run.clockMs + (double)deltaSeconds * 1000;
            bool importantChange = false;
            // Process deadlines chronologically, not once per rendered frame.
            while (state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialAttack)
            {
                double next = double.PositiveInfinity;
                if (run.attackHitAtMs >= 0) next = Math.Min(next, run.attackHitAtMs);
                if (state.phase == SessionPhase.Combat)
                {
                    if (run.enemyHitAtMs >= 0) next = Math.Min(next, run.enemyHitAtMs);
                    if (run.stunUntilMs >= 0) next = Math.Min(next, run.stunUntilMs);
                }
                if (next > target + TimeEpsilonMs) { run.clockMs = Math.Max(run.clockMs, target); break; }
                run.clockMs = Math.Max(run.clockMs, next);
                importantChange = true;
                if (run.stunUntilMs >= 0 && run.stunUntilMs <= run.clockMs + TimeEpsilonMs)
                {
                    run.stunUntilMs = -1;
                    run.stagger = 0;
                    run.patternStep = 0;
                    ScheduleCurrentCue();
                }
                // At a simultaneous deadline enemy damage/death takes priority over the player's pending strike.
                if (state.phase == SessionPhase.Combat && run.enemyHitAtMs >= 0 && run.enemyHitAtMs <= run.clockMs + TimeEpsilonMs)
                    ResolveEnemyHit();
                if ((state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialAttack) &&
                    run.attackHitAtMs >= 0 && run.attackHitAtMs <= run.clockMs + TimeEpsilonMs)
                    ResolvePlayerHit();
            }
            Publish(importantChange);
        }

        private void ResolveEnemyHit()
        {
            var run = state.run;
            DodgeDirection required = ParseDirection(CurrentStep.requiredDodge);
            run.enemyHitAtMs = -1;
            double elapsed = run.clockMs - run.dodgeStartMs;
            bool dodged = run.dodgeDirection == required && elapsed >= -TimeEpsilonMs &&
                elapsed <= catalog.combat.dodgeActiveMs + TimeEpsilonMs;
            if (dodged)
            {
                bool perfect = elapsed <= catalog.combat.perfectWindowMs + TimeEpsilonMs;
                if (run.clockMs + TimeEpsilonMs >= run.immunityUntilMs)
                {
                    run.stagger = Math.Min(catalog.combat.staggerThreshold,
                        run.stagger + (perfect ? run.stats.perfectStagger : run.stats.normalStagger));
                    if (run.stagger + TimeEpsilonMs >= catalog.combat.staggerThreshold)
                    {
                        run.stagger = 0;
                        run.stunUntilMs = run.clockMs + catalog.GetStunDurationMs(catalog.GetStage(run.stage).kind);
                        run.immunityUntilMs = run.stunUntilMs + catalog.combat.postStunImmunityMs;
                    }
                }
                Emit(perfect ? SessionEventKind.PerfectDodge : SessionEventKind.Dodged, direction: required);
                if (IsEnemyStunned) Emit(SessionEventKind.EnemyStunned);
            }
            else
            {
                long damage = Milli(Math.Max(1, catalog.GetStage(run.stage).damage * (1 - run.stats.damageReduction)));
                damage = Math.Min(damage, run.playerHpMilli);
                run.playerHpMilli -= damage;
                run.stagger = Math.Max(0, run.stagger - catalog.combat.damageStaggerPenalty);
                Emit(SessionEventKind.PlayerDamaged, damage);
                if (run.playerHpMilli <= 0) { DefeatPlayer(); return; }
            }
            if (!IsEnemyStunned) ScheduleNextHit();
        }

        private void ResolvePlayerHit()
        {
            var run = state.run;
            run.attackHitAtMs = -1;
            bool tutorial = state.phase == SessionPhase.TutorialAttack;
            bool critical = !tutorial && NextRandom() / 4294967296.0 < run.stats.critChance;
            double damageHp = run.stats.attack * (IsEnemyStunned ? run.stats.stunDamageMultiplier : 1) *
                (critical ? Number(catalog.combat.critDamageMultiplier) : 1);
            long damage = Math.Min(run.enemyHpMilli, Milli(Math.Max(1, damageHp)));
            run.enemyHpMilli -= damage;
            Emit(SessionEventKind.EnemyDamaged, damage, critical);
            if (tutorial)
            {
                state.tutorialStep = 2;
                state.phase = SessionPhase.Combat;
                LastMessage = "";
                ScheduleCurrentCue();
            }
            if (run.enemyHpMilli <= 0) DefeatEnemy();
        }
    }
}
