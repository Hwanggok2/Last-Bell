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
    public sealed partial class DragonThemeSmokeProbe
    {
        readonly List<BufferedV2Frame> v2Frames=new List<BufferedV2Frame>();
        readonly List<SessionEvent> v2Events=new List<SessionEvent>();
        readonly V2Evidence v2Evidence=new V2Evidence();
        StreamWriter v2Index;
        [Serializable] sealed class V2Evidence
        {
            public string visualReview="NOT_RUN",osInput="DEFERRED",capturePolicy="Every rendered frame; PNG encoding happens after each segment. Use recorded realtime intervals for playback, never assume 60 fps.";
            public int writtenFrames;
            public List<V2Clip> clips=new List<V2Clip>();
            public List<V2Segment> segments=new List<V2Segment>();
            public List<V2Hp> hp=new List<V2Hp>();
        }
        [Serializable] sealed class V2Clip
        {
            public string artId,motion,sourceId;public int contactFrame;public float duration,referenceHeight;
            public int[] holds,sourceFrames;public string[] sprites;public Vector2[] pivots,sizes;
        }
        [Serializable] sealed class V2Segment
        {
            public string name,clock;public int frames,missingRenderedFrames;public float firstRealtime,lastRealtime,maxDelta,maxRealtimeGap,encodeSeconds;
        }
        [Serializable] sealed class V2Frame
        {
            public string file,sha256,scenario,clock,artId,motion,sourceId,phase,transition;
            public int frameCount,clipFrame,sourceFrame;public float realtime,delta,transitionSeconds,travel;
            public double combatMs;public float playerHp,enemyHp,playerBar,enemyBar;public Vector3 root,bodyOffset,bodyScale;
        }
        sealed class BufferedV2Frame { public Texture2D pixels;public V2Frame index; }
        [Serializable] sealed class V2Hp
        {
            public string file,role;public float value;public int capacity,expected,actual;public Rect bounds;
        }

        IEnumerator PlayerV2(bool hpOnly=false)
        {
            report.limits=hpOnly?"HP raster defect recheck only. No motion campaign, PlayMode tests or OS input. Compare 30 HP states across two resolutions separately from automated mesh assertions."
                :"Player-v2/HP changed scope only. Synthetic accepted commands, not OS input. Automated checks and frame collection do not constitute visual acceptance; inspect all sequences and HP images separately.";
            Require(Application.version=="0.9.1"&&catalog.dataVersion=="mvp1.14.0","v2 candidate version 0.9.1 / unchanged data mvp1.14.0");
            Require((Screen.width==1280&&Screen.height==720)||(Screen.width==640&&Screen.height==360),"v2 target resolution");
            if(!hpOnly)
            {
                Require(catalog.combat.attackWindupMs==200&&catalog.combat.attackIntervalMs==500&&catalog.combat.dodgeActiveMs==240&&catalog.combat.dodgeRecoveryMs==120&&catalog.combat.perfectWindowMs==90,"unchanged attack and defense windows");
                v2Index=new StreamWriter(new FileStream(Path.Combine(output,"frames.jsonl"),FileMode.CreateNew));
            }
            try
            {
                if(hpOnly)
                {
                    yield return V2Health();
                    Require(v2Evidence.hp.Count==15,"15 HP bar states at this resolution; HP-only defect recheck");
                    yield break;
                }
                yield return V2Prepare(1);
                var actor=p.PlayerActor;
                Require(actor.ArtId==catalog.GetCharacter(session.CurrentCharacterId).artId,"actual player matches selected catalog character");
                foreach(var motion in new[]{ActorMotion.Idle,ActorMotion.Run,ActorMotion.Attack,ActorMotion.DodgeUp,ActorMotion.DodgeBack,ActorMotion.DodgeDown})
                {
                    var clip=actor.GetClip(motion);
                    Require(clip!=null&&clip.frames.Length>1&&clip.frames.All(x=>x),"selected player clip exists: "+motion);
                    if(motion!=ActorMotion.Idle&&motion!=ActorMotion.Run)Require(clip.contactFrame>=0&&clip.contactFrame<clip.frames.Length,"selected clip contact/hold: "+motion);
                    v2Evidence.clips.Add(new V2Clip{artId=actor.ArtId,motion=motion.ToString(),sourceId=clip.sourceId,contactFrame=clip.contactFrame,
                        duration=clip.Duration,referenceHeight=clip.referenceHeightPixels,holds=clip.frameDurationsMs,sourceFrames=clip.sourceFrames,
                        sprites=clip.frames.Select(x=>x.name).ToArray(),pivots=clip.frames.Select(x=>x.pivot).ToArray(),sizes=clip.frames.Select(x=>x.rect.size).ToArray()});
                }
                var run=actor.GetClip(ActorMotion.Run);
                Require(run.frameDurationsMs.SequenceEqual(new[]{40,40,60,60,40,40,60,60})&&Mathf.Abs(run.Duration-.4f)<.00001f,"actual selected Run retains authored 400ms rhythm");
                // Idle's visual clock runs normally; hold the combat session so enemy hits cannot interrupt two full cycles.
                yield return V2Observe("idle",actor.GetClip(ActorMotion.Idle).Duration*2+.05f,false);
                foreach(int stage in new[]{1,10})yield return V2Attack(stage);
                for(int direction=0;direction<3;direction++)yield return V2Defense(direction);
                yield return V2Travel();
                yield return V2Health();
                Require(v2Evidence.hp.Count==15,"15 bar states at this resolution (30 across two cases)");
                Require(v2Evidence.writtenFrames==v2Evidence.segments.Sum(x=>x.frames),"every indexed motion frame written once");
                Require(Directory.GetFiles(output,"v2-frame-*.png").Length==v2Evidence.writtenFrames,"motion PNG count equals frame index count");
            }
            finally
            {
                if(p)p.TickSession=false;
                if(session!=null)session.EventRaised-=V2Event;
                ReleaseV2Frames();
                File.WriteAllText(Path.Combine(output,"v2-evidence.json"),JsonUtility.ToJson(v2Evidence,true));
            }
        }
        IEnumerator V2Prepare(int stage,bool weak=false)
        {
            if(session!=null)session.EventRaised-=V2Event;
            BattleFixture(stage,weak,false);v2Events.Clear();session.EventRaised+=V2Event;
            clockMode="fixture-settle";yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            Require(p.PlayerActor.ArtId==catalog.GetCharacter(session.CurrentCharacterId).artId,"fixture uses actual selected player at stage "+stage);
        }
        void V2Event(SessionEvent value)=>v2Events.Add(value);
        void V2Press(PointerCommand command)
        {
            Require(command&&command.isActiveAndEnabled&&command.Interactable,"v2 synthetic command is available");Press(command);
        }
        IEnumerator V2Attack(int stage)
        {
            yield return V2Prepare(stage);bool sent=false;float hp=session.EnemyHp;
            yield return V2Observe("attack-stage-"+stage.ToString("D2"),.85f,true,elapsed=>
            {if(!sent&&elapsed>=.15f){V2Press(p.View.Attack);sent=true;}});
            var starts=v2Events.Where(x=>x.Kind==SessionEventKind.AttackStarted).ToArray();
            var hits=v2Events.Where(x=>x.Kind==SessionEventKind.EnemyDamaged).ToArray();
            Require(starts.Length==1&&hits.Length==1&&Math.Abs(hits[0].TimeMs-starts[0].TimeMs-200)<.01,"one accepted attack, one damage event at 200ms: stage "+stage);
            Require(session.EnemyHp<hp,"attack damage reflected by session HP: stage "+stage);
            Require(Mathf.Approximately(p.View.EnemyHealthBar.Value,session.EnemyHp/session.EnemyMaxHp),"actual damage reflected by HP graphic: stage "+stage);
            Require(p.PlayerActor.CurrentMotion==ActorMotion.Idle,"attack recovers to Idle: stage "+stage);
        }
        IEnumerator V2Defense(int direction)
        {
            yield return V2Prepare(direction+1);bool sent=false;float hp=session.PlayerHp;
            var motion=direction==0?ActorMotion.DodgeUp:direction==1?ActorMotion.DodgeBack:ActorMotion.DodgeDown;
            bool sawMotion=false,sawHold=false,sawRecovery=false;
            yield return V2Observe("defense-"+motion,1.45f,true,elapsed=>
            {
                var threat=session.EnemyThreats.FirstOrDefault();
                if(!sent&&threat!=null&&threat.ImpactAtMs-session.ClockMs<=80)
                {
                    Require(threat.Direction==(DodgeDirection)direction,"fixture's actual danger matches requested defense");
                    V2Press(p.View.Dodge[direction]);sent=true;
                }
                if(p.PlayerActor.CurrentMotion==motion)
                {sawMotion=true;int hold=p.PlayerActor.GetClip(motion).contactFrame;sawHold|=p.PlayerActor.CurrentFrame==hold;sawRecovery|=p.PlayerActor.CurrentFrame>hold;}
            });
            Require(sent&&v2Events.Count(x=>x.Kind==SessionEventKind.DodgeStarted||x.Kind==SessionEventKind.ParryStarted)==1,"one accepted defense: "+motion);
            Require(v2Events.Count(x=>x.Kind==SessionEventKind.PerfectDodge||x.Kind==SessionEventKind.PerfectParry)==1&&session.PlayerHp==hp,"perfect defense preserves HP: "+motion);
            Require(sawMotion&&sawHold&&sawRecovery,"selected defense clip, authored contact/hold and later recovery were rendered: "+motion);
            Require(p.PlayerActor.CurrentMotion==ActorMotion.Idle&&Vector3.Distance(p.PlayerActor.transform.localPosition,ParallaxWorld.PlanPoint(500,590))<.001f,"defense returns to Idle/home: "+motion);
        }
        IEnumerator V2Travel()
        {
            yield return V2Prepare(1,true);bool sent=false,runValid=true;int samples=0;float first=-1,last=-1;
            float homeX=ParallaxWorld.PlanPoint(500,590).x;
            yield return V2Observe("defeat-travel-arrival",4.05f,true,elapsed=>
            {
                if(!sent&&elapsed>=.15f){V2Press(p.View.Attack);sent=true;}
                var tr=session.Transition;if(tr==null||tr.Kind!=TransitionKind.Travel)return;
                var actor=p.PlayerActor;var clip=actor.GetClip(ActorMotion.Run);samples++;
                if(first<0)first=tr.ElapsedSeconds;last=tr.ElapsedSeconds;
                runValid&=Mathf.Abs(actor.transform.localPosition.x-homeX)<.0001f&&actor.CurrentMotion==ActorMotion.Run
                    &&actor.CurrentFrame==clip.FrameIndex(tr.ElapsedSeconds)&&Mathf.Abs(tr.DurationSeconds-2)<.0001f
                    &&Mathf.Abs(p.Presentation.TravelPixels-tr.ElapsedSeconds*960)<.1f;
            });
            Require(samples>1&&last-first>=.8f&&runValid,"actual Run: at least two 400ms cycles, fixed root X, 960 logical px/s for two seconds");
            Require(session.Stage==2&&session.Phase==SessionPhase.Combat&&Mathf.Abs(p.Presentation.TravelPixels-1920)<.1f,"travel completes at 1920 logical pixels and returns to Combat");
        }

        IEnumerator V2Observe(string label,float seconds,bool realtime,Action<float> afterFrame=null)
        {
            scenario=label;clockMode=realtime?"real-frame-v2":"real-frame-idle-static-session";p.TickSession=realtime;
            var segment=new V2Segment{name=label,clock=clockMode};v2Evidence.segments.Add(segment);
            float start=Time.realtimeSinceStartup;int previous=-1;float previousTime=0;
            try
            {
                while(Time.realtimeSinceStartup-start<seconds)
                {
                    yield return new WaitForEndOfFrame();
                    var actor=p.PlayerActor;var clip=actor.GetClip(actor.CurrentMotion);var tr=session.Transition;
                    if(clip==null||actor.CurrentFrame<0||actor.CurrentFrame>=clip.frames.Length||actor.Body.sprite!=clip.frames[actor.CurrentFrame])
                        throw new InvalidOperationException("Displayed sprite is outside the selected actor clip: "+label);
                    float now=Time.realtimeSinceStartup;
                    if(previous>=0)
                    {
                        segment.missingRenderedFrames+=Mathf.Max(0,Time.frameCount-previous-1);
                        if(Time.frameCount<=previous)throw new InvalidOperationException("Duplicate rendered frame in "+label);
                        segment.maxRealtimeGap=Mathf.Max(segment.maxRealtimeGap,now-previousTime);
                    }
                    else segment.firstRealtime=now;
                    previous=Time.frameCount;previousTime=now;segment.lastRealtime=now;
                    segment.maxDelta=Mathf.Max(segment.maxDelta,Time.unscaledDeltaTime);
                    // Bound native capture memory; do not silently drop frames or change game timing on overload.
                    if((long)(v2Frames.Count+1)*Screen.width*Screen.height*3>768L*1024*1024)throw new InvalidOperationException("Continuous capture exceeds 768 MiB raw budget; no frames discarded silently.");
                    var index=new V2Frame{file="v2-frame-"+Screen.width+"x"+Screen.height+"-"+label+"-"+actor.CurrentMotion+"-"+(captureSequence++).ToString("D6")+".png",
                        scenario=label,clock=clockMode,artId=actor.ArtId,motion=actor.CurrentMotion.ToString(),sourceId=clip?.sourceId,
                        frameCount=Time.frameCount,clipFrame=actor.CurrentFrame,sourceFrame=clip!=null&&clip.sourceFrames.Length>actor.CurrentFrame?clip.sourceFrames[actor.CurrentFrame]:-1,
                        realtime=now,delta=Time.unscaledDeltaTime,combatMs=session.ClockMs,phase=session.Phase.ToString(),transition=tr?.Kind.ToString(),
                        transitionSeconds=tr?.ElapsedSeconds??0,travel=p.Presentation.TravelPixels,playerHp=session.PlayerHp,enemyHp=session.EnemyHp,playerBar=p.View.PlayerHealthBar.Value,enemyBar=p.View.EnemyHealthBar.Value,
                        root=actor.transform.localPosition,bodyOffset=actor.Body.transform.localPosition,bodyScale=actor.Body.transform.localScale};
                    v2Frames.Add(new BufferedV2Frame{pixels=CapturePixels(),index=index});segment.frames++;Trace();afterFrame?.Invoke(now-start);
                }
                Require(segment.frames>1&&segment.missingRenderedFrames==0,"continuous rendered-frame sequence: "+label);
            }
            finally
            {
                // Encoding time is outside the recorded interval. The next scenario gets a fresh fixture and settles first.
                p.TickSession=false;float begin=Time.realtimeSinceStartup;
                try
                {
                    foreach(var frame in v2Frames)
                    {
                        string path=Path.Combine(output,frame.index.file);var bytes=frame.pixels.EncodeToPNG();
                        using(var hash=SHA256.Create())frame.index.sha256=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
                        using(var stream=new FileStream(path,FileMode.CreateNew))stream.Write(bytes,0,bytes.Length);
                        v2Index.WriteLine(JsonUtility.ToJson(frame.index));v2Evidence.writtenFrames++;
                    }
                    v2Index.Flush();timeline.Flush();events.Flush();
                }
                finally
                {
                    foreach(var frame in v2Frames)if(frame.pixels)Destroy(frame.pixels);v2Frames.Clear();
                    segment.encodeSeconds=Time.realtimeSinceStartup-begin;
                }
            }
        }
        IEnumerator V2Health()
        {
            float[] values={0,.000001f,.25f,.5f,1};string[] labels={"zero","tiny","25","50","100"};
            foreach(int stage in new[]{1,10})
            {
                yield return V2Prepare(stage);scenario="hp-stage-"+stage;clockMode="display-only-hp-fixture";
                var bars=stage==1?new[]{p.View.PlayerHealthBar,p.View.EnemyHealthBar}:new[]{p.View.EnemyHealthBar};
                var centers=bars.Select(x=>ScreenBounds(x.rectTransform).center).ToArray();
                for(int i=0;i<values.Length;i++)
                {
                    yield return new WaitForEndOfFrame();
                    string name="v2-hp-"+Screen.width+"x"+Screen.height+"-stage-"+stage+"-"+labels[i];
                    var originals=bars.Select(x=>x.Value).ToArray();
                    try
                    {
                        foreach(var bar in bars)bar.SetValue(values[i]);Canvas.ForceUpdateCanvases();
                        for(int b=0;b<bars.Length;b++)
                        {
                            var bar=bars[b];string role=bar==p.View.PlayerHealthBar?"player":stage==10?"boss":"enemy";
                            int capacity=role=="player"?26:role=="boss"?50:31;int width=role=="player"?160:role=="boss"?280:184;
                            int expected=values[i]<=0?0:Mathf.Max(1,Mathf.CeilToInt(values[i]*capacity));
                            var bounds=ScreenBounds(bar.rectTransform);float scale=Screen.height/720f;
                            Require(bar.DragonTheme&&Mathf.Abs(bounds.width-width*scale)<1&&Mathf.Abs(bounds.height-36*scale)<1&&Vector2.Distance(bounds.center,centers[b])<.01f,"HP size/center: "+role+" "+labels[i]);
                            // Unity 6000.3 returns the renderer's current mesh; QA only reads it.
                            var mesh=bar.canvasRenderer.GetMesh();Require(mesh,"HP renderer mesh exists");
                            Color32 bright=bar.Enemy?DragonUiPalette.DangerLight:DragonUiPalette.SuccessLight;
                            int vertices=mesh.colors32.Count(c=>c.Equals(bright));Require(vertices%4==0,"HP highlight quad count");int actual=vertices/4;
                            Require(actual==expected&&HealthBarGraphic.FilledCells(values[i],capacity)==expected,"rendered HP columns: "+role+" "+labels[i]);
                            v2Evidence.hp.Add(new V2Hp{file=name+".png",role=role,value=values[i],capacity=capacity,expected=expected,actual=actual,bounds=bounds});
                        }
                        CaptureNow(name);WriteLayout(name);
                    }
                    finally{for(int b=0;b<bars.Length;b++)bars[b].SetValue(originals[b]);}
                }
            }
        }
        void ReleaseV2Frames()
        {
            foreach(var frame in v2Frames)if(frame.pixels)Destroy(frame.pixels);v2Frames.Clear();
            v2Index?.Dispose();v2Index=null;
        }
    }
}
#endif
