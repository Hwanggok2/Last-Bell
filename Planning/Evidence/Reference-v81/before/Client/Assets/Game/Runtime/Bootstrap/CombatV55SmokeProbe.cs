#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace LastBell
{
    // Opt-in validation of the stored scene in the real player. Never installed in ordinary play.
    public sealed class CombatV55SmokeProbe : MonoBehaviour
    {
        string output;MvpPresenter presenter;GameCatalog catalog;GameSession session;bool failed;
        readonly List<string> checks=new List<string>();int peakParticles;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-combat-v55-smoke")
            {var p=new GameObject("Combat v5.5 verification").AddComponent<CombatV55SmokeProbe>();p.output=Path.GetFullPath(args[i+1]);break;}
        }
        GameSession Playing(int stage,int stagger=0,bool immune=false)
        {
            var save=new GameSession(catalog).ExportSave();save.tutorialStep=2;save.phase=SessionPhase.Lobby;save.run=null;
            var s=new GameSession(catalog,save);s.StartRun();save=s.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;
            save.run.stage=stage;save.run.stagger=stagger;save.run.immunityUntilMs=immune?100000:-1;
            save.run.enemyMaxHpMilli=save.run.enemyHpMilli=catalog.GetStage(stage).maxHp*1000L;
            s=new GameSession(catalog,save);s.SetPaused(false);presenter.Bind(s);presenter.TickSession=false;session=s;return s;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;Application.targetFrameRate=60;
            yield return null;var bootstrap=FindFirstObjectByType<GameBootstrap>();presenter=bootstrap.Presenter;catalog=bootstrap.Session.Catalog;
            Check(presenter.Presentation.EnemyEffects&&presenter.Presentation.Telegraphs,"stored combat references");
            presenter.View.ReducedShake.isOn=false;presenter.Effects.ReducedShake=false;
            Check(presenter.Presentation.EnemyEffects.ParticleCapacity==96,"pool capacity 96");
            for(int stage=1;stage<=4;stage++)
            {
                Playing(stage);yield return Capture("shape-"+stage+"-bright");
                Check(!presenter.View.Timing.gameObject.activeSelf&&!presenter.View.Danger.gameObject.activeSelf,"removed gauge and old cue "+stage);
                session.Tick(.13f);yield return Capture("shape-"+stage+"-dim");
                session.Tick(.65f);yield return Capture("shape-"+stage+"-release");
                session.Tick(.06f);yield return Capture("shape-"+stage+"-flight");
                var release=presenter.Presentation.EnemyEffects.Releases[0];var trail=release.Trails[1];var mesh=new Mesh();release.WeaponRibbons[1].BakeMesh(mesh,presenter.WorldCamera,true);
                Check(trail.positionCount>=20&&mesh.vertexCount>=40,"sampled weapon ribbon and trail "+stage);
                float area=0;var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int k=0;k<indices.Length;k+=3)area+=Vector3.Cross(vertices[indices[k+1]]-vertices[indices[k]],vertices[indices[k+2]]-vertices[indices[k]]).magnitude*.5f;
                Check(area>.001f,"weapon ribbon has visible triangle area "+stage);
                Debug.Log("COMBAT_TRAIL_BOUNDS: "+stage+" "+mesh.bounds.size+" width="+trail.widthMultiplier+" curve="+trail.widthCurve.Evaluate(0)+" count="+trail.positionCount+" first="+trail.GetPosition(0)+" last="+trail.GetPosition(trail.positionCount-1)+" age="+(session.ClockMs-presenter.Presentation.EnemyEffects.Releases[0].StartedMs));Destroy(mesh);
                session.Tick(.01f);Press(presenter.View.Dodge[(int)session.EnemyIntent.Direction]);session.Tick(.05f);
                Check(session.PlayerHp==100&&session.Stagger==40,"confirmed geometric perfect "+stage);
                yield return Capture("shape-"+stage+"-impact");
                session.Tick(.1f);yield return Capture("shape-"+stage+"-wave");
            }
            // Exact fast front/rear chain. Every frame comes from the Unity camera and saved UI.
            Playing(9,0,true);var events=new List<SessionEvent>();session.EventRaised+=events.Add;
            double first=0,second=0;foreach(var t in session.EnemyThreats){if(first==0)first=t.ImpactAtMs;else second=t.ImpactAtMs;}
            Check(first==1180&&second==1500,"fast combo deadlines 1180 / 1500");
            for(int ms=0;ms<=2100;ms+=10)
            {
                if(ms>0)session.Tick(.01f);
                if(ms==1130||ms==1450)Press(presenter.View.Dodge[(int)session.EnemyIntent.Direction]);
                if(ms==1590)
                {
                    Check(session.CanPerfectCounter,"counter ready after second perfect");Press(presenter.View.Attack);
                    Check(!session.CanPerfectCounter,"accepted attack consumes counter");
                    Check((presenter.PlayerActor.transform.localPosition-ParallaxWorld.PlanPoint(500,590)).sqrMagnitude<.00001f,"counter snaps at input");
                }
                if(ms%20==0)yield return Capture("combo-"+(ms/20).ToString("D3"));
                peakParticles=Math.Max(peakParticles,presenter.Presentation.EnemyEffects.ActiveParticles);
                CheckQuiet(peakParticles<=96,"particle cap");
            }
            var cues=events.FindAll(e=>e.Kind==SessionEventKind.EnemyCueStarted);
            var hits=events.FindAll(e=>e.Kind==SessionEventKind.PerfectDodge);
            Check(cues.Count==2&&cues[0].TimeMs==0&&cues[1].TimeMs==530,"all warnings before strikes, no repeated cue");
            Check(hits.Count==2&&hits[0].TimeMs==1180&&hits[1].TimeMs==1500,"two separate confirmed perfects");
            Check(events.FindAll(e=>e.Kind==SessionEventKind.EnemyDamaged).Count==1,"one counter contact, no extra hit");
            Playing(1);session.Tick(.85f);session.Dodge(DodgeDirection.Up);session.Tick(.05f);session.Tick(.1f);yield return Capture("wave-center");
            var wave=presenter.View.DodgeResult;
            var center=RectTransformUtility.WorldToScreenPoint(null,wave.rectTransform.TransformPoint(wave.WaveCenter));
            Check(Vector2.Distance(center,new Vector2(Screen.width,Screen.height)*.5f)<1,"wave at viewport center");
            Check(wave.MaximumRadius>wave.rectTransform.rect.size.magnitude*.5f,"wave clears every corner");
            session.SetPaused(true);double clock=session.ClockMs;int count=presenter.Presentation.EnemyEffects.ActiveParticles;
            yield return Capture("pause-before");yield return new WaitForSecondsRealtime(.3f);yield return Capture("pause-after");
            Check(session.ClockMs==clock&&presenter.Presentation.EnemyEffects.ActiveParticles==count,"pause freezes core and particles");
            session.SetPaused(false);session.Tick(.25f);yield return Capture("wave-beyond-edges");
            presenter.View.ReducedShake.isOn=true;presenter.Effects.ReducedShake=true;
            Playing(1);session.Tick(.85f);session.Dodge(DodgeDirection.Up);session.Tick(.05f);session.Tick(.1f);yield return Capture("reduced-static-wave");
            Check(presenter.Presentation.EnemyEffects.ActiveParticles==0,"reduced motion suppresses particles");
            Check(presenter.Effects.WorldOffset.sqrMagnitude<.000001f,"reduced motion has no shake");
            presenter.View.ReducedShake.isOn=false;presenter.Effects.ReducedShake=false;
            Playing(5,80);session.Tick(1.63f);session.Dodge(DodgeDirection.Up);session.Tick(.05f);yield return Capture("stun-cancels-combo");
            Check(session.IsEnemyStunned&&session.EnemyThreats.Length==0,"stun cancels queued attacks");
            Check(presenter.Art.Find("hero_on").FindClip(ActorMotion.Run).sourceId=="hero-approved-dash-20261021-02","approved run preserved");
            if(failed)throw new InvalidOperationException("Combat v5.5 smoke logged an error");
            File.WriteAllText(Path.Combine(output,"combat-result.json"),"{\"passed\":true,\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"peakParticles\":"+peakParticles+",\"comboFrames\":106,\"frameMs\":20,\"checks\":[\""+string.Join("\",\"",checks)+"\"]}");
            Debug.Log("LAST_BELL_COMBAT_V55_SMOKE_PASSED");Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            yield return null;yield return null;
            var camera=presenter.WorldCamera;var canvas=presenter.View.GetComponent<Canvas>();
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;int sort=canvas.sortingOrder;
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float aspect=camera.aspect;
            var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);target.Create();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
            camera.targetTexture=target;camera.aspect=(float)Screen.width/Screen.height;Canvas.ForceUpdateCanvases();yield return null;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);
            camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;canvas.sortingOrder=sort;
            target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
        }
        static void Press(PointerCommand command){var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};command.OnPointerDown(e);command.OnPointerUp(e);}
        void Check(bool value,string text){CheckQuiet(value,text);checks.Add(text);Debug.Log("COMBAT_V55_CHECK: "+text);}
        void CheckQuiet(bool value,string text){if(!value)throw new InvalidOperationException("COMBAT_V55_FAILED: "+text);}
        void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception){failed=true;File.WriteAllText(Path.Combine(output,"error.txt"),message+"\n"+trace);}}
    }
}
#endif
