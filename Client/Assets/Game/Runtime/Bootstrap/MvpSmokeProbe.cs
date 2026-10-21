#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastBell
{
    // Opt-in development player verification. Normal launches never create this component.
    public sealed class MvpSmokeProbe : MonoBehaviour
    {
        GameBootstrap bootstrap;
        MvpPresenter presenter;
        GameSession session;
        string output;
        int perfect,normal,critical,hits,rewards;
        bool failed,finished;
        bool resumeOnly,pendingCritical,criticalCaptured,stunCaptured;
        int pendingDodge=-1;
        readonly bool[] dodgeCaptured=new bool[4];
        float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-smoke"||args[i]=="-lastbell-resume-smoke")
            {
                var probe=new GameObject("Development smoke probe").AddComponent<MvpSmokeProbe>();
                probe.output=Path.GetFullPath(args[i+1]);probe.resumeOnly=args[i]=="-lastbell-resume-smoke";break;
            }
        }

        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;
            Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;
            yield return null;
            bootstrap=FindFirstObjectByType<GameBootstrap>();
            Require(bootstrap&&bootstrap.Session!=null,"Bootstrap/session ready");
            presenter=bootstrap.Presenter;session=bootstrap.Session;presenter.TickSession=false;
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            if(resumeOnly)
            {
                Require(session.Phase==SessionPhase.Combat&&session.Stage==11&&session.IsPaused,"Disk save restores paused Stage 11");
                Require(session.GetStatLevel(0)==29&&session.EquippedRelicIds.Count==1,"Growth and equipped relic survive process restart");
                yield return Capture("17-restored-pause");
                presenter.View.Resume.onClick.Invoke();Require(!session.IsPaused,"Continue UI resumes restored battle");
                yield return Capture("18-restored-battle");
                File.WriteAllText(Path.Combine(output,"resume-result.json"),"{\"passed\":true,\"stage\":11,\"attackLevel\":29,\"equippedRelics\":1}");
                Debug.Log("LAST_BELL_RESUME_PASSED");finished=true;Application.Quit(0);yield break;
            }
            session.SetPaused(false);
            session.EventRaised+=e=>{
                if(e.Kind==SessionEventKind.PerfectDodge)perfect++;
                if(e.Kind==SessionEventKind.Dodged)normal++;
                if(e.Kind==SessionEventKind.PlayerDamaged)hits++;
                if(e.Kind==SessionEventKind.DodgeStarted&&session.Phase==SessionPhase.Combat)pendingDodge=(int)e.Direction;
                if(e.Kind==SessionEventKind.EnemyDamaged&&e.Critical){critical++;pendingCritical=true;}
            };
            Require(session.Phase==SessionPhase.TutorialDodge,"Fresh profile opens Stage 1 tutorial");
            yield return Capture("01-tutorial-dodge");
            Press(presenter.View.Dodge[0]);
            Require(session.Phase==SessionPhase.TutorialAttack,"Dodge UI advances tutorial");
            yield return Capture("02-tutorial-attack");
            Press(presenter.View.Attack);session.Tick(.11f);
            Require(session.Phase==SessionPhase.Combat,"Attack UI finishes tutorial");
            yield return Capture("03-stage-1");
            int seenStage=1,iterations=0;
            while(session.Stage<=10&&iterations++<100000&&!failed)
            {
                if(session.Phase==SessionPhase.RewardChoice)
                {
                    int stage=session.Stage;int expected=stage+1;
                    Require(session.RelicChoices.Count==3,"Three distinct reward choices");
                    Require(session.RelicChoices[0]!=session.RelicChoices[1]&&session.RelicChoices[1]!=session.RelicChoices[2]&&session.RelicChoices[0]!=session.RelicChoices[2],"No duplicate reward cards");
                    yield return Capture(stage==5?"05-miniboss-reward":"07-boss-reward");
                    int choice=0;
                    for(int i=0;i<3;i++)if(session.GetRelicLevel(session.RelicChoices[i])>0){choice=i;break;}
                    presenter.View.Rewards[choice].Receive.onClick.Invoke();rewards++;
                    Require(session.Stage==expected&&session.Phase==SessionPhase.Combat,"Reward continues directly to Stage "+expected);
                }
                if(session.Stage!=seenStage)
                {
                    seenStage=session.Stage;
                    if(seenStage==5||seenStage==10)yield return Capture(seenStage==5?"04-miniboss":"06-boss");
                }
                if(session.Phase!=SessionPhase.Combat)throw new InvalidOperationException("SMOKE FAILED: autoplay died before Stage 10");
                if(session.IsPaused)session.SetPaused(false);
                if(pendingDodge>=0&&!dodgeCaptured[pendingDodge])
                {int d=pendingDodge;dodgeCaptured[d]=true;yield return Capture("dodge-"+((DodgeDirection)d).ToString().ToLowerInvariant(),.08f);}
                if(pendingCritical&&!criticalCaptured){criticalCaptured=true;yield return Capture("16-critical-hit",.03f);}
                if(session.IsEnemyStunned&&!stunCaptured){stunCaptured=true;yield return Capture("enemy-stunned",.12f);}
                for(int sub=0;sub<6&&session.Phase==SessionPhase.Combat&&session.Stage<=10;sub++)
                {
                    var cue=session.EnemyIntent;
                    if(cue.Visible&&cue.SecondsToHit<=.04f)Press(presenter.View.Dodge[(int)cue.Direction]);
                    else Press(presenter.View.Attack);
                    // The probe runs six simulation ticks per rendered frame. Clear only previous
                    // presentation effects before a new impact so screenshots do not stack six
                    // times as many numbers as normal realtime play. Rules and HP are unchanged.
                    var snapshot=session.ExportSave().run;
                    if(snapshot.attackHitAtMs>=0&&snapshot.attackHitAtMs<=session.ClockMs+10.001)presenter.Effects.Clear();
                    session.Tick(.01f);
                }
                yield return null;
            }
            Require(session.Stage==11&&session.Phase==SessionPhase.Combat,"Stage 10 does not stop the run");
            Require(rewards==2&&session.Fragments==4,"Both rewards granted exactly 1 + 3 fragments");
            yield return Capture("08-stage-11-continuation");
            // Let the actual boundary enemy defeat the player; do not edit HP or saved state.
            for(int i=0;i<10000&&session.Phase==SessionPhase.Combat;i++)
            {session.Tick(.1f);if(i%6==0)yield return null;}
            Require(session.Phase==SessionPhase.Lobby&&session.CheckpointStartStage==11,"Death returns to Stage 11 checkpoint lobby");
            yield return Capture("09-lobby");
            int startingLevel=session.GetStatLevel(0);
            presenter.View.Stats[0].Upgrade.onClick.Invoke();
            Require(session.GetStatLevel(0)==startingLevel+1,"Lobby + upgrades attack");
            yield return Capture("10-upgrade");
            presenter.View.OpenRelics.onClick.Invoke();
            yield return Capture("11-relic-inventory");
            presenter.View.RelicEquip.onClick.Invoke();
            Require(session.EquippedRelicIds.Count==1,"Selected owned relic equips through UI");
            yield return Capture("12-relic-equipped");
            presenter.View.RelicBack.onClick.Invoke();yield return null;
            for(int i=0;i<50&&session.Phase==SessionPhase.Lobby;i++)presenter.View.Stats[0].Upgrade.onClick.Invoke();
            Require(session.Phase==SessionPhase.ExchangeConfirmation,"Insufficient gold opens exchange");
            yield return Capture("13-gold-exchange");
            var quote=session.PendingExchange;int levelBefore=session.GetStatLevel(0),goldBefore=session.Gold,fragmentsBefore=session.Fragments;
            Require(session.CanConfirmExchange,"Earned fragments cover the offered exchange");
            presenter.View.ModalConfirm.onClick.Invoke();
            Require(session.Gold==goldBefore+quote.gold&&session.Fragments==fragmentsBefore-quote.fragments,"Exchange applies the quoted amounts once");
            Require(session.GetStatLevel(0)==levelBefore,"Exchange does not silently buy upgrade");
            presenter.View.Stats[0].Upgrade.onClick.Invoke();
            Require(session.GetStatLevel(0)==levelBefore+1,"Explicit second + buys upgrade");
            yield return Capture("14-lobby-grown");
            presenter.View.StartRun.onClick.Invoke();
            Require(session.Stage==11&&session.Phase==SessionPhase.Combat,"Stage button resumes saved checkpoint");
            session.SetPaused(true);bootstrap.Save();
            yield return Capture("15-checkpoint-resume");
            string report=$"{{\"passed\":true,\"tutorial\":true,\"bossRewards\":{rewards},\"perfectDodges\":{perfect},\"normalDodges\":{normal},\"criticalHits\":{critical},\"playerHits\":{hits},\"checkpoint\":{session.CheckpointStartStage},\"attackLevel\":{session.GetStatLevel(0)},\"equippedRelics\":{session.EquippedRelicIds.Count}}}";
            File.WriteAllText(Path.Combine(output,"smoke-result.json"),report);
            Debug.Log("LAST_BELL_SMOKE_PASSED: "+report);finished=true;
            yield return null;Application.Quit(0);
        }

        IEnumerator Capture(string name,float settleSeconds=.36f)
        {
            yield return null;yield return null;
            yield return new WaitForSecondsRealtime(settleSeconds);
            // Hidden/batch windows have no presented back buffer. Render the same scene and UI explicitly.
            var camera=presenter.WorldCamera;var canvas=presenter.View.GetComponent<Canvas>();
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;int oldSort=canvas.sortingOrder;
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float oldAspect=camera.aspect;
            int width=Screen.width,height=Screen.height;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
            camera.targetTexture=target;camera.aspect=(float)width/height;Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());Destroy(pixels);
            camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;
            canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;canvas.sortingOrder=oldSort;
            target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
            Debug.Log("LAST_BELL_CAPTURE: "+name);
        }
        static void Press(PointerCommand button){button.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});}
        void Require(bool condition,string description)
        {
            if(!condition)throw new InvalidOperationException("SMOKE FAILED: "+description);
            Debug.Log("LAST_BELL_CHECK: "+description);
        }
        void OnLog(string condition,string trace,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){failed=true;File.WriteAllText(Path.Combine(output,"smoke-error.txt"),condition+"\n"+trace);}
        }
        void Update()
        {
            if(!finished&&(failed||Time.realtimeSinceStartup-started>300))Application.Quit(1);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif
