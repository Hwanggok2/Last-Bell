using System;

namespace LastBell
{
    public sealed partial class GameSession
    {
        // Presentation-frame handoff only. Never serialized: restore starts a fresh cue.
        private RunStateData pendingImpactRun;
        private double pendingImpactAtMs = -1;
        private double carriedFrameMs;
        private bool pendingImpactNeedsDisplay;

        private bool IsPendingEnemyImpact(RunStateData run)
        {
            return state.phase == SessionPhase.Combat && run != null &&
                ReferenceEquals(run, pendingImpactRun) && run.enemyHitAtMs >= 0 &&
                Math.Abs(run.enemyHitAtMs - pendingImpactAtMs) <= TimeEpsilonMs;
        }

        private void ClearPendingEnemyImpact()
        {
            pendingImpactRun = null;
            pendingImpactAtMs = -1;
            carriedFrameMs = 0;
            pendingImpactNeedsDisplay = false;
        }

        private PatternStep CurrentStep => catalog.GetStage(state.run.stage).patternSteps[state.run.patternStep];

        // Transient queue: restoring an interrupted combo restarts its complete warning.
        private EnemyThreatView[] threats = new EnemyThreatView[0];
        private int threatIndex;
        private RunStateData counterRun;
        private double perfectCounterUntilMs = -1;
        private bool dodgePoseCancelled;
        private double attackPoseAtMs=-10000;
        private EnemyThreatView parryThreat;
        private double parryAtMs=-10000,dodgeEnterMs=50,dodgeHoldUntilMs=-10000;
        public float DodgeVisualAmount=>state.run==null||dodgePoseCancelled?0:CombatGeometry.DodgeAmount(ClockMs-state.run.dodgeStartMs,dodgeEnterMs,dodgeHoldUntilMs-state.run.dodgeStartMs);
        public bool IsParryWindow=>state.phase==SessionPhase.Combat&&threatIndex<threats.Length&&threats[threatIndex].RequiresParry&&
            threats[threatIndex].ImpactAtMs-ClockMs>=-TimeEpsilonMs&&threats[threatIndex].ImpactAtMs-ClockMs<=catalog.combat.dodgeActiveMs+TimeEpsilonMs;
        public bool CanParry=>IsParryWindow&&DodgeRecoverySeconds<=.000001f;
        public CombatRect PlayerCombatBody
        {
            get
            {
                var run=state.run;if(run==null)return CombatGeometry.Body(DodgeDirection.Up,false);
                float amount=DodgeVisualAmount;
                return amount>0?CombatGeometry.Body(run.dodgeDirection,true,amount):CombatGeometry.Body(run.dodgeDirection,false).Offset(
                    CombatGeometry.AttackX(catalog.GetStage(Stage).kind,ClockMs-attackPoseAtMs)-CombatGeometry.HomeX,0);
            }
        }
        public bool CanPerfectCounter => state.phase == SessionPhase.Combat && ReferenceEquals(counterRun,state.run) &&
            perfectCounterUntilMs >= ClockMs - TimeEpsilonMs;
        public EnemyThreatView[] EnemyThreats
        {
            get
            {
                if(state.phase==SessionPhase.TutorialDodge)
                    return new[]{new EnemyThreatView{PatternId="low_sweep",Direction=DodgeDirection.Up,CueAtMs=0,CueEndMs=catalog.combat.singleTelegraphMs,ReleaseAtMs=catalog.combat.singleTelegraphMs-catalog.combat.attackFlightMs,ImpactAtMs=catalog.combat.singleTelegraphMs}};
                if(state.phase!=SessionPhase.Combat||IsEnemyStunned)return new EnemyThreatView[0];
                return Array.ConvertAll(Array.FindAll(threats,t=>!t.Resolved),t=>t.Copy());
            }
        }
        private void ClearCounter(){perfectCounterUntilMs=-1;counterRun=null;}
        private void ClearCombatQueue(){threats=new EnemyThreatView[0];threatIndex=0;attackPoseAtMs=parryAtMs=dodgeHoldUntilMs=-10000;parryThreat=null;dodgePoseCancelled=false;ClearCounter();}

