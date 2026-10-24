#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace LastBell
{
    // Explicit observation only. Expectations come from the handed-over manifest,
    // never from the previous generation's frame counts, pivots or body heights.
    public sealed partial class DragonThemeSmokeProbe
    {
        [Serializable] sealed class V3Manifest { public V3Art[] actors; public V3Texture[] textures; }
        [Serializable] sealed class V3Art { public string id; public float height,referenceHeightPixels; public bool facesRight; public V3Clip[] clips; }
        [Serializable] sealed class V3Clip
        {
            public string motion,sourceId; public string[] frames; public int[] sourceFrames,frameDurationsMs;
            public int contactFrame=-1; public float fps,referenceHeightPixels; public bool loop;
        }
        [Serializable] sealed class V3Texture { public string path,sha256; public int width,height; public Vector2 pivot; }
        [Serializable] sealed class V3Bindings { public V3Binding[] mapping; public bool skillsImplemented; public int playableStages; }
        [Serializable] sealed class V3Binding { public int stage; public string stableEnemyId,displayName,artId; }
        [Serializable] sealed class V3Evidence
        {
            public string visualReview="NOT_RUN",naturalMotionReview="NOT_RUN",osInput="DEFERRED",mobile="DEFERRED";
            public string defaultSaveRuntime="NOT_RUN: observed launches use an explicit isolated override; selector contract checked separately";
            public string skillImplementation="Source/planning review required; runtime probe does not certify absence of all skill code";
            public int writtenFrames; public bool playtestDefine;
            public List<string> clips=new List<string>(); public List<V3Ground> grounds=new List<V3Ground>();
            public List<string> attackAliases=new List<string>();
            public List<V2Segment> segments=new List<V2Segment>();
        }
        [Serializable] sealed class V3Ground { public int stage; public string region,kind; public float planY; public Vector3 playerHome,enemyHome; }
        [Serializable] sealed class V3ActorFrame
        {
            public string art,motion,clipMotion,sourceId; public int clipFrame,sourceFrame;
            public Vector3 root,bodyOffset,bodyScale,feetWorld,feetScreen,groundScreen,faceScreen;
            public float rootHeightAboveGround; public Vector2 pivot,size;
        }
        [Serializable] sealed class V3Frame
        {
            public string file,sha256,scenario,clock,phase,transition; public int frameCount,stage;
            public float realtime,delta,cameraSize,travel,playerHp,enemyHp,stunRemaining;
            public double combatMs; public V3ActorFrame player,enemy;
        }
        sealed class V3Buffered { public Texture2D pixels; public V3Frame index; }
        readonly V3Evidence v3Evidence=new V3Evidence();
        readonly List<V3Buffered> v3Frames=new List<V3Buffered>();
        readonly List<SessionEvent> v3Events=new List<SessionEvent>();
        V3Manifest v3Manifest; GameCatalog v3ExpectedCatalog; StreamWriter v3Index; bool v3Active;

        IEnumerator DragonV3(bool resume)
        {
            v3Active=true;
            report.limits="V3 changed-scope fixtures and synthetic commands. Manifest/ground/save assertions are separate from visual/natural-motion review. Default-path exe launch, OS input and mobile are not covered.";
            Require(Application.version=="0.9.2"&&catalog.dataVersion=="mvp1.15.0","V3 version 0.9.2 / data mvp1.15.0");
            Require((Screen.width==1280&&Screen.height==720)||(Screen.width==640&&Screen.height==360),"V3 target resolution");
#if LASTBELL_PLAYTEST_092 && UNITY_STANDALONE_WIN && !UNITY_EDITOR
            v3Evidence.playtestDefine=true;
#endif
            Require(v3Evidence.playtestDefine,"Windows observed build includes LASTBELL_PLAYTEST_092");
            V3SaveSelector();
            if(resume){yield return V3Resume();yield break;}
            v3Manifest=JsonUtility.FromJson<V3Manifest>(File.ReadAllText(Path.Combine(output,"art-manifest.json")));
            v3ExpectedCatalog=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(output,"expected-catalog.json")));
            var bindings=JsonUtility.FromJson<V3Bindings>(File.ReadAllText(Path.Combine(output,"data-bindings.json")));
            Require(bindings.mapping.Length==10&&bindings.mapping.Select(x=>x.stage).OrderBy(x=>x).SequenceEqual(Enumerable.Range(11,10)),"ten forest stage bindings");
            Require(!bindings.skillsImplemented&&bindings.playableStages==20,"handoff retains twenty stages and planned-only skills");
            v3Index=new StreamWriter(new FileStream(Path.Combine(output,"frames.jsonl"),FileMode.CreateNew));
            try
            {
                yield return V3Fresh();
                for(int stage=1;stage<=20;stage++)
                {
                    var ground=ParallaxWorld.GroundForStage(stage);
                    Require(stage>=ground.FirstStage&&stage<=ground.LastStage,"ground range stage "+stage);
                    v3Evidence.grounds.Add(new V3Ground{stage=stage,region=ground.Region,kind=ground.Kind.ToString(),planY=ground.PlanY,
                        playerHome=ParallaxWorld.GroundPoint(500,stage),enemyHome=ParallaxWorld.GroundPoint(900,stage)});
                }
                foreach(var binding in bindings.mapping.OrderBy(x=>x.stage))
                {
                    var stage=catalog.GetStage(binding.stage);var enemy=catalog.GetEnemy(stage.enemyId);
                    Require(stage.enemyId==binding.stableEnemyId&&enemy.artId==binding.artId&&enemy.displayName==binding.displayName,"catalog binding stage "+binding.stage);
                    yield return V3Prepare(binding.stage);
                    Require(p.EnemyActor.ArtId==binding.artId,"rendered forest actor stage "+binding.stage);
                    V3ValidateArt(p.EnemyActor);V3GroundIdle();
                    foreach(string pattern in stage.patternSteps.Select(x=>x.danger).Concat(stage.randomPatterns).Distinct())
                    {
                        var motion=BattlePresentation.EnemyMotion(pattern);var actual=p.Art.Find(binding.artId);
                        var expected=actual.FindClip(motion)??actual.FindClip(ActorMotion.Attack);
                        Require(expected!=null&&p.EnemyActor.GetClip(motion)==expected,"pattern resolves to new art clip: "+binding.stage+"/"+pattern);
                        v3Evidence.attackAliases.Add(binding.stage+"/"+pattern+"/"+motion+" -> "+expected.motion+"/"+expected.sourceId);
                    }
                    yield return Capture("v3-stage-"+binding.stage+"-idle");
                    yield return V3Observe("enemy-"+binding.stage+"-idle",p.EnemyActor.GetClip(ActorMotion.Idle).Duration*2+.05f,false);
                    yield return V3EnemyAttack(binding.stage);
                    yield return V3Stun(binding.stage);
                }
                foreach(int stage in new[]{1,11})yield return V3Player(stage);
                foreach(int stage in new[]{15,20})
                {yield return V3Boss(stage);yield return V3PlayerAttack(stage);}
                foreach(int stage in new[]{10,14,15})yield return V3Travel(stage);
                yield return V3StoryTwenty();
                // Leave an identified, paused mid-run snapshot for a second native
                // process using this exact directory (not a copy or an in-memory reload).
                yield return V3Prepare(11);session.SetPaused(true);Save();
                File.WriteAllText(Path.Combine(output,"v3-resume-expected.json"),JsonUtility.ToJson(session.ExportSave(),true));
                Require(v3Evidence.writtenFrames==v3Evidence.segments.Sum(x=>x.frames),"V3 all captured frames written");
            }
            finally { ReleaseV3Frames(); }
        }
        void V3SaveSelector()
        {
            string root=Path.Combine(output,"selector-only");
            Require(Same(SaveDirectorySelector.Select(root,new string[0],true),Path.Combine(root,"Playtests","dragon-0.9.2")),"new default save namespace selector");
            Require(Same(SaveDirectorySelector.Select(root,new string[0],false),root),"legacy/Editor default namespace preserved");
            Require(Same(SaveDirectorySelector.Select(root,new[]{"-lastbell-save-dir",saveDirectory},true),saveDirectory),"explicit QA override wins over playtest namespace");
            Require(!Directory.Exists(root),"selector is side-effect free");
        }
        IEnumerator V3Fresh()
        {
            Require(session.Phase==SessionPhase.Prologue&&session.CheckpointStartStage==1,"fresh isolated profile begins at stage one");
            yield return Capture("v3-fresh-prologue");Click(p.View.Prologue.Skip);yield return Rendered();
            string[] ids={"on","dam","rin"},names={"루카","다렌","미엘"};
            Require(catalog.characters.Length==3&&catalog.defaultCharacterId=="on","stable character roster");
            string selected=session.CurrentCharacterId;long gold=session.Gold;int fragments=session.Fragments;
            Require(selected=="on"&&session.Phase==SessionPhase.Lobby,"fresh selection keeps Luka selected");
            for(int i=0;i<3;i++)
            {
                var ch=catalog.GetCharacter(ids[i]);
                Require(catalog.characters[i].id==ids[i]&&ch.displayName.Split('·')[0].Trim()==names[i]&&ch.availableInMvp==(i==0),"name and availability: "+ids[i]);
                Require(p.View.CharacterLabels[i].text==names[i]+(i==0?"":" (제작 중)"),"roster label: "+ids[i]+" = "+p.View.CharacterLabels[i].text);
                // Roster buttons preview every character; StartRun owns selection availability.
                Click(p.View.Characters[i]);yield return Capture("v3-character-preview-"+ids[i]);
                Require(p.View.CharacterName.text==names[i],"preview name: "+ids[i]+" = "+p.View.CharacterName.text);
                Require(p.View.StartRun.isActiveAndEnabled&&p.View.StartRun.interactable==(i==0),"preview selection availability: "+ids[i]+" = "+p.View.StartRun.interactable);
                Require(p.View.StartLabel.text==(i==0?"선택":"제작 중")&&!p.View.Reference.UpgradeScreen,"preview selection label: "+ids[i]+" = "+p.View.StartLabel.text);
                Require(session.CurrentCharacterId==selected&&session.Gold==gold&&session.Fragments==fragments&&session.Phase==SessionPhase.Lobby,"preview preserves selection/wallet/lobby: "+ids[i]+" = "+session.CurrentCharacterId+"/"+session.Gold+"/"+session.Fragments+"/"+session.Phase);
            }
            Require(!session.SelectCharacter("dam")&&!session.SelectCharacter("rin")&&session.CurrentCharacterId==selected&&session.Gold==gold&&session.Fragments==fragments,"locked characters cannot change selection or wallet");
            Click(p.View.Characters[0]);yield return Capture("v3-selection-names");
            Require(p.View.CharacterName.text=="루카"&&p.View.StartRun.interactable&&p.View.StartLabel.text=="선택"&&!p.View.Reference.UpgradeScreen,"return to Luka preview enables selection");
            Require(session.CurrentCharacterId==selected&&session.Gold==gold&&session.Fragments==fragments,"return to Luka preserves selection and wallet");
            Click(p.View.StartRun);yield return Capture("v3-upgrade-name");
            Require(p.View.CharacterName.text=="루카","upgrade shows Luka");Click(p.View.StartRun);yield return Rendered();
            Require(session.Stage==1&&session.Phase==SessionPhase.TutorialDodge,"first departure is stage one tutorial");
            Press(p.View.Dodge[0]);yield return Rendered();Press(p.View.Attack);session.Tick(.2f);yield return Capture("v3-first-stage-one");
            Require(session.Stage==1&&session.TutorialStep==2,"new progress starts at stage one");
        }
        IEnumerator V3Resume()
        {
            var expected=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(Path.Combine(output,"v3-resume-expected.json")));
            var actual=session.ExportSave();
            Require(actual.run!=null&&expected.run!=null&&actual.phase==expected.phase&&actual.run.stage==expected.run.stage&&actual.run.runId==expected.run.runId,"native restart preserves phase/stage/run identity");
            Require(actual.selectedCharacterId==expected.selectedCharacterId&&actual.gold==expected.gold&&actual.fragments==expected.fragments&&actual.tutorialStep==expected.tutorialStep,"native restart preserves character/wallet/tutorial");
            Require(actual.characters.Count==expected.characters.Count&&actual.characters.Zip(expected.characters,(a,b)=>JsonUtility.ToJson(a)==JsonUtility.ToJson(b)).All(x=>x),"native restart preserves growth/checkpoints/seen records");
            Require(actual.relics.Count==expected.relics.Count&&actual.relics.Zip(expected.relics,(a,b)=>JsonUtility.ToJson(a)==JsonUtility.ToJson(b)).All(x=>x),"native restart preserves relics");
            yield return Capture("v3-native-resume");ReleaseV3Frames();
        }
        void V3Event(SessionEvent value)=>v3Events.Add(value);
        void V3Bind(GameSession value)
        {
            if(session!=null)session.EventRaised-=V3Event;
            value.SetPaused(false);Bind(value,false);v3Events.Clear();session.EventRaised+=V3Event;
        }
        IEnumerator V3Prepare(int stage,bool weak=false)
        {
            if(session!=null)session.EventRaised-=V3Event;
            // A valid HP-growth fixture survives a forest hit so Hit/recovery can
            // be observed. Attack level, stage damage and gameplay data are unchanged.
            var lobby=LobbyFixture();lobby.characters.Single(x=>x.id==catalog.defaultCharacterId).statLevels[1]=catalog.growth.maxLevel;
            var temp=new GameSession(catalog,lobby);temp.StartRun();var save=temp.ExportSave();
            save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;save.run.patternStep=save.run.patternIndex=0;
            save.run.randomSequence=new string[0];save.run.sequenceCursor=0;
            save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
            V3Bind(new GameSession(catalog,save));
            yield return new WaitForSecondsRealtime(.7f);yield return Rendered();
        }
        void V3GroundIdle()
        {
            // Horizontal attack/approach offsets may still be active at recovery.
            Require(Mathf.Abs(p.PlayerActor.transform.localPosition.y-ParallaxWorld.GroundPoint(500,session.Stage).y)<.001f,"player root returns to stage ground "+session.Stage);
            Require(Mathf.Abs(p.EnemyActor.transform.localPosition.y-ParallaxWorld.GroundPoint(900,session.Stage).y)<.001f,"enemy root uses stage ground "+session.Stage);
            var world=p.Presentation.World;var tint=ParallaxWorld.GroundForStage(session.Stage).FloorTint;
            Require(world.GroundTiles.Length>0&&world.GroundTiles.All(x=>x&&x.color==tint),"stored ground tiles use stage tint "+session.Stage);
        }
        void V3ValidateArt(ActorView actor)
        {
            var expected=v3Manifest.actors.Single(x=>x.id==actor.ArtId);var actual=p.Art.Find(actor.ArtId);
            Require(actual!=null&&Mathf.Approximately(actual.height,expected.height)&&Mathf.Approximately(actual.referenceHeightPixels,expected.referenceHeightPixels)&&actual.facesRight==expected.facesRight,"manifest body scale/facing: "+actor.ArtId);
            foreach(var item in expected.clips)
            {
                var motion=(ActorMotion)Enum.Parse(typeof(ActorMotion),item.motion);var clip=actual.FindClip(motion);
                Require(clip!=null&&clip.frames.Length==item.frames.Length&&clip.frames.Length>0&&clip.frames.All(x=>x),"manifest explicit clip: "+actor.ArtId+"/"+motion);
                Require(clip.sourceId==item.sourceId&&clip.contactFrame==item.contactFrame&&clip.loop==item.loop&&Mathf.Approximately(clip.fps,item.fps)&&Mathf.Approximately(clip.referenceHeightPixels,item.referenceHeightPixels),"manifest timing/reference: "+actor.ArtId+"/"+motion);
                Require((clip.frameDurationsMs??new int[0]).SequenceEqual(item.frameDurationsMs??new int[0])&&(clip.sourceFrames??new int[0]).SequenceEqual(item.sourceFrames??new int[0]),"manifest selected frames/holds: "+actor.ArtId+"/"+motion);
                for(int i=0;i<clip.frames.Length;i++)
                {
                    var tex=v3Manifest.textures.Single(x=>x.path==item.frames[i]);var sprite=clip.frames[i];
                    Require(sprite.rect.width==tex.width&&sprite.rect.height==tex.height&&Vector2.Distance(sprite.pivot,new Vector2(tex.pivot.x,tex.height-tex.pivot.y))<.01f,"manifest frame geometry: "+actor.ArtId+"/"+motion+"/"+i);
                    Require(sprite.texture.filterMode==FilterMode.Point&&sprite.texture.mipmapCount==1,"pixel texture sampling: "+actor.ArtId+"/"+motion+"/"+i);
                }
                v3Evidence.clips.Add(actor.ArtId+"/"+motion+"/"+clip.sourceId);
            }
            foreach(var motion in actor==p.PlayerActor?new[]{ActorMotion.Idle,ActorMotion.Run,ActorMotion.Attack,ActorMotion.DodgeUp,ActorMotion.DodgeBack,ActorMotion.DodgeDown,ActorMotion.Hit}:new[]{ActorMotion.Idle,ActorMotion.Attack,ActorMotion.Stun})
                Require(actual.FindClip(motion)!=null&&actual.FindClip(motion).frames.Length>0,"required explicit motion: "+actor.ArtId+"/"+motion);
        }
        IEnumerator V3EnemyAttack(int stage)
        {
            yield return V3Prepare(stage);var threat=session.EnemyThreats.First();
            // Position the clock before the existing cue's impact; observe the actual
            // accepted combat event in real frames. Do not rewrite the pattern/data.
            float seek=Mathf.Max(0,(float)((threat.ImpactAtMs-session.ClockMs)/1000)-p.EnemyActor.GetClip(ActorMotion.Attack).Duration-.15f);
            if(seek>0)session.Tick(seek);
            float seconds=(float)((threat.ImpactAtMs-session.ClockMs)/1000)+p.EnemyActor.GetClip(ActorMotion.Attack).Duration+.25f;
            bool sawHit=false;
            yield return V3Observe("enemy-"+stage+"-attack-hit",seconds,true,t=>{sawHit|=p.PlayerActor.CurrentMotion==ActorMotion.Hit;});
            Require(v3Events.Any(x=>x.Kind==SessionEventKind.PlayerDamaged)&&sawHit,"actual enemy attack and player Hit rendered: "+stage);
        }
        IEnumerator V3Stun(int stage)
        {
            yield return V3Prepare(stage);var save=session.ExportSave();save.run.stagger=catalog.combat.staggerThreshold;
            V3Bind(new GameSession(catalog,save));yield return Rendered();bool sent=false,saw=false;
            yield return V3Observe("enemy-"+stage+"-stun",catalog.GetStunDurationMs(catalog.GetStage(stage).kind)/1000f+.7f,true,t=>
            {if(!sent){Require(session.Attack(),"stun triggering attack accepted");sent=true;}saw|=session.IsEnemyStunned&&p.EnemyActor.CurrentMotion==ActorMotion.Stun;});
            Require(saw&&v3Events.Count(x=>x.Kind==SessionEventKind.EnemyStunned)==1&&!session.IsEnemyStunned,"actual Stun entry and recovery: "+stage);
        }
        IEnumerator V3PlayerAttack(int stage)
        {
            yield return V3Prepare(stage);bool sent=false;
            yield return V3Observe("player-"+stage+"-attack",.85f,true,t=>{if(!sent&&t>=.1f){Require(session.Attack(),"player attack accepted");sent=true;}});
            var start=v3Events.Single(x=>x.Kind==SessionEventKind.AttackStarted);var hit=v3Events.Single(x=>x.Kind==SessionEventKind.EnemyDamaged);
            Require(Math.Abs(hit.TimeMs-start.TimeMs-catalog.combat.attackWindupMs)<.01,"player attack hits at catalog windup: "+stage);V3GroundIdle();
        }
        IEnumerator V3Player(int stage)
        {
            yield return V3Prepare(stage);V3ValidateArt(p.PlayerActor);
            yield return V3Observe("player-"+stage+"-idle",p.PlayerActor.GetClip(ActorMotion.Idle).Duration*2+.05f,false);
            yield return V3PlayerAttack(stage);
            // Accepted input at a quiet fixture boundary covers all three motions;
            // this is motion/ground QA, not a claim of perfect defense per direction.
            for(int direction=0;direction<3;direction++)
            {
                yield return V3Prepare(stage);bool sent=false,saw=false;
                var motion=direction==0?ActorMotion.DodgeUp:direction==1?ActorMotion.DodgeBack:ActorMotion.DodgeDown;
                yield return V3Observe("player-"+stage+"-defense-"+direction,.8f,true,t=>
                {if(!sent&&t>=.08f){Require(session.Dodge((DodgeDirection)direction),"defense command accepted: "+direction);sent=true;}saw|=p.PlayerActor.CurrentMotion==motion;});
                Require(saw,"defense motion rendered: "+motion);
                V3GroundIdle();
            }
            yield return V3EnemyAttack(stage); // Hit is caused by the actual enemy attack.
            yield return V3Travel(stage);
        }
        IEnumerator V3Boss(int stage)
        {
            yield return V3Prepare(stage);var save=session.ExportSave();save.phase=SessionPhase.Transition;
            save.run.enemyHitAtMs=save.run.attackHitAtMs=-1;
            save.run.transition=new TransitionData{kind=TransitionKind.BossIntro,fromStage=stage-1,toStage=stage,elapsedMs=0,
                durationMs=stage==20?catalog.transitions.bossIntroMs:catalog.transitions.minibossIntroMs};
            V3Bind(new GameSession(catalog,save));yield return Rendered();float hp=session.PlayerHp;bool saw=false;
            Require(!session.Attack()&&!session.Dodge(DodgeDirection.Up),"boss intro rejects combat: "+stage);
            yield return V3Observe("boss-"+stage+"-intro",save.run.transition.durationMs/1000f+.1f,true,t=>{saw|=p.EnemyActor.CurrentMotion==ActorMotion.Roar;});
            Require(saw&&session.Phase==SessionPhase.Combat&&session.PlayerHp==hp,"boss roar and combat return: "+stage);V3GroundIdle();
        }
        IEnumerator V3Travel(int stage)
        {
            yield return V3Prepare(stage,true);Require(session.Attack(),"travel kill accepted: "+stage);session.Tick(.2f);yield return Rendered();
            if(session.Phase==SessionPhase.StoryDialogue){yield return Capture("v3-story-"+stage+"-boundary");session.SkipStory();}
            bool sawRun=false;
            yield return V3Observe("travel-"+stage+"-to-"+(stage+1),catalog.transitions.enemyDefeatMs/1000f+catalog.transitions.travelMs/1000f+catalog.transitions.arrivalMs/1000f+.1f,true,t=>
            {sawRun|=p.PlayerActor.CurrentMotion==ActorMotion.Run;});
            Require(sawRun&&session.Stage==stage+1,"run reaches adjacent stage: "+stage);yield return Capture("v3-arrival-"+(stage+1));
        }
        IEnumerator V3StoryTwenty()
        {
            yield return V3Prepare(20,true);Require(session.Attack(),"stage20 kill accepted");session.Tick(.2f);session.Tick(catalog.transitions.enemyDefeatMs/1000f);yield return Rendered();
            Require(session.Phase==SessionPhase.StoryDialogue,"stage20 opens story before settlement");
            long gold=session.Gold;int fragments=session.Fragments;var scene=v3ExpectedCatalog.FindStory(20);
            Require(scene!=null&&scene.lines.Length>0,"handed-over stage20 story exists");
            var skipSave=session.ExportSave();
            for(int i=0;i<scene.lines.Length;i++)
            {
                Require(session.Story!=null&&session.Story.LineIndex==i&&session.Story.Text==scene.lines[i].text&&session.Story.Speaker==scene.lines[i].speaker,"stage20 story page "+i);
                yield return Capture("v3-story20-page-"+i);
                if(i==1){Save();bool recovered;V3Bind(new GameSession(catalog,repository.Load(out recovered)));Require(!recovered&&session.Story.LineIndex==i,"story mid-page file restore");}
                Click(p.Presentation.Overlay.ButtonNext);yield return Rendered();
            }
            session.Tick(10);yield return Rendered();
            Require(session.Phase==SessionPhase.RunEnd&&session.Stage==20&&session.Gold==gold&&session.Fragments==fragments,"stage20 story ends at MVP boundary without duplicate reward");
            yield return Capture("v3-twenty-run-end");
            Require(session.ContinueSettlement()&&session.ContinueSettlement()&&session.Phase==SessionPhase.RewardChoice,"stage20 pending reward opens explicitly");
            string selected=session.RelicChoices[0];Require(session.ChooseRelic(selected),"stage20 reward chosen once");session.Tick(10);yield return Capture("v3-twenty-complete");
            Require(session.Phase==SessionPhase.MvpComplete&&!session.ChooseRelic(selected)&&session.Stage==20,"twenty completes without stage21 or repeat reward");
            V3Bind(new GameSession(catalog,skipSave));Require(session.SkipStory(),"story skip accepted");session.Tick(10);
            Require(session.Gold==gold&&session.Fragments==fragments&&session.PendingRewardCount==1&&session.Phase==SessionPhase.RunEnd,"skip preserves pending reward and boundary");
        }
        V3ActorFrame V3Actor(ActorView actor)
        {
            var clip=actor.GetClip(actor.CurrentMotion);var sprite=actor.Body.sprite;int index=actor.CurrentFrame;
            if(clip==null||index<0||index>=clip.frames.Length||sprite!=clip.frames[index])throw new InvalidOperationException("Rendered sprite outside selected clip: "+actor.ArtId+"/"+actor.CurrentMotion);
            var local=actor.transform.localPosition;var ground=ParallaxWorld.GroundPoint(0,session.Stage);
            Vector3 groundWorld=actor.transform.parent.TransformPoint(new Vector3(local.x,ground.y,local.z));
            return new V3ActorFrame{art=actor.ArtId,motion=actor.CurrentMotion.ToString(),clipMotion=clip.motion.ToString(),sourceId=clip.sourceId,
                clipFrame=index,sourceFrame=clip.sourceFrames!=null&&index<clip.sourceFrames.Length?clip.sourceFrames[index]:-1,root=local,
                bodyOffset=actor.Body.transform.localPosition,bodyScale=actor.Body.transform.localScale,feetWorld=actor.FeetWorldPosition,
                feetScreen=p.WorldCamera.WorldToScreenPoint(actor.FeetWorldPosition),groundScreen=p.WorldCamera.WorldToScreenPoint(groundWorld),
                faceScreen=p.WorldCamera.WorldToScreenPoint(actor.FaceWorldPosition),rootHeightAboveGround=local.y-ground.y,pivot=sprite.pivot,size=sprite.rect.size};
        }
        IEnumerator V3Observe(string label,float seconds,bool realtime,Action<float> afterFrame=null)
        {
            scenario=label;clockMode=realtime?"real-frame-v3":"real-frame-v3-static-session";p.TickSession=realtime;
            var segment=new V2Segment{name=label,clock=clockMode};v3Evidence.segments.Add(segment);
            float start=Time.realtimeSinceStartup,previousTime=0;int previous=-1;
            try
            {
                while(Time.realtimeSinceStartup-start<seconds)
                {
                    yield return new WaitForEndOfFrame();float now=Time.realtimeSinceStartup;
                    if(previous>=0){segment.missingRenderedFrames+=Mathf.Max(0,Time.frameCount-previous-1);segment.maxRealtimeGap=Mathf.Max(segment.maxRealtimeGap,now-previousTime);}
                    else segment.firstRealtime=now;
                    if(previous>=Time.frameCount)throw new InvalidOperationException("Repeated rendered frame");
                    previous=Time.frameCount;previousTime=now;segment.lastRealtime=now;segment.maxDelta=Mathf.Max(segment.maxDelta,Time.unscaledDeltaTime);
                    if((long)(v3Frames.Count+1)*Screen.width*Screen.height*3>768L*1024*1024)throw new InvalidOperationException("V3 segment exceeds raw capture memory budget; frames were not discarded");
                    var frame=new V3Frame{file="v3-frame-"+label+"-"+(captureSequence++).ToString("D6")+".png",scenario=label,clock=clockMode,
                        frameCount=Time.frameCount,stage=session.Stage,phase=session.Phase.ToString(),transition=session.Transition?.Kind.ToString(),
                        realtime=now,delta=Time.unscaledDeltaTime,combatMs=session.ClockMs,cameraSize=p.WorldCamera.orthographicSize,
                        travel=p.Presentation.TravelPixels,playerHp=session.PlayerHp,enemyHp=session.EnemyHp,stunRemaining=session.StunSecondsRemaining,
                        player=V3Actor(p.PlayerActor),enemy=V3Actor(p.EnemyActor)};
                    v3Frames.Add(new V3Buffered{pixels=CapturePixels(),index=frame});segment.frames++;Trace();afterFrame?.Invoke(now-start);
                }
                Require(segment.frames>1&&segment.missingRenderedFrames==0,"continuous V3 segment: "+label);
            }
            finally
            {
                p.TickSession=false;float begin=Time.realtimeSinceStartup;
                try
                {
                    foreach(var frame in v3Frames)
                    {
                        byte[] bytes=frame.pixels.EncodeToPNG();using(var sha=SHA256.Create())frame.index.sha256=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
                        using(var stream=new FileStream(Path.Combine(output,frame.index.file),FileMode.CreateNew))stream.Write(bytes,0,bytes.Length);
                        v3Index.WriteLine(JsonUtility.ToJson(frame.index));v3Evidence.writtenFrames++;
                    }
                    v3Index.Flush();timeline.Flush();events.Flush();
                }
                finally {foreach(var frame in v3Frames)if(frame.pixels)Destroy(frame.pixels);v3Frames.Clear();segment.encodeSeconds=Time.realtimeSinceStartup-begin;}
            }
        }
        void ReleaseV3Frames()
        {
            if(!v3Active)return;
            if(p)p.TickSession=false;if(session!=null)session.EventRaised-=V3Event;
            foreach(var frame in v3Frames)if(frame.pixels)Destroy(frame.pixels);v3Frames.Clear();v3Index?.Dispose();v3Index=null;
            if(Directory.Exists(output))File.WriteAllText(Path.Combine(output,"v3-evidence.json"),JsonUtility.ToJson(v3Evidence,true));
        }
    }
}
#endif
