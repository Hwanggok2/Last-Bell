#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastBell
{
    public sealed class FeedbackV6SmokeProbe : MonoBehaviour
    {
        string output;MvpPresenter presenter;GameCatalog catalog;bool failed;readonly List<string> checks=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-feedback-v6-smoke")
            {var p=new GameObject("Feedback v6 verification").AddComponent<FeedbackV6SmokeProbe>();p.output=Path.GetFullPath(args[i+1]);break;}
        }
        GameSession Playing(int stage,bool weak=false)
        {
            var save=new GameSession(catalog).ExportSave();save.tutorialStep=2;save.prologueSeen=save.comboHintSeen=true;
            save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;save.run.hitCombo=12;save.gold=12345;
            save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
            var s=new GameSession(catalog,save);s.SetPaused(false);presenter.Bind(s);presenter.TickSession=false;return s;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;Application.targetFrameRate=60;
            yield return null;var bootstrap=FindFirstObjectByType<GameBootstrap>();presenter=bootstrap.Presenter;catalog=bootstrap.Session.Catalog;
            presenter.Effects.ReducedShake=false;
            Check(presenter.View.BattleGold&&presenter.View.EnemyName&&presenter.View.GoldPickup&&presenter.View.HitCombo,"saved HUD and collection bindings");
            Check(presenter.Presentation.Audio.GoldPickup&&presenter.Presentation.Audio.BellA&&presenter.Presentation.Audio.BellB,"saved pickup and dedicated bell audio");
            for(int stage=1;stage<=20;stage++)
            {
                var s=Playing(stage);yield return null;yield return Capture("stage-"+stage.ToString("D2"));
                Check(presenter.EnemyActor.ArtId==catalog.GetEnemy(catalog.GetStage(stage).enemyId).artId,"stage "+stage+" actor");
                Check(presenter.View.EnemyName.text==catalog.GetEnemy(catalog.GetStage(stage).enemyId).displayName,"stage "+stage+" name");
                Check(presenter.View.BossHpRoot.activeSelf==(stage%5==0),"stage "+stage+" boss HP visibility");
                if(stage==12||stage==20)
                {
                    s.Tick(s.EnemyIntent.SecondsToHit-.05f);yield return null;yield return Capture("stage-"+stage+"-attack");
                    s.Dodge(s.EnemyIntent.Direction);s.Tick(.05f);yield return null;
                    Check(presenter.Presentation.Audio.BellA.isPlaying||presenter.Presentation.Audio.BellB.isPlaying,"perfect defense plays bell "+stage);
                }
            }
            var normal=Playing(1,true);yield return null;normal.Attack();normal.Tick(.2f);yield return new WaitForSecondsRealtime(.2f);
            Check(presenter.View.GoldPickup.DotCount==20,"20 gold emits 20 dots");yield return Capture("gold-20");
            normal.SetPaused(true);long pending=presenter.View.GoldPickup.Pending;yield return new WaitForSecondsRealtime(.15f);
            Check(pending==presenter.View.GoldPickup.Pending,"gold pauses without paying again");normal.SetPaused(false);
            yield return new WaitForSecondsRealtime(1.1f);Check(presenter.View.GoldPickup.Pending==0,"gold animation finishes within 1.2 seconds");
            var boss=Playing(10,true);yield return null;yield return Capture("stage10-before-defeat");
            var bossSprite=presenter.EnemyActor.Body.sprite;boss.Attack();boss.Tick(.2f);yield return null;
            Check(boss.Phase==SessionPhase.StoryDialogue&&presenter.EnemyActor.DissolveAmount==0,"stage 10 boss remains visible during dialogue");
            Check(presenter.View.GoldPickup.Pending==750,"stage 10 visual gold waits for dialogue");yield return Capture("stage10-dialogue",.2f);
            Check(presenter.EnemyActor.Body.sprite==bossSprite,"stage 10 first dialogue keeps the actual combat sprite");
            boss.AdvanceStory();yield return Capture("stage10-dialogue-2");boss.AdvanceStory();yield return Capture("stage10-dialogue-3");
            Check(presenter.EnemyActor.Body.sprite==bossSprite,"stage 10 all dialogue lines preserve the same sprite");
            boss.SkipStory();boss.Tick(.25f);yield return new WaitForSecondsRealtime(.25f);
            Check(presenter.View.GoldPickup.DotCount==750,"750 gold emits 750 separate dots");yield return Capture("gold-750");
            Check(presenter.EnemyActor.Body.sprite==bossSprite,"stage 10 fades the same sprite after dialogue");
            yield return new WaitForSecondsRealtime(1.05f);Check(presenter.View.GoldPickup.Pending==0,"full 750 display catches up exactly");
            boss.Tick(1);yield return null;boss.ChooseRelic(boss.RelicChoices[0]);boss.Tick(10);yield return Capture("after10-stage11");
            Check(boss.Stage==11&&boss.PlayerHp==boss.PlayerMaxHp,"stage 10 reward heals and continues to 11");
            var end=Playing(20,true);yield return null;end.Attack();end.Tick(.2f);yield return new WaitForSecondsRealtime(.2f);
            Check(presenter.View.GoldPickup.DotCount==9600,"stage 20 reward emits 9600 dots without mesh overflow");yield return Capture("gold-9600");
            end.Tick(10);end.SkipStory();end.ChooseRelic(end.RelicChoices[0]);yield return Capture("stage20-clear");
            Check(end.Phase==SessionPhase.MvpComplete&&presenter.View.ResultTitle.text=="20스테이지 클리어!","stage 20 clear result");
            File.WriteAllText(Path.Combine(output,"checks.txt"),string.Join("\n",checks));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(!failed?"true":"false")+",\"checks\":"+checks.Count+",\"width\":"+Screen.width+",\"height\":"+Screen.height+"}");
            Application.logMessageReceived-=OnLog;Application.Quit(failed?1:0);
        }
        IEnumerator Capture(string name,float settleSeconds=.001f)
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

        void Check(bool ok,string message){checks.Add((ok?"PASS ":"FAIL ")+message);if(!ok){failed=true;Debug.LogWarning(message);}}
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){failed=true;checks.Add("ERROR "+message);}}
    }
}
#endif