        private void ScheduleCurrentCue(){ScheduleSequence(state.run.clockMs,true);}
        private void ScheduleSequence(double start,bool rewind)
        {
            var run=state.run;var steps=catalog.GetStage(run.stage).patternSteps;
            if(rewind)while(run.patternStep>0&&steps[run.patternStep].comboStep>1)run.patternStep--;
            int count=1;
            if(steps[run.patternStep].comboStep==1)
                while(run.patternStep+count<steps.Length&&steps[run.patternStep+count].comboStep==count+1)count++;
            threats=new EnemyThreatView[count];threatIndex=0;
            double cue=start;
            for(int i=0;i<count;i++)
            {
                var step=steps[run.patternStep+i];
                threats[i]=new EnemyThreatView{PatternId=step.danger,Direction=ParseDirection(step.requiredDodge),Step=run.patternStep+i,SequenceIndex=i,SequenceCount=count,
                    CueAtMs=cue,CueEndMs=cue+step.telegraphMs};
                cue=threats[i].CueEndMs;
            }
            bool fast=steps[run.patternStep].telegraphMs==catalog.combat.fastTelegraphMs;
            int gap=fast?catalog.combat.comboMinGapMs:catalog.combat.comboDefaultGapMs;
            int flight=fast?catalog.combat.fastAttackFlightMs:catalog.combat.attackFlightMs;
            for(int i=0;i<count;i++)
            {
                threats[i].ImpactAtMs=cue+i*gap;
                threats[i].ReleaseAtMs=threats[i].ImpactAtMs-flight;
            }
            SetCurrentThreat();
        }
        private void SetCurrentThreat()
        {
            var t=threats[threatIndex];var run=state.run;
            run.patternStep=t.Step;run.patternId=t.PatternId;run.telegraphStartMs=t.CueAtMs;run.enemyHitAtMs=t.ImpactAtMs;
        }
        private void ScheduleNextHit()
        {
            if(++threatIndex<threats.Length){SetCurrentThreat();return;}
            var run=state.run;var steps=catalog.GetStage(run.stage).patternSteps;
            run.patternStep=(run.patternStep+1)%steps.Length;
            if(run.patternStep==0)run.patternIndex++;
            // intervalMs is the minimum spacing from the preceding hit to the next sequence's first hit.
            ScheduleSequence(run.clockMs,false);
            double idle=Math.Max(0,CurrentStep.intervalMs-(run.enemyHitAtMs-run.clockMs));
            if(idle>0){foreach(var t in threats){t.CueAtMs+=idle;t.CueEndMs+=idle;t.ReleaseAtMs+=idle;t.ImpactAtMs+=idle;}SetCurrentThreat();}
        }
        private EnemyIntentView GetEnemyIntent()
        {
            var view=new EnemyIntentView();var ts=EnemyThreats;if(ts.Length==0)return view;
            var first=ts[0];double clock=ClockMs;
            view.Visible=clock+TimeEpsilonMs>=first.CueAtMs;
            view.Direction=first.Direction;view.PatternId=first.PatternId;
            view.SecondsToHit=(float)Math.Max(0,(first.ImpactAtMs-clock)/1000);
            view.TelegraphMs=(float)(first.ImpactAtMs-first.CueAtMs);
            view.Progress=(float)Math.Max(0,Math.Min(1,(clock-first.CueAtMs)/view.TelegraphMs));
            view.HasNext=view.Visible&&ts.Length>1;view.NextDirection=ts.Length>1?ts[1].Direction:first.Direction;
            return view;
        }
        private double NextCueEvent()
        {
            if(state.phase!=SessionPhase.Combat||IsEnemyStunned)return double.PositiveInfinity;
            double next=double.PositiveInfinity;
            foreach(var t in threats)if(!t.Resolved){if(!t.CueEmitted)next=Math.Min(next,t.CueAtMs);if(!t.ReleaseEmitted)next=Math.Min(next,t.ReleaseAtMs);}
            return next;
        }
        private void EmitCueEvents()
        {
            if(state.phase!=SessionPhase.Combat||IsEnemyStunned)return;
            foreach(var t in threats)if(!t.Resolved)
            {
                if(!t.CueEmitted&&ClockMs+TimeEpsilonMs>=t.CueAtMs){t.CueEmitted=true;Emit(SessionEventKind.EnemyCueStarted,direction:t.Direction,patternId:t.PatternId);}
                if(!t.ReleaseEmitted&&ClockMs+TimeEpsilonMs>=t.ReleaseAtMs){t.ReleaseEmitted=true;Emit(SessionEventKind.EnemyReleased,direction:t.Direction,patternId:t.PatternId);}
            }
        }

