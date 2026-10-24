from pathlib import Path
import json, shutil
root=Path('C:/Github/Test2')
def edit(p,old,new):
 p=root/p;s=p.read_text(encoding='utf-8-sig');assert old in s,(p,old[:100]);p.write_text(s.replace(old,new),encoding='utf-8')
def write(p,s):
 p=root/p;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8')
# Preserve the dirty starting point independently of git history.
backup=root/'Planning/Evidence/Reference-v81/before'
for base in ['Client/Assets/Game/Runtime','Client/Assets/Game/Editor','GameData']:
 for p in (root/base).rglob('*'):
  if p.suffix in ('.cs','.json','.py') and not any(x in p.parts for x in ['obj','bin']):
   q=backup/p.relative_to(root);q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
p=root/'GameData/mvp-catalog.json';d=json.loads(p.read_text(encoding='utf-8-sig'))
d['dataVersion']='mvp1.10.0';d['sourcePlanningVersion']='reference-v8.1'
c=d['combat'];c.update(dodgeActiveMs=240,dodgeRecoveryMs=120,perfectWindowMs=90,comboMinGapMs=750,singleTelegraphMs=900,fastTelegraphMs=900,perfectCounterMs=1200,comboDefaultGapMs=750)
d['economy'].update(minibossFragments=0,bossFragments=4)
for s in d['stages']:
 s['randomPatterns']=list(dict.fromkeys('high_sweep' if x=='outer_arc' else x for x in s['randomPatterns']))
 if len(s['randomPatterns'])<2:s['randomPatterns'].append('thrust')
 if s['number']==4:s['teachDodge']='';s['randomPatterns']=['low_sweep','thrust','high_sweep']
 for step in s['patternSteps']:
  if step['danger']=='outer_arc':step['danger']='high_sweep';step['requiredDodge']='parry'
  step['telegraphMs']=900;step['intervalMs']=750 if step['comboStep']>1 else {'normal':4800,'miniboss':3600,'boss':2700}[s['kind']]
for r in d['relics']:
 if r['id'] in ['relic_10','relic_45','relic_50']:print('RELIC',r)
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
edit('GameData/export_catalog.py',', "outer_arc": "forward"','')
edit('GameData/export_catalog.py','e["minibossFragments"] == 1 and e["bossFragments"] == 3','e["minibossFragments"] == 0 and e["bossFragments"] == 4')
edit('GameData/export_catalog.py','fixed per-kill fragments must be 1/3','boss-only fragments must be 0/4')
edit('GameData/export_catalog.py','<= 4:', '<= 3:')
edit('Client/Assets/Game/Runtime/Data/GameCatalog.cs','economy.minibossFragments == 1 && economy.bossFragments == 3','economy.minibossFragments == 0 && economy.bossFragments == 4')
edit('Client/Assets/Game/Runtime/Data/GameCatalog.cs','fixed 1/3 rewards','boss-only 0/4 rewards')
edit('Client/Assets/Game/Runtime/Data/GameCatalog.cs','s.number <= 4','s.number <= 3')
edit('Client/Assets/Game/Runtime/Data/GameCatalog.cs',' || direction == "forward"','')
edit('Client/Assets/Game/Runtime/Data/GameCatalog.cs',' case "outer_arc": return "forward";','')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','if (paused || !Enum.IsDefined(typeof(DodgeDirection), direction))','if (paused || direction==DodgeDirection.Forward || !Enum.IsDefined(typeof(DodgeDirection), direction))')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','return Array.ConvertAll(Array.FindAll(threats,t=>!t.Resolved),t=>t.Copy());','return Array.ConvertAll(Array.FindAll(threats,t=>!t.Resolved),t=>t.Copy());')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','private void ClearCounter()', 'public EnemyThreatView[] BellThreats => state.phase==SessionPhase.Combat&&!IsEnemyStunned?Array.ConvertAll(threats,t=>t.Copy()):new EnemyThreatView[0];\n        private void ClearCounter()')
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs','run.randomSequence[i]=pattern;', 'if(run.patternIndex==0 && i==0 && !string.IsNullOrEmpty(stage.teachDodge))pattern=stage.patternSteps[0].danger;\n                run.randomSequence[i]=pattern;')
start='''            // intervalMs is the minimum spacing from the preceding hit to the next sequence's first hit.
            ScheduleSequence(run.clockMs,false);
            double idle=Math.Max(0,CurrentStep.intervalMs-(run.enemyHitAtMs-run.clockMs));
            if(idle>0){foreach(var t in threats){t.CueAtMs+=idle;t.CueEndMs+=idle;t.ReleaseAtMs+=idle;t.ImpactAtMs+=idle;}SetCurrentThreat();}'''
edit('Client/Assets/Game/Runtime/Core/GameSession.Combat.cs',start,'''            // Rest is measured from the last actual contact to the next warning.
            ScheduleSequence(run.clockMs+CurrentStep.intervalMs,false);''')
