from pathlib import Path
import json, shutil
root=Path('C:/Github/Test2')
def edit(p,old,new):
 p=root/p;s=p.read_text(encoding='utf-8-sig');assert old in s,(p,old[:100]);p.write_text(s.replace(old,new),encoding='utf-8')
def write(p,s):
 p=root/p;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8')
a='''            long oldMax = run.combatStats.maxHpMilli;''';b='''            Emit(SessionEventKind.RelicChosen, relicId: id, eventStage: offer.stage);'''
p=root/'Client/Assets/Game/Runtime/Core/GameSession.Progression.cs';s=p.read_text(encoding="utf-8");i=s.index(a);j=s.index(b,i);s=s[:i]+'''            run.lastAppliedRewardId = offer.rewardId;
            run.claimedRewards.Add(offer.rewardId);
            run.pendingRewards.RemoveAll(x=>x.rewardId==offer.rewardId);
            run.offer = null;
            ShowNextSettlement();
'''+s[j:];p.write_text(s,encoding='utf-8')
edit('Client/Assets/Game/Runtime/Core/GameSession.Transitions.cs','if (state.phase != SessionPhase.MvpComplete)','if (state.phase != SessionPhase.MvpComplete && state.phase != SessionPhase.RunEnd)')
edit('Client/Assets/Game/Runtime/Core/GameSession.Transitions.cs','                        else if (state.run.offer != null) state.phase = SessionPhase.RewardChoice;\n','')
edit('Client/Assets/Game/Runtime/Core/GameSession.Transitions.cs','state.phase = SessionPhase.DeathResult;','BeginSettlement(false);')
edit('Client/Assets/Game/Runtime/Core/GameSession.Transitions.cs','rewardId = state.run.offer.rewardId','rewardId = state.run.runId+":"+state.run.cycle+":"+state.run.stage')
edit('Client/Assets/Game/Runtime/Core/GameSession.Transitions.cs','else state.phase = SessionPhase.RewardChoice;','else BeginTravel(state.run.stage,state.run.stage+1);')
edit('Client/Assets/Game/Runtime/Core/GameSession.Onboarding.cs','if(replay)state.phase=SessionPhase.Lobby;else ContinueOpening();Publish();return true;','state.phase=SessionPhase.Lobby;Publish();return true;')
edit('Client/Assets/Game/Runtime/Core/GameSession.Onboarding.cs','if(paused||prologuePaused||prologueManual)return;','if(paused||prologuePaused)return;')
edit('Client/Assets/Game/Runtime/Core/GameSession.cs','(state.phase!=SessionPhase.DeathResult&&state.phase!=SessionPhase.MvpComplete)||state.run==null','(state.run==null||!state.run.ended)')
edit('Client/Assets/Game/Runtime/Core/GameSession.cs','Cleared=state.phase==SessionPhase.MvpComplete','Cleared=state.run.cleared')
# Strict timing: expired active window cannot succeed simply because the sprite is returning.
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','bool dodged=parried||!threat.Bounds.Overlaps(PlayerCombatBody);','bool dodged=parried||active&&run.dodgeDirection==required&&!threat.Bounds.Overlaps(PlayerCombatBody);')
# Simultaneous deadlines resolve both before choosing death/kill transition.
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','private EnemyThreatView parryThreat;','private EnemyThreatView parryThreat;\n        private bool resolvingSimultaneous;')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','''                    ResolveEnemyHit();''','''                    resolvingSimultaneous=run.attackHitAtMs>=0&&run.attackHitAtMs<=run.clockMs+TimeEpsilonMs;
                    ResolveEnemyHit();
                    if(resolvingSimultaneous)
                    {
                        ResolvePlayerHit();resolvingSimultaneous=false;
                        if(run.playerHpMilli<=0)DefeatPlayer();
                        else if(run.enemyHpMilli<=0)DefeatEnemy();
                    }''')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','if (run.playerHpMilli <= 0) { DefeatPlayer(); return; }','if (run.playerHpMilli <= 0) { if(!resolvingSimultaneous)DefeatPlayer(); return; }')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','if (run.enemyHpMilli <= 0) DefeatEnemy();','if (run.enemyHpMilli <= 0) DefeatEnemy();')