        private bool BeginDodge(DodgeDirection direction)
        {
            if (paused || direction==DodgeDirection.Down || !Enum.IsDefined(typeof(DodgeDirection), direction)) return false;
            var run = state.run;
            if (state.phase == SessionPhase.TutorialDodge)
            {
                if (direction != DodgeDirection.Up) return Reject("▲를 눌러 피해!");
                run.dodgeStartMs = run.clockMs;
                run.dodgeDirection = direction;
                dodgeEnterMs=50;dodgeHoldUntilMs=run.clockMs+150;
                run.stagger = catalog.combat.normalStagger;
                state.tutorialStep = 1;
                state.phase = SessionPhase.TutorialAttack;
                LastMessage = "검을 눌러 공격해!";
                Emit(SessionEventKind.DodgeStarted, direction: direction);
                Emit(SessionEventKind.Dodged, direction: direction, staggerGained: catalog.combat.normalStagger);
                Publish();
                return true;
            }
            if (state.phase != SessionPhase.Combat ||
                run.clockMs - run.dodgeStartMs + TimeEpsilonMs < catalog.combat.dodgeActiveMs + catalog.combat.dodgeRecoveryMs)
                return false;
            ClearCounter();parryThreat=null;dodgePoseCancelled=false;attackPoseAtMs=-10000;
            double lead=run.enemyHitAtMs-run.clockMs;
            dodgeEnterMs=Math.Min(50,Math.Max(0,lead));
            // A timed defense holds through contact; an early attempt finishes within
            // its existing 160ms active duration, without extending the timing window.
            dodgeHoldUntilMs=lead>=0&&lead<=catalog.combat.dodgeActiveMs+TimeEpsilonMs?run.enemyHitAtMs+100:run.clockMs+30;
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
            // The attack command becomes a defense only inside a high attack's window.
            // It spends defense reuse, never refunds or consumes the ordinary attack interval.
            if(IsParryWindow)
            {
                if(!CanParry)return false;
                ClearCounter();parryThreat=threats[threatIndex];parryAtMs=run.clockMs;
                run.dodgeStartMs=run.clockMs;dodgePoseCancelled=true;attackPoseAtMs=-10000;run.attackHitAtMs=-1;
                Emit(SessionEventKind.ParryStarted,direction:DodgeDirection.Down);Publish();return true;
            }
            bool counter=CanPerfectCounter;
            if (run.attackHitAtMs >= 0 || run.clockMs + TimeEpsilonMs < run.attackReadyAtMs ||
                (state.phase == SessionPhase.Combat && !counter && run.clockMs - run.dodgeStartMs + TimeEpsilonMs < catalog.combat.dodgeActiveMs))
                return false;
            run.attackReadyAtMs = run.clockMs + catalog.combat.attackIntervalMs;
            run.attackHitAtMs = run.clockMs + catalog.combat.attackWindupMs;
            attackPoseAtMs=run.clockMs;
            dodgePoseCancelled=true;parryThreat=null;
            ClearCounter();
            if(counter)dodgePoseCancelled=true;
            Emit(SessionEventKind.AttackStarted,perfectCounter:counter);
            Publish();
            return true;
        }

        private void AdvanceTime(float deltaSeconds, bool showCompletedCue = false)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (!paused && state.phase == SessionPhase.Transition) { AdvanceTransition(deltaSeconds); return; }
            if (paused || (state.phase != SessionPhase.Combat && state.phase != SessionPhase.TutorialAttack)) return;
            var run = state.run;
            bool pending = IsPendingEnemyImpact(run);
            if (!pending) ClearPendingEnemyImpact();
            double shownImpactAtMs = pending && !pendingImpactNeedsDisplay ? pendingImpactAtMs : -1;
            double target = run.clockMs + (double)deltaSeconds * 1000 + carriedFrameMs;
            ClearPendingEnemyImpact();
            bool importantChange = false;
            // Process deadlines chronologically, not once per rendered frame.
            while (state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialAttack)
            {
                double next = NextCueEvent();
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
                EmitCueEvents();
                // At a simultaneous deadline enemy damage/death takes priority over the player's pending strike.
                if (state.phase == SessionPhase.Combat && run.enemyHitAtMs >= 0 && run.enemyHitAtMs <= run.clockMs + TimeEpsilonMs)
                {
                    // Stop at T once so Progress=1 can be drawn. The next input batch
                    // is recorded at the same T before this deadline is resolved.
                    // Carry the remainder so later combo deadlines keep their spacing.
                    if (showCompletedCue && Math.Abs(run.enemyHitAtMs - shownImpactAtMs) > TimeEpsilonMs)
                    {
                        pendingImpactRun = run;
                        pendingImpactAtMs = run.enemyHitAtMs;
                        carriedFrameMs = Math.Max(0, target - run.clockMs);
                        break;
                    }
                    shownImpactAtMs = -1;
                    ResolveEnemyHit();
                }
                if ((state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialAttack) &&
                    run.attackHitAtMs >= 0 && run.attackHitAtMs <= run.clockMs + TimeEpsilonMs)
                    ResolvePlayerHit();
            }
            Publish(importantChange);
        }

