#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastBell
{
    // Explicit verification switch only; normal game startup never adds this probe.
    public sealed class RouteV4SmokeProbe : MonoBehaviour
    {
        string output;MvpPresenter presenter;GameSession session;GameCatalog catalog;
        readonly List<string> checks=new List<string>();bool failed,finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-route-v4-smoke")
            {var p=new GameObject("Route v4 verification").AddComponent<RouteV4SmokeProbe>();p.output=Path.GetFullPath(args[i+1]);break;}
        }
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        void Bind(GameSession value){session=value;presenter.Bind(value);presenter.TickSession=false;}
        GameSession Arrival(int stage,TransitionKind kind=TransitionKind.Arrival,float progress=0)
        {
            var s=new GameSession(catalog);s.FinishPrologue();var save=s.ExportSave();save.tutorialStep=2;
            save.run.stage=stage;save.run.enemyHpMilli=save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;
            save.phase=SessionPhase.Transition;int duration=kind==TransitionKind.Travel?2000:700;
            save.run.transition=new TransitionData{kind=kind,fromStage=Math.Max(1,stage-1),toStage=stage,durationMs=duration,elapsedMs=duration*progress};
            return new GameSession(catalog,save);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;Application.targetFrameRate=60;
            yield return null;var bootstrap=FindFirstObjectByType<GameBootstrap>();presenter=bootstrap.Presenter;catalog=bootstrap.Session.Catalog;
            Bind(new GameSession(catalog));session.SetPrologueManual(false);
            Check(presenter.View.Prologue&&presenter.View.ReplayPrologue,"saved prologue controls and replay button");
            Check(session.Phase==SessionPhase.Prologue,"fresh save starts prologue");session.Tick(5);
            yield return Capture("01-prologue-scroll");
            presenter.View.Prologue.Pause.onClick.Invoke();float progress=session.PrologueProgress;session.Tick(5);
            Check(session.PrologueProgress==progress&&session.ClockMs==0,"pause freezes scroll and combat clock");
            session.SetPrologueManual(true);yield return Capture("02-prologue-reading");
            presenter.View.Prologue.Skip.onClick.Invoke();Check(session.Phase==SessionPhase.TutorialDodge&&session.ExportSave().prologueSeen,"skip UI saves seen and starts existing tutorial");
            yield return Capture("03-stage1-sentry");
            Bind(Arrival(5));session.Tick(10);yield return Capture("04-stage5-central-hint");
            Check(session.Phase==SessionPhase.ComboHint&&presenter.View.ModalPanel.activeSelf&&!presenter.View.ModalCancel.gameObject.activeSelf,"Stage5 shows centered confirm-only panel");
            double clock=session.ClockMs;session.Tick(60);Check(session.ClockMs==clock&&session.EnemyThreats.Length==0,"hint blocks combat without spawning warnings");
            presenter.View.ModalConfirm.onClick.Invoke();yield return Capture("05-stage5-confirmed");
            Check(session.Phase==SessionPhase.Combat&&session.EnemyThreats.Length==2&&session.EnemyThreats[0].ImpactAtMs==clock+1440,"confirmation immediately starts full regular combo");
            Check(presenter.EnemyActor.ArtId=="gate_lock","Stage5 reviewed gate lock");
            session.SetPaused(true);presenter.EnemyActor.FlashHit();
            yield return Capture("05a-enemy-white-flash",true);
            Check(presenter.EnemyActor.HitFlashAmount==1&&!presenter.EnemyActor.IsHitPlaying,"white flash without recoil; pause preserves flash");
            session.SetPaused(false);yield return new WaitForSecondsRealtime(.2f);
            yield return Capture("05b-enemy-color-restored");
            Check(presenter.EnemyActor.HitFlashAmount==0,"enemy returns to original colors after 120ms");
            Bind(Arrival(9));session.Tick(10);yield return Capture("06-stage9-no-gate");
            Check(!presenter.Presentation.World.Gate.gameObject.activeSelf,"gate absent through Stage9");
            Bind(Arrival(10,TransitionKind.Travel,.8f));yield return Capture("07-gate-enters-on-ground");
            var gate=presenter.Presentation.World.Gate;Check(gate.gameObject.activeSelf,"gate appears only while traveling into Stage10");
            Check(Mathf.Abs(gate.transform.localPosition.y-ParallaxWorld.PlanPoint(0,590).y)<.0001f,"gate foot stays at actor floor");
            session.Tick(10);yield return Capture("08-stage10-guardian");Check(session.Stage==10&&presenter.EnemyActor.ArtId=="bell_knight_concept","Stage10 reviewed guardian art");
            var save=session.ExportSave();save.run.enemyHpMilli=1000;save.run.enemyHitAtMs=-1;save.phase=SessionPhase.Combat;
            Bind(new GameSession(catalog,save));session.SetPaused(false);session.Attack();session.Tick(.2f);session.Tick(1);
            yield return Capture("09-gate-open-story");Check(presenter.Presentation.World.Gate.sprite==presenter.Presentation.World.GateOpen,"gate opens after guardian defeat");
            string[] ids={"gate_sentinel","chain_thrall","ash_wisp","gate_warden","bell_knight"};
            foreach(var id in ids)
            {
                var art=presenter.Art.Find(id);Check(art!=null,"catalog actor "+id);
                foreach(var motion in new[]{ActorMotion.Idle,ActorMotion.Attack,ActorMotion.Stun})
                    Check(art.FindClip(motion)!=null&&art.FindClip(motion).frames.Length>0,"preserved original "+id+"/"+motion);
                Check(art.FindClip(ActorMotion.Hit)==null&&art.FindClip(ActorMotion.Death)==null,"no enemy recoil or death clip: "+id);
            }
            Check(presenter.Art.actors.Length==16,"hero, original five and reviewed ten monsters in runtime catalog");
            Check(catalog.mvpLastStage==10,"prototype still ends at Stage10");
            session.SkipStory();session.ChooseRelic(session.RelicChoices[0]);Check(session.Phase==SessionPhase.MvpComplete,"boss reward ends prototype");
            Check(session.RetryRun()&&session.Stage==1&&session.Phase!=SessionPhase.Prologue,"retry starts Stage1 without repeated prologue");
            session.ReturnToLobby();yield return Capture("10-lobby");presenter.View.ReplayPrologue.onClick.Invoke();Check(session.Phase==SessionPhase.Prologue,"lobby prologue replay");
            session.FinishPrologue();Check(session.Phase==SessionPhase.Lobby,"replay returns to lobby without starting run");
            yield return VerifyReviewedAssets();
            if(failed)throw new InvalidOperationException("Smoke logged a runtime error.");
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"checks\":[\""+string.Join("\",\"",checks)+"\"]}");
            finished=true;Debug.Log("LAST_BELL_ROUTE_V4_SMOKE_PASSED");Application.Quit(0);
        }
        IEnumerator VerifyReviewedAssets()
        {
            Bind(Arrival(1));session.Tick(10);yield return null;
            presenter.enabled=false;
            var actor=presenter.EnemyActor;
            string[] fresh={"gate_sentry","bell_hound","cracked_blade","gate_lock","bell_knight_concept","chain_puppet","hook_hound","chain_dancer","pendulum_warden","chain_jailer"};
            foreach(string id in fresh)
            {
                var art=presenter.Art.Find(id);Check(art!=null,"reviewed actor "+id);
                actor.ApplyArt(art,false);actor.SetPlaybackPaused(true);
                var attack=art.FindClip(ActorMotion.Attack);var stun=art.FindClip(ActorMotion.Stun);
                Check(attack.HasAuthoredTiming&&stun.HasAuthoredTiming,"authored attack and stun holds "+id);
                Check(!actor.Body.flipX,"reviewed left-facing source "+id);
                for(int frame=0;frame<attack.frames.Length;frame++)
                {
                    actor.SampleClipFrame(ActorMotion.Attack,frame);
                    Check(Vector3.Distance(actor.SourcePixelToWorld(new Vector2(78,114)),actor.transform.position)<.0001f,"fixed ground pivot "+id+" frame "+frame);
                    if(id=="hook_hound")yield return Capture("hook-hound-frame-"+frame.ToString("00"));
                }
                actor.SampleClipFrame(ActorMotion.Attack,attack.contactFrame);yield return Capture("fresh-"+id+"-contact");
                actor.SampleClipFrame(ActorMotion.Stun,0);yield return Capture("fresh-"+id+"-stun");
            }
            var audio=presenter.Presentation.Audio;audio.SetPaused(false);audio.Muted=false;
            Check(audio.Swing.name=="sword-clash"&&audio.Hit.name=="hit-sharp","latest sword clash and metal hit are linked");
            Check(audio.LongBell.name=="bell-low-long"&&audio.LongBell.length>5,"long low bell keeps full decay");
            Check(audio.Windup.name=="cue-low"&&audio.Critical==audio.Windup,"short low bell for repeated cues");
            Check(audio.QuarterMusic&&audio.QuarterMusic.channels==2,"future quarter music stored in stereo");
            foreach(var mood in new[]{MusicMood.Battle,MusicMood.Boss,MusicMood.Lobby})
            {
                audio.SetMood(mood);yield return new WaitForSecondsRealtime(.4f);
                Check(audio.Music.isPlaying&&audio.Music.time>0&&audio.Music.clip.channels==2,"native stereo music playback "+mood);
            }
            audio.Play(audio.Swing,.75f);yield return new WaitForSecondsRealtime(.08f);
            Check(audio.Sfx.isPlaying,"native sword collision playback");
            audio.SetPaused(true);Check(!audio.Music.isPlaying,"pause stops music transport");
        }
        IEnumerator Capture(string name,bool hidePausePanel=false)
        {
            yield return null;yield return null;
            var camera=presenter.WorldCamera;var canvas=presenter.View.GetComponent<Canvas>();
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;int sort=canvas.sortingOrder;
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float aspect=camera.aspect;
            var target=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);target.Create();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
            camera.targetTexture=target;camera.aspect=960f/540;Canvas.ForceUpdateCanvases();yield return null;
            // Freeze the 45ms flash for inspection, hiding only the obstructing pause menu.
            bool wasPauseVisible=presenter.View.PausePanel.activeSelf;
            if(hidePausePanel)presenter.View.PausePanel.SetActive(false);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(960,540,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);
            if(hidePausePanel)presenter.View.PausePanel.SetActive(wasPauseVisible);
            camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;canvas.sortingOrder=sort;
            target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
        }
        void OnLog(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception){failed=true;File.WriteAllText(Path.Combine(output,"error.txt"),message+"\n"+trace);if(!finished)Application.Quit(1);}}
    }
}
#endif