# DefeatEnemy pays first but never heals an already dead player; outer block gives death priority.
write('Client/Assets/Game/Runtime/Core/GameSession.Settlement.cs','''using System;
using System.Collections.Generic;
namespace LastBell
{
    public sealed partial class GameSession
    {
        public int PendingRewardCount=>state.run?.pendingRewards.Count??0;
        public int RewardStage=>state.run?.offer?.stage??(PendingRewardCount>0?state.run.pendingRewards[0].stage:0);
        public long EarnedGold=>state.run?.earnedGold??0;
        public int EarnedFragments=>state.run?.earnedFragments??0;
        public int HighestStage=>CurrentCharacter.highestStage;
        void BeginSettlement(bool cleared)
        {
            var run=state.run;run.ended=true;run.cleared=cleared;
            run.attackHitAtMs=run.enemyHitAtMs=-1;run.transition=null;run.story=null;
            paused=false;ClearPendingEnemyImpact();ClearCombatQueue();state.phase=SessionPhase.RunEnd;
        }
        void ShowNextSettlement()
        {
            state.phase=PendingRewardCount>0?SessionPhase.RewardAnnouncement:state.run.cleared?SessionPhase.MvpComplete:SessionPhase.DeathResult;
        }
        public bool ContinueSettlement()
        {
            if(state.phase==SessionPhase.RunEnd){ShowNextSettlement();Publish();return true;}
            if(state.phase!=SessionPhase.RewardAnnouncement||PendingRewardCount==0)return false;
            var offer=state.run.pendingRewards[0].Copy();
            offer.offeredLevels=Array.ConvertAll(offer.relicIds,FindRelicLevel);
            state.run.offer=offer;state.phase=SessionPhase.RewardChoice;Publish();return true;
        }
        void MigrateReferenceSave()
        {
            if(state.schemaVersion!=6)return;
            var run=state.run;
            if(run!=null)
            {
                for(int i=0;i<run.randomSequence.Length;i++)if(run.randomSequence[i]=="outer_arc")run.randomSequence[i]="high_sweep";
                if(run.patternId=="outer_arc")run.patternId="high_sweep";
                if(run.previousPattern=="outer_arc")run.previousPattern="high_sweep";
                if(run.penultimatePattern=="outer_arc")run.penultimatePattern="high_sweep";
                if(run.offer!=null && run.offer.rewardId!=run.lastAppliedRewardId)
                {
                    run.pendingRewards.Add(run.offer.Copy());
                    if(state.phase==SessionPhase.RewardChoice){run.ended=true;run.offer.offeredLevels=Array.ConvertAll(run.offer.relicIds,FindRelicLevel);}
                    else run.offer=null;
                }
                if(state.phase==SessionPhase.DeathResult||state.phase==SessionPhase.MvpComplete)
                {run.ended=true;run.cleared=state.phase==SessionPhase.MvpComplete;}
                if(state.phase==SessionPhase.Prologue){state.run=null;}
            }
            state.schemaVersion=7;
        }
        void ValidateSettlement()
        {
            var r=state.run;if(r==null)return;
            var ids=new HashSet<string>();
            foreach(var id in r.claimedRewards)if(string.IsNullOrEmpty(id)||!ids.Add(id))throw new InvalidOperationException("Invalid claimed reward.");
            int previous=0;
            foreach(var q in r.pendingRewards)
            {
                if(q==null||q.stage<previous||q.stage>r.stage||!catalog.TryGetStage(q.stage,out StageDefinition stage)||
                    (stage.kind!="boss"&&!(q.legacyMidbossReward&&stage.kind=="miniboss"))||q.rewardId!=r.runId+":"+r.cycle+":"+q.stage||
                    !ids.Add(q.rewardId)||q.relicIds.Length!=3||q.maxGold<0)throw new InvalidOperationException("Invalid queued reward.");
                previous=q.stage;var candidates=new HashSet<string>();
                foreach(var id in q.relicIds){catalog.GetRelic(id);if(!candidates.Add(id))throw new InvalidOperationException("Duplicate reward candidate.");}
            }
            if(r.offer!=null&&(r.pendingRewards.Count==0||r.offer.rewardId!=r.pendingRewards[0].rewardId))throw new InvalidOperationException("Reward cursor mismatch.");
            if((state.phase==SessionPhase.RunEnd||state.phase==SessionPhase.RewardAnnouncement||state.phase==SessionPhase.RewardChoice)&&!r.ended)
                throw new InvalidOperationException("Settlement requires an ended run.");
            if(state.phase==SessionPhase.RewardAnnouncement&&r.pendingRewards.Count==0)throw new InvalidOperationException("Missing settlement reward.");
        }
    }
}
''')
print('core v81 written')