        private void ResolveEnemyHit()
        {
            var run = state.run;
            var threat=threats[threatIndex];threat.Resolved=true;
            DodgeDirection required=threat.Direction;
            run.enemyHitAtMs = -1;
            double elapsed = run.clockMs - run.dodgeStartMs;
            bool active=!dodgePoseCancelled&&elapsed>=-TimeEpsilonMs&&elapsed<=catalog.combat.dodgeActiveMs+TimeEpsilonMs;
            bool parried=ReferenceEquals(parryThreat,threat)&&run.clockMs-parryAtMs<=catalog.combat.dodgeActiveMs+TimeEpsilonMs;
            bool dodged=parried||!threat.Bounds.Overlaps(PlayerCombatBody);
            if (dodged)
            {
                bool perfect = (active||parried) && elapsed <= catalog.combat.perfectWindowMs + TimeEpsilonMs;
                if(active&&!parried)dodgeHoldUntilMs=run.clockMs+100;
                if(perfect){counterRun=run;perfectCounterUntilMs=run.clockMs+catalog.combat.perfectCounterMs;}
                double before = run.stagger, gained = 0;
                if ((active||parried) && run.clockMs + TimeEpsilonMs >= run.immunityUntilMs)
                {
                    run.stagger = Math.Min(catalog.combat.staggerThreshold,
                        run.stagger + (perfect ? run.combatStats.perfectStagger : run.combatStats.normalStagger));
                    gained = run.stagger - before;
                    if (run.stagger + TimeEpsilonMs >= catalog.combat.staggerThreshold)
                    {
                        run.stagger = 0;
                        run.stunUntilMs = run.clockMs + catalog.GetStunDurationMs(catalog.GetStage(run.stage).kind);
                        run.immunityUntilMs = run.stunUntilMs + catalog.combat.postStunImmunityMs;
                    }
                }
                Emit(parried?(perfect?SessionEventKind.PerfectParry:SessionEventKind.Parried):(perfect?SessionEventKind.PerfectDodge:SessionEventKind.Dodged), direction: required, staggerBefore: before, staggerGained: gained);
                if (IsEnemyStunned){threats=new EnemyThreatView[0];Emit(SessionEventKind.EnemyStunned);}
            }
            else
            {
                ClearCounter();
                long damage = Milli(Math.Max(1, Rounded(catalog.GetStage(run.stage).damage * (1 - run.combatStats.damageReduction))));
                damage = Math.Min(damage, run.playerHpMilli);
                run.playerHpMilli -= damage;
                run.stagger = Math.Max(0, run.stagger - catalog.combat.damageStaggerPenalty);
                Emit(SessionEventKind.PlayerDamaged, damage, direction: required);
                if (run.playerHpMilli <= 0) { DefeatPlayer(); return; }
            }
            parryThreat=null;
            if (!IsEnemyStunned) ScheduleNextHit();
        }

        private void ResolvePlayerHit()
        {
            var run = state.run;
            run.attackHitAtMs = -1;
            bool tutorial = state.phase == SessionPhase.TutorialAttack;
            bool critical = !tutorial && NextRandom() / 4294967296.0 < run.combatStats.critChance;
            double damageHp = run.combatStats.attack * (IsEnemyStunned ? run.combatStats.stunDamageMultiplier : 1) *
                (critical ? Number(catalog.combat.critDamageMultiplier) : 1);
            long damage = Math.Min(run.enemyHpMilli, Milli(Math.Max(1, Rounded(damageHp))));
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
