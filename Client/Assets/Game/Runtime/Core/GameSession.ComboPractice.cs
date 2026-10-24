using System;
namespace LastBell
{
    public sealed partial class GameSession
    {
        GameSaveData practiceBackupState;
        Action restorePractice;
        bool GuidedPractice=>state.run!=null&&(state.run.comboPractice==ComboPracticeMode.Guided||state.run.comboPractice==ComboPracticeMode.AwaitingInput||state.run.comboPractice==ComboPracticeMode.Recovery);
        public ComboPracticeView ComboPractice
        {
            get
            {
                var mode=state.run?.comboPractice??ComboPracticeMode.None;int step=mode==ComboPracticeMode.Recovery?1:Math.Min(1,threatIndex);
                return new ComboPracticeView{Active=Phase==SessionPhase.ComboHint||GuidedPractice,IsReplay=practiceBackupState!=null,AwaitingInput=mode==ComboPracticeMode.AwaitingInput,Step=step,RequiredDirection=step==0?DodgeDirection.Up:DodgeDirection.Down,IsRealTrial=mode==ComboPracticeMode.RealTrial,
                    Message=Phase==SessionPhase.ComboHint?"두 번 온다!":mode==ComboPracticeMode.Recovery?"잘했어! 이번엔 직접 피해 봐!":mode==ComboPracticeMode.AwaitingInput?(step==0?"위로 피해!":"한 번 더! 아래로 쳐내!"):"두 번 온다!"};
            }
        }
        void StartGuidedPractice()
        {
            var run=state.run;ClearPendingEnemyImpact();ClearCombatQueue();CancelCompanionWindups();
            run.attackHitAtMs=-1;run.pendingSkillAttack=false;run.dodgeStartMs=-1000000;run.stunUntilMs=-1;run.enemyHitAtMs=-1;
            run.comboPractice=ComboPracticeMode.Guided;state.phase=SessionPhase.Combat;
            SchedulePracticeSequence();Emit(SessionEventKind.ComboPracticeChanged);
        }
        void SchedulePracticeSequence()
        {
            state.run.randomSequence=new[]{"low_sweep","high_sweep"};state.run.sequenceCursor=0;
            ScheduleSequence(ClockMs,true);
        }
        bool HoldGuidedImpact()
        {
            if(state.run.comboPractice!=ComboPracticeMode.Guided)return false;
            state.run.comboPractice=ComboPracticeMode.AwaitingInput;ClearPendingEnemyImpact();Emit(SessionEventKind.ComboPracticeChanged);return true;
        }
        bool PracticeDodge(DodgeDirection direction)
        {
            if(paused||state.run.comboPractice!=ComboPracticeMode.AwaitingInput||direction!=(threatIndex==0?DodgeDirection.Up:DodgeDirection.Down))return false;
            var run=state.run;var threat=threats[threatIndex];run.dodgeStartMs=ClockMs;run.dodgeDirection=direction;run.attackHitAtMs=-1;
            ClearCounter();attackPoseAtMs=-10000;dodgePoseCancelled=direction==DodgeDirection.Down;
            dodgeEnterMs=0;dodgeHoldUntilMs=ClockMs+100;
            if(direction==DodgeDirection.Down){parryAtMs=ClockMs;parryThreat=threat;}
            Emit(direction==DodgeDirection.Down?SessionEventKind.ParryStarted:SessionEventKind.DodgeStarted,direction:direction,patternId:threat.PatternId);
            threat.Resolved=true;run.sequenceCursor=threat.Step+1;run.enemyHitAtMs=-1;
            Emit(direction==DodgeDirection.Down?SessionEventKind.Parried:SessionEventKind.Dodged,direction:direction,patternId:threat.PatternId);
            if(threatIndex==0){threatIndex=1;SetCurrentThreat();run.comboPractice=ComboPracticeMode.Guided;}
            else
            {
                run.comboPractice=ComboPracticeMode.Recovery;run.practiceRecoveryUntilMs=ClockMs+catalog.combat.dodgeActiveMs+catalog.combat.dodgeRecoveryMs;
                if(practiceBackupState==null)state.comboPracticeCompleted=true;
                Emit(SessionEventKind.ComboPracticeCompleted);
            }
            Emit(SessionEventKind.ComboPracticeChanged);Publish();return true;
        }
        void FinishPracticeRecovery()
        {
            if(practiceBackupState!=null){EndComboPractice();return;}
            state.run.comboPractice=ComboPracticeMode.RealTrial;ClearCombatQueue();SchedulePracticeSequence();Emit(SessionEventKind.ComboPracticeChanged);
        }
        public bool SkipComboPractice()
        {
            if(practiceBackupState!=null)return EndComboPractice();
            if(Phase!=SessionPhase.ComboHint&&!GuidedPractice)return false;
            state.comboPracticeSkipped=true;state.comboHintSeen=true;state.run.comboPractice=ComboPracticeMode.None;
            state.run.randomSequence=new string[0];state.run.sequenceCursor=0;state.run.forceTwoHitSequence=false;
            CombatReady();Publish();return true;
        }
        public bool BeginComboPractice()
        {
            if(practiceBackupState!=null||!(Phase==SessionPhase.Lobby||Phase==SessionPhase.Combat&&paused))return false;
            var oldState=state;bool oldPaused=paused,oldDirty=IsDirty;string oldMessage=LastMessage;
            var oldThreats=threats;int oldIndex=threatIndex;var oldCounter=counterRun;double oldCounterUntil=perfectCounterUntilMs;
            bool oldPose=dodgePoseCancelled;double oldAttack=attackPoseAtMs,oldParry=parryAtMs,oldEnter=dodgeEnterMs,oldHold=dodgeHoldUntilMs;var oldParryThreat=parryThreat;
            var oldPending=pendingImpactRun;double oldImpact=pendingImpactAtMs,oldCarry=carriedFrameMs;bool oldDisplay=pendingImpactNeedsDisplay;
            restorePractice=()=>{state=oldState;paused=oldPaused;IsDirty=oldDirty;LastMessage=oldMessage;threats=oldThreats;threatIndex=oldIndex;counterRun=oldCounter;perfectCounterUntilMs=oldCounterUntil;dodgePoseCancelled=oldPose;attackPoseAtMs=oldAttack;parryAtMs=oldParry;dodgeEnterMs=oldEnter;dodgeHoldUntilMs=oldHold;parryThreat=oldParryThreat;pendingImpactRun=oldPending;pendingImpactAtMs=oldImpact;carriedFrameMs=oldCarry;pendingImpactNeedsDisplay=oldDisplay;};
            var stats=PlayerStats;practiceBackupState=state;state=CloneSave(state);paused=false;
            state.run=new RunStateData{characterId=CurrentCharacterId,stage=5,runId=state.runSequence,skillRulesVersion=1,statLevels=(int[])CurrentCharacter.statLevels.Clone(),combatStats=stats,playerHpMilli=oldState.run!=null&&!oldState.run.ended?oldState.run.playerHpMilli:stats.maxHpMilli,enemyHpMilli=1000000,enemyMaxHpMilli=1000000};
            state.phase=SessionPhase.ComboHint;Changed?.Invoke();return true;
        }
        public bool EndComboPractice()
        {
            if(practiceBackupState==null)return false;
            var restore=restorePractice;restore();restorePractice=null;practiceBackupState=null;Emit(SessionEventKind.ComboPracticeChanged);Changed?.Invoke();return true;
        }
    }
}
