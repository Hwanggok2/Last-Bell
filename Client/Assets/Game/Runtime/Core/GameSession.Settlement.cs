using System;
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
            run.comboPractice=ComboPracticeMode.None;
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
            if(r.playerHpMilli<0||r.combatStats!=null&&r.playerHpMilli>r.combatStats.maxHpMilli)throw new InvalidOperationException("Invalid player health.");
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
