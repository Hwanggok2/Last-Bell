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
    public sealed class CombatV59SmokeProbe : MonoBehaviour
    {
        string output;MvpPresenter presenter;GameCatalog catalog;GameSession session;bool failed;
        readonly List<string> checks=new List<string>();int peakParticles;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-combat-v59-smoke")
            {var p=new GameObject("Combat v5.9 verification").AddComponent<CombatV59SmokeProbe>();p.output=Path.GetFullPath(args[i+1]);break;}
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
            Check(presenter.View.Rhythm&&presenter.Presentation.Telegraphs,"saved rhythm and telegraph references");
            presenter.View.ReducedShake.isOn=false;presenter.Effects.ReducedShake=false;
            for(int stage=1;stage<=4;stage++)
            {
                Playing(stage);yield return Capture("shape-"+stage+"-blink1");
                Check(!presenter.View.Dodge[2].gameObject.activeSelf&&!presenter.View.Timing.gameObject.activeSelf,"no crouch or overhead gauge "+stage);
                session.Tick(.18f);yield return Capture("shape-"+stage+"-blink2");
                session.Tick(.18f);yield return Capture("shape-"+stage+"-blink3");
                session.Tick(.13f);yield return Capture("shape-"+stage+"-prepare");
                if(stage==3)Press(presenter.View.Attack);else Press(presenter.View.Dodge[(int)session.EnemyIntent.Direction]);
                session.TickInputFrame(.05f);yield return Capture("shape-"+stage+"-contact");
                Check(session.IsEnemyImpactPending&&System.Math.Abs(session.ClockMs-540)<.001,"contact exactly540 before resolution "+stage);
                Check(System.Math.Abs(presenter.View.Rhythm.WaveProgress-1)<.001,"wave meets at contact "+stage);
                session.TickInputFrame(0);yield return Capture("shape-"+stage+"-impact");
                Check(session.PlayerHp==100&&session.Stagger==40,"perfect defense via actual UI "+stage);
                if(stage==3)Check(presenter.PlayerActor.transform.localPosition==ParallaxWorld.PlanPoint(500,590),"parry remains standing at home");
                session.Tick(.1f);yield return Capture("shape-"+stage+"-wave");
            }
            Playing(9,0,true);Check(session.EnemyThreats[0].ImpactAtMs==960&&session.EnemyThreats[1].ImpactAtMs==1280,"fast chain960 /1280");
            var events=new List<SessionEvent>();session.EventRaised+=events.Add;
            for(int ms=0;ms<=1880;ms+=10)
            {
                if(ms>0)session.Tick(.01f);
                if(ms==910||ms==1230){yield return null;Press(presenter.View.Dodge[(int)session.EnemyIntent.Direction]);}
                if(ms==1370){yield return null;Press(presenter.View.Attack);Check(!session.CanPerfectCounter,"counter consumes only accepted input");Check(presenter.PlayerActor.transform.localPosition==ParallaxWorld.PlanPoint(500,590),"counter snaps immediately");}
                if(ms%20==0)yield return Capture("combo-"+(ms/20).ToString("D3"));
                peakParticles=System.Math.Max(peakParticles,presenter.Presentation.EnemyEffects.ActiveParticles);
            }
            Check(events.FindAll(e=>e.Kind==SessionEventKind.PerfectDodge).Count==2,"two independent perfect dodges");
            Check(events.FindAll(e=>e.Kind==SessionEventKind.EnemyDamaged).Count==1,"counter deals one hit");
            Check(peakParticles<=96,"particle cap96");
            Playing(10,0,true);yield return Capture("three-bells");Check(presenter.View.Rhythm.BellCount==3,"three upcoming attacks three bells");
            session.Tick(1.39f);yield return null;Press(presenter.View.Dodge[0]);session.Tick(.05f);yield return Capture("two-bells");Check(presenter.View.Rhythm.BellCount==2,"one resolved bell disappears without recentering");
            session.Tick(.27f);yield return null;Press(presenter.View.Attack);session.Tick(.05f);yield return Capture("mixed-parry");Check(session.PlayerHp==100,"dodge then parry mixed chain");
            Playing(3);session.Tick(.42f);yield return null;Press(presenter.View.Attack);session.Tick(.12f);yield return Capture("normal-parry");
            Check(!session.CanPerfectCounter&&presenter.View.DodgeResult.Label.text=="쳐냄","ordinary parry has no perfect counter");
            Playing(3);session.Tick(.49f);yield return null;Press(presenter.View.Dodge[1]);session.Tick(.05f);yield return Capture("wrong-dodge");Check(session.PlayerHp<100,"high punishes dodge within gold footprint");
            Playing(2);session.Tick(.49f);yield return null;Press(presenter.View.Dodge[1]);session.Tick(.025f);yield return Capture("back-entry");
            Check(System.Math.Abs(session.DodgeVisualAmount-.875)<.001,"50ms eased dodge entry");session.Tick(.025f);session.Tick(.165f);yield return Capture("back-return");
            Check(System.Math.Abs(session.DodgeVisualAmount-.125)<.001,"130ms eased return");
            session.Tick(.065f);yield return null;Press(presenter.View.Attack);session.Tick(.06f);yield return Capture("attack-approach");
            Check(presenter.PlayerActor.CurrentMotion!=ActorMotion.Run,"attack approach uses prepared slash not run");
            session.Tick(.06f);yield return Capture("attack-endpoint");Check(System.Math.Abs(session.PlayerCombatBody.X-518)<.01,"attack165px endpoint");
            Playing(2);session.Tick(.36f);yield return Capture("pause-before");session.SetPaused(true);double clock=session.ClockMs;yield return new WaitForSecondsRealtime(.25f);yield return Capture("pause-after");Check(session.ClockMs==clock,"pause freezes wave clock");
            presenter.View.ReducedShake.isOn=true;presenter.Effects.ReducedShake=true;Playing(2);session.Tick(.36f);yield return Capture("reduced-wave");
            Check(presenter.View.Rhythm.WaveProgress>0&&presenter.Presentation.EnemyEffects.ActiveParticles==0,"reduced motion keeps essential circle without particles");
            presenter.View.ReducedShake.isOn=false;presenter.Effects.ReducedShake=false;
            session.ReturnToLobby();yield return Capture("lobby-clears-rhythm");Check(presenter.View.Rhythm.BellCount==0,"lobby clears rhythm overlay");
            Check(presenter.Art.Find("hero_on").FindClip(ActorMotion.Run).sourceId=="hero-approved-dash-20261021-02","approved travel run preserved");
            if(failed)throw new InvalidOperationException("Combat v5.9 smoke logged an error");
            File.WriteAllText(Path.Combine(output,"combat-result.json"),"{\"passed\":true,\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"peakParticles\":"+peakParticles+",\"comboFrames\":95,\"frameMs\":20,\"checks\":[\""+string.Join("\",\"",checks)+"\"]}");
            Debug.Log("LAST_BELL_COMBAT_V59_SMOKE_PASSED");Application.Quit(0);
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
        void Check(bool value,string text){CheckQuiet(value,text);checks.Add(text);Debug.Log("COMBAT_V59_CHECK: "+text);}
        void CheckQuiet(bool value,string text){if(!value)throw new InvalidOperationException("COMBAT_V59_FAILED: "+text);}
        void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception){failed=true;File.WriteAllText(Path.Combine(output,"error.txt"),message+"\n"+trace);}}
    }
}
#endif
