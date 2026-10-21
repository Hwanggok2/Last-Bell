#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastBell.Tests
{
    public sealed class CombatPresentationTests
    {
        GameObject root,viewObject,cameraObject,eventObject;
        MvpPresenter presenter;
        GameCatalog catalog;
        BattlePresentation presentation;
        PresentationOverlay overlay;

        [SetUp]
        public void SetUp()
        {
            catalog=JsonUtility.FromJson<GameCatalog>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Data/Generated/mvp-catalog.json").text);
            root=new GameObject("Presentation test");root.SetActive(false);
            var world=new GameObject("World");world.transform.SetParent(root.transform,false);
            cameraObject=new GameObject("Test camera");var camera=cameraObject.AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=3.1f;camera.transform.position=new Vector3(0,0,-10);
            viewObject=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/GameView.prefab"));
            var actor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Art/Presentation/PresentationActor.prefab");
            Assert.That(actor,Is.Not.Null,"The integration fixture requires the stored presentation actor.");
            presenter=root.AddComponent<MvpPresenter>();presenter.WorldRoot=world.transform;presenter.WorldCamera=camera;
            presenter.PlayerActor=Object.Instantiate(actor,world.transform).GetComponent<ActorView>();
            presenter.EnemyActor=Object.Instantiate(actor,world.transform).GetComponent<ActorView>();
            presenter.PlayerActor.transform.localPosition=new Vector3(-.8f,-1.984f,0);
            presenter.EnemyActor.transform.localPosition=new Vector3(1.45f,-1.984f,0);
            presenter.View=viewObject.GetComponent<MvpView>();presenter.Effects=viewObject.GetComponentInChildren<BattleEffects>(true);
            presenter.Effects.WorldRoot=world.transform;
            presenter.Art=AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/Game/Art/Presentation/PresentationArtCatalog.asset");
            Assert.That(presenter.Art,Is.Not.Null);
            // Use the real stored UI/actor assets and explicitly wire the new director.
            // No audio device or camera pixel comparison is part of these automated tests.
            presentation=root.AddComponent<BattlePresentation>();presenter.Presentation=presentation;
            presentation.World=world.AddComponent<ParallaxWorld>();
            presentation.EnemyEffects=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/CombatV55/EnemyAttackPool.prefab"),world.transform).GetComponent<EnemyAttackEffects>();
            presentation.Telegraphs=presentation.EnemyEffects.GetComponentInChildren<EnemyTelegraphGraphic>();
            presentation.Vfx=Graphic<CombatVfx>("Test combat VFX",presenter.View.SafeRoot);
            presentation.Banner=Graphic<StageBannerGraphic>("Test stage banner",presenter.View.SafeRoot);
            overlay=Graphic<PresentationOverlay>("Test story overlay",presenter.View.SafeRoot);presentation.Overlay=overlay;
            overlay.StorySpeaker=Graphic<Text>("Speaker",overlay.transform);overlay.StorySpeaker.font=presenter.View.Stage.font;
            overlay.StoryText=Graphic<Text>("Dialogue",overlay.transform);overlay.StoryText.font=presenter.View.Stage.font;
            overlay.ButtonNext=new GameObject("Next",typeof(RectTransform),typeof(Button)).GetComponent<Button>();overlay.ButtonNext.transform.SetParent(overlay.transform,false);
            overlay.ButtonSkip=new GameObject("Skip",typeof(RectTransform),typeof(Button)).GetComponent<Button>();overlay.ButtonSkip.transform.SetParent(overlay.transform,false);
            eventObject=new GameObject("Test event system",typeof(EventSystem));
            root.SetActive(true);presenter.Effects.ReducedShake=false;
        }

        static T Graphic<T>(string name,Transform parent) where T:Graphic
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(T));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var graphic=obj.GetComponent<T>();graphic.raycastTarget=false;return graphic;
        }

        GameSession PlayingAt(int stage=1,bool weakEnemy=false)
        {
            var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.tutorialStep=2;save.run=null;
            var session=new GameSession(catalog,save);session.StartRun();save=session.ExportSave();
            save.phase=SessionPhase.Combat;save.run.transition=null;
            save.run.stage=stage;save.run.patternStep=0;
            save.run.enemyHpMilli=weakEnemy?1000:catalog.GetStage(stage).maxHp*1000L;
            save.run.enemyMaxHpMilli=save.run.enemyHpMilli;
            session=new GameSession(catalog,save);session.SetPaused(false);presenter.Bind(session);return session;
        }

        static void Press(PointerCommand button)
        {
            button.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
        }

        [UnityTest]
        public IEnumerator HighContactFrameAcceptsAttackAndStaysAtHome()
        {
            var session=PlayingAt(3);presenter.TickSession=true;int perfect=0;
            session.EventRaised+=e=>{if(e.Kind==SessionEventKind.PerfectParry)perfect++;};
            float deadline=Time.realtimeSinceStartup+3;
            while(!session.IsEnemyImpactPending&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(session.IsEnemyImpactPending,Is.True);Assert.That(presenter.View.Attack.Interactable,Is.True);
            Assert.That(presenter.View.Rhythm.WaveProgress,Is.EqualTo(1).Within(.0001));
            Assert.That(presenter.View.Dodge[2].gameObject.activeSelf,Is.False);
            Press(presenter.View.Attack);yield return null;
            Assert.That(perfect,Is.EqualTo(1));Assert.That(session.PlayerHp,Is.EqualTo(100));
            Assert.That(presenter.View.DodgeResult.Label.text,Is.EqualTo("완벽 쳐내기"));
            Assert.That(presenter.PlayerActor.transform.localPosition,Is.EqualTo(ParallaxWorld.PlanPoint(500,590)));
            Assert.That(presenter.PlayerActor.CurrentMotion,Is.EqualTo(ActorMotion.AttackUp));
        }

        [UnityTest]
        public IEnumerator TimingCirclesUseFullViewportAndPauseWithTheSameClock()
        {
            var session=PlayingAt(9);presenter.TickSession=false;session.Tick(.8f);yield return null;
            var rhythm=presenter.View.Rhythm;Assert.That(rhythm.transform.parent,Is.EqualTo(presenter.View.transform));
            Assert.That(rhythm.BellCount,Is.EqualTo(2));Assert.That(rhythm.ContactRadius,Is.EqualTo(rhythm.rectTransform.rect.width*.625f));
            float progress=rhythm.WaveProgress;session.SetPaused(true);yield return new WaitForSecondsRealtime(.1f);
            Assert.That(rhythm.WaveProgress,Is.EqualTo(progress));
            presenter.Effects.ReducedShake=true;yield return null;Assert.That(rhythm.WaveProgress,Is.EqualTo(progress));
            Assert.That(rhythm.raycastTarget,Is.False);
        }

        [UnityTest]
        public IEnumerator SelectedPreviewPosesMatchBack08AndSlashSequence()
        {
            var session=PlayingAt(2);presenter.TickSession=false;session.Tick(.49f);session.Dodge(DodgeDirection.Back);session.Tick(.025f);yield return null;
            var actor=presenter.PlayerActor;var back=presenter.Art.Find("hero_on").FindClip(ActorMotion.DodgeBack);
            Assert.That(back.sourceFrames[actor.CurrentFrame],Is.EqualTo(8));
            session.Tick(.025f);session.Attack();session.Tick(.06f);yield return null;
            Assert.That(actor.CurrentMotion,Is.EqualTo(ActorMotion.AttackUp));Assert.That(actor.CurrentFrame,Is.EqualTo(0));
            session.Tick(.14f);yield return null;Assert.That(actor.CurrentFrame,Is.EqualTo(7));
            session.Tick(.18f);yield return null;Assert.That(actor.CurrentFrame,Is.EqualTo(11));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);Object.DestroyImmediate(viewObject);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(eventObject);
        }

        [UnityTest]
        public IEnumerator GoldWarningsReplaceGaugeTextAndAnswerHighlights()
        {
            for(int stage=1;stage<=4;stage++)
            {
                var session=PlayingAt(stage);presenter.TickSession=false;yield return null;
                Assert.That(presenter.View.Timing.gameObject.activeSelf,Is.False);
                Assert.That(presenter.View.Danger.gameObject.activeSelf,Is.False);
                foreach(var symbol in presenter.View.DodgeSymbols.Where((x,i)=>i!=2))Assert.That(symbol.color,Is.EqualTo(presenter.View.DodgeSymbols[0].color));
                Assert.That(presenter.View.Dodge[2].gameObject.activeSelf,Is.False);
                Assert.That(presentation.Telegraphs.canvas.renderMode,Is.EqualTo(RenderMode.WorldSpace));
                Assert.That(session.EnemyThreats[0].Bounds.Overlaps(CombatGeometry.Body(session.EnemyIntent.Direction,true)),Is.EqualTo(stage==3));
            }
        }

        [UnityTest]
        public IEnumerator PerfectWaveUsesViewportAndCounterSnapsOnlyOnAcceptedAttack()
        {
            var session=PlayingAt();presenter.TickSession=false;
            session.Tick(.49f);session.Dodge(DodgeDirection.Up);session.Tick(.05f);yield return null;
            var feedback=presenter.View.DodgeResult;
            Assert.That(feedback.transform.parent,Is.EqualTo(presenter.View.transform));
            Assert.That(feedback.WaveCenter,Is.EqualTo(Vector2.zero));
            Assert.That(feedback.MaximumRadius,Is.GreaterThan(feedback.rectTransform.rect.size.magnitude/2));
            Assert.That(feedback.Label.text,Is.EqualTo("완벽"));
            Assert.That(presenter.PlayerActor.transform.localPosition.y,Is.GreaterThan(ParallaxWorld.PlanPoint(500,590).y));
            Assert.That(session.Attack(),Is.True);presentation.TickVisuals(0);
            Assert.That(presenter.PlayerActor.transform.localPosition,Is.EqualTo(ParallaxWorld.PlanPoint(500,590)));
            session.Tick(.12f);presentation.TickVisuals(0);Assert.That(presenter.PlayerActor.transform.localPosition.x,Is.GreaterThan(ParallaxWorld.PlanPoint(500,590).x));
            int particles=presentation.EnemyEffects.ActiveParticles;session.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);
            Assert.That(feedback.Label.text,Is.EqualTo("완벽"));Assert.That(presentation.EnemyEffects.ActiveParticles,Is.EqualTo(particles));
            Assert.That(presentation.EnemyEffects.ParticleCapacity,Is.EqualTo(96));
            session.SetPaused(false);session.Tick(.31f);yield return null;Assert.That(feedback.Label.text,Is.Empty);
        }

        [UnityTest]
        public IEnumerator ApprovedRunPreservesAuthoredRhythmAndTravels1920PixelsInTwoSeconds()
        {
            var session=PlayingAt(1,true);presenter.TickSession=false;
            session.Attack();session.Tick(.2f);session.Tick(.55f);presentation.TickVisuals(0);
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Travel));
            var run=presenter.Art.Find("hero_on").FindClip(ActorMotion.Run);
            Assert.That(run.sourceId,Is.EqualTo("hero-approved-dash-20261021-02"));Assert.That(run.frames.Length,Is.EqualTo(8));
            Assert.That(run.frameDurationsMs,Is.EqualTo(new[]{40,40,60,60,40,40,60,60}));Assert.That(run.Duration,Is.EqualTo(.4f));
            session.Tick(1);presentation.TickVisuals(0);Assert.That(presentation.TravelPixels,Is.EqualTo(960).Within(.01));
            session.Tick(1);presentation.TickVisuals(0);Assert.That(presentation.TravelPixels,Is.EqualTo(1920).Within(.01));
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Arrival));yield return null;
        }

        [Test]
        public void ApprovedRunKeepsScaleAndRootAcrossAllFramesBothFacingsAndIdleTransitions()
        {
            var actor=presenter.PlayerActor;var art=presenter.Art.Find("hero_on");
            var rootPosition=actor.transform.position;
            foreach(bool right in new[]{true,false})
            {
                actor.ApplyArt(art,right);actor.SampleMotion(ActorMotion.Idle,0);
                var scale=actor.Body.transform.localScale;var offset=actor.Body.transform.localPosition;
                float[] times={0,.04f,.08f,.14f,.20f,.24f,.28f,.34f};
                for(int i=0;i<times.Length;i++)
                {
                    actor.SampleMotion(ActorMotion.Run,times[i]);
                    Assert.That(actor.CurrentFrame,Is.EqualTo(i));Assert.That(actor.Body.flipX,Is.EqualTo(!right));
                    Assert.That(actor.Body.transform.localScale,Is.EqualTo(scale));Assert.That(actor.Body.transform.localPosition,Is.EqualTo(offset));
                    Assert.That(actor.transform.position,Is.EqualTo(rootPosition));
                    Assert.That(Vector2.Distance(actor.Body.sprite.pivot,new Vector2(60,17)),Is.LessThan(.01f));
                    Assert.That(Vector3.Distance(actor.SourcePixelToWorld(new Vector2(60,111)),rootPosition),Is.LessThan(.0001f));
                }
                actor.SampleMotion(ActorMotion.Idle,0);Assert.That(actor.Body.transform.localScale,Is.EqualTo(scale));
                Assert.That(actor.transform.position,Is.EqualTo(rootPosition));
            }
        }

        [UnityTest]
        public IEnumerator VisibleImpactFrameAcceptsPointerDownBeforeResolution()
        {
            var session=PlayingAt();float hp=session.PlayerHp;int perfect=0;
            session.EventRaised+=e=>{if(e.Kind==SessionEventKind.PerfectDodge)perfect++;};
            float deadline=Time.realtimeSinceStartup+5;
            while(!session.IsEnemyImpactPending&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(session.IsEnemyImpactPending,Is.True);
            Assert.That(presenter.View.Timing.gameObject.activeInHierarchy,Is.False);
            Assert.That(session.EnemyThreats[0].ImpactAtMs,Is.EqualTo(session.ClockMs));
            Assert.That(session.PlayerHp,Is.EqualTo(hp));
            Press(presenter.View.Dodge[(int)session.EnemyIntent.Direction]);
            yield return null;
            Assert.That(perfect,Is.EqualTo(1));Assert.That(session.PlayerHp,Is.EqualTo(hp));
        }

        [UnityTest]
        public IEnumerator VisibleImpactWithoutInputStillDealsDamage()
        {
            var session=PlayingAt();float hp=session.PlayerHp;float deadline=Time.realtimeSinceStartup+5;
            while(!session.IsEnemyImpactPending&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(session.EnemyThreats[0].ImpactAtMs,Is.EqualTo(session.ClockMs));Assert.That(session.PlayerHp,Is.EqualTo(hp));
            yield return null;
            Assert.That(session.PlayerHp,Is.LessThan(hp));
        }

        [UnityTest]
        public IEnumerator KillingPopupAndShakeSurviveStagePreparedThenExpire()
        {
            var session=PlayingAt(1,true);presenter.TickSession=false;Press(presenter.View.Attack);session.Tick(.2f);
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
            var popup=presenter.Effects.Numbers.FirstOrDefault(x=>x.gameObject.activeSelf);
            Assert.That(popup,Is.Not.Null);var start=popup.rectTransform.anchoredPosition;
            yield return null;
            Assert.That(presenter.Effects.WorldOffset.sqrMagnitude,Is.GreaterThan(0));
            session.Tick(.55f);yield return null;
            Assert.That(session.Stage,Is.EqualTo(2));
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Travel));
            Assert.That(popup.gameObject.activeSelf,Is.True,"StagePrepared must not erase the killing blow.");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(popup.gameObject.activeSelf,Is.True);
            Assert.That(popup.rectTransform.anchoredPosition.x,Is.GreaterThan(start.x));
            Assert.That(popup.rectTransform.anchoredPosition.y,Is.GreaterThan(start.y));
            Assert.That(presenter.Effects.WorldOffset,Is.EqualTo(Vector3.zero));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(popup.gameObject.activeSelf,Is.False);
        }

        [UnityTest]
        public IEnumerator BossCollapseThenStoryButtonsThenRelicChoiceOpenTheNextStage()
        {
            var session=PlayingAt(10,true);presenter.TickSession=false;Press(presenter.View.Attack);session.Tick(.2f);yield return null;
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
            Assert.That(presenter.View.BattlePanel.activeSelf,Is.True);
            Assert.That(presenter.View.RewardPanel.activeSelf,Is.False);
            Assert.That(presenter.View.Attack.Interactable,Is.False);
            Assert.That(session.CheckpointStartStage,Is.EqualTo(1));
            session.Tick(.55f);yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.StoryDialogue));
            Assert.That(overlay.StoryText.gameObject.activeInHierarchy,Is.True);
            Assert.That(overlay.StoryText.text,Is.EqualTo("문은… 안에서 잠갔다."));
            Assert.That(presenter.View.RewardPanel.activeSelf,Is.False);
            overlay.ButtonNext.onClick.Invoke();yield return null;
            Assert.That(session.Story.LineIndex,Is.EqualTo(1));
            overlay.ButtonSkip.onClick.Invoke();yield return null;
            Assert.That(presenter.View.RewardPanel.activeSelf,Is.True);
            Assert.That(session.RelicChoices.Count,Is.EqualTo(3));
            Assert.That(session.CheckpointStartStage,Is.EqualTo(1));
            presenter.View.Rewards[0].Receive.onClick.Invoke();yield return null;
            Assert.That(session.Stage,Is.EqualTo(11));
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Travel));
            Assert.That(session.CheckpointStartStage,Is.EqualTo(11));
            session.Tick(2);yield return null;
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Arrival));
            session.Tick(.4f);yield return null;
            Assert.That(presentation.ShowsLargeStage,Is.True);
            Assert.That(presenter.View.Stage.gameObject.activeSelf,Is.False);
            Assert.That(presenter.View.Danger.gameObject.activeSelf,Is.False);
            session.Tick(.3f);yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Combat));
            Assert.That(presenter.View.Stage.text,Is.EqualTo("Stage 11"));
            Assert.That(presenter.View.Danger.gameObject.activeSelf,Is.False);
            Assert.That(presenter.View.Attack.Interactable,Is.True);
        }

        [UnityTest]
        public IEnumerator AttackApproachesAndContactsOnceAt200MsThenReturns()
        {
            var session=PlayingAt();presenter.TickSession=false;
            float before=session.EnemyHp;int hits=0;
            session.EventRaised+=e=>{if(e.Kind==SessionEventKind.EnemyDamaged)hits++;};
            Press(presenter.View.Attack);session.Tick(.12f);yield return null;
            Assert.That(presenter.PlayerActor.transform.localPosition.x,Is.GreaterThan(ParallaxWorld.PlanPoint(500,590).x));
            Assert.That(session.EnemyHp,Is.EqualTo(before));
            session.Tick(.079f);yield return null;Assert.That(hits,Is.Zero);
            session.Tick(.001f);
            Assert.That(hits,Is.EqualTo(1));Assert.That(session.EnemyHp,Is.LessThan(before));
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.EqualTo(1));
            session.Tick(.299f);yield return null;
            Assert.That(presenter.PlayerActor.transform.localPosition.x,Is.EqualTo(ParallaxWorld.PlanPoint(500,590).x).Within(.001));
            Assert.That(hits,Is.EqualTo(1));Assert.That(session.Attack(),Is.False);
            session.Tick(.001f);Assert.That(session.Attack(),Is.True);
        }

        [UnityTest]
        public IEnumerator PauseFreezesTransitionActorAndPopupUntilResume()
        {
            var session=PlayingAt(weakEnemy:true);presenter.TickSession=false;
            Press(presenter.View.Attack);session.Tick(.2f);session.Tick(.8f);yield return null;
            var popup=presenter.Effects.Numbers.First(x=>x.gameObject.activeSelf);
            presenter.View.Pause.onClick.Invoke();
            Assert.That(session.IsPaused,Is.True);
            double time=session.Transition.ElapsedSeconds;var position=popup.rectTransform.anchoredPosition;
            var actorPosition=presenter.PlayerActor.transform.localPosition;int frame=presenter.PlayerActor.CurrentFrame;
            session.Tick(1);yield return new WaitForSecondsRealtime(.15f);
            Assert.That(session.Transition.ElapsedSeconds,Is.EqualTo(time));
            Assert.That(popup.rectTransform.anchoredPosition,Is.EqualTo(position));
            Assert.That(presenter.PlayerActor.transform.localPosition,Is.EqualTo(actorPosition));
            Assert.That(presenter.PlayerActor.CurrentFrame,Is.EqualTo(frame));
            presenter.View.Resume.onClick.Invoke();session.Tick(.1f);yield return null;
            Assert.That(session.IsPaused,Is.False);Assert.That(session.Transition.ElapsedSeconds,Is.GreaterThan(time));
        }

        [UnityTest]
        public IEnumerator MinibossPaysFragmentsAndContinuesWithoutRewardOrStoryPanel()
        {
            var session=PlayingAt(5,true);presenter.TickSession=false;Press(presenter.View.Attack);session.Tick(.2f);yield return null;
            Assert.That(session.Fragments,Is.EqualTo(1));Assert.That(session.RelicChoices.Count,Is.Zero);
            session.Tick(.55f);yield return null;
            Assert.That(session.Stage,Is.EqualTo(6));Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Travel));
            Assert.That(presenter.View.RewardPanel.activeSelf,Is.False);Assert.That(overlay.ButtonNext.gameObject.activeSelf,Is.False);
            session.Tick(2.7f);yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Combat));Assert.That(session.CheckpointStartStage,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BossZoomHoldsThenRestoresBeforeAnyCueOrDamage()
        {
            var session=PlayingAt(9,true);presenter.TickSession=false;Press(presenter.View.Attack);session.Tick(.2f);session.Tick(3.25f);
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.BossIntro));
            session.Tick(.7f);yield return null;float zoom=presenter.WorldCamera.orthographicSize;
            Assert.That(zoom,Is.LessThan(3.1f));Assert.That(presenter.View.Danger.gameObject.activeSelf,Is.False);
            float hp=session.PlayerHp,enemyHp=session.EnemyHp;
            Press(presenter.View.Attack);session.Tick(.2f);yield return null;
            Assert.That(presenter.WorldCamera.orthographicSize,Is.EqualTo(zoom).Within(.001));
            Assert.That(session.PlayerHp,Is.EqualTo(hp));Assert.That(session.EnemyHp,Is.EqualTo(enemyHp));
            session.Tick(1.3f);yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Combat));
            Assert.That(presenter.WorldCamera.orthographicSize,Is.EqualTo(3.1f).Within(.001));
            Assert.That(presenter.View.Danger.gameObject.activeSelf,Is.False);Assert.That(presenter.View.BossHpRoot.activeSelf,Is.True);
        }

        [UnityTest]
        public IEnumerator DeathShowsResultAfterLanternAndRetryStartsAtCheckpoint()
        {
            var session=PlayingAt();presenter.TickSession=false;
            var save=session.ExportSave();save.run.playerHpMilli=1000;
            session=new GameSession(catalog,save);session.SetPaused(false);presenter.Bind(session);
            session.Tick(.9f);session.Tick(.9f);yield return null;
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Death));
            Assert.That(presenter.View.LobbyPanel.activeSelf,Is.False);
            Assert.That(presenter.View.ResultPanel.activeSelf,Is.False);
            presenter.View.StartRun.onClick.Invoke();Assert.That(session.Phase,Is.EqualTo(SessionPhase.Transition));
            session.Tick(.9f);yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.DeathResult));
            Assert.That(presenter.View.ResultPanel.activeSelf,Is.True);
            Assert.That(presenter.View.ResultTitle.text,Does.Contain("Stage 1"));
            presenter.View.ResultRetry.onClick.Invoke();yield return null;
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Arrival));
        }

        [UnityTest]
        public IEnumerator ImportantEnemyWindupKeepsAttackWithHitFlashAndLethalHitKeepsDeath()
        {
            // QA-004: damage is a color overlay during an important attack pose;
            // it must neither replace the telegraph with Hit nor restart its clock.
            var session=PlayingAt();presenter.TickSession=false;
            presentation.TickVisuals(0);
            var cue=session.EnemyIntent;var before=session.ExportSave().run;
            Assert.That(cue.Visible,Is.True);
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.Attack));
            Press(presenter.View.Attack);session.Tick(.2f);
            // Sample synchronously so a slow test frame cannot outlast the 120ms flash.
            presentation.TickVisuals(0);
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.GreaterThan(0));
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.Attack));
            Assert.That(session.EnemyIntent.Visible,Is.True);
            Assert.That(session.EnemyIntent.PatternId,Is.EqualTo(cue.PatternId));
            Assert.That(session.EnemyIntent.Direction,Is.EqualTo(cue.Direction));
            Assert.That(session.ExportSave().run.enemyHitAtMs,Is.EqualTo(before.enemyHitAtMs));
            Assert.That(session.ExportSave().run.telegraphStartMs,Is.EqualTo(before.telegraphStartMs));
            Assert.That(session.EnemyIntent.SecondsToHit,Is.EqualTo(cue.SecondsToHit-.2f).Within(.0001));

            session=PlayingAt(weakEnemy:true);Press(presenter.View.Attack);session.Tick(.2f);
            presentation.TickVisuals(0);
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.GreaterThan(0));
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.Death));
            yield return new WaitForSecondsRealtime(.2f);
            session.Tick(.3f);presentation.TickVisuals(0);
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.Death),"After the 180ms hurt playback expires, defeat must not fall back to Idle.");
            Assert.That(presenter.EnemyActor.DissolveAmount,Is.GreaterThan(0));
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
        }

        [UnityTest]
        public IEnumerator ReducedMotionKeepsPopupStationaryWithoutWorldShake()
        {
            PlayingAt();presenter.TickSession=false;presenter.Effects.ReducedShake=true;
            presenter.Effects.Hit(123,true,new Vector2(50,50),Vector2.zero,true);
            var popup=presenter.Effects.Numbers.First(x=>x.gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(popup.gameObject.activeSelf,Is.True);
            Assert.That(popup.rectTransform.anchoredPosition,Is.EqualTo(new Vector2(50,50)));
            Assert.That(popup.color.a,Is.InRange(.01f,.9f));
            Assert.That(presenter.Effects.WorldOffset,Is.EqualTo(Vector3.zero));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(popup.gameObject.activeSelf,Is.False);
        }
    }
}
#endif
