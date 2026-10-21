#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
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
        int storyLines;
        bool sawMinibossCurrency,sawBossIntro,sawTravel,sawBanner,sawDeath;
        readonly HashSet<string> captures=new HashSet<string>();
        bool failed,finished;
        bool resumeOnly,feedbackOnly,runOnly,pendingCritical,criticalCaptured,stunCaptured;
        int pendingDodge=-1;
        readonly bool[] dodgeCaptured=new bool[4];
        float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-lastbell-smoke"||args[i]=="-lastbell-resume-smoke"||args[i]=="-lastbell-feedback-smoke"||args[i]=="-lastbell-run-smoke")
            {
                var probe=new GameObject("Development smoke probe").AddComponent<MvpSmokeProbe>();
                probe.output=Path.GetFullPath(args[i+1]);probe.resumeOnly=args[i]=="-lastbell-resume-smoke";probe.feedbackOnly=args[i]=="-lastbell-feedback-smoke";probe.runOnly=args[i]=="-lastbell-run-smoke";break;
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
            Require(presenter.Presentation&&presenter.Presentation.Overlay,"Stored battle presentation and story UI connected");
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            if(runOnly){yield return ApprovedRunReview();yield break;}
            if(feedbackOnly){yield return FeedbackReview();yield break;}
            if(resumeOnly)
            {
                Require(session.Phase==SessionPhase.Combat&&session.Stage==11&&session.IsPaused,"Disk save restores paused Stage 11");
                Require(session.GetStatLevel(0)>0&&session.EquippedRelicIds.Count==1,"Growth and equipped relic survive process restart");
                yield return Capture("17-restored-pause");
                presenter.View.Resume.onClick.Invoke();Require(!session.IsPaused,"Continue UI resumes restored battle");
                yield return Capture("18-restored-battle");
                File.WriteAllText(Path.Combine(output,"resume-result.json"),$"{{\"passed\":true,\"stage\":11,\"attackLevel\":{session.GetStatLevel(0)},\"equippedRelics\":1}}");
                Debug.Log("LAST_BELL_RESUME_PASSED");finished=true;Application.Quit(0);yield break;
            }
            session.SetPaused(false);
            session.EventRaised+=e=>{
                if(e.Kind==SessionEventKind.PerfectDodge)perfect++;
                if(e.Kind==SessionEventKind.Dodged)normal++;
                if(e.Kind==SessionEventKind.PlayerDamaged)hits++;
                if(e.Kind==SessionEventKind.DodgeStarted&&session.Phase==SessionPhase.Combat)pendingDodge=(int)e.Direction;
                if(e.Kind==SessionEventKind.EnemyDamaged&&e.Critical){critical++;pendingCritical=true;}
                if(e.Kind==SessionEventKind.EnemyDefeated&&e.Stage==5)
                {
                    sawMinibossCurrency=true;
                    Require(session.Fragments==1&&session.RelicChoices.Count==0,"Miniboss grants one fragment without a relic offer");
                }
            };
            Require(session.Phase==SessionPhase.TutorialDodge,"Fresh profile opens Stage 1 tutorial");
            yield return Capture("01-tutorial-dodge");
            Press(presenter.View.Dodge[0]);
            Require(session.Phase==SessionPhase.TutorialAttack,"Dodge UI advances tutorial");
            yield return Capture("02-tutorial-attack");
            Press(presenter.View.Attack);session.Tick(.199f);
            Require(session.Phase==SessionPhase.TutorialAttack,"Attack does not contact before 200ms");
            session.Tick(.001f);
            Require(session.Phase==SessionPhase.Combat,"Attack UI finishes tutorial");
            yield return Capture("03-stage-1");
            int seenStage=1,iterations=0;
            while(!(session.Stage==11&&session.Phase==SessionPhase.Combat)&&iterations++<100000&&!failed)
            {
                if(session.Phase==SessionPhase.Transition)
                {
                    var transition=session.Transition;
                    if(transition.Kind==TransitionKind.Death)throw new InvalidOperationException("SMOKE FAILED: autoplay died before Stage 10 reward");
                    if(transition.Kind==TransitionKind.Travel)
                    {
                        sawTravel=true;
                        if(transition.FromStage==1&&transition.Progress>=.35f&&captures.Add("transition-travel"))yield return Capture("transition-travel",.05f);
                    }
                    if(transition.Kind==TransitionKind.Arrival&&transition.ElapsedSeconds>=.35f)
                    {
                        sawBanner=true;
                        if(transition.ToStage==2&&captures.Add("stage-2-banner"))yield return Capture("stage-2-banner",.05f);
                    }
                    if(transition.Kind==TransitionKind.BossIntro&&transition.ElapsedSeconds>=.65f)
                    {
                        sawBossIntro=true;
                        string name=session.Stage==5?"04-miniboss-intro":"06-boss-intro";
                        if(captures.Add(name))yield return Capture(name,.05f);
                    }
                    session.Tick(.05f);yield return null;continue;
                }
                if(session.Phase==SessionPhase.StoryDialogue)
                {
                    Require(session.Stage==10&&session.CheckpointStartStage==1,"First boss story precedes checkpoint and relic selection");
                    var story=session.Story;
                    Require(story!=null&&story.LineCount==3,"Three-line Stage 10 story");
                    string name="story-10-line-"+(story.LineIndex+1);
                    if(captures.Add(name))yield return Capture(name,.05f);
                    Require(presenter.Presentation.Overlay.ButtonNext.gameObject.activeInHierarchy,"Story next UI visible");
                    presenter.Presentation.Overlay.ButtonNext.onClick.Invoke();storyLines++;yield return null;continue;
                }
                if(session.Phase==SessionPhase.RewardChoice)
                {
                    float rewardDeadline=Time.realtimeSinceStartup+5;
                    while(!presenter.View.RewardPanel.activeSelf&&Time.realtimeSinceStartup<rewardDeadline)yield return null;
                    Require(presenter.View.RewardPanel.activeSelf,"Reward panel visible after story");
                    int stage=session.Stage;int expected=stage+1;
                    Require(stage==10&&storyLines==3,"Only main boss grants relic after all story lines");
                    Require(session.RelicChoices.Count==3,"Three distinct reward choices");
                    Require(session.RelicChoices[0]!=session.RelicChoices[1]&&session.RelicChoices[1]!=session.RelicChoices[2]&&session.RelicChoices[0]!=session.RelicChoices[2],"No duplicate reward cards");
                    yield return Capture("07-boss-reward");
                    int choice=0;
                    for(int i=0;i<3;i++)if(session.GetRelicLevel(session.RelicChoices[i])>0){choice=i;break;}
                    presenter.View.Rewards[choice].Receive.onClick.Invoke();rewards++;
                    Require(session.Stage==expected&&session.Transition!=null&&session.Transition.Kind==TransitionKind.Travel,"Reward starts automatic travel to Stage "+expected);
                    yield return null;continue;
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
                    if(cue.Visible&&cue.SecondsToHit<=.04f)Press(session.EnemyThreats[0].RequiresParry?presenter.View.Attack:presenter.View.Dodge[(int)cue.Direction]);
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
            Require(rewards==1&&session.Fragments==4&&sawMinibossCurrency,"One boss relic choice and exactly 1 + 3 fragments");
            Require(sawTravel&&sawBanner&&sawBossIntro&&storyLines==3,"Travel, stage banner, boss entrance and story were traversed");
            yield return Capture("08-stage-11-continuation");
            // Let the actual boundary enemy defeat the player; do not edit HP or saved state.
            for(int i=0;i<10000&&session.Phase==SessionPhase.Combat;i++)
            {session.Tick(.1f);if(i%6==0)yield return null;}
            Require(session.Transition!=null&&session.Transition.Kind==TransitionKind.Death,"Death starts dissolve and lantern flow");
            sawDeath=true;session.Tick(.9f);yield return Capture("death-lantern",.05f);
            session.Tick(.9f);yield return null;
            Require(session.Phase==SessionPhase.DeathResult&&session.Result.DefeatedStage==11&&session.Result.RetryStage==11,"Death shows Stage 11 result and checkpoint");
            Require(session.Result.Gold==session.Gold&&session.Result.Fragments==4,"Result includes exactly this run earnings");
            yield return Capture("death-result");
            presenter.View.ResultLobby.onClick.Invoke();yield return null;
            Require(session.Phase==SessionPhase.Lobby,"Result lobby button returns to lobby");
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
            var quote=session.PendingExchange;int levelBefore=session.GetStatLevel(0),fragmentsBefore=session.Fragments;long goldBefore=session.Gold;
            Require(session.CanConfirmExchange,"Earned fragments cover the offered exchange");
            presenter.View.ModalConfirm.onClick.Invoke();
            Require(session.Gold==goldBefore+quote.gold&&session.Fragments==fragmentsBefore-quote.fragments,"Exchange applies the quoted amounts once");
            Require(session.GetStatLevel(0)==levelBefore,"Exchange does not silently buy upgrade");
            presenter.View.Stats[0].Upgrade.onClick.Invoke();
            Require(session.GetStatLevel(0)==levelBefore+1,"Explicit second + buys upgrade");
            yield return Capture("14-lobby-grown");
            presenter.View.StartRun.onClick.Invoke();
            Require(session.Transition!=null&&session.Transition.Kind==TransitionKind.Arrival,"Checkpoint departure displays Stage before combat");
            session.Tick(.7f);yield return null;
            Require(session.Stage==11&&session.Phase==SessionPhase.Combat,"Stage button resumes saved checkpoint");
            session.SetPaused(true);bootstrap.Save();
            yield return Capture("15-checkpoint-resume");
            string report=$"{{\"passed\":true,\"tutorial\":true,\"bossRewards\":{rewards},\"storyLines\":{storyLines},\"minibossCurrencyOnly\":{sawMinibossCurrency.ToString().ToLowerInvariant()},\"travel\":{sawTravel.ToString().ToLowerInvariant()},\"stageBanner\":{sawBanner.ToString().ToLowerInvariant()},\"bossIntro\":{sawBossIntro.ToString().ToLowerInvariant()},\"deathLantern\":{sawDeath.ToString().ToLowerInvariant()},\"perfectDodges\":{perfect},\"normalDodges\":{normal},\"criticalHits\":{critical},\"playerHits\":{hits},\"checkpoint\":{session.CheckpointStartStage},\"attackLevel\":{session.GetStatLevel(0)},\"equippedRelics\":{session.EquippedRelicIds.Count}}}";
            File.WriteAllText(Path.Combine(output,"smoke-result.json"),report);
            Debug.Log("LAST_BELL_SMOKE_PASSED: "+report);finished=true;
            yield return null;Application.Quit(0);
        }

        IEnumerator FeedbackReview()
        {
            // Explicit isolated visual fixture. Never writes this high-level wallet to a player save.
            var save=new GameSession(session.Catalog).ExportSave();save.phase=SessionPhase.Lobby;save.tutorialStep=2;save.run=null;
            save.gold=3000000000L;save.fragments=999;save.characters[0].statLevels=new[]{49,49,49,49};
            session=new GameSession(session.Catalog,save);presenter.Bind(session);yield return null;
            Require(presenter.View.Stats[0].Price.text=="8,968,311 G","Exact highest upgrade price visible");
            yield return Capture("lobby-high-price");
            var texts=presenter.View.LobbyPanel.GetComponentsInChildren<UnityEngine.UI.Text>();var sizes=new int[texts.Length];
            for(int i=0;i<texts.Length;i++){sizes[i]=texts[i].fontSize;texts[i].fontSize=Mathf.CeilToInt(sizes[i]*1.2f);}
            yield return Capture("lobby-text-120");
            for(int i=0;i<texts.Length;i++)texts[i].fontSize=sizes[i];
            presenter.View.StartRun.onClick.Invoke();session.Tick(.7f);yield return null;
            session.Tick(.49f);yield return Capture("timing-normal-start",.01f);
            session.Tick(.11f);yield return null;
            Press(presenter.View.Dodge[0]);Require(presenter.View.DodgeResult.Label.text=="","No perfect feedback on press");
            session.Tick(.05f);yield return Capture("perfect-confirmed",.01f);
            Require(presenter.View.DodgeResult.Label.text=="완벽","Perfect feedback only after impact");
            session.SetPaused(true);float alpha=presenter.View.DodgeResult.Label.color.a;yield return new WaitForSecondsRealtime(.4f);
            Require(Mathf.Approximately(alpha,presenter.View.DodgeResult.Label.color.a),"Perfect feedback lifetime freezes during pause");
            session.SetPaused(false);yield return new WaitForSecondsRealtime(.4f);
            Require(presenter.View.DodgeResult.Label.color.a<=.001f,"Perfect label fades after resume");
            var combatSave=session.ExportSave();
            for(int step=0;step<4;step++)
            {
                var directionSave=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(combatSave));
                directionSave.run.stage=8;directionSave.run.patternStep=step;
                session=new GameSession(session.Catalog,directionSave);session.SetPaused(false);presenter.Bind(session);yield return null;
                Require((int)presenter.View.Timing.DirectionMarker.Symbol==(int)session.EnemyIntent.Direction,"Direction marker matches the required dodge");
                yield return Capture("direction-"+session.EnemyIntent.Direction,.01f);
            }
            var travelSave=session.ExportSave();travelSave.run.enemyHpMilli=1000;travelSave.run.enemyMaxHpMilli=1000;
            session=new GameSession(session.Catalog,travelSave);session.SetPaused(false);presenter.Bind(session);
            session.Attack();session.Tick(.2f);session.Tick(.55f);yield return null;
            Require(session.Transition.Kind==TransitionKind.Travel,"Run preview samples actual travel");
            for(int frame=0;frame<48;frame++)
            {yield return Capture("run-"+frame.ToString("D2"),.001f);session.Tick(1f/24);}
            session.Tick(10);yield return null;
            var deadSave=session.ExportSave();deadSave.run.playerHpMilli=1000;
            session=new GameSession(session.Catalog,deadSave);session.SetPaused(false);presenter.Bind(session);
            session.Tick(session.EnemyIntent.SecondsToHit);session.Tick(1.8f);yield return null;
            Require(session.Phase==SessionPhase.DeathResult&&presenter.View.ResultPanel.activeSelf,"Death holds a visible result screen");
            long resultGold=session.Result.Gold,wallet=session.Gold;int resultStage=session.Result.DefeatedStage;
            yield return Capture("death-result");
            session=new GameSession(session.Catalog,JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.ExportSave())));presenter.Bind(session);yield return null;
            Require(session.Result.DefeatedStage==resultStage&&session.Result.Gold==resultGold&&session.Gold==wallet,"Result restore retains earnings without paying twice");
            presenter.View.ResultRetry.onClick.Invoke();yield return null;
            Require(session.Phase==SessionPhase.Transition&&session.Stage==1&&session.Gold==wallet&&session.ExportSave().run.earnedGold==0,"Retry starts checkpoint and resets only run earnings");
            yield return Capture("result-retry");
            session.ReturnToLobby();yield return null;
            File.WriteAllText(Path.Combine(output,"feedback-result.json"),"{\"passed\":true,\"exactHighPrice\":8968311,\"largeWallet\":3000000000,\"perfectAfterImpact\":true,\"pauseFreezesFeedback\":true,\"fourDirections\":true,\"runFrames\":48,\"captureFps\":24,\"runCycleMs\":400,\"travelPixels\":1920,\"deathResult\":true,\"resultRestore\":true,\"retry\":true}");
            Debug.Log("LAST_BELL_FEEDBACK_SMOKE_PASSED");finished=true;Application.Quit(0);
        }

        IEnumerator ApprovedRunReview()
        {
            var save=new GameSession(session.Catalog).ExportSave();save.phase=SessionPhase.Lobby;save.tutorialStep=2;save.run=null;
            session=new GameSession(session.Catalog,save);session.StartRun();session.Tick(.7f);
            save=session.ExportSave();save.run.enemyHpMilli=save.run.enemyMaxHpMilli=1000;
            session=new GameSession(session.Catalog,save);session.SetPaused(false);presenter.Bind(session);presenter.Effects.ReducedShake=false;
            yield return Capture("idle-before",.01f);
            var actor=presenter.PlayerActor;var scale=actor.Body.transform.localScale;var rootPosition=actor.transform.localPosition;
            session.Attack();session.Tick(.2f);session.Tick(.55f);yield return null;
            Require(session.Transition.Kind==TransitionKind.Travel,"Approved run entered actual travel after the killing blow");
            foreach(var number in presenter.Effects.Numbers)number.gameObject.SetActive(false);
            int[] expected={0,0,1,1,2,2,2,3,3,3,4,4,5,5,6,6,6,7,7,7};
            var timeline=new List<int>();
            for(int i=0;i<100;i++)
            {
                yield return Capture("run-"+i.ToString("D3"),.001f);
                Require(actor.CurrentMotion==ActorMotion.Run&&actor.CurrentFrame==expected[i%20],"Authored 40/60ms pose at travel sample "+i);
                Require(actor.Body.transform.localScale==scale&&Vector3.Distance(actor.transform.localPosition,rootPosition)<.001f,"Run keeps idle scale and fixed travel root");
                timeline.Add(actor.CurrentFrame);
                if(i==25)
                {
                    session.SetPaused(true);int frame=actor.CurrentFrame;float pixels=presenter.Presentation.TravelPixels;
                    yield return new WaitForSecondsRealtime(.15f);
                    Require(actor.CurrentFrame==frame&&presenter.Presentation.TravelPixels==pixels,"Pause freezes run pose and world travel");session.SetPaused(false);
                }
                session.Tick(.02f);
            }
            // The isolated fixture also verifies the same authored pose through both facing paths.
            presenter.enabled=false;actor.ApplyArt(presenter.Art.Find("hero_on"),false);actor.SampleMotion(ActorMotion.Run,.08f);actor.SetPlaybackPaused(true);
            yield return Capture("run-facing-left",.001f);Require(actor.Body.flipX&&actor.CurrentFrame==2,"Approved run mirrors left without changing pose");
            actor.ApplyArt(presenter.Art.Find("hero_on"),true);actor.SampleMotion(ActorMotion.Run,.08f);actor.SetPlaybackPaused(true);
            yield return Capture("run-facing-right",.001f);Require(!actor.Body.flipX&&actor.CurrentFrame==2,"Approved run faces right");
            actor.SetPlaybackPaused(false);presenter.enabled=true;session.Tick(.001f);yield return Capture("arrival-after",.01f);
            Require(session.Transition.Kind==TransitionKind.Arrival&&Mathf.Abs(presenter.Presentation.TravelPixels-1920)<.01f,"Run exits to arrival after 1920 planning pixels");
            Require(actor.CurrentMotion==ActorMotion.Idle&&actor.Body.transform.localScale==scale&&Vector3.Distance(actor.transform.localPosition,rootPosition)<.001f,"Arrival restores idle without root or scale jump");
            File.WriteAllText(Path.Combine(output,"run-result.json"),"{\"passed\":true,\"sourceId\":\"hero-approved-dash-20261021-02\",\"frameDurationsMs\":[40,40,60,60,40,40,60,60],\"cycleMs\":400,\"captureFps\":50,\"captureFrames\":100,\"travelPixels\":1920,\"pause\":true,\"bothFacings\":true,\"entryExit\":true,\"frameTimeline\":["+string.Join(",",timeline)+"]}");
            Debug.Log("LAST_BELL_APPROVED_RUN_SMOKE_PASSED");finished=true;Application.Quit(0);
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
        static void Press(PointerCommand button){var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};button.OnPointerDown(e);button.OnPointerUp(e);}
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
