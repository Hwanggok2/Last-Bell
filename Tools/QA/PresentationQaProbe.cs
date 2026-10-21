#if LASTBELL_QA
using System;
using System.Collections;
using System.IO;
using System.Globalization;
using LastBell;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Opt-in QA-only assembly: built separately from the delivery candidate.
// Uses the saved scene, original runtime, real frame clock, and explicit fixture saves.
public sealed class PresentationQaProbe : MonoBehaviour
{
    string output,mode; MvpPresenter p; GameSession s; GameCatalog catalog;
    StreamWriter trace; int sample; float nextCapture; string scenario;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var args=Environment.GetCommandLineArgs();
        for(int i=0;i+2<args.Length;i++) if(args[i]=="-lastbell-qa")
        {var q=new GameObject("Opt-in QA probe").AddComponent<PresentationQaProbe>();q.output=args[i+1];q.mode=args[i+2];break;}
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(output);yield return null;
        var boot=FindFirstObjectByType<GameBootstrap>();p=boot.Presenter;catalog=boot.Session.Catalog;boot.enabled=false;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        File.WriteAllText(Path.Combine(output,"environment.json"),"{\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"gpu\":\""+SystemInfo.graphicsDeviceName+"\",\"fixture\":true,\"clock\":\"real frame TickInputFrame; no manual Tick\"}");
        trace=new StreamWriter(Path.Combine(output,"timeline.csv"));
        trace.WriteLine("scenario,realtime,frame,delta,stage,phase,kind,transitionMs,combatMs,playerHp,enemyHp,playerX,playerY,motion,clipFrame,flash,zoom,travel,cueVisible,cueProgress,enemyMotion,enemyFrame,enemyFlash");
        if(mode=="hit-priority")
        {
            BindFixture(1);scenario="windup-hit";Press(p.View.Attack);yield return Observe(.5f,.025f);
            BindFixture(1,true);scenario="lethal-hit";Press(p.View.Attack);yield return Observe(.8f,.025f);
        }
        else if(mode=="sequence")
        {
            foreach(int from in new[]{1,4,9})
            {BindFixture(from,true);scenario="arrival-from-"+from;Press(p.View.Attack);yield return Observe(from==1?4.1f:6.2f,.08f);}
            BindFixture(9,false,true);scenario="death";yield return Observe(3.0f,.08f);
            foreach(int stage in new[]{1,2,3,4})
            {
                BindFixture(stage);scenario="dodge-"+stage;
                while(!s.EnemyIntent.Visible||s.EnemyIntent.SecondsToHit>.04f)yield return null;
                Press(p.View.Dodge[(int)s.EnemyIntent.Direction]);yield return Observe(.4f,.025f);
            }
            BindFixture(1);scenario="attack-and-hit-flash";Press(p.View.Attack);yield return Observe(1.0f,.025f);
            BindFixture(1,true);scenario="pause-travel";Press(p.View.Attack);
            while(s.Transition==null||s.Transition.Kind!=TransitionKind.Travel)yield return null;
            yield return new WaitForSecondsRealtime(.3f);s.SetPaused(true);yield return Observe(.4f,.1f);s.SetPaused(false);yield return Observe(.4f,.1f);
            p.View.ReducedShake.isOn=true;BindFixture(4,true);scenario="reduced-mini";Press(p.View.Attack);yield return Observe(6.0f,.15f);p.View.ReducedShake.isOn=false;
        }
        else
        {
            BindFixture(10,true);Press(p.View.Attack);
            while(s.Phase!=SessionPhase.StoryDialogue)yield return null;
            for(int i=0;i<3;i++){scenario="story-"+(i+1);yield return Observe(.15f,.2f);Capture(scenario);p.Presentation.Overlay.ButtonNext.onClick.Invoke();}
            yield return Rendered();Capture("relic-choice");
            s.ChooseRelic(s.RelicChoices[0]);scenario="post-relic";yield return Observe(2.8f,.3f);s.SetPaused(true);yield return Rendered();Capture("pause");
            s.SetPaused(false);s.ReturnToLobby();yield return Rendered();Capture("lobby");p.View.OpenRelics.onClick.Invoke();yield return Rendered();Capture("inventory");p.View.RelicBack.onClick.Invoke();yield return Rendered();
            for(int i=0;i<50&&s.Phase==SessionPhase.Lobby;i++)p.View.Stats[0].Upgrade.onClick.Invoke();yield return Rendered();Capture("exchange");
            s.SetPaused(true);yield return Rendered();Capture("exchange-paused");s.SetPaused(false);s.CancelExchange();
            // Observe the real mixer output with the actual saved AudioSources.
            p.MuteAudio.isOn=false;yield return Rendered();yield return new WaitForSecondsRealtime(1f);yield return AudioWindow();float on=audioPeak;
            p.MuteAudio.isOn=true;yield return Rendered();yield return new WaitForSecondsRealtime(.2f);yield return AudioWindow();float muted=audioPeak;
            p.MuteAudio.isOn=false;yield return Rendered();yield return new WaitForSecondsRealtime(.2f);yield return AudioWindow();float restored=audioPeak;
            File.WriteAllText(Path.Combine(output,"audio-output.json"),FormattableString.Invariant($"{{\"onRms\":{on},\"mutedRms\":{muted},\"restoredRms\":{restored}}}"));
        }
        trace.Dispose();File.WriteAllText(Path.Combine(output,"completed.txt"),"Fixture observations finished; not OS input or full-gameplay validation.");Application.Quit(0);
    }
    static IEnumerator Rendered(){yield return null;yield return new WaitForEndOfFrame();}
    float audioPeak;
    IEnumerator AudioWindow(){audioPeak=0;float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<1.2f){yield return null;audioPeak=Mathf.Max(audioPeak,Rms());}}
    void BindFixture(int stage,bool weak=false,bool lowPlayer=false)
    {
        var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.tutorialStep=2;save.run=null;
        var temp=new GameSession(catalog,save);temp.StartRun();save=temp.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;
        save.run.stage=stage;save.run.patternStep=0;save.run.enemyHpMilli=weak?1000:catalog.GetStage(stage).maxHp*1000L;save.run.enemyMaxHpMilli=save.run.enemyHpMilli;
        if(lowPlayer)save.run.playerHpMilli=1000;s=new GameSession(catalog,save);s.SetPaused(false);p.Bind(s);
        sample=0;nextCapture=0;
    }
    IEnumerator Observe(float seconds,float interval)
    {
        float start=Time.realtimeSinceStartup;nextCapture=0;
        while(Time.realtimeSinceStartup-start<seconds)
        {
            yield return new WaitForEndOfFrame();var tr=s.Transition;var a=p.PlayerActor;Vector3 pos=a.transform.localPosition;
            trace.WriteLine(FormattableString.Invariant($"{scenario},{Time.realtimeSinceStartup:F6},{Time.frameCount},{Time.unscaledDeltaTime:F6},{s.Stage},{s.Phase},{(tr==null?"":tr.Kind.ToString())},{(tr==null?0:tr.ElapsedSeconds*1000):F4},{s.ClockMs:F4},{s.PlayerHp},{s.EnemyHp},{pos.x:F5},{pos.y:F5},{a.CurrentMotion},{a.CurrentFrame},{a.HitFlashAmount:F4},{3.1f/p.WorldCamera.orthographicSize:F4},{p.Presentation.TravelPixels:F4},{s.EnemyIntent.Visible},{s.EnemyIntent.Progress:F4},{p.EnemyActor.CurrentMotion},{p.EnemyActor.CurrentFrame},{p.EnemyActor.HitFlashAmount:F4}"));
            float elapsed=Time.realtimeSinceStartup-start;
            if(elapsed>=nextCapture){Capture(scenario+"-"+(sample++).ToString("D4"));nextCapture=elapsed+interval;}
        }
        trace.Flush();
    }
    void Capture(string name)
    {
        var cam=p.WorldCamera;var canvas=p.View.GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCam=canvas.worldCamera;float plane=canvas.planeDistance;int sort=canvas.sortingOrder;
        var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;float aspect=cam.aspect;
        var target=new RenderTexture(Screen.width,Screen.height,24);target.Create();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;canvas.sortingOrder=100;
        cam.targetTexture=target;cam.aspect=(float)Screen.width/Screen.height;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;
        var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
        File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());Destroy(pixels);
        cam.targetTexture=oldTarget;cam.aspect=aspect;RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=oldCam;canvas.planeDistance=plane;canvas.sortingOrder=sort;target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
    }
    static float Rms(){var data=new float[1024];AudioListener.GetOutputData(data,0);double sum=0;foreach(float x in data)sum+=x*x;return(float)Math.Sqrt(sum/data.Length);}
    static void Press(PointerCommand b){b.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});}
}
#endif
