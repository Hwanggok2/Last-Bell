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
            var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.tutorialStep=2;save.prologueSeen=true;save.comboHintSeen=true;save.comboPracticeCompleted=true;save.run=null;
            var session=new GameSession(catalog,save);session.StartRun();save=session.ExportSave();
            save.phase=SessionPhase.Combat;save.run.transition=null;
            save.run.stage=stage;save.run.patternStep = 0;
            save.run.randomSequence=catalog.GetStage(stage).patternSteps.Select(x=>x.danger).ToArray();save.run.sequenceCursor=0;
            save.run.enemyHpMilli=weakEnemy?1000:catalog.GetStage(stage).maxHp*1000L;
            save.run.enemyMaxHpMilli=save.run.enemyHpMilli;
            session=new GameSession(catalog,save);session.SetPaused(false);presenter.Bind(session);return session;
        }

        [UnityTest]
        public IEnumerator HeavyDamageTrailFreezesOnPauseAndResumesWithoutChangingHealth()
        {
            var save=PlayingAt(11).ExportSave();
            save.run.combatStats.damageReductionPercent=0;
            save.run.combatStats.maxHp=catalog.GetStage(11).damage*2;
            save.run.playerHpMilli=save.run.combatStats.maxHpMilli;
            var session=new GameSession(catalog,save);session.SetPaused(false);presenter.Bind(session);presenter.TickSession=false;
            var bar=presenter.View.PlayerHealthBar;
            for(int i=0;i<250&&!bar.TrailActive;i++)session.Tick(.02f);
            Assert.That(bar.TrailActive,Is.True,"actual enemy contact must start the heavy-damage trail");
            float hp=bar.Value,trail=bar.TrailValue;session.SetPaused(true);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(bar.TrailValue,Is.EqualTo(trail));Assert.That(bar.Value,Is.EqualTo(hp));
            session.SetPaused(false);yield return new WaitForSecondsRealtime(.8f);
            Assert.That(bar.TrailActive,Is.False);Assert.That(bar.TrailValue,Is.EqualTo(hp));Assert.That(bar.Value,Is.EqualTo(hp));
        }

        static void Press(PointerCommand button)
        {
            button.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
        }

        [UnityTest]
        public IEnumerator AcceptedDodgeAndPerfectDodgePlayAirOnceWithoutBell()
        {
            presentation.Audio=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/PresentationAudio.prefab"),root.transform).GetComponent<BattleAudio>();
            // Isolate player feedback from the enemy's separate release/impact sounds.
            presentation.EnemyEffects=null;
            var audio=presentation.Audio;audio.Muted=false;
            cameraObject.AddComponent<AudioListener>();
            Assert.That(audio.Dodge.name,Is.EqualTo("dodge-air"));
            Assert.That(audio.LongBell,Is.Null);
            foreach(var bell in new[]{audio.BellA,audio.BellB})if(bell){Assert.That(bell.enabled,Is.False);Assert.That(bell.clip,Is.Null);}
            foreach(float lead in new[]{.12f,.05f})
            {
                var session=PlayingAt();presenter.TickSession=false;
                session.Tick(.9f-lead);audio.Sfx.Stop();
                Assert.That(session.Dodge(DodgeDirection.Up),Is.True);
                yield return null;
                Assert.That(audio.Sfx.isPlaying,Is.True,"accepted dodge produces immediate sound for normal and perfect timing");
                audio.Sfx.Stop();
                Assert.That(session.Dodge(DodgeDirection.Up),Is.False);
                yield return null;
                Assert.That(audio.Sfx.isPlaying,Is.False,"rejected input does not produce sound");
                session.Tick(lead+.0001f);yield return null;
                Assert.That(session.CanPerfectCounter,Is.EqualTo(lead<.06f));
                Assert.That(audio.Sfx.isPlaying,Is.False,"result does not replay dodge or add bell");
            }
            var parry=PlayingAt(3);presenter.TickSession=false;parry.Tick(.85f);audio.Sfx.Stop();
            Assert.That(parry.Dodge(DodgeDirection.Down),Is.True);yield return null;
            Assert.That(audio.Sfx.isPlaying,Is.True,"down-key deflection also has immediate dodge sound");
            parry.Tick(.05f);Assert.That(parry.CanPerfectCounter,Is.True);
            parry.SetPaused(true);audio.PlayDodge();yield return null;
            Assert.That(audio.Sfx.isPlaying,Is.False,"paused sound cannot restart");
        }

        [UnityTest]
        public IEnumerator LobbyPortraitPlaysIdleAndRefreshDoesNotResetIt()
        {
            var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.prologueSeen=true;save.tutorialStep=2;save.run=null;
            var session=new GameSession(catalog,save);presenter.Bind(session);presenter.TickSession=false;
            session.SetPaused(true);yield return null;
            var view=presenter.View;var lobby=view.Reference;
            var idle=presenter.Art.Find(catalog.GetCharacter(session.CurrentCharacterId).artId).FindClip(ActorMotion.Idle);
            Assert.That(idle.frames.Length,Is.GreaterThan(1));
            lobby.SamplePortrait(idle.Duration*.55f);var animated=view.LobbyPortrait.sprite;
            Assert.That(animated,Is.EqualTo(idle.frames[idle.FrameIndex(idle.Duration*.55f)]));
            Assert.That(animated,Is.Not.EqualTo(idle.frames[0]));
            presenter.Refresh();yield return null;
            Assert.That(view.LobbyPortrait.sprite,Is.EqualTo(animated),"wallet/stat refresh must preserve the idle phase");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(view.LobbyPortrait.sprite,Is.EqualTo(animated),"paused preview stays on its current frame");
            // Locked characters have no approved art yet; inspecting them must not show the selected hero.
            view.Characters[1].onClick.Invoke();yield return null;
            Assert.That(session.CurrentCharacterId,Is.EqualTo("on"));
            Assert.That(view.LobbyPortrait.sprite,Is.Null);
            view.Characters[0].onClick.Invoke();yield return null;
            Assert.That(view.LobbyPortrait.sprite,Is.EqualTo(idle.frames[0]));
            session.SetPaused(false);yield return new WaitForSecondsRealtime(idle.Duration*.55f);
            Assert.That(view.LobbyPortrait.sprite,Is.Not.EqualTo(idle.frames[0]),"normal selection screen advances without combat ticking");
            session.SetPaused(true);animated=view.LobbyPortrait.sprite;
            view.StartRun.onClick.Invoke();yield return null;
            Assert.That(lobby.UpgradeScreen,Is.True);
            Assert.That(lobby.UpgradePortrait.sprite,Is.EqualTo(animated),"existing preview images share the selected character idle phase");
        }

        [UnityTest]
        public IEnumerator EnemyCuePoseAndIdleRespectPause()
        {
            foreach(int stage in new[]{1,14,15})
            {
                var session=PlayingAt(stage);presenter.TickSession=false;
                yield return null;yield return null;
                var enemy=presenter.EnemyActor;var idle=enemy.GetClip(ActorMotion.Idle);
                Assert.That(idle.frames.Length,Is.GreaterThan(1));
                bool dragon=enemy.ArtId.StartsWith("dragon_");
                // Dragon's approved preparation holds frame zero from cue start;
                // legacy stages retain animated idle until the authored attack begins.
                var cueMotion=enemy.CurrentMotion;
                if(dragon){Assert.That(cueMotion,Is.Not.EqualTo(ActorMotion.Idle));Assert.That(enemy.CurrentFrame,Is.Zero);}
                else Assert.That(cueMotion,Is.EqualTo(ActorMotion.Idle),"legacy early telegraph stays idle at stage "+stage);
                var initial=enemy.Body.sprite;yield return new WaitForSecondsRealtime(idle.Duration*.55f);
                Assert.That(enemy.CurrentMotion,Is.EqualTo(cueMotion));
                if(dragon)Assert.That(enemy.Body.sprite,Is.EqualTo(initial),"manual combat clock holds the preparation pose");
                else Assert.That(enemy.Body.sprite,Is.Not.EqualTo(initial));
                session.SetPaused(true);yield return null;initial=enemy.Body.sprite;
                yield return new WaitForSecondsRealtime(.25f);Assert.That(enemy.Body.sprite,Is.EqualTo(initial));
                session.SetPaused(false);session.TickInputFrame((float)(session.EnemyThreats[0].ImpactAtMs/1000));yield return null;
                Assert.That(enemy.CurrentFrame,Is.EqualTo(enemy.GetClip(ActorMotion.Attack).contactFrame));
            }
        }

        [UnityTest]
        public IEnumerator HighContactFrameAcceptsDownButtonAndStaysAtHome()
        {
            var session=PlayingAt(3);presenter.TickSession=true;int perfect=0;
            session.EventRaised+=e=>{if(e.Kind==SessionEventKind.PerfectParry)perfect++;};
            float deadline=Time.realtimeSinceStartup+3;
            while(!session.IsEnemyImpactPending&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(session.IsEnemyImpactPending,Is.True);Assert.That(presenter.View.Dodge[2].Interactable,Is.True);
            Assert.That(presenter.View.Rhythm.WaveProgress,Is.EqualTo(1).Within(.0001));
            Assert.That(presenter.View.Dodge[2].gameObject.activeSelf,Is.True);
            Press(presenter.View.Dodge[2]);yield return null;
            Assert.That(perfect,Is.EqualTo(1));Assert.That(session.PlayerHp,Is.EqualTo(100));
            Assert.That(presenter.View.DodgeResult.Label.text,Is.EqualTo("완벽 쳐내기"));
            Assert.That(presenter.PlayerActor.transform.localPosition,Is.EqualTo(ParallaxWorld.PlanPoint(500,590)));
            Assert.That(presenter.PlayerActor.CurrentMotion,Is.EqualTo(ActorMotion.DodgeDown));
        }

        [UnityTest]
        public IEnumerator TimingCirclesUseFullViewportAndPauseWithTheSameClock()
        {
            var session=PlayingAt(5);presenter.TickSession=false;session.Tick(1.28f);yield return null;
            var rhythm=presenter.View.Rhythm;Assert.That(rhythm.transform.parent,Is.EqualTo(presenter.View.transform));
            Assert.That(rhythm.BellCount,Is.EqualTo(2));Assert.That(rhythm.ContactRadius,Is.EqualTo(rhythm.rectTransform.rect.width*.625f));
            float progress=rhythm.WaveProgress;session.SetPaused(true);yield return new WaitForSecondsRealtime(.1f);
            Assert.That(rhythm.WaveProgress,Is.EqualTo(progress));
            presenter.Effects.ReducedShake=true;yield return null;Assert.That(rhythm.WaveProgress,Is.EqualTo(progress));
            Assert.That(rhythm.raycastTarget,Is.False);
        }

        [UnityTest]
        public IEnumerator SelectedNativeDodgeAndAttackFollowAcceptedTiming()
        {
            foreach(int stage in new[]{1,2})
            {
                var session=PlayingAt(stage);presenter.TickSession=false;session.Tick(.85f);
                var direction=stage==1?DodgeDirection.Up:DodgeDirection.Back;
                var motion=stage==1?ActorMotion.DodgeUp:ActorMotion.DodgeBack;
                Assert.That(session.Dodge(direction),Is.True);session.Tick(.025f);yield return null;
                var actor=presenter.PlayerActor;var dodge=actor.GetClip(motion);var attack=actor.GetClip(ActorMotion.Attack);
                Assert.That(actor.ArtId,Is.EqualTo(catalog.GetCharacter(session.CurrentCharacterId).artId));
                Assert.That(actor.CurrentMotion,Is.EqualTo(motion));
                Assert.That(actor.Body.sprite,Is.EqualTo(dodge.frames[actor.CurrentFrame]));
                if(stage==2)Assert.That(actor.transform.localPosition.x,Is.LessThan(ParallaxWorld.PlanPoint(500,590).x));
                else Assert.That(actor.transform.localPosition.y,Is.GreaterThan(ParallaxWorld.PlanPoint(500,590).y));
                session.Tick(.025f);yield return null;
                Assert.That(actor.CurrentFrame,Is.EqualTo(dodge.contactFrame),"hold comes from the actual selected clip, not a fixed third pose");
                Assert.That(session.Attack(),Is.True);session.Tick(.119f);yield return null;
                Assert.That(actor.CurrentMotion,Is.EqualTo(ActorMotion.Attack));Assert.That(actor.CurrentFrame,Is.Zero,"first approach pose held before 120ms");
                session.Tick(.001f);yield return null;Assert.That(actor.CurrentFrame,Is.Zero);
                session.Tick(.04f);yield return null;
                Assert.That(actor.CurrentFrame,Is.GreaterThan(0).And.LessThan(attack.contactFrame),"preparation occupies 120-200ms");
                session.Tick(.04f);yield return null;
                Assert.That(actor.CurrentFrame,Is.EqualTo(attack.contactFrame),"native contact remains aligned to the 200ms gameplay hit");
                Assert.That(actor.Body.sprite,Is.EqualTo(attack.frames[attack.contactFrame]));
                session.Tick(.18f);yield return null;Assert.That(actor.CurrentFrame,Is.GreaterThan(attack.contactFrame));
            }
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
                foreach(var symbol in presenter.View.DodgeSymbols)Assert.That(symbol.color,Is.EqualTo(presenter.View.DodgeSymbols[0].color));
                Assert.That(presenter.View.Dodge[2].gameObject.activeSelf,Is.True);
                Assert.That(presentation.Telegraphs.canvas.renderMode,Is.EqualTo(RenderMode.WorldSpace));
                Assert.That(session.EnemyThreats[0].Bounds.Overlaps(CombatGeometry.Body(session.EnemyIntent.Direction,true)),Is.EqualTo(session.EnemyThreats[0].RequiresParry));
            }
        }

        [UnityTest]
        public IEnumerator PerfectWaveUsesViewportAndCounterSnapsOnlyOnAcceptedAttack()
        {
            var session=PlayingAt();presenter.TickSession=false;
            session.Tick(.85f);session.Dodge(DodgeDirection.Up);session.Tick(.05f);yield return null;
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
            var actor=presenter.PlayerActor;var run=actor.GetClip(ActorMotion.Run);
            Assert.That(actor.ArtId,Is.EqualTo(catalog.GetCharacter(session.CurrentCharacterId).artId));
            Assert.That(run.sourceId,Is.Not.Empty);Assert.That(run.frames.Length,Is.EqualTo(8));
            Assert.That(run.frameDurationsMs,Is.EqualTo(new[]{40,40,60,60,40,40,60,60}));Assert.That(run.Duration,Is.EqualTo(.4f));
            float rootX=actor.transform.localPosition.x;
            session.Tick(1);presentation.TickVisuals(0);Assert.That(presentation.TravelPixels,Is.EqualTo(960).Within(.01));
            Assert.That(actor.CurrentMotion,Is.EqualTo(ActorMotion.Run));Assert.That(actor.CurrentFrame,Is.EqualTo(run.FrameIndex(1)));
            Assert.That(actor.transform.localPosition.x,Is.EqualTo(rootX).Within(.0001));
            session.Tick(1);presentation.TickVisuals(0);Assert.That(presentation.TravelPixels,Is.EqualTo(1920).Within(.01));
            Assert.That(actor.transform.localPosition.x,Is.EqualTo(rootX).Within(.0001));
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Arrival));yield return null;
        }

        [Test]
        public void ApprovedRunKeepsScaleAndRootAcrossAllFramesBothFacingsAndIdleTransitions()
        {
            var session=PlayingAt();presenter.TickSession=false;
            var actor=presenter.PlayerActor;var art=presenter.Art.Find(catalog.GetCharacter(session.CurrentCharacterId).artId);
            Assert.That(actor.ArtId,Is.EqualTo(art.id));var run=actor.GetClip(ActorMotion.Run);
            var pivot=run.frames[0].pivot;var size=run.frames[0].rect.size;
            var rootPosition=actor.transform.position;
            foreach(bool right in new[]{true,false})
            {
                actor.ApplyArt(art,right);actor.SampleMotion(ActorMotion.Idle,0);
                var scale=actor.Body.transform.localScale;var offset=actor.Body.transform.localPosition;
                float[] times={0,.04f,.08f,.14f,.20f,.24f,.28f,.34f};
                for(int i=0;i<times.Length;i++)
                {
                    actor.SampleMotion(ActorMotion.Run,times[i]);
                    Assert.That(actor.CurrentFrame,Is.EqualTo(i));Assert.That(actor.Body.flipX,Is.EqualTo(art.facesRight!=right));
                    Assert.That(actor.Body.transform.localScale,Is.EqualTo(scale));Assert.That(actor.Body.transform.localPosition,Is.EqualTo(offset));
                    Assert.That(actor.transform.position,Is.EqualTo(rootPosition));
                    Assert.That(Vector2.Distance(actor.Body.sprite.pivot,pivot),Is.LessThan(.01f));
                    Assert.That(actor.Body.sprite.rect.size,Is.EqualTo(size));
                    Assert.That(Vector3.Distance(actor.SourcePixelToWorld(new Vector2(pivot.x,size.y-pivot.y)),actor.Body.transform.position),Is.LessThan(.0001f));
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
        public IEnumerator BossTenRemainsForDialogueThenRewardContinuesAndTwentyClears()
        {
            var session=PlayingAt(10,true);presenter.TickSession=false;yield return null;
            var bossSprite=presenter.EnemyActor.Body.sprite;
            Press(presenter.View.Attack);session.Tick(.2f);yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.StoryDialogue));Assert.That(session.Transition,Is.Null);
            Assert.That(presenter.EnemyActor.DissolveAmount,Is.Zero);Assert.That(presenter.View.RewardPanel.activeSelf,Is.False);
            Assert.That(presenter.EnemyActor.Body.sprite,Is.SameAs(bossSprite),"Defeat must not swap the boss to another pose asset.");
            Assert.That(overlay.StoryText.text,Is.EqualTo("와, 큰엄니도 물리쳤네! 이제 숲으로 갈 수 있겠다."));
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(session.Story.LineIndex,Is.Zero);
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.Zero,"lethal flash expires while the first dialogue page remains open");
            overlay.ButtonNext.onClick.Invoke();yield return null;Assert.That(session.Story.LineIndex,Is.EqualTo(1));
            Assert.That(presenter.EnemyActor.Body.sprite,Is.SameAs(bossSprite));
            overlay.ButtonNext.onClick.Invoke();yield return null;Assert.That(presenter.EnemyActor.Body.sprite,Is.SameAs(bossSprite));
            overlay.ButtonSkip.onClick.Invoke();yield return null;
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
            session.Tick(.2f);yield return null;Assert.That(presenter.EnemyActor.Body.sprite,Is.SameAs(bossSprite));
            Assert.That(presenter.EnemyActor.DissolveAmount,Is.GreaterThan(0));
            session.Tick(10);yield return null;Assert.That(session.Stage,Is.EqualTo(11));Assert.That(presenter.View.RewardPanel.activeSelf,Is.False);
            session.ReturnToLobby();session.ContinueSettlement();session.ContinueSettlement();yield return null;Assert.That(presenter.View.RewardPanel.activeSelf,Is.True);
            presenter.View.Rewards[0].Receive.onClick.Invoke();yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.RewardChoice),"preview must not grant the relic");
            Assert.That(presenter.View.RewardConfirm.interactable,Is.True);presenter.View.RewardConfirm.onClick.Invoke();yield return null;
            Assert.That(session.Stage,Is.EqualTo(11));Assert.That(session.PlayerHp,Is.EqualTo(session.PlayerMaxHp));
            session=PlayingAt(20,true);presenter.TickSession=false;Press(presenter.View.Attack);session.Tick(.2f);session.Tick(.55f);yield return null;
            overlay.ButtonSkip.onClick.Invoke();yield return null;
            Assert.That(session.Story,Is.Null);Assert.That(session.Phase,Is.EqualTo(SessionPhase.RunEnd));
            session.ContinueSettlement();session.ContinueSettlement();yield return null;presenter.View.Rewards[0].Receive.onClick.Invoke();yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.RewardChoice),"preview must not grant the relic");
            Assert.That(presenter.View.RewardConfirm.interactable,Is.True);presenter.View.RewardConfirm.onClick.Invoke();yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.MvpComplete));Assert.That(session.Stage,Is.EqualTo(20));
            Assert.That(presenter.View.ResultPanel.activeSelf,Is.True);Assert.That(presenter.View.ResultTitle.text,Is.EqualTo("승 리"));Assert.That(presenter.View.ResultScope.text,Does.Contain("20"));
            presenter.View.ResultRetry.onClick.Invoke();yield return null;
            Assert.That(session.Stage,Is.EqualTo(11));Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Arrival));
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
            Press(presenter.View.Attack);session.Tick(.2f);session.Tick(1.28f);yield return null;
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
        public IEnumerator MinibossPaysOnlyGoldAndContinuesWithoutRewardOrStoryPanel()
        {
            var session=PlayingAt(5,true);presenter.TickSession=false;Press(presenter.View.Attack);session.Tick(.2f);yield return null;
            Assert.That(session.Fragments,Is.EqualTo(0));Assert.That(session.RelicChoices.Count,Is.Zero);
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
            Assert.That(presenter.View.Danger.gameObject.activeSelf,Is.False);Assert.That(presenter.View.BossHpRoot.activeSelf,Is.False);Assert.That(presenter.View.EnemyHpRoot.gameObject.activeSelf,Is.True);
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
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.RunEnd));session.ContinueSettlement();yield return null;
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.DeathResult));Assert.That(presenter.View.ResultPanel.activeSelf,Is.True);
            Assert.That(presenter.View.ResultTitle.text,Is.EqualTo("게임 오버"));Assert.That(presenter.View.ResultScope.text,Does.Contain("스테이지 1"));
            presenter.View.ResultRetry.onClick.Invoke();yield return null;
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.Arrival));
        }

        [UnityTest]
        public IEnumerator ReviewedEnemyKeepsItsAuthoredHoldsAndContactsOnTheCombatBeat()
        {
            var session=PlayingAt();presenter.TickSession=false;
            var clip=presenter.EnemyActor.GetClip(ActorMotion.Attack);
            Assert.That(presenter.EnemyActor.ArtId,Is.EqualTo(catalog.GetEnemy(catalog.GetStage(1).enemyId).artId));
            var threat=session.EnemyThreats[0];
            double start=threat.ImpactAtMs-clip.ContactTime*1000;
            int elapsed=0;
            for(int i=0;i<=clip.contactFrame;i++)
            {
                double target=start+elapsed+.01;
                session.TickInputFrame((float)((target-session.ClockMs)/1000));presentation.TickVisuals(0);
                Assert.That(presenter.EnemyActor.CurrentFrame,Is.EqualTo(i),"authored frame "+i);
                if(i<clip.contactFrame)
                {
                    session.Tick((clip.frameDurationsMs[i]-1)/1000f);presentation.TickVisuals(0);
                    Assert.That(presenter.EnemyActor.CurrentFrame,Is.EqualTo(i),"hold "+i);
                }
                elapsed+=clip.frameDurationsMs[i];
            }
            Assert.That(session.IsEnemyImpactPending,Is.True);
            Assert.That(presenter.EnemyActor.CurrentFrame,Is.EqualTo(clip.contactFrame));
            Assert.That(session.Dodge(DodgeDirection.Up),Is.True);
            session.Tick(.03f);presentation.TickVisuals(0);
            Assert.That(presenter.EnemyActor.CurrentFrame,Is.EqualTo(clip.contactFrame),"contact pose retains its authored hold");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyFlashPreservesAttackClockAndLethalHitFreezesPoseWithoutDeathClip()
        {
            // Damage flashes the current pose without inserting recoil or death frames.
            var session=PlayingAt();presenter.TickSession=false;
            session.Tick(.65f); // Start inside the authored attack, after its natural idle lead-in.
            presentation.TickVisuals(0);
            var cue=session.EnemyIntent;var before=session.ExportSave().run;
            Assert.That(cue.Visible,Is.True);
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.AttackLow));
            Press(presenter.View.Attack);session.Tick(.2f);
            // Sample synchronously so a slow test frame cannot outlast the 120ms flash.
            presentation.TickVisuals(0);
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.GreaterThan(0));
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.AttackLow));
            Assert.That(presenter.EnemyActor.IsHitPlaying,Is.False);
            Assert.That(session.EnemyIntent.Visible,Is.True);
            Assert.That(session.EnemyIntent.PatternId,Is.EqualTo(cue.PatternId));
            Assert.That(session.EnemyIntent.Direction,Is.EqualTo(cue.Direction));
            Assert.That(session.ExportSave().run.enemyHitAtMs,Is.EqualTo(before.enemyHitAtMs));
            Assert.That(session.ExportSave().run.telegraphStartMs,Is.EqualTo(before.telegraphStartMs));
            Assert.That(session.EnemyIntent.SecondsToHit,Is.EqualTo(cue.SecondsToHit-.2f).Within(.0001));
            session.Tick(.025f);presentation.TickVisuals(0);
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(ActorMotion.AttackLow));

            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.EqualTo(0));
            session=PlayingAt(weakEnemy:true);presentation.TickVisuals(0);
            var pose=presenter.EnemyActor.CurrentMotion;int frame=presenter.EnemyActor.CurrentFrame;
            Press(presenter.View.Attack);session.Tick(.2f);
            presentation.TickVisuals(0);
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.GreaterThan(0));
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(pose));
            Assert.That(presenter.EnemyActor.CurrentFrame,Is.EqualTo(frame));
            yield return new WaitForSecondsRealtime(.2f);
            session.Tick(.3f);presentation.TickVisuals(0);
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.EqualTo(pose));
            Assert.That(presenter.EnemyActor.CurrentFrame,Is.EqualTo(frame));
            Assert.That(presenter.EnemyActor.CurrentMotion,Is.Not.EqualTo(ActorMotion.Death));
            Assert.That(presenter.EnemyActor.HitFlashAmount,Is.EqualTo(0));
            Assert.That(presenter.EnemyActor.DissolveAmount,Is.GreaterThan(0));
            Assert.That(session.Transition.Kind,Is.EqualTo(TransitionKind.EnemyDefeat));
        }

        [UnityTest]
        public IEnumerator ReducedMotionKeepsPopupStationaryWithoutWorldShake()
        {
            PlayingAt();presenter.TickSession=false;presenter.Effects.ReducedShake=true;
            presenter.Effects.Hit(123,true,new Vector2(50,50),Vector2.zero,true);
            var popup=presenter.Effects.Numbers.First(x=>x.gameObject.activeSelf);
            yield return null;
            var snapped=popup.rectTransform.anchoredPosition;
            Assert.That(Vector2.Distance(snapped,new Vector2(50,50)),Is.LessThanOrEqualTo(.708f/popup.canvas.scaleFactor),"one initial pixel-grid alignment is allowed");
            yield return new WaitForSecondsRealtime(BattleEffects.PopupDuration-.15f);
            Assert.That(popup.gameObject.activeSelf,Is.True);
            Assert.That(popup.rectTransform.anchoredPosition,Is.EqualTo(snapped),"reduced motion stays still after initial grid alignment");
            Assert.That(popup.color.a,Is.InRange(.01f,.9f));
            Assert.That(presenter.Effects.WorldOffset,Is.EqualTo(Vector3.zero));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(popup.gameObject.activeSelf,Is.False);
        }
    }
}
#endif
