#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastBell
{
    public sealed class FeedbackV7SmokeProbe : MonoBehaviour
    {
        string output;MvpPresenter p;GameCatalog catalog;bool failed;readonly List<string> checks=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-v7-smoke")
            {var probe=new GameObject("Feedback v7 verification").AddComponent<FeedbackV7SmokeProbe>();probe.output=Path.GetFullPath(args[i+1]);break;}
        }
        GameSession Playing(int stage,bool weak=false)
        {
            var save=new GameSession(catalog).ExportSave();save.tutorialStep=2;save.prologueSeen=save.comboHintSeen=true;
            save.phase=SessionPhase.Lobby;save.run=null;save.characters[0].statLevels=new[]{10,10,10,10};save.gold=12345;
            var s=new GameSession(catalog,save);s.StartRun();save=s.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;
            save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
            s=new GameSession(catalog,save);s.SetPaused(false);p.Bind(s);p.TickSession=false;return s;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;yield return null;yield return null;
            p=FindFirstObjectByType<MvpPresenter>();catalog=p.Session.Catalog;p.TickSession=false;
            var s=Playing(1);yield return null;yield return Capture("01-mobile-controls");
            var buttons=new List<Transform>();foreach(var d in p.View.Dodge)buttons.Add(d.transform);buttons.Add(p.View.Attack.transform);
            foreach(var button in buttons)
            {
                var rt=(RectTransform)button;var corners=new Vector3[4];rt.GetWorldCorners(corners);
                var lo=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var hi=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
                Check(lo.x>=20&&lo.y>=20&&hi.x<=Screen.width-20&&hi.y<=Screen.height-20,"input inset: "+button.name);
                Check(hi.x-lo.x>=48&&hi.y-lo.y>=48,"input minimum 48px: "+button.name);
                var eventData=new PointerEventData(EventSystem.current){position=(lo+hi)*.5f};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(eventData,hits);
                Check(hits.Count>0&&(hits[0].gameObject.transform==button||hits[0].gameObject.transform.IsChildOf(button)),"touch raycast reaches "+button.name);
            }
            float before=s.EnemyHp;p.View.Attack.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});s.Tick(.2f);
            Check(s.EnemyHp<before,"attack pointer produces damage");
            s.ReturnToLobby();yield return Capture("02-lobby");
            p.Bind(new GameSession(catalog));p.Session.Tick(6);yield return Capture("03-prologue-scroll");
            p.Session.SetPrologueManual(true);p.Session.NextPrologueParagraph();yield return Capture("04-prologue-long-paragraph");
            Check(p.View.Prologue.Scroll&&p.View.Prologue.LampGlow,"saved parchment and upper-left lamp");
            s=Playing(10,true);yield return Capture("08-stage10-closed");
            var world=p.Presentation.World;var closed=world.Gate.sprite;Vector3 gateAtTen=world.Gate.transform.localPosition;
            s.Attack();s.Tick(.2f);s.SkipStory();s.Tick(.275f);yield return Capture("09-stage10-opening");
            Check(s.CheckpointStartStage==11,"boss kill unlocks checkpoint before reward selection");
            Check(world.Gate.sprite==closed&&world.Gate.transform.localPosition==gateAtTen,"opening retains original gate sprite and frame position");
            s.Tick(.325f);yield return Capture("05-relic-unselected");
            Check(s.Phase==SessionPhase.RewardChoice&&!p.View.RewardConfirm.interactable,"reward requires explicit selection");
            long gold=s.Gold;string first=s.RelicChoices[0],second=s.RelicChoices[1];int firstLevel=s.GetRelicLevel(first),secondLevel=s.GetRelicLevel(second);
            Click(p.View.Rewards[0].Receive);yield return null;
            Check(s.Phase==SessionPhase.RewardChoice&&s.Gold==gold&&s.GetRelicLevel(first)==firstLevel,"preview grants nothing");
            Click(p.View.Rewards[1].Receive);yield return Capture("06-relic-selected");
            Check(p.View.Rewards[1].Selection.enabled&&!p.View.Rewards[0].Selection.enabled,"selection moves to another icon");
            Check(p.View.RewardDescription.text.Length>10&&p.View.RewardConfirm.interactable,"description and confirm enabled");
            Click(p.View.RewardConfirm);p.View.RewardConfirm.onClick.Invoke();yield return null;
            Check(s.Stage==11&&s.GetRelicLevel(second)==secondLevel+1&&s.GetRelicLevel(first)==firstLevel,"only selected relic granted once");
            yield return Capture("10-gate-travel-start");
            Check(world.Gate.sprite==closed&&world.Gate.transform.localPosition==gateAtTen,"stage 10 to 11 starts with the same gate at the same position");
            s.Tick(s.Transition.DurationSeconds/2);yield return Capture("11-gate-travel-middle");
            Check(world.Gate.transform.localPosition.x<gateAtTen.x,"gate moves left through travel");
            s.Tick(10);yield return Capture("07-stage11");
            Check(s.EnemyMaxHp==1200,"stage 11 breakthrough HP");
            Check(world.Gate.sprite==closed&&world.Gate.gameObject.activeSelf,"stage 11 retains the same gate");
            var death=s.ExportSave();death.run.playerHpMilli=1000;
            s=new GameSession(catalog,death);s.SetPaused(false);p.Bind(s);p.TickSession=false;
            s.Tick(s.EnemyIntent.SecondsToHit);s.Tick(10);yield return Capture("12-death-at11");
            Check(s.Phase==SessionPhase.DeathResult&&s.Result.RetryStage==11&&p.View.ResultRetryHint.text.Contains("11"),"death result advertises stage 11 checkpoint");
            long wallet=s.Gold;int fragments=s.Fragments;Click(p.View.ResultRetry);yield return Capture("13-retry-at11");
            Check(s.Stage==11&&s.PlayerHp==s.PlayerMaxHp&&s.ExportSave().run.earnedGold==0,"retry button starts stage 11 at full health with fresh run earnings");
            Check(s.Gold==wallet&&s.Fragments==fragments,"retry preserves wallet without duplicate rewards");
            s.ReturnToLobby();yield return Capture("14-lobby-stage11");
            Check(p.View.StartLabel.text=="Stage 11","lobby advertises checkpoint");
            s=new GameSession(catalog,JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(s.ExportSave())));s.StartRun();s.Tick(10);p.Bind(s);p.TickSession=false;
            Check(s.Stage==11,"save reload and lobby start retain checkpoint");
            var next=s.ExportSave();next.run.enemyHpMilli=1000;
            s=new GameSession(catalog,next);s.SetPaused(false);p.Bind(s);p.TickSession=false;
            s.Attack();s.Tick(.2f);s.Tick(.55f);yield return Capture("15-leaving-stage11");
            Check(s.Stage==12&&world.Gate.gameObject.activeSelf,"gate remains visible at start of stage 11 to 12 travel");
            s.Tick(10);yield return Capture("16-stage12");
            Check(!world.Gate.gameObject.activeSelf,"gate is gone after passing into stage 12");
            yield return VerifyAudioAndCredits();
            File.WriteAllText(Path.Combine(output,"checks.txt"),string.Join("\n",checks));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(!failed?"true":"false")+",\"checks\":"+checks.Count+",\"width\":"+Screen.width+",\"height\":"+Screen.height+"}");
            Application.logMessageReceived-=OnLog;Application.Quit(failed?1:0);
        }
        IEnumerator VerifyAudioAndCredits()
        {
            var a=p.Presentation.Audio;a.Muted=false;a.Sfx.Stop();
            Check(a.Swing.name=="sword-fire"&&a.Hit.name=="player-body-hit"&&a.Dodge.name=="dodge-whoosh","saved combat audio bindings use v7.2 clips");
            Check(a.EnemySwing.name=="sword-clash"&&a.EnemyImpact.name=="hit-sharp"&&a.Hit!=a.EnemyImpact,"enemy releases and impacts retain separate clips from player body damage");
            foreach(var clip in new[]{a.Swing,a.Hit,a.Dodge})
            {
                var samples=new float[clip.samples*clip.channels];bool read=clip.GetData(samples,0);float peak=0;
                foreach(float value in samples)peak=Mathf.Max(peak,Mathf.Abs(value));
                Check(read&&peak>.1f&&peak<.9f,"runtime audio decodes with headroom: "+clip.name);
            }
            var s=Playing(1);a.Sfx.Stop();s.Attack();yield return null;
            Check(!a.Sfx.isPlaying,"attack audio waits for confirmed contact");
            s.Tick(.2f);yield return null;Check(a.Sfx.isPlaying,"attack contact plays fire sword");
            s.SetPaused(true);yield return null;Check(!a.Sfx.isPlaying,"pause stops one-shot audio");
            s=Playing(1);a.Sfx.Stop();bool dodged=false;
            s.EventRaised+=e=>{if(e.Kind==SessionEventKind.Dodged)dodged=true;};
            s.Tick(s.EnemyIntent.SecondsToHit-.12f);s.Dodge(s.EnemyIntent.Direction);s.Tick(.12f);yield return null;
            Check(dodged&&a.Sfx.isPlaying,"normal dodge plays whoosh");
            s=Playing(1);a.Sfx.Stop();float hp=s.PlayerHp;s.Tick(s.EnemyIntent.SecondsToHit);yield return null;
            Check(s.PlayerHp<hp&&a.Sfx.isPlaying,"player damage plays body impact");
            s=Playing(1);a.Sfx.Stop();s.Tick(s.EnemyIntent.SecondsToHit-.03f);s.Dodge(s.EnemyIntent.Direction);s.Tick(.03f);yield return null;
            Check(s.PerfectStreak>0&&(a.BellA.isPlaying||a.BellB.isPlaying),"perfect dodge retains bell alongside enemy attack effects");
            a.Muted=true;s=Playing(1);a.Sfx.Stop();s.Attack();s.Tick(.2f);yield return null;
            Check(!a.Sfx.isPlaying,"mute suppresses new attack audio");a.Muted=false;
            s.ReturnToLobby();yield return Capture("17-audio-credits-button");
            var c=p.View.AudioCredits;var before=s.ExportSave();Click(c.OpenButton);yield return Capture("18-audio-credits");
            Check(c.IsOpen&&c.Body.text.Contains("SonoFxAudio")&&c.Body.text.Contains("CC-BY-4.0")&&c.Body.text.Contains("649361"),"credits show required attribution and original link");
            Check(c.SourceUrl=="https://freesound.org/people/SonoFxAudio/sounds/649361/"&&c.LicenseUrl=="https://creativecommons.org/licenses/by/4.0/","credits have original and license links");
            Check(c.Scroll.content.rect.height>c.Scroll.viewport.rect.height,"long credits scroll without truncation");
            var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,p.View.StartRun.transform.position)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&!hits[0].gameObject.transform.IsChildOf(p.View.StartRun.transform),"credits overlay blocks lobby start button");
            c.Scroll.verticalNormalizedPosition=0;yield return Capture("19-audio-credits-bottom");Click(c.CloseButton);yield return null;
            Check(!c.IsOpen&&s.Gold==before.gold&&s.Stage==1,"closing credits returns to unchanged lobby");
        }
        void Click(Button button)
        {
            var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0,"button has raycast hit: "+button.name);
            if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        IEnumerator Capture(string name)
        {
            yield return null;yield return null;
            var camera=p.WorldCamera;var canvas=p.View.GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;int oldSort=canvas.sortingOrder;
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float oldAspect=camera.aspect;
            var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);target.Create();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;camera.targetTexture=target;camera.aspect=(float)Screen.width/Screen.height;Canvas.ForceUpdateCanvases();yield return null;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());Destroy(pixels);
            camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;canvas.sortingOrder=oldSort;target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
        }
        void Check(bool value,string name){checks.Add((value?"PASS ":"FAIL ")+name);if(!value)failed=true;}
        void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){failed=true;checks.Add("ERROR "+message);}}
    }
}
#endif