edit('Client/Assets/Game/Runtime/Core/CombatGeometry.cs','internal bool CueEmitted,ReleaseEmitted,Resolved;', 'public bool Resolved {get;internal set;}\n        internal bool CueEmitted,ReleaseEmitted;')
edit('Client/Assets/Game/Runtime/Presentation/EnemyTelegraphGraphic.cs','bool bright=beat<.5f;', 'bool bright=beat<1f/3f;')
# schema and queue
edit('Client/Assets/Game/Runtime/Core/SessionState.cs','Prologue, ComboHint }','Prologue, ComboHint, RunEnd, RewardAnnouncement }')
edit('Client/Assets/Game/Runtime/Core/SessionState.cs','schemaVersion = 6','schemaVersion = 7')
edit('Client/Assets/Game/Runtime/Core/SessionState.cs','public RelicOfferData offer;','''public RelicOfferData offer;
        public List<RelicOfferData> pendingRewards = new List<RelicOfferData>();
        public List<string> claimedRewards = new List<string>();
        public bool ended, cleared;''')
edit('Client/Assets/Game/Runtime/Core/SessionState.cs','copy.offer = offer?.Copy();','''copy.offer = offer?.Copy();
            copy.pendingRewards = pendingRewards == null ? new List<RelicOfferData>() : pendingRewards.ConvertAll(x=>x.Copy());
            copy.claimedRewards = claimedRewards == null ? new List<string>() : new List<string>(claimedRewards);''')
edit('Client/Assets/Game/Runtime/Bootstrap/JsonSaveRepository.cs','&&value.schemaVersion!=6','&&value.schemaVersion!=6&&value.schemaVersion!=7')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','            ValidateSave();','            MigrateReferenceSave();\n            ValidateSave();')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','if (saved == null) { BeginRun(); return; }','if (saved == null) { state.phase=SessionPhase.Prologue; IsDirty=true; return; }')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','state.schemaVersion != 6','state.schemaVersion != 7')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','state.phase == SessionPhase.RewardChoice ||\n                state.phase', 'state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.RunEnd || state.phase == SessionPhase.RewardAnnouncement ||\n                state.phase')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','state.phase == SessionPhase.Prologue&&!state.prologueSeen || ','')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','(death ? state.run.playerHpMilli != 0 : state.run.playerHpMilli <= 0)','(death ? state.run.playerHpMilli != 0 : !state.run.ended && state.run.playerHpMilli <= 0)')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','(state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.StoryDialogue)','(state.phase == SessionPhase.RewardChoice)')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','state.run.offer.offeredLevels.Length != state.run.offer.relicIds.Length || !state.run.stageRewardApplied','state.run.offer.offeredLevels.Length != state.run.offer.relicIds.Length || !state.run.ended')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','if (!state.run.stageRewardApplied || offer.stage != state.run.stage ||','if (!state.run.ended ||')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','state.run.runId + ":" + state.run.cycle + ":" + state.run.stage ||','state.run.runId + ":" + state.run.cycle + ":" + offer.stage ||')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','scene.stage != state.run.stage || state.run.offer == null ||\n                    story.rewardId != state.run.offer.rewardId','scene.stage != state.run.stage || !state.run.stageRewardApplied ||\n                    story.rewardId != state.run.runId+":"+state.run.cycle+":"+state.run.stage')
edit('Client/Assets/Game/Runtime/Core/GameSession.State.cs','            if(state.phase==SessionPhase.Prologue&&!state.prologueSeen&&(state.run.stage!=1||state.run.enemyHitAtMs!=-1))\n                throw new InvalidOperationException("Invalid prologue state.");','            ValidateSettlement();')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''            if(!state.prologueSeen)
            {
                prologueElapsedMs=0;prologueParagraph=0;prologuePaused=false;
                state.phase=SessionPhase.Prologue;state.run.enemyHitAtMs=-1;
            }
            else ContinueOpening();''','            ContinueOpening();')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''                state.phase = SessionPhase.MvpComplete;''','''                run.cleared=true;run.ended=true;
                state.phase = SessionPhase.RunEnd;''')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''            if (state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.StoryDialogue ||''','''            if (state.phase == SessionPhase.RunEnd || state.phase == SessionPhase.RewardAnnouncement || state.phase == SessionPhase.RewardChoice || state.phase == SessionPhase.StoryDialogue ||''')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''            state.phase = SessionPhase.Lobby;
            state.exchange = null;''','''            if(state.run!=null&&!state.run.ended && state.run.pendingRewards.Count>0)
            { BeginSettlement(false);Publish();return true; }
            state.phase = SessionPhase.Lobby;
            state.exchange = null;''')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''                CreateRelicOffer();
                PrepareStory();''','''                CreateRelicOffer();
                if(run.playerHpMilli>0)run.playerHpMilli=run.combatStats.maxHpMilli;
                PrepareStory();''')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''            run.offer = new RelicOfferData''','''            run.pendingRewards.Add(new RelicOfferData''')
edit('Client/Assets/Game/Runtime/Core/GameSession.Progression.cs','''                maxGold = catalog.GetMaxRelicGold(run.stage)
            };''','''                maxGold = catalog.GetMaxRelicGold(run.stage)
            });''')
a='''            long oldMax = run.combatStats.maxHpMilli;''';b='''            Emit(SessionEventKind.RelicChosen, relicId: id, eventStage: offer.stage);'''
p=root/'Client/Assets/Game/Runtime/Core/GameSession.Progression.cs';s=p.read_text();i=s.index(a);j=s.index(b,i);s=s[:i]+'''            run.lastAppliedRewardId = offer.rewardId;
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
