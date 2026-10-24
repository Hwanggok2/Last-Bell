#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastBell
{
    // Explicit QA only. Ordinary launches install nothing. Uses the saved scene;
    // fixture sessions and synthetic UI events are not physical mouse/keyboard input.
    public sealed partial class DragonThemeSmokeProbe : MonoBehaviour
    {
        string output, saveDirectory, caseName, scenario="startup", clockMode="manual";
        GameBootstrap bootstrap; MvpPresenter p; GameSession session; GameCatalog catalog;
        JsonSaveRepository repository; StreamWriter timeline, events;
        readonly Report report=new Report(); bool finished; float deadline; int captureSequence;
        GameSaveData resumeSave;
        [Serializable] sealed class Report
        {
            public bool passed, completed, osInput=false;
            public string testCase, version, dataVersion, graphics, savePath, error;
            public int width,height; public List<string> checks=new List<string>();
            public List<string> errors=new List<string>();
            public string limits="Synthetic UI events; accelerated route plus explicitly marked real-frame fixtures. Not OS input, mobile performance, or full acceptance.";
        }
        [Serializable] sealed class ExpectedSave
        {
            public int stage,attackLevel,relicCount,fragments; public long gold; public string phase;
        }
        [Serializable] sealed class Layout
        {
            public string scenario; public int width,height; public Rect safeArea;
            public List<Element> elements=new List<Element>();
        }
        [Serializable] sealed class Element
        {
            public string name,text; public Rect bounds; public int fontSize;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-dragon-theme-smoke")<0)return;
            // GameBootstrap initializes in Start, after this callback. Disable it
            // before validating so malformed QA arguments cannot use the default save.
            var boots=FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var boot in boots)boot.enabled=false;
            try
            {
                if(boots.Length!=1)throw new InvalidOperationException("Expected one saved GameBootstrap.");
                string dest=Absolute(Argument(args,"-dragon-theme-smoke",true));
                string save=Absolute(Argument(args,"-lastbell-save-dir",true));
                string mode=Argument(args,"-dragon-theme-case",false)??"full";
                if(mode!="full"&&mode!="layout"&&mode!="resume"&&mode!="review"&&mode!="cards"&&mode!="v2"&&mode!="v2-hp"&&mode!="v3"&&mode!="v3-resume"&&mode!="v3-story"&&mode!="v3-recovery"&&mode!="v4"&&mode!="v4-resume"&&mode!="v4-tail"&&mode!="v4-dash"&&mode!="v5"&&mode!="v5-legacy"&&mode!="v5-resume"&&mode!="v5-story12"&&mode!="v6"&&mode!="v6-legacy"&&mode!="v6-resume"&&mode!="v6-continue")throw new ArgumentException("Unknown Dragon QA case.");
                if(!Inside(save,dest))throw new ArgumentException("QA save directory must be a strict child of the output directory.");
                string user=Path.GetFullPath(Application.persistentDataPath);
                if(Same(dest,user)||Inside(dest,user)||Inside(user,dest))throw new ArgumentException("QA output overlaps the default user-save directory.");
                RejectLinks(dest);RejectLinks(save);
                if(File.Exists(Path.Combine(dest,"result.json")))throw new IOException("Refusing to overwrite existing QA results.");
                if(mode!="resume"&&mode!="v3-resume"&&mode!="v4-resume"&&mode!="v5-resume"&&mode!="v5-legacy"&&mode!="v6-legacy"&&mode!="v6-resume"&&Directory.Exists(save)&&Directory.EnumerateFileSystemEntries(save).Any())throw new IOException("Fresh QA case requires an empty save directory.");
                if(mode.StartsWith("v5",StringComparison.Ordinal))V5Preflight(dest,save,mode);
                if(mode.StartsWith("v6",StringComparison.Ordinal))V6Preflight(dest,save,mode);
                if(mode=="v4-resume"&&(!File.Exists(Path.Combine(save,"last-bell-save.json"))||!File.Exists(Path.Combine(dest,"v4-resume-expected.json"))))throw new IOException("V4 resume requires its isolated save and expectation.");
                if(mode=="v4-tail")
                {RejectLinks(Path.Combine(dest,"continuation-source"));if(!File.Exists(Path.Combine(dest,"continuation-source","last-bell-save.json")))throw new IOException("V4 tail requires the copied prior QA save.");}
                if(mode=="v3-resume"&&(!File.Exists(Path.Combine(save,"last-bell-save.json"))||!File.Exists(Path.Combine(dest,"v3-resume-expected.json"))))throw new IOException("V3 resume requires its isolated save and expectation.");
                if(mode=="resume"&&(!File.Exists(Path.Combine(save,"last-bell-save.json"))||!File.Exists(Path.Combine(dest,"expected-save.json"))))
                    throw new IOException("Resume requires an explicit QA save copy and expectation.");
                if(args.Any(x=>x.StartsWith("-lastbell-",StringComparison.Ordinal)&&x!="-lastbell-save-dir"))throw new ArgumentException("Do not combine legacy smoke flags with Dragon QA.");
                var q=new GameObject("Explicit Dragon theme QA").AddComponent<DragonThemeSmokeProbe>();
                q.output=dest;q.saveDirectory=save;q.caseName=mode;q.bootstrap=boots[0];
            }
            catch(Exception error)
            {
                Debug.LogError("DRAGON_QA_PREFLIGHT_REJECTED: "+error.Message);
                // In Editor leave the disabled bootstrap untouched; never close the Editor.
                if(!Application.isEditor)Application.Quit(64);
            }
        }
        static string Argument(string[] args,string key,bool required)
        {
            int index=Array.IndexOf(args,key);
            if(index<0){if(required)throw new ArgumentException("Missing "+key);return null;}
            if(Array.LastIndexOf(args,key)!=index||index+1>=args.Length||args[index+1].StartsWith("-"))throw new ArgumentException("Invalid "+key);
            return args[index+1];
        }
        static string Absolute(string path)
        {
            if(!Path.IsPathRooted(path))throw new ArgumentException("QA paths must be absolute.");
            string full=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(full.Length<4||Same(full,Path.GetPathRoot(full)))throw new ArgumentException("QA paths cannot be filesystem roots.");
            return full;
        }
        static bool Same(string a,string b)=>string.Equals(a.TrimEnd('\\','/'),b.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase);
        static bool Inside(string path,string parent)=>path.StartsWith(parent.TrimEnd('\\','/')+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
        static void RejectLinks(string path)
        {
            for(var dir=new DirectoryInfo(path);dir!=null;dir=dir.Parent)
                if(dir.Exists&&(dir.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("QA paths cannot pass through junctions or symbolic links.");
        }

        IEnumerator Start()
        {
            deadline=Time.realtimeSinceStartup+(caseName.StartsWith("v6",StringComparison.Ordinal)?1800:caseName.StartsWith("v5",StringComparison.Ordinal)?900:caseName=="v3"||caseName=="v4"||caseName=="v4-tail"?600:300);
            Application.logMessageReceived+=OnLog;
            // Drive nested iterators ourselves so assertion/capture failures produce a report.
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
                catch(Exception error){report.error=error.ToString();break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            foreach(var item in stack)(item as IDisposable)?.Dispose();
            if(finished)yield break;
            report.completed=string.IsNullOrEmpty(report.error)&&stack.Count==0;
            yield return null; // Allow deferred capture texture cleanup before native quit.
            Finish();
        }
        void Update()
        {
            if(!finished&&deadline>0&&Time.realtimeSinceStartup>deadline)
            {report.error="QA wall-clock timeout.";StopAllCoroutines();Finish();}
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(output);
            timeline=new StreamWriter(Path.Combine(output,"timeline.csv"));
            timeline.WriteLine("scenario,clockMode,realtime,frame,delta,stage,phase,transition,transitionMs,combatMs,playerHp,enemyHp,playerMotion,playerFrame,enemyMotion,enemyFrame,playerX,playerY,enemyX,enemyY,travel,layer0X,layer1X,enemyFlash");
            events=new StreamWriter(Path.Combine(output,"events.csv"));events.WriteLine("scenario,clockMode,frame,combatMs,stage,event,gold,fragments");
            bootstrap.Initialize();
            Require(bootstrap.Session!=null&&string.IsNullOrEmpty(bootstrap.LastStorageError),"saved scene bootstrap initialized");
            Require(Same(bootstrap.SavePath,Path.Combine(saveDirectory,"last-bell-save.json")),"bootstrap uses required isolated save path");
            p=bootstrap.Presenter;catalog=bootstrap.Session.Catalog;var initial=bootstrap.Session;
            // Remove only the test scene's bootstrap after initialization. Its normal
            // quit/autosave callback must not overwrite a later fixture or resume save.
            Destroy(bootstrap);yield return null;
            repository=new JsonSaveRepository(saveDirectory);Bind(initial,false);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            report.testCase=caseName;report.version=Application.version;report.dataVersion=catalog.dataVersion;
            report.width=Screen.width;report.height=Screen.height;report.graphics=SystemInfo.graphicsDeviceVersion;report.savePath=repository.FilePath;
            Require(p.View.Dragon&&p.Presentation.World.DragonTheme,"saved Dragon UI and meadow references");
            Require(p.PlayerActor.ArtId==(caseName=="v4"||caseName=="v4-resume"||caseName=="v4-tail"||caseName=="v4-dash"||caseName.StartsWith("v5",StringComparison.Ordinal)||caseName.StartsWith("v6",StringComparison.Ordinal)?catalog.GetCharacter(session.CurrentCharacterId).artId:"dragon_hero_on"),"saved Dragon player art");
            Require(catalog.mvpLastStage==20&&catalog.economy.minibossFragments==0&&catalog.economy.bossFragments==4,"twenty stages and 0/4 special currency retained");
            if(caseName.StartsWith("v6",StringComparison.Ordinal)){yield return DragonV6(caseName=="v6-resume",caseName=="v6-legacy",caseName=="v6-continue");yield break;}
            if(caseName.StartsWith("v5",StringComparison.Ordinal)){yield return DragonV5(caseName=="v5-resume",caseName=="v5-legacy",caseName=="v5-story12");yield break;}
            if(caseName=="v4"||caseName=="v4-resume"||caseName=="v4-tail"||caseName=="v4-dash") {yield return DragonV4(caseName=="v4-resume",caseName=="v4-tail",caseName=="v4-dash");yield break;}
            if(caseName=="v3"||caseName=="v3-resume"||caseName=="v3-story"||caseName=="v3-recovery") {yield return DragonV3(caseName=="v3-resume",caseName=="v3-story",caseName=="v3-recovery");yield break;}
            if(caseName=="v2"||caseName=="v2-hp") {yield return PlayerV2(caseName=="v2-hp");yield break;}
            if(caseName=="resume") {yield return Resume();yield break;}
            if(caseName=="review"||caseName=="cards") {yield return Review();yield break;}
            if(caseName=="full")yield return Route();
            yield return Gallery();
            if(caseName=="full")yield return Motion();
            if(resumeSave!=null)
            {
                repository.Save(resumeSave);
                var expected=new ExpectedSave{stage=resumeSave.run.stage,phase=resumeSave.phase.ToString(),gold=resumeSave.gold,
                    fragments=resumeSave.fragments,attackLevel=resumeSave.characters.First(x=>x.id==resumeSave.selectedCharacterId).statLevels[0],relicCount=resumeSave.relics.Count};
                File.WriteAllText(Path.Combine(output,"expected-save.json"),JsonUtility.ToJson(expected,true));
            }
        }
        void Bind(GameSession value,bool realtime)
        {
            if(session!=null){session.SaveRequested-=Save;session.EventRaised-=Event;}
            session=value;p.Bind(value);p.TickSession=realtime;clockMode=realtime?"real-frame-fixture":"manual-fixture";
            session.SaveRequested+=Save;session.EventRaised+=Event;
        }
        void Save(){repository.Save(session.ExportSave());session.MarkSaved();}
        void Event(SessionEvent e)=>events.WriteLine(FormattableString.Invariant($"{scenario},{clockMode},{Time.frameCount},{session.ClockMs:F4},{session.Stage},{e.Kind},{session.Gold},{session.Fragments}"));

        IEnumerator Route()
        {
            scenario="fresh-route";clockMode="accelerated-route-6x50ms-per-frame";
            Require(session.Phase==SessionPhase.Prologue,"fresh profile starts prologue");yield return Capture("route-00-prologue");
            Click(p.View.Prologue.Skip);yield return Rendered();Require(session.Phase==SessionPhase.Lobby,"skip reaches existing selection flow");
            yield return Capture("route-01-selection");Click(p.View.StartRun);yield return Rendered();
            Require(p.View.Reference.UpgradeScreen,"selection opens upgrade screen");Click(p.View.StartRun);yield return Rendered();
            Require(session.Phase==SessionPhase.TutorialDodge,"first departure starts dodge tutorial");
            Press(p.View.Dodge[0]);yield return Rendered();Require(session.Phase==SessionPhase.TutorialAttack,"Up UI advances tutorial");
            Press(p.View.Attack);session.Tick(.2f);yield return Rendered();Require(session.ExportSave().tutorialStep==2,"attack UI completes tutorial");
            int iterations=0,lastStage=0,storyPage=0;var reached=new HashSet<int>();
            var lines=new[]{"용은 저 봉우리에 있어.","숲길로 가면 빨라.","좋아. 아직 해도 높네."};
            while(!(session.Stage==11&&session.Phase==SessionPhase.Combat)&&iterations++<20000)
            {
                if(lastStage!=session.Stage)
                {lastStage=session.Stage;reached.Add(lastStage);yield return Capture("route-stage-"+lastStage.ToString("D2"));}
                if(session.Phase==SessionPhase.ComboHint){yield return Capture("route-combo-hint");Click(p.View.ModalConfirm);yield return Rendered();}
                if(session.Phase==SessionPhase.StoryDialogue)
                {
                    Require(session.Stage==10&&storyPage<3&&session.Story.Text==lines[storyPage],"Stage10 dialogue page "+storyPage);
                    Require(session.Story.Speaker==(storyPage<2?"토리":"온"),"Stage10 dialogue speaker "+storyPage);
                    yield return Capture("route-story-"+(++storyPage));Click(p.Presentation.Overlay.ButtonNext);yield return Rendered();continue;
                }
                Require(session.Phase!=SessionPhase.RunEnd&&session.PlayerHp>0,"route remains alive before Stage11");
                for(int i=0;i<6;i++)
                {
                    if(session.Phase==SessionPhase.Combat)
                    {
                        var cue=session.EnemyIntent;
                        if(cue.Visible&&cue.SecondsToHit<=.06f)Press(p.View.Dodge[(int)cue.Direction]);
                        else if(!cue.Visible||cue.SecondsToHit>.4f)Press(p.View.Attack);
                    }
                    session.TickInputFrame(.05f);
                    if(session.Phase==SessionPhase.StoryDialogue||session.Phase==SessionPhase.ComboHint||session.Stage==11)break;
                }
                Trace();yield return null;
            }
            Require(session.Stage==11&&session.Phase==SessionPhase.Combat&&storyPage==3,"route reaches 10-to-11 boundary with three dialogue pages");
            Require(Enumerable.Range(1,10).All(reached.Contains),"route visits all ten stages");
            Require(session.PendingRewardCount==1&&session.Fragments==4&&session.CheckpointStartStage==11,"boss reward queued once and checkpoint11 preserved");
            yield return Capture("route-stage-11-combat");yield return Settlement("route");
            Click(p.View.ResultLobby);yield return Rendered();Click(p.View.StartRun);yield return Rendered();
            var quote=session.QuoteUpgrade(0,false);long before=session.Gold;int level=session.GetStatLevel(0);
            Click(p.View.Reference.OneLevel);yield return Rendered();Click(p.View.Reference.Purchase);yield return Rendered();
            Require(session.Gold==before-quote.Cost&&session.GetStatLevel(0)==level+1,"explicit one-level growth uses live quote exactly once");
            Click(p.View.Reference.Maximum);yield return Capture("route-max-quote");
            Click(p.View.OpenRelics);yield return Capture("route-owned-relic");Require(!p.View.RelicEquip.gameObject.activeInHierarchy,"owned relics remain automatic");
            Click(p.View.RelicBack);yield return Rendered();Click(p.View.StartRun);session.Tick(5);yield return Rendered();
            Require(session.Stage==11&&session.Phase==SessionPhase.Combat,"next departure uses checkpoint11");
            session.SetPaused(true);Save();resumeSave=session.ExportSave();yield return Capture("route-resume-save");
        }
        IEnumerator Settlement(string prefix)
        {
            // Real saved buttons; the session itself owns queue and reward mutation.
            Click(p.View.Pause);yield return Rendered();Click(p.View.ReturnHome);yield return Rendered();
            Require(session.Phase==SessionPhase.RunEnd,"end run opens summary");yield return Capture(prefix+"-summary");
            Click(p.View.Reference.SettlementContinue);yield return Capture(prefix+"-announcement");
            Require(session.Phase==SessionPhase.RewardAnnouncement,"queued boss reward announced at settlement");
            Click(p.View.Reference.SettlementContinue);yield return Capture(prefix+"-relic-choice");
            Require(session.Phase==SessionPhase.RewardChoice&&!p.View.RewardConfirm.interactable,"relic selection requires explicit confirmation");
            if(caseName=="review"||caseName=="cards")foreach(var card in p.View.Rewards)
                Require(card.Icon.isActiveAndEnabled&&card.Title.isActiveAndEnabled&&!string.IsNullOrWhiteSpace(card.Title.text)
                    &&card.Receive.transform.GetSiblingIndex()<card.Icon.transform.GetSiblingIndex()
                    &&card.Receive.transform.GetSiblingIndex()<card.Title.transform.GetSiblingIndex(),"reward icon and title render after the button face");
            string selected=session.RelicChoices[0];Click(p.View.Rewards[0].Receive);yield return Rendered();
            if(caseName=="review"||caseName=="cards")
            {Require(p.View.RewardConfirm.interactable,"selected relic enables confirmation");yield return Capture(prefix+"-relic-selected");}
            Click(p.View.RewardConfirm);yield return Capture(prefix+"-result");
            Require(session.GetRelicLevel(selected)==1&&session.PendingRewardCount==0&&session.Phase==SessionPhase.DeathResult,"one relic granted and queue consumed once");
        }

        IEnumerator Review()
        {
            scenario="targeted-story-and-relic";BattleFixture(10,true);yield return Rendered();
            Press(p.View.Attack);session.Tick(.2f);yield return Rendered();
            Require(session.Phase==SessionPhase.StoryDialogue&&session.Story.LineIndex==0,"targeted first boss dialogue");
            if(caseName=="review")
            {
                yield return Capture("review-story-initial");clockMode="real-frame-story-wait";Trace();
                yield return new WaitForSecondsRealtime(.35f);yield return Capture("review-story-after-035s");Trace();
                Require(session.Story.LineIndex==0&&p.EnemyActor.HitFlashAmount==0,"first dialogue stays open while lethal flash expires");
            }
            clockMode="manual-fixture";
            for(int i=0;i<3;i++){Click(p.Presentation.Overlay.ButtonNext);yield return Rendered();}
            session.Tick(5);yield return Rendered();yield return Settlement("review");
        }

        GameSaveData LobbyFixture()
        {
            var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.prologueSeen=save.comboHintSeen=true;
            save.tutorialStep=2;save.run=null;save.gold=12345;return save;
        }
        void BattleFixture(int stage,bool weak=false,bool realtime=false)
        {
            var temp=new GameSession(catalog,LobbyFixture());temp.StartRun();var save=temp.ExportSave();
            save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;save.run.patternStep=save.run.patternIndex=0;
            save.run.randomSequence=new string[0];save.run.sequenceCursor=0;
            save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
            var value=new GameSession(catalog,save);value.SetPaused(false);Bind(value,realtime);
        }
        IEnumerator Gallery()
        {
            scenario="layout-fixtures";
            foreach(int stage in new[]{1,3,5,6,10})
            {
                BattleFixture(stage);yield return Capture("body-stage-"+stage.ToString("D2"));
                Require(p.EnemyActor.ArtId==catalog.GetEnemy(catalog.GetStage(stage).enemyId).artId,"saved actor mapping stage "+stage);
                Require(p.EnemyActor.Body.sprite&&p.PlayerActor.Body.sprite,"visible actor sprites stage "+stage);
                CheckControls();
            }
            Click(p.View.Pause);yield return Capture("layout-pause");Click(p.View.Resume);yield return Rendered();
            Bind(new GameSession(catalog,LobbyFixture()),false);yield return Capture("layout-selection");
            Click(p.View.StartRun);yield return Capture("layout-upgrade");Click(p.View.Reference.Maximum);yield return Capture("layout-max-quote");
            Click(p.View.OpenRelics);yield return Capture("layout-inventory");Click(p.View.RelicBack);yield return Rendered();
            BattleFixture(10,true);yield return Rendered();Press(p.View.Attack);session.Tick(.2f);yield return Rendered();
            Require(session.Phase==SessionPhase.StoryDialogue,"boss fixture reaches existing story state");
            for(int i=0;i<3;i++){yield return Capture("layout-story-"+(i+1));Click(p.Presentation.Overlay.ButtonNext);yield return Rendered();}
            session.Tick(5);yield return Rendered();yield return Settlement("layout");
        }
        IEnumerator Motion()
        {
            foreach(int stage in new[]{1,3,5,6,10})
            {
                scenario="motion-stage-"+stage;BattleFixture(stage,false,true);
                yield return Observe(1.2f,.1f);Press(p.View.Attack);yield return Observe(.55f,.05f);
            }
            for(int direction=0;direction<3;direction++)
            {
                scenario="defense-input-"+direction;BattleFixture(1,false,true);yield return Rendered();
                Press(p.View.Dodge[direction]);yield return Observe(.5f,.05f);
            }
            foreach(int from in new[]{1,4,9})
            {
                scenario="travel-from-"+from;BattleFixture(from,true,true);yield return Rendered();
                Press(p.View.Attack);yield return Observe(from==1?4.2f:6f,.1f);
                Require(session.Stage==from+1&&session.Phase==SessionPhase.Combat,"real-frame transition completes from "+from);
            }
        }
        IEnumerator Resume()
        {
            scenario="separate-process-resume";
            var expected=JsonUtility.FromJson<ExpectedSave>(File.ReadAllText(Path.Combine(output,"expected-save.json")));
            Require(session.Stage==expected.stage&&session.Phase.ToString()==expected.phase&&session.IsPaused,"bootstrap resumes the isolated paused battle");
            Require(session.Gold==expected.gold&&session.Fragments==expected.fragments&&session.GetStatLevel(0)==expected.attackLevel&&session.ExportSave().relics.Count==expected.relicCount,"saved wallet growth and relics survive restart");
            yield return Capture("resume-paused");Click(p.View.Resume);p.TickSession=true;clockMode="real-frame-resume";
            yield return Observe(.4f,.1f);Require(!session.IsPaused,"saved resume button continues battle");
        }

        IEnumerator Observe(float duration,float interval)
        {
            float start=Time.realtimeSinceStartup,next=0;
            while(Time.realtimeSinceStartup-start<duration)
            {
                yield return new WaitForEndOfFrame();Trace();float elapsed=Time.realtimeSinceStartup-start;
                if(elapsed>=next){CaptureNow(scenario+"-"+(captureSequence++).ToString("D5"));next=elapsed+interval;}
            }
            timeline.Flush();events.Flush();
        }
        void Trace()
        {
            var tr=session.Transition;var a=p.PlayerActor;var b=p.EnemyActor;var world=p.Presentation.World;
            float x0=world.Layers.Length>0?world.Layers[0].Tiles[0].localPosition.x:0;
            float x1=world.Layers.Length>1?world.Layers[1].Tiles[0].localPosition.x:0;
            timeline.WriteLine(FormattableString.Invariant($"{scenario},{clockMode},{Time.realtimeSinceStartup:F6},{Time.frameCount},{Time.unscaledDeltaTime:F6},{session.Stage},{session.Phase},{tr?.Kind.ToString()??""},{(tr?.ElapsedSeconds??0)*1000:F4},{session.ClockMs:F4},{session.PlayerHp},{session.EnemyHp},{a.CurrentMotion},{a.CurrentFrame},{b.CurrentMotion},{b.CurrentFrame},{a.transform.localPosition.x:F5},{a.transform.localPosition.y:F5},{b.transform.localPosition.x:F5},{b.transform.localPosition.y:F5},{world.TravelPixels:F4},{x0:F5},{x1:F5},{b.HitFlashAmount:F4}"));
        }
        static IEnumerator Rendered(){yield return null;yield return new WaitForEndOfFrame();}
        IEnumerator Capture(string name){yield return Rendered();CaptureNow(name);WriteLayout(name);}
        void CaptureNow(string name)
        {
            string path=Path.Combine(output,name+".png");
            if(File.Exists(path))throw new IOException("Refusing duplicate capture: "+path);
            var pixels=CapturePixels();
            try{File.WriteAllBytes(path,pixels.EncodeToPNG());}
            finally{Destroy(pixels);}
        }
        Texture2D CapturePixels()
        {
            var camera=p.WorldCamera;var canvas=p.View.GetComponent<Canvas>();
            var mode=canvas.renderMode;var canvasCamera=canvas.worldCamera;float plane=canvas.planeDistance;int sort=canvas.sortingOrder;
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float aspect=camera.aspect;
            var target=new RenderTexture(Screen.width,Screen.height,24);Texture2D pixels=null;
            try
            {
                target.Create();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
                camera.targetTexture=target;camera.aspect=(float)Screen.width/Screen.height;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);
                return pixels; // CPU pixels can be encoded after the real-time segment ends.
            }
            catch{if(pixels)Destroy(pixels);throw;}
            finally
            {
                camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=canvasCamera;canvas.planeDistance=plane;canvas.sortingOrder=sort;
                target.Release();Destroy(target);Canvas.ForceUpdateCanvases();
            }
        }
        Camera UiCamera=>p.View.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay?null:p.View.GetComponent<Canvas>().worldCamera;
        Rect ScreenBounds(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(x=>RectTransformUtility.WorldToScreenPoint(UiCamera,x)).ToArray();
            return Rect.MinMaxRect(points.Min(x=>x.x),points.Min(x=>x.y),points.Max(x=>x.x),points.Max(x=>x.y));
        }
        void CheckControls()
        {
            float[,] centers={{122f/1084,380f/540},{60f/1084,460f/540},{161f/1084,481f/540}};var safe=Screen.safeArea;
            for(int i=0;i<3;i++)
            {
                var box=ScreenBounds((RectTransform)p.View.Dodge[i].transform);
                var expected=new Vector2(safe.xMin+safe.width*centers[i,0],safe.yMax-safe.height*centers[i,1]);
                Require(Vector2.Distance(box.center,expected)<=1.01f,"reference center "+i+" at "+Screen.width+"x"+Screen.height);
                Require(box.xMin>=safe.xMin-1&&box.yMin>=safe.yMin-1&&box.xMax<=safe.xMax+1&&box.yMax<=safe.yMax+1,"safe hitbox "+i);
            }
            Require(!p.View.Dodge[3].gameObject.activeInHierarchy&&!session.Dodge(DodgeDirection.Forward),"Forward remains unavailable");
        }
        void WriteLayout(string name)
        {
            var layout=new Layout{scenario=name,width=Screen.width,height=Screen.height,safeArea=Screen.safeArea};
            foreach(var text in p.View.GetComponentsInChildren<Text>())
                if(text.enabled&&!string.IsNullOrWhiteSpace(text.text))layout.elements.Add(new Element{name=text.name,text=text.text,fontSize=text.fontSize,bounds=ScreenBounds(text.rectTransform)});
            foreach(var button in p.View.Dodge.Take(3))if(button.gameObject.activeInHierarchy)layout.elements.Add(new Element{name=button.name,bounds=ScreenBounds((RectTransform)button.transform)});
            File.WriteAllText(Path.Combine(output,name+"-layout.json"),JsonUtility.ToJson(layout,true));
        }
        void Click(Button button)
        {
            Require(button&&button.isActiveAndEnabled&&button.interactable,"UI button ready: "+(button?button.name:"null"));
            var data=new PointerEventData(EventSystem.current){position=ScreenBounds((RectTransform)button.transform).center,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"UI raycast reaches "+button.name);
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        static void Press(PointerCommand button)
        {
            if(!button||!button.isActiveAndEnabled||!button.Interactable)return;
            var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};button.OnPointerDown(data);button.OnPointerUp(data);
        }
        void Require(bool condition,string label)
        {report.checks.Add((condition?"PASS ":"FAIL ")+label);if(!condition)throw new InvalidOperationException(label);}
        void OnLog(string message,string trace,LogType type)
        {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message);}
        void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=OnLog;
            if(session!=null){session.SaveRequested-=Save;session.EventRaised-=Event;}
            ReleaseV2Frames();
            ReleaseV6Frames();
            ReleaseV5Frames();
            ReleaseV4Frames();
            ReleaseV3Frames();
            timeline?.Dispose();events?.Dispose();report.passed=report.completed&&string.IsNullOrEmpty(report.error)&&report.errors.Count==0;
            if(Directory.Exists(output))File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
            Debug.Log("DRAGON_QA_FINISHED: "+caseName+" passed="+report.passed);
            if(!Application.isEditor)Application.Quit(report.passed?0:1);
        }
    }
}
#endif
