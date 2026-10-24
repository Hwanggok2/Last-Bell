#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastBell
{
    public sealed class ReferenceV81SmokeProbe : MonoBehaviour
    {
        string output;MvpPresenter p;GameCatalog catalog;bool failed;readonly List<string> checks=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-v81-smoke")
            {var probe=new GameObject("Reference v8.1 verification").AddComponent<ReferenceV81SmokeProbe>();probe.output=Path.GetFullPath(args[i+1]);break;}
        }
        GameSaveData Lobby()
        {
            var save=new GameSession(catalog).ExportSave();save.tutorialStep=2;save.prologueSeen=save.comboHintSeen=true;save.phase=SessionPhase.Lobby;
            save.gold=12345;save.characters[0].statLevels=new[]{10,10,10,10};save.run=null;return save;
        }
        GameSession Playing(int stage=1,string[] sequence=null,bool weak=false)
        {
            var s=new GameSession(catalog,Lobby());s.StartRun();var save=s.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;
            save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
            save.run.randomSequence=sequence??new string[0];save.run.sequenceCursor=0;
            s=new GameSession(catalog,save);s.SetPaused(false);p.Bind(s);p.TickSession=false;return s;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;yield return null;yield return null;
            p=FindFirstObjectByType<MvpPresenter>();catalog=p.Session.Catalog;p.TickSession=false;
            var s=new GameSession(catalog,Lobby());p.Bind(s);var v=p.View;var r=v.Reference;
            yield return Capture("01-selection");CheckText("selection");
            Check(r.Heading.text=="용사 선택"&&r.Heading.fontSize>r.Summary.fontSize&&r.Heading.rectTransform.localScale.x==.75f,"measured heading and body typography");
            Check(r.Summary.text.Contains("스테이지 도달")&&r.SelectionStats.text.Contains("소지 골드"),"reference selection copy with live game values");
            Check(r.Selection.activeInHierarchy&&!r.Upgrade.activeInHierarchy,"separate selection screen");
            var portraitFrames=new HashSet<Sprite>();
            for(int i=0;i<8;i++){yield return new WaitForSecondsRealtime(.12f);portraitFrames.Add(v.LobbyPortrait.sprite);}
            Check(portraitFrames.Count>1&&!portraitFrames.Contains(null),"selection portrait advances approved idle frames");
            yield return Capture("v85-selection-idle");
            Click(v.Characters[1]);yield return Capture("02-unavailable-character");Check(!v.StartRun.interactable&&s.CurrentCharacterId=="on","inspect unavailable hero without selecting");
            Click(v.Characters[0]);yield return null;Click(v.StartRun);yield return Capture("03-upgrade");CheckText("upgrade");
            Check(r.Heading.text=="업그레이드"&&!r.UpgradePortrait.gameObject.activeInHierarchy&&r.Price.text.Contains("골드 필요"),"reference upgrade copy and single detail panel");
            Check(r.Upgrade.activeInHierarchy&&!r.Selection.activeInHierarchy,"selection opens separate upgrade");
            Click(r.Maximum);yield return Capture("04-max-quote");long before=s.Gold;Click(r.Purchase);yield return null;
            Check(s.GetStatLevel(0)==20&&s.Gold==before-9633,"MAX pointer buys exact sequence to level 20");
            Click(r.StatButtons[2]);yield return Capture("05-luck-detail");CheckText("luck");
            s.Gold.ToString();Click(v.OpenRelics);yield return Capture("06-relic-collection");CheckText("collection");Click(v.RelicBack);yield return null;
            Click(r.Back);yield return null;Click(v.ReplayPrologue);s.Tick(6);yield return Capture("07-prologue");CheckText("prologue");
            Check(v.Prologue.Skip.isActiveAndEnabled&&!v.Prologue.Pause.isActiveAndEnabled&&!v.Prologue.Next.isActiveAndEnabled&&!v.Prologue.ReadingMode.isActiveAndEnabled,"prologue has only skip control");
            Click(v.Prologue.Skip);yield return null;
            s=Playing();yield return Capture("08-combat");CheckText("combat");
            var pauseLabel=v.Pause.GetComponentInChildren<Text>();Check(pauseLabel.preferredHeight<=pauseLabel.fontSize*1.2f,"pause label remains on one line");
            Check(!v.Dodge[3].gameObject.activeInHierarchy&&v.Dodge.Take(3).All(x=>x.gameObject.activeInHierarchy),"only three dodge controls visible");
            float[,] normalized={{122f/1084,160f/540},{60f/1084,80f/540},{161f/1084,59f/540}};
            for(int i=0;i<3;i++)
            {
                var rt=(RectTransform)v.Dodge[i].transform;var pt=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));var safe=Screen.safeArea;
                Check(Mathf.Abs(pt.x-(safe.xMin+safe.width*normalized[i,0]))<1&&Mathf.Abs(pt.y-(safe.yMin+safe.height*normalized[i,1]))<1,"reference control anchor "+i);
                var corners=new Vector3[4];rt.GetWorldCorners(corners);var lo=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var hi=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
                Check(hi.x-lo.x>=48&&hi.y-lo.y>=48,"48px minimum touch size "+i);
                Check(safe.Contains(lo)&&safe.Contains(hi),"safe-area bounds "+i);
            }
            var hpPosition=v.PlayerHpRoot.position;v.Attack.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});s.Tick(.15f);yield return null;
            Check(v.PlayerHpRoot.position==hpPosition,"HP anchor does not follow lunge");s.Tick(.05f);Check(s.EnemyHp<s.EnemyMaxHp,"attack pointer damages enemy");
            s=Playing(5,new[]{"low_sweep","high_sweep"});s.Tick(1.1f);yield return Capture("09-combo-preview");
            Check(v.Rhythm.BellCount==2,"two fixed bells show double attack");s.Tick(.65f);s.Dodge(DodgeDirection.Up);s.Tick(.05f);yield return Capture("10-perfect-wave");
            Check(s.CanPerfectCounter&&v.Rhythm.BellCount==2,"perfect counter and spent bell slot retained");
            s=Playing(10,new[]{"thrust","high_sweep"},true);yield return Capture("11-boss-bottom-hp");
            Check(!v.BossHpRoot.activeInHierarchy&&v.EnemyHpRoot.gameObject.activeInHierarchy&&v.EnemyName.text.StartsWith("Lv.10"),"boss uses lower HP with level and name");
            s.Attack();s.Tick(.2f);if(s.Phase==SessionPhase.StoryDialogue)s.SkipStory();s.Tick(10);
            Check(s.Stage==11&&s.PendingRewardCount==1&&s.Fragments==4,"boss kill continues to 11 with queued reward");
            s.ReturnToLobby();yield return Capture("12-run-end");Click(r.SettlementContinue);yield return Capture("13-stage-clear");
            Check(s.Phase==SessionPhase.RewardAnnouncement&&!v.ResultRetry.gameObject.activeInHierarchy,"clear announcement prevents skipping rewards");
            Click(r.SettlementContinue);yield return Capture("14-relic-unselected");Check(!v.RewardConfirm.interactable,"relic must be selected before confirming");
            var repository=new JsonSaveRepository(Path.Combine(output,"reward-save"));repository.Save(s.ExportSave());
            bool recovered;var reloaded=new GameSession(catalog,repository.Load(out recovered));
            Check(reloaded.Phase==SessionPhase.RewardChoice&&reloaded.RelicChoices.SequenceEqual(s.RelicChoices),"JsonUtility/file round trip preserves ended run reward");
            string id=s.RelicChoices[1];Click(v.Rewards[1].Receive);yield return Capture("15-relic-selected");CheckText("reward");Click(v.RewardConfirm);yield return Capture("16-final-result");
            Check(s.GetRelicLevel(id)==1&&s.Phase==SessionPhase.DeathResult,"confirm grants chosen relic and opens final result");
            Click(v.ResultLobby);yield return null;Click(v.OpenRelics);yield return Capture("18-owned-relic");CheckText("owned relic");Click(v.RelicBack);yield return null;
            Click(v.AudioCredits.OpenButton);yield return Capture("17-credits");
            Check(v.AudioCredits.Body.text.Contains("NeoDunggeunmo")&&File.Exists(Path.Combine(Application.streamingAssetsPath,"FontLicenses/NeoDunggeunmoPro-OFL.txt")),"font attribution and distributed OFL present");
            v.AudioCredits.Close();
            Check(catalog.stages.Take(20).Select(x=>x.enemyId).Distinct().Count()==20,"all twenty stages have distinct enemies");
            Check(catalog.stages.Take(20).Select(x=>catalog.GetEnemy(x.enemyId).artId).Distinct().Count()==20,"all twenty stages have distinct designs");
            var audio=p.Presentation.Audio;
            Check(audio.Swing.name=="sword-strike"&&audio.Dodge.name=="dodge-air"&&audio.PerfectDodge==audio.Dodge,"new attack and shared normal/perfect dodge clips");
            Check(!audio.LongBell&&!audio.Windup&&!audio.Critical&&(!audio.BellA||!audio.BellA.enabled&&!audio.BellA.clip)&&(!audio.BellB||!audio.BellB.enabled&&!audio.BellB.clip),"bell clips disconnected and both sources disabled");
            Check(new[]{audio.LobbyMusic,audio.BattleMusic,audio.QuarterMusic,audio.BossMusic}.Select(x=>x.name).SequenceEqual(new[]{"bgm-lobby","bgm-road","bgm-quarter","bgm-boss"}),"four chiptune music tracks bound");
            Check(v.AudioCredits.Credits.text.Contains("Juhani Junkala")&&File.Exists(Path.Combine(Application.streamingAssetsPath,"AudioLicenses/Juhani-Junkala-CC0.txt")),"current audio provenance and author CC0 notice distributed");
            foreach(int stage in Enumerable.Range(1,20))
            {
                s=Playing(stage,new[]{catalog.GetStage(stage).randomPatterns[0]});
                yield return Capture("v83-stage-"+stage.ToString("00"));
                var idle=p.EnemyActor.GetClip(ActorMotion.Idle);
                Check(idle!=null&&idle.loop&&idle.frames.Length>1,"natural enemy idle available stage "+stage);
                var idleFrames=new HashSet<Sprite>();
                for(int i=0;i<6;i++){yield return new WaitForSecondsRealtime(.16f);idleFrames.Add(p.EnemyActor.Body.sprite);}
                Check(idleFrames.Count>1&&p.EnemyActor.CurrentMotion==ActorMotion.Idle,"enemy idle plays during early cue stage "+stage);
                yield return Capture("v85-idle-"+stage.ToString("00"));
                Check(v.PlayerHealthBar&&v.EnemyHealthBar&&!v.PlayerHp.enabled&&!v.EnemyHp.enabled,"saved framed health bars stage "+stage);
                Check(Mathf.Abs(v.EnemyHealthBar.Value-s.EnemyHp/s.EnemyMaxHp)<.0001f,"health mesh reads actual HP stage "+stage);
                s.TickInputFrame(.9f);yield return Capture("v83-attack-"+stage.ToString("00"));
                if(BattlePresentation.IsNewCreature(p.EnemyActor.ArtId))
                {
                    var clip=p.EnemyActor.GetClip(ActorMotion.Attack);
                    Check(p.EnemyActor.CurrentFrame==clip.contactFrame,"authored contact frame matches impact stage "+stage);
                    Check(Mathf.Abs(p.EnemyActor.transform.localPosition.x+p.EnemyActor.ContactOffsetX(clip)-ParallaxWorld.PlanPoint(500,590).x)<.01f,"reviewed contact point reaches player home stage "+stage);
                }
            }
            s=Playing(1,null,true);long goldBefore=s.Gold;s.Attack();s.Tick(.2f);
            yield return Capture("v83-gold-reward");
            Check(v.GoldPickup.RewardAmount==s.Gold-goldBefore&&v.GoldPickup.RewardAmount>0,"kill popup uses exact granted gold once");
            Check(v.GoldPickup.Reward.gameObject.activeInHierarchy&&v.GoldPickup.Reward.text=="+"+v.GoldPickup.RewardAmount.ToString("N0"),"central positive gold popup");
            Check(!v.HitCombo.gameObject.activeInHierarchy&&s.HitCombo==0,"player hit combo removed after confirmed hit");
            s.SetPaused(true);var tint=v.GoldPickup.Reward.color;v.GoldPickup.Sample(5);
            Check(v.GoldPickup.Reward.color==tint&&s.Gold-goldBefore==v.GoldPickup.RewardAmount,"reward presentation pauses and cannot grant again");
            s.SetPaused(false);v.GoldPickup.Sample(2);v.GoldPickup.Sample(0);
            Check(!v.GoldPickup.Reward.gameObject.activeSelf,"reward popup expires");
            s=Playing();Check(v.GoldPickup.RewardAmount==0,"new run binding clears previous popup");
            // v8.4: real pointer/state captures of ready stun and storage acquisition.
            s=Playing(5,new[]{"low_sweep","high_sweep"});var readySave=s.ExportSave();readySave.run.stagger=100;
            s=new GameSession(catalog,readySave);s.SetPaused(false);p.Bind(s);
            yield return Capture("v84-stun-ready");CheckText("stun ready");
            Check(s.IsStunReady&&!s.IsEnemyStunned&&v.StunReady.gameObject.activeInHierarchy&&s.EnemyThreats.Length==2,"full stack displays ready while enemy attacks continue");
            int stunEvents=0;s.EventRaised+=e=>{if(e.Kind==SessionEventKind.EnemyStunned)stunEvents++;};s.Attack();s.Tick(.199f);
            Check(!s.IsEnemyStunned,"attack press/windup cannot prematurely stun");s.Tick(.001f);
            yield return Capture("v84-stun-hit");
            Check(s.IsEnemyStunned&&!s.IsStunReady&&stunEvents==1&&s.EnemyThreats.Length==0&&!v.StunReady.gameObject.activeInHierarchy,"confirmed hit consumes stack and cancels remaining attacks once");
            foreach(int storyStage in new[]{10,20})
            {
                s=Playing(storyStage,null,true);s.Attack();s.Tick(.2f);if(s.Phase==SessionPhase.Transition)s.Tick(.7f);
                yield return Capture("v84-story-"+storyStage);CheckText("story "+storyStage);
                Check(s.Phase==SessionPhase.StoryDialogue,"updated boss dialogue opens "+storyStage);
            }
            s=Playing(10,null,true);s.Attack();s.Tick(.2f);s.SkipStory();s.Tick(10);s.ReturnToLobby();s.ContinueSettlement();s.ContinueSettlement();
            var fullSave=s.ExportSave();fullSave.storageCapacity=3;fullSave.fragments=4;
            foreach(var relic in catalog.relics.Take(3))fullSave.relics.Add(new OwnedRelicData{id=relic.id,level=1});
            fullSave.run.offer.relicIds=catalog.relics.Skip(3).Take(3).Select(x=>x.id).ToArray();fullSave.run.offer.offeredLevels=new[]{0,0,0};
            fullSave.run.pendingRewards[0]=fullSave.run.offer;
            s=new GameSession(catalog,fullSave);p.Bind(s);yield return null;Click(v.Rewards[0].Receive);
            yield return Capture("v84-storage-full");CheckText("storage full");
            Check(v.RewardGold.gameObject.activeInHierarchy&&v.RewardConfirm.interactable&&v.RewardConfirm.GetComponentInChildren<Text>().text.Contains("파편 4"),"full storage explicitly quotes expansion cost and gold alternative");
            string acquired=s.RelicChoices[0];Click(v.RewardConfirm);yield return null;
            Check(s.StorageCapacity==4&&s.Fragments==0&&s.GetRelicLevel(acquired)==1,"pointer confirms expansion and acquisition together");
            Click(v.ResultLobby);yield return null;Click(v.OpenRelics);yield return Capture("v84-auto-relics");CheckText("automatic relics");
            Check(!v.RelicEquip.gameObject.activeInHierarchy&&v.RelicLevel.text.Contains("자동 적용")&&v.EquippedSlots.text.Contains("4 / 4"),"collection displays automatic application and owned capacity");
            fullSave.fragments=3;s=new GameSession(catalog,fullSave);p.Bind(s);yield return null;Click(v.Rewards[0].Receive);
            yield return Capture("v84-storage-short");CheckText("storage short");
            Check(!v.RewardConfirm.interactable&&v.RewardGold.interactable&&s.PendingRewardCount==1,"insufficient fragments keeps reward and offers a working alternative");
            long replacement=s.PendingMaxRelicGold,oldGold=s.Gold;Click(v.RewardGold);yield return null;
            Check(s.Gold==oldGold+replacement&&s.StorageCapacity==3&&s.Fragments==3&&s.GetRelicLevel(acquired)==0,"explicit gold choice cannot buy storage or silently equip candidate");
            File.WriteAllText(Path.Combine(output,"checks.txt"),string.Join("\n",checks));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":"+(!failed?"true":"false")+",\"checks\":"+checks.Count+",\"width\":"+Screen.width+",\"height\":"+Screen.height+"}");
            Application.logMessageReceived-=OnLog;Application.Quit(failed?1:0);
        }
        void CheckText(string screen)
        {
            foreach(var t in p.View.GetComponentsInChildren<Text>())
            {
                if(!t.enabled||string.IsNullOrWhiteSpace(t.text))continue;
                Check(t.font.name=="NeoDunggeunmoPro-Regular"&&t.fontSize>=14&&t.fontSize<=84&&!t.resizeTextForBestFit,screen+" reference typography: "+t.name);
                if(t.verticalOverflow==VerticalWrapMode.Truncate)Check(t.preferredHeight<=t.rectTransform.rect.height+2,screen+" text fits: "+t.name);
            }
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
            var items=new List<TextBounds>();
            foreach(var t in p.View.GetComponentsInChildren<Text>())
            {
                if(!t.enabled||string.IsNullOrWhiteSpace(t.text))continue;
                var corners=new Vector3[4];t.rectTransform.GetWorldCorners(corners);
                var lo=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var hi=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
                items.Add(new TextBounds{name=t.name,text=t.text,font=t.fontSize,x=lo.x,y=Screen.height-hi.y,width=hi.x-lo.x,height=hi.y-lo.y});
            }
            File.WriteAllText(Path.Combine(output,name+"-text.json"),JsonUtility.ToJson(new TextLayout{items=items.ToArray()},true));
        }
        [Serializable] public class TextBounds{public string name,text;public int font;public float x,y,width,height;}
        [Serializable] public class TextLayout{public TextBounds[] items;}
        void Check(bool value,string name){checks.Add((value?"PASS ":"FAIL ")+name);if(!value)failed=true;}
        void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){failed=true;checks.Add("ERROR "+message);}}
    }
}
#endif
