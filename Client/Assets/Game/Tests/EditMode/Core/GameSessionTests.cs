using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LastBell.Tests
{
    public sealed partial class GameSessionTests
    {
        private GameCatalog catalog;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            string path = Path.Combine(UnityEngine.Application.dataPath, "Game/Data/Generated/mvp-catalog.json");
            catalog = UnityEngine.JsonUtility.FromJson<GameCatalog>(File.ReadAllText(path));
#else
            catalog = System.Text.Json.JsonSerializer.Deserialize<GameCatalog>(
                File.ReadAllText(Environment.GetEnvironmentVariable("LAST_BELL_CATALOG")),
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
            catalog.ValidateOrThrow();
        }

        private static GameSaveData RoundTrip(GameSaveData save)
        {
#if UNITY_EDITOR
            return UnityEngine.JsonUtility.FromJson<GameSaveData>(UnityEngine.JsonUtility.ToJson(save));
#else
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            return System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(System.Text.Json.JsonSerializer.Serialize(save, options), options);
#endif
        }

        private GameSaveData LobbySave()
        {
            var save = new GameSession(catalog).ExportSave();
            save.phase = SessionPhase.Lobby;
            save.tutorialStep = 2; save.prologueSeen=true; save.comboHintSeen=true; save.comboPracticeCompleted=true;
            save.run = null;
            save.randomState = 0x12345678;
            return save;
        }

        private static GameSaveData AsLegacySave(GameSaveData save, int schemaVersion)
        {
            save.schemaVersion = schemaVersion;
            foreach (var character in save.characters) Array.Resize(ref character.statLevels, 4);
            save.comboPracticeCompleted = save.comboPracticeSkipped = false;
            if (save.run != null)
            {
                Array.Resize(ref save.run.statLevels, 4);
                if (save.run.combatStats != null) save.run.combatStats.baseAttack = save.run.combatStats.recovery = 0;
            }
            return save;
        }

        private GameSession PlayingAt(int stage = 1, GameSaveData lobby = null, bool weakEnemy = false)
        {
            var session = new GameSession(catalog, lobby ?? LobbySave());
            Assert.That(session.StartRun(), Is.True);
            var save = session.ExportSave();
            save.phase = SessionPhase.Combat;
            save.run.transition = null;
            save.run.stage = stage;
            save.run.patternStep = 0;
            save.run.randomSequence=catalog.GetStage(stage).patternSteps.Select(x=>x.danger).ToArray();save.run.sequenceCursor=0;
            save.run.enemyHpMilli = weakEnemy ? 1000 : catalog.GetStage(stage).maxHp * 1000L;
            save.run.enemyMaxHpMilli = save.run.enemyHpMilli;
            session = new GameSession(catalog, save);
            session.SetPaused(false);
            return session;
        }

        private GameSession KillBoss(int stage, GameSaveData lobby = null)
        {
            var session = PlayingAt(stage, lobby, true);
            Assert.That(session.Attack(), Is.True);
            session.Tick(.1f);
            session.Tick(.1f);
            FinishTransitions(session);
            if (session.Phase == SessionPhase.StoryDialogue)
            {
                session.SkipStory();
                if (session.Story != null && session.Story.AwaitingCompanionChoice)
                    Assert.That(session.ChooseCompanionJoin(false), Is.True);
                FinishTransitions(session);
            }
            if(session.Phase==SessionPhase.Combat)session.ReturnToLobby();
            if(session.Phase==SessionPhase.RunEnd)session.ContinueSettlement();
            if(session.Phase==SessionPhase.RewardAnnouncement)session.ContinueSettlement();
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RewardChoice));
            return session;
        }

        private static void FinishTransitions(GameSession session)
        {
            for (int i = 0; i < 8 && session.Phase == SessionPhase.Transition; i++) session.Tick(10);
            if(session.Phase==SessionPhase.RunEnd&&session.PendingRewardCount==0)session.ContinueSettlement();
        }

        private static void Settle(GameSession s)
        {
            while(s.Phase==SessionPhase.RunEnd||s.Phase==SessionPhase.RewardAnnouncement||s.Phase==SessionPhase.RewardChoice)
            {if(s.Phase==SessionPhase.RewardChoice)s.ChooseRelic(s.RelicChoices[0]);else s.ContinueSettlement();}
        }
        private static bool Home(GameSession s)
        {
            bool result=s.ReturnToLobby();
            if(s.Phase==SessionPhase.RunEnd){Settle(s);s.ReturnToLobby();}
            return result;
        }

        private static void EvadeNext(GameSession session, float leadMs = 30)
        {
            session.Tick(Math.Max(0, session.EnemyIntent.SecondsToHit - leadMs / 1000f));
            Assert.That(session.Dodge(session.EnemyIntent.Direction), Is.True);
            session.Tick(leadMs / 1000f + .000001f);
        }

        [Test]
        public void FirstLaunchRequiresCorrectDodgeThenActualAttack()
        {
            var session = new GameSession(catalog);
            session.FinishPrologue();session.StartRun();
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.TutorialDodge));
            session.Tick(100);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Dodge(DodgeDirection.Back), Is.False);
            Assert.That(session.Attack(), Is.False);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.TutorialAttack));
            Assert.That(session.Attack(), Is.True);
            session.Tick(.199f);
            Assert.That(session.TutorialStep, Is.EqualTo(1));
            session.Tick(.001f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Combat));
            Assert.That(session.TutorialStep, Is.EqualTo(2));
            Assert.That(session.EnemyHp, Is.EqualTo(110));
        }

        [TestCase(59, true, true)]
        [TestCase(90, true, true)]
        [TestCase(91, true, false)]
        [TestCase(240, true, false)]
        [TestCase(241, false, false)]
        public void DodgeWindowsUseTimeNotFrameCount(int leadMs, bool success, bool perfect)
        {
            var session = PlayingAt();
            SessionEventKind? result = null;
            session.EventRaised += e => { if (e.Kind == SessionEventKind.Dodged || e.Kind == SessionEventKind.PerfectDodge) result = e.Kind; };
            EvadeNext(session, leadMs);
            Assert.That(session.PlayerHp, Is.EqualTo(success ? 100 : 92));
            Assert.That(session.Stagger, Is.EqualTo(success ? (perfect ? 40 : 20) : 0));
            Assert.That(result, Is.EqualTo(success ? (SessionEventKind?)(perfect ? SessionEventKind.PerfectDodge : SessionEventKind.Dodged) : null));
        }

        [Test]
        public void InputFrameShowsFullCueAndAcceptsDodgeAtTheDisplayedDeadline()
        {
            var session = PlayingAt();
            session.TickInputFrame(.9f);
            Assert.That(session.IsEnemyImpactPending, Is.True);
            Assert.That(session.ClockMs, Is.EqualTo(900).Within(.001));
            Assert.That(session.EnemyIntent.Visible, Is.True);
            Assert.That(session.EnemyIntent.Progress, Is.EqualTo(1));
            Assert.That(session.PlayerHp, Is.EqualTo(100));

            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            session.TickInputFrame(0);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Stagger, Is.EqualTo(40));
            Assert.That(session.IsEnemyImpactPending, Is.False);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void CompletionInputOpportunityDoesNotDependOnDisplayRate(int fps)
        {
            var session = PlayingAt();
            float frame = 1f / fps;
            int frames = 0;
            while (!session.IsEnemyImpactPending && frames < 200)
            {
                session.TickInputFrame(frame);
                frames++;
            }
            Assert.That(session.IsEnemyImpactPending, Is.True);
            Assert.That(session.EnemyIntent.Progress, Is.EqualTo(1));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            session.TickInputFrame(frame);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Stagger, Is.EqualTo(40));
            Assert.That(session.ClockMs, Is.EqualTo((frames + 1) * (double)frame * 1000).Within(.001));
        }

        [TestCase(241, false, false)]
        [TestCase(240, true, false)]
        [TestCase(91, true, false)]
        [TestCase(90, true, true)]
        public void CompletedCueDoesNotExtendAnEarlierDodge(int leadMs, bool success, bool perfect)
        {
            var session = PlayingAt();
            session.TickInputFrame((900 - leadMs) / 1000f);
            session.Dodge(DodgeDirection.Up);
            session.TickInputFrame(leadMs / 1000f);
            Assert.That(session.IsEnemyImpactPending, Is.True);
            session.TickInputFrame(0);
            Assert.That(session.PlayerHp, Is.EqualTo(success ? 100 : 92));
            Assert.That(session.Stagger, Is.EqualTo(success ? (perfect ? 40 : 20) : 0));
        }

        [Test]
        public void CompletedCueWithoutInputDealsDamageOnTheNextInputFrame()
        {
            var session = PlayingAt();
            session.TickInputFrame(.9f);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            session.TickInputFrame(0);
            Assert.That(session.PlayerHp, Is.EqualTo(92));
            Assert.That(session.IsEnemyImpactPending, Is.False);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            Assert.That(session.PlayerHp, Is.EqualTo(92));
        }

        [TestCase(1, DodgeDirection.Up)]
        [TestCase(2, DodgeDirection.Back)]
        [TestCase(3, DodgeDirection.Down)]
        [TestCase(4, DodgeDirection.Up)]
        public void CompletedCueRequiresTheCorrectDirection(int stage, DodgeDirection correct)
        {
            var session = PlayingAt(stage);
            session.TickInputFrame(.9f);
            Assert.That(session.EnemyIntent.Direction, Is.EqualTo(correct));
            var wrong = correct == DodgeDirection.Up ? DodgeDirection.Back : DodgeDirection.Up;
            Assert.That(session.Dodge(wrong), Is.True);
            session.TickInputFrame(0);
            Assert.That(session.Stagger, Is.Zero);
            Assert.That(session.PlayerHp, Is.EqualTo(100 - catalog.GetStage(stage).damage).Within(.001));
        }

        [Test]
        public void CompletionFrameDoesNotBypassDodgeReuse()
        {
            var session = PlayingAt();
            session.TickInputFrame(.8f);
            Assert.That(session.Dodge(DodgeDirection.Back), Is.True);
            session.TickInputFrame(.1f);
            Assert.That(session.IsEnemyImpactPending, Is.True);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.False);
            session.TickInputFrame(0);
            Assert.That(session.PlayerHp, Is.EqualTo(92));
        }

        [Test]
        public void LargeInputFrameKeepsRemainderAndExposesEachImpactBeforeDamage()
        {
            var session=PlayingAt(10);session.TickInputFrame(3);
            Assert.That(session.ClockMs,Is.EqualTo(1800));Assert.That(session.PlayerHp,Is.EqualTo(100));
            session.Dodge(session.EnemyIntent.Direction);session.TickInputFrame(0);
            Assert.That(session.ClockMs,Is.EqualTo(2550));Assert.That(session.IsEnemyImpactPending,Is.True);Assert.That(session.PlayerHp,Is.EqualTo(100));
            session.TickInputFrame(0);Assert.That(session.ClockMs,Is.EqualTo(3000));Assert.That(session.PlayerHp,Is.EqualTo(56));Assert.That(session.IsEnemyImpactPending,Is.False);
        }

        [Test]
        public void CompletionFramePreservesFullyWarnedBossCombo()
        {
            var session=PlayingAt(10);
            session.TickInputFrame(1.8f);Assert.That(session.IsEnemyImpactPending,Is.True);
            session.Dodge(session.EnemyIntent.Direction);session.TickInputFrame(0);
            Assert.That(session.EnemyIntent.SecondsToHit,Is.EqualTo(.75f).Within(.001));
            Assert.That(session.EnemyIntent.Visible,Is.True);
            session.TickInputFrame(.75f);Assert.That(session.IsEnemyImpactPending,Is.True);
            session.Dodge(session.EnemyIntent.Direction);session.TickInputFrame(0);
            Assert.That(session.PlayerHp,Is.EqualTo(100));Assert.That(session.Stagger,Is.EqualTo(35.2f).Within(.001));
        }

        [Test]
        public void PausedCompletionFrameIsDisplayedAgainBeforeResumeDamage()
        {
            var session = PlayingAt();
            session.TickInputFrame(.9f);
            session.SetPaused(true);
            session.TickInputFrame(5);
            Assert.That(session.ClockMs, Is.EqualTo(900).Within(.001));
            session.SetPaused(false);
            session.TickInputFrame(.1f);
            Assert.That(session.IsEnemyImpactPending, Is.True);
            Assert.That(session.EnemyIntent.Progress, Is.EqualTo(1));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            session.Dodge(DodgeDirection.Up);
            session.TickInputFrame(0);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Stagger, Is.EqualTo(40));
            Assert.That(session.ClockMs, Is.EqualTo(1000).Within(.001));
        }

        [Test]
        public void SavedCompletionFrameRestoresAsAFreshPausedCue()
        {
            var session = PlayingAt();
            session.TickInputFrame(.9f);
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.IsPaused, Is.True);
            Assert.That(restored.IsEnemyImpactPending, Is.False);
            Assert.That(restored.EnemyIntent.Progress, Is.Zero);
            Assert.That(restored.EnemyIntent.SecondsToHit, Is.EqualTo(.9f).Within(.001));
            restored.SetPaused(false);
            restored.TickInputFrame(.01f);
            Assert.That(restored.PlayerHp, Is.EqualTo(100));
            Assert.That(restored.IsEnemyImpactPending, Is.False);
        }

        [Test]
        public void CompletionFrameIsNotCarriedIntoANewRun()
        {
            var session = PlayingAt();
            session.TickInputFrame(3);
            Assert.That(Home(session), Is.True);
            Assert.That(session.IsEnemyImpactPending, Is.False);
            Assert.That(session.StartRun(), Is.True);
            FinishTransitions(session);
            session.TickInputFrame(.01f);
            Assert.That(session.ClockMs, Is.EqualTo(10).Within(.001));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.IsEnemyImpactPending, Is.False);
        }

        [Test]
        public void NewStageNeedsItsOwnCompletedCue()
        {
            var session = PlayingAt(weakEnemy: true);
            session.TickInputFrame(.9f);
            session.Attack();
            session.TickInputFrame(catalog.combat.attackWindupMs / 1000f);
            FinishTransitions(session);
            Assert.That(session.Stage, Is.EqualTo(2));
            Assert.That(session.IsEnemyImpactPending, Is.False);
            float hp = session.PlayerHp;
            session.TickInputFrame(.9f);
            Assert.That(session.IsEnemyImpactPending, Is.True);
            Assert.That(session.EnemyIntent.Direction, Is.EqualTo(DodgeDirection.Back));
            Assert.That(session.PlayerHp, Is.EqualTo(hp));
        }

        [Test]
        public void CompletedCueResolvesBothHitsAndUsesDeathResult()
        {
            var save = PlayingAt(weakEnemy: true).ExportSave();
            save.run.playerHpMilli = 1000;
            var session = new GameSession(catalog, save);
            session.SetPaused(false);
            session.TickInputFrame((900 - catalog.combat.attackWindupMs) / 1000f);
            session.Attack();
            session.TickInputFrame(catalog.combat.attackWindupMs / 1000f);
            Assert.That(session.IsEnemyImpactPending, Is.True);
            Assert.That(session.PlayerHp, Is.EqualTo(1));
            Assert.That(session.EnemyHp, Is.EqualTo(1));
            session.TickInputFrame(0);
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Death));
            FinishTransitions(session);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.DeathResult));
            Assert.That(session.EnemyHp, Is.Zero);
            Assert.That(session.Gold, Is.EqualTo(20));
        }

        [Test]
        public void WrongDirectionInsidePerfectWindowStillHurts()
        {
            var session = PlayingAt();
            session.Tick(.87f);
            session.Dodge(DodgeDirection.Back);
            session.Tick(.03f);
            Assert.That(session.PlayerHp, Is.EqualTo(92));
            Assert.That(session.Stagger, Is.Zero);
        }

        [TestCase(1, DodgeDirection.Up)]
        [TestCase(2, DodgeDirection.Back)]
        [TestCase(3, DodgeDirection.Down)]
        [TestCase(4, DodgeDirection.Up)]
        public void FirstFourStagesIntroduceThreeDodgesAndAttackParry(int stage, DodgeDirection expected)
        {
            var session = PlayingAt(stage);
            Assert.That(session.EnemyIntent.Direction, Is.EqualTo(expected));
            EvadeNext(session);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Stagger, Is.EqualTo(40));
        }

        [Test]
        public void BossWarnsAllThreeAreasBeforeAttackingAt750MsIntervals()
        {
            var session=PlayingAt(20);var queue=session.EnemyThreats;
            Assert.That(queue.Length,Is.EqualTo(3));
            Assert.That(queue.Select(t=>t.CueAtMs),Is.EqualTo(new double[]{0,900,1800}));
            Assert.That(queue.Select(t=>t.ImpactAtMs),Is.EqualTo(new double[]{2700,3450,4200}));
            EvadeNext(session);Assert.That(session.EnemyIntent.Visible,Is.True);
            Assert.That(session.EnemyIntent.SecondsToHit,Is.EqualTo(.75f).Within(.001));
            EvadeNext(session);Assert.That(session.Stagger,Is.EqualTo(35.2f).Within(.001));Assert.That(session.PlayerHp,Is.EqualTo(100));
        }

        [Test]
        public void TutorialAttackStepIsRestoredWithoutRepeatingTheDodge()
        {
            var session = new GameSession(catalog);
            session.FinishPrologue();session.StartRun();
            session.Dodge(DodgeDirection.Up);
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.Phase, Is.EqualTo(SessionPhase.TutorialAttack));
            restored.Attack(); restored.Tick(.2f);
            Assert.That(restored.TutorialStep, Is.EqualTo(2));
            Assert.That(restored.EnemyHp, Is.EqualTo(110));
        }

        [Test]
        public void DodgeReuseIs360MsAndAttackIsAllowedAfter240Ms()
        {
            var session = PlayingAt();
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            session.Tick(.239f);
            Assert.That(session.Attack(), Is.False);
            session.Tick(.001f);
            Assert.That(session.Attack(), Is.True);
            session.Tick(.119f);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.False);
            session.Tick(.001f);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
        }

        [Test]
        public void DodgeCancelsWindupWithoutRefundingAttackInterval()
        {
            var session = PlayingAt();
            session.Attack();
            session.Tick(.05f);
            session.Dodge(DodgeDirection.Up);
            session.Tick(.449f);
            Assert.That(session.EnemyHp, Is.EqualTo(120));
            Assert.That(session.Attack(), Is.False);
            session.Tick(.001f);
            Assert.That(session.Attack(), Is.True);
            session.Tick(.2f);
            Assert.That(session.EnemyHp, Is.EqualTo(110));
        }

        [Test]
        public void LargeAndSmallTicksGiveSameScheduledResults()
        {
            var a = PlayingAt();
            var b = PlayingAt();
            a.Attack(); b.Attack();
            a.Tick(.8f);
            for (int i = 0; i < 80; i++) b.Tick(.01f);
            Assert.That(a.PlayerHp, Is.EqualTo(b.PlayerHp));
            Assert.That(a.EnemyHp, Is.EqualTo(b.EnemyHp));
            Assert.That(a.ExportSave().randomState, Is.EqualTo(b.ExportSave().randomState));
        }

        [Test]
        public void SimultaneousLethalPlayerDamageStillResolvesPlayerStrike()
        {
            var save = PlayingAt().ExportSave();
            save.run.playerHpMilli = 1000;
            var session = new GameSession(catalog, save);
            session.SetPaused(false);
            session.Tick(.7f);
            session.Attack();
            session.Tick(.2f);
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Death));
            FinishTransitions(session);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.DeathResult));
            Assert.That(session.EnemyHp, Is.EqualTo(110));
            Assert.That(session.Gold, Is.Zero);
        }

        [Test]
        public void StunCancelsHitsAndBlocksGainForTwoSecondsAfterward()
        {
            var session = PlayingAt();
            EvadeNext(session); EvadeNext(session); EvadeNext(session);
            Assert.That(session.IsStunReady, Is.True); Assert.That(session.IsEnemyStunned, Is.False);
            session.Attack(); session.Tick(.2f);
            Assert.That(session.IsEnemyStunned, Is.True);
            Assert.That(session.Stagger, Is.Zero);
            session.Tick(1.5f);
            Assert.That(session.IsEnemyStunned, Is.False);
            EvadeNext(session);
            Assert.That(session.Stagger, Is.Zero);
            EvadeNext(session);
            Assert.That(session.Stagger, Is.EqualTo(72));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
        }

        [Test]
        public void CriticalAndStunMultipliersApplyExactlyOnce()
        {
            var save = PlayingAt().ExportSave();
            save.randomState = 1;
            save.run.stunUntilMs = 1000;
            save.run.enemyHitAtMs = -1;
            save.run.immunityUntilMs = 3000;
            var session = new GameSession(catalog, save);
            session.SetPaused(false);
            SessionEvent damage = null;
            session.EventRaised += e => { if (e.Kind == SessionEventKind.EnemyDamaged) damage = e; };
            session.Attack(); session.Tick(.2f);
            Assert.That(damage.Critical, Is.True);
            Assert.That(damage.DamageMilli, Is.EqualTo(21000));
            Assert.That(session.EnemyHp, Is.EqualTo(99));
        }

        [Test]
        public void RelicDamageRoundsToWholePointsAndDetachedSnapshotSurvivesSerialization()
        {
            var lobby = LobbySave();
            lobby.relics.Add(new OwnedRelicData { id = "relic_05", level = 1 });
            lobby.characters[0].equippedRelicIds = new[] { "relic_05" };
            var session = PlayingAt(1, lobby);
            session.Attack(); session.Tick(.2f);
            var save = RoundTrip(session.ExportSave());
            Assert.That(save.run.enemyHpMilli, Is.EqualTo(110000));
            save.run.enemyHpMilli = 1;
            Assert.That(session.ExportSave().run.enemyHpMilli, Is.EqualTo(110000));
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.ExportSave().run.enemyHpMilli, Is.EqualTo(110000));
            Assert.That(restored.IsPaused, Is.True);
        }

        [Test]
        public void PausedOrRestoredCombatDoesNotDamageOrReroll()
        {
            var session = PlayingAt();
            session.SetPaused(true);
            var before = session.ExportSave();
            session.Tick(60);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.False);
            Assert.That(session.Attack(), Is.False);
            Assert.That(session.ClockMs, Is.EqualTo(before.run.clockMs));
            var restored = new GameSession(catalog, RoundTrip(before));
            Assert.That(restored.EnemyIntent.SecondsToHit, Is.EqualTo(.9f).Within(.001f));
            Assert.That(restored.ExportSave().randomState, Is.EqualTo(before.randomState));
        }

        [Test]
        public void MinibossPaysCurrencyWithoutRelicStoryOrCheckpointAndResumesTravel()
        {
            var session = PlayingAt(5, weakEnemy: true);
            session.Attack(); session.Tick(.2f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Transition));
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.EnemyDefeat));
            Assert.That(session.Fragments, Is.EqualTo(0));
            Assert.That(session.Gold, Is.EqualTo(150));
            Assert.That(session.RelicChoices.Count, Is.Zero);
            Assert.That(session.ExportSave().run.story, Is.Null);
            var before = session.ExportSave();
            var restored = new GameSession(catalog, RoundTrip(before));
            FinishTransitions(restored);
            Assert.That(restored.Gold, Is.EqualTo(150));
            Assert.That(restored.ExportSave().randomState, Is.EqualTo(before.randomState));
            Assert.That(restored.Stage, Is.EqualTo(6));
            Assert.That(restored.Phase, Is.EqualTo(SessionPhase.Combat));
            Assert.That(restored.CheckpointStartStage, Is.EqualTo(1));
            Assert.That(restored.ChooseRelic("relic_05"), Is.False);
        }

        [Test]
        public void OrdinaryDuplicateLevelsWithoutGoldAndMaxOnlyConvertsGold()
        {
            foreach (int level in new[] { 2, 5 })
            {
                var lobby = LobbySave();
                lobby.storageCapacity=10; foreach (var relic in catalog.relics) lobby.relics.Add(new OwnedRelicData { id = relic.id, level = level });
                var session = KillBoss(10, lobby);
                string id = session.RelicChoices[0];
                session.ChooseRelic(id);
                Assert.That(session.GetRelicLevel(id), Is.EqualTo(level == 5 ? 5 : 3));
                Assert.That(session.Gold, Is.EqualTo(level == 5 ? 825 : 750));
            }
        }

        [Test]
        public void EquippedHpGrowthAndBossFullHealApplyOnce()
        {
            var lobby=LobbySave();lobby.relics.Add(new OwnedRelicData{id="relic_10",level=1});lobby.characters[0].equippedRelicIds=new[]{"relic_10"};
            var s=KillBoss(10,lobby);Assert.That(s.PlayerHp,Is.EqualTo(106));
            var save=s.ExportSave();save.run.offer.relicIds=new[]{"relic_10","relic_05","relic_15"};save.run.offer.offeredLevels=new[]{1,0,0};
            save.run.pendingRewards[0].relicIds=(string[])save.run.offer.relicIds.Clone();s=new GameSession(catalog,RoundTrip(save));
            Assert.That(s.ChooseRelic("relic_10"),Is.True);Assert.That(s.PlayerHp,Is.EqualTo(106));Assert.That(s.PlayerMaxHp,Is.EqualTo(106));
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));Assert.That(s.ChooseRelic("relic_10"),Is.False);
            Assert.That(s.RetryRun(),Is.True);Assert.That(s.PlayerMaxHp,Is.EqualTo(108));Assert.That(s.PlayerHp,Is.EqualTo(108));
        }

        [Test]
        public void TwentyChoiceCompletesPrototypeAndRetryKeepsProgressAtStageEleven()
        {
            var save = KillBoss(20).ExportSave();
            save.run.playerHpMilli = 30000;
            Assert.That(save.characters[0].checkpoint, Is.EqualTo(10));
            var session = new GameSession(catalog, RoundTrip(save));
            Assert.That(session.Fragments, Is.EqualTo(4));
            Assert.That(Home(session), Is.False);
            session.ChooseRelic(session.RelicChoices[0]);
            Assert.That(session.Transition,Is.Null);
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.MvpComplete));
            Assert.That(session.Stage,Is.EqualTo(20));Assert.That(session.Result.Cleared,Is.True);
            Assert.That(session.PlayerHp,Is.EqualTo(30));Assert.That(session.CheckpointStartStage,Is.EqualTo(11));
            long gold=session.Gold;int fragments=session.Fragments;var relics=session.ExportSave().relics.Count;
            session.Tick(100);Assert.That(session.Stage,Is.EqualTo(20));Assert.That(session.EnemyThreats,Is.Empty);
            session=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(session.Result.Cleared,Is.True);
            Assert.That(session.RetryRun(),Is.True);Assert.That(session.Stage,Is.EqualTo(11));
            Assert.That(session.Gold,Is.EqualTo(gold));Assert.That(session.Fragments,Is.EqualTo(fragments));
            Assert.That(session.ExportSave().relics.Count,Is.EqualTo(relics));
            Home(session);session.StartRun();Assert.That(session.Stage,Is.EqualTo(11));
        }

        [Test]
        public void GateCheckpointUnlocksOnKillAndSurvivesDeathRetryAndLobby()
        {
            var s=PlayingAt(10,weakEnemy:true);s.Attack();s.Tick(.2f);
            Assert.That(s.CheckpointStartStage,Is.EqualTo(11));
            Assert.That(s.ExportSave().characters[0].checkpoint,Is.EqualTo(10));
            long wallet=s.Gold;int fragments=s.Fragments;
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));
            Assert.That(s.CheckpointStartStage,Is.EqualTo(11));
            Assert.That(s.Gold,Is.EqualTo(wallet));Assert.That(s.Fragments,Is.EqualTo(fragments));
            Assert.That(Home(s),Is.False,"Pending boss reward must not be skipped.");
            s.SkipStory();FinishTransitions(s);
            var save=s.ExportSave();save.run.playerHpMilli=1000;
            s=new GameSession(catalog,RoundTrip(save));s.SetPaused(false);
            s.Tick(s.EnemyIntent.SecondsToHit);FinishTransitions(s);Settle(s);
            Assert.That(s.Phase,Is.EqualTo(SessionPhase.DeathResult));
            Assert.That(s.Result.DefeatedStage,Is.EqualTo(11));Assert.That(s.Result.RetryStage,Is.EqualTo(11));
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));
            Assert.That(s.RetryRun(),Is.True);Assert.That(s.Stage,Is.EqualTo(11));
            Assert.That(s.PlayerHp,Is.EqualTo(s.PlayerMaxHp));Assert.That(s.ExportSave().run.earnedGold,Is.Zero);
            Assert.That(s.Gold,Is.EqualTo(wallet));Assert.That(s.Fragments,Is.EqualTo(fragments));
            Assert.That(Home(s),Is.True);Assert.That(s.Stage,Is.EqualTo(11));
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));s.StartRun();Assert.That(s.Stage,Is.EqualTo(11));
        }

        [TestCase(9,0,1)] [TestCase(10,0,11)] [TestCase(20,0,11)] [TestCase(0,10,11)] [TestCase(20,20,11)]
        public void PriorSavesRecoverOnlyTheirCharacterCheckpoint(int cleared,int checkpoint,int start)
        {
            var save=LobbySave();save.characters[0].highestStage=cleared;save.characters[0].checkpoint=checkpoint;
            save.highestClearedStage=20;save.gold=1234;save.fragments=7;
            catalog.GetCharacter("dam").availableInMvp=true;
            var s=new GameSession(catalog,RoundTrip(save));
            Assert.That(s.CheckpointStartStage,Is.EqualTo(start));s.StartRun();Assert.That(s.Stage,Is.EqualTo(start));
            Home(s);Assert.That(s.SelectCharacter("dam"),Is.True);
            Assert.That(s.CheckpointStartStage,Is.EqualTo(1),"Shared progress must not unlock another character's checkpoint.");
            Assert.That(s.Gold,Is.EqualTo(1234));Assert.That(s.Fragments,Is.EqualTo(7));
        }

        [Test]
        public void DeathAtNineShowsResultAndRetriesFirstCheckpoint()
        {
            var save = PlayingAt(9).ExportSave();
            save.run.playerHpMilli = 1000;
            var session = new GameSession(catalog, save);
            session.SetPaused(false);
            session.Tick(session.EnemyIntent.SecondsToHit);
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Death));
            FinishTransitions(session);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.DeathResult));
            Assert.That(session.Result.DefeatedStage, Is.EqualTo(9));
            Assert.That(session.CheckpointStartStage, Is.EqualTo(1));
            session.RetryRun();
            Assert.That(session.Stage, Is.EqualTo(1));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
        }

        [Test]
        public void TransitionEventsKeepTheFloorWhereTheActionOccurred()
        {
            var reward = KillBoss(10);
            int rewardStage = 0;
            reward.EventRaised += e => { if (e.Kind == SessionEventKind.RelicChosen) rewardStage = e.Stage; };
            reward.ChooseRelic(reward.RelicChoices[0]);
            Assert.That(reward.Stage, Is.EqualTo(11));
            Assert.That(rewardStage, Is.EqualTo(10));
            var save = PlayingAt(9).ExportSave();
            save.run.playerHpMilli = 1000;
            var death = new GameSession(catalog, save);
            int deathStage = 0;
            death.EventRaised += e => { if (e.Kind == SessionEventKind.PlayerDefeated) deathStage = e.Stage; };
            death.SetPaused(false);
            death.Tick(death.EnemyIntent.SecondsToHit);
            FinishTransitions(death);
            Assert.That(death.Result.DefeatedStage, Is.EqualTo(9));
            Assert.That(deathStage, Is.EqualTo(9));
        }

        [Test]
        public void OldBoundarySaveReturnsToLobbyWithoutLosingProgress()
        {
            var save=AsLegacySave(PlayingAt().ExportSave(),5);save.run.stage=11;save.characters[0].checkpoint=10;
            save.characters[0].statLevels[0]=4;save.gold=1234;save.fragments=4;
            save.relics.Add(new OwnedRelicData{id=catalog.relics[0].id,level=1});
            var session=new GameSession(catalog,RoundTrip(save));
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Lobby));Assert.That(session.Stage,Is.EqualTo(11));
            Assert.That(session.Gold,Is.EqualTo(1234));Assert.That(session.Fragments,Is.EqualTo(4));
            Assert.That(session.GetStatLevel(0),Is.EqualTo(4));Assert.That(session.GetRelicLevel(catalog.relics[0].id),Is.EqualTo(1));
            Assert.That(session.StartRun(),Is.True);Assert.That(session.Stage,Is.EqualTo(11));
        }

        [Test]
        public void NormalKillFreezesCombatClockThenPreparesAndOpensTheNextCueOnce()
        {
            var session = PlayingAt(weakEnemy: true);
            int prepared = 0, ready = 0;
            session.EventRaised += e =>
            {
                if (e.Kind == SessionEventKind.StagePrepared) { prepared++; Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Travel)); }
                if (e.Kind == SessionEventKind.StageStarted) ready++;
            };
            session.Attack(); session.Tick(.2f);
            double clock = session.ClockMs;
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.EnemyDefeat));
            Assert.That(session.Gold, Is.EqualTo(20));
            Assert.That(session.Attack(), Is.False);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.False);
            Assert.That(session.EnemyIntent.Visible, Is.False);
            session.TickInputFrame(.549f);
            Assert.That(session.Stage, Is.EqualTo(1));
            session.TickInputFrame(.001f);
            Assert.That(session.Stage, Is.EqualTo(2));
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Travel));
            Assert.That(prepared, Is.EqualTo(1));
            session.TickInputFrame(2);
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Arrival));
            session.TickInputFrame(.699f);
            Assert.That(session.EnemyIntent.Visible, Is.False);
            session.TickInputFrame(.001f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Combat));
            Assert.That(session.ClockMs, Is.EqualTo(clock));
            Assert.That(ready, Is.EqualTo(1));
            Assert.That(session.EnemyIntent.Progress, Is.Zero);
            Assert.That(session.EnemyIntent.SecondsToHit, Is.EqualTo(.9f).Within(.001));
            Assert.That(session.Attack(), Is.True, "Next encounter starts ready, not with a frozen cooldown remainder.");
        }

        [TestCase(5, 1.8f, 1.0f)]
        [TestCase(10, 2.2f, 1.2f)]
        public void BossIntroBlocksAllCombatAndMarksSeenOnlyAtCompletion(int stage, float firstSeconds, float repeatSeconds)
        {
            var session = PlayingAt(stage - 1, weakEnemy: true);
            session.Attack(); session.Tick(.2f); session.Tick(3.25f);
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.BossIntro));
            Assert.That(session.Transition.DurationSeconds, Is.EqualTo(firstSeconds));
            Assert.That(session.Transition.IsRepeat, Is.False);
            Assert.That(session.ExportSave().characters[0].seenBossIntros, Is.Empty);
            Assert.That(session.Attack(), Is.False);
            Assert.That(session.EnemyIntent.Visible, Is.False);
            session.Tick(.3f);
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.Transition.ElapsedSeconds, Is.EqualTo(.3f).Within(.001));
            Assert.That(restored.Transition.IsRepeat, Is.False);
            restored.SetPaused(true); restored.Tick(10);
            Assert.That(restored.Transition.ElapsedSeconds, Is.EqualTo(.3f).Within(.001));
            restored.SetPaused(false); restored.Tick(firstSeconds - .3f);
            Assert.That(restored.Phase, Is.EqualTo(SessionPhase.Combat));
            Assert.That(restored.EnemyIntent.Progress, Is.Zero);
            var lobby = restored.ExportSave(); lobby.phase = SessionPhase.Lobby; lobby.run = null;
            var repeat = PlayingAt(stage - 1, lobby, true);
            repeat.Attack(); repeat.Tick(.2f); repeat.Tick(3.25f);
            Assert.That(repeat.Transition.Kind, Is.EqualTo(TransitionKind.BossIntro));
            Assert.That(repeat.Transition.IsRepeat, Is.True);
            Assert.That(repeat.Transition.DurationSeconds, Is.EqualTo(repeatSeconds));
        }

        [Test]
        public void BossRewardIsFixedBeforeCollapseThenStoryResumesBeforeRelicChoice()
        {
            var session=PlayingAt(10,weakEnemy:true);session.Attack();session.Tick(.2f);
            var choices=session.ExportSave().run.pendingRewards[0].relicIds;
            Assert.That(choices.Distinct().Count(),Is.EqualTo(3));Assert.That(session.RelicChoices,Is.Empty);
            Assert.That(session.Gold,Is.EqualTo(750));Assert.That(session.Fragments,Is.EqualTo(4));Assert.That(session.CheckpointStartStage,Is.EqualTo(11));
            session=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(session.Phase,Is.EqualTo(SessionPhase.StoryDialogue));
            session.AdvanceStory();session=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(session.Story.LineIndex,Is.EqualTo(1));
            CollectionAssert.AreEqual(choices,session.ExportSave().run.pendingRewards[0].relicIds);
            session.Tick(100);Assert.That(session.Story.LineIndex,Is.EqualTo(1));session.SkipStory();FinishTransitions(session);
            Assert.That(session.Stage,Is.EqualTo(11));Assert.That(session.Phase,Is.EqualTo(SessionPhase.Combat));Assert.That(session.ChooseRelic(choices[0]),Is.False);
            session.ReturnToLobby();session.ContinueSettlement();session.ContinueSettlement();
            CollectionAssert.AreEqual(choices,session.RelicChoices);Assert.That(session.ChooseRelic(choices[0]),Is.True);
            var committed=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(committed.ChooseRelic(choices[0]),Is.False);
            Assert.That(committed.GetRelicLevel(choices[0]),Is.EqualTo(1));Assert.That(committed.Gold,Is.EqualTo(750));Assert.That(committed.Fragments,Is.EqualTo(4));
        }

        [Test]
        public void StoryIsOncePerCharacterAndSkipNeverChoosesRelic()
        {
            var first = KillBoss(10);
            var lobby = first.ExportSave(); lobby.phase = SessionPhase.Lobby; lobby.run = null;
            var replay = PlayingAt(10, lobby, true); replay.Attack(); replay.Tick(.2f); FinishTransitions(replay);
            Assert.That(replay.Phase, Is.EqualTo(SessionPhase.Combat));Assert.That(replay.PendingRewardCount,Is.EqualTo(1));
            Assert.That(replay.Story, Is.Null);
            catalog.GetCharacter("dam").availableInMvp = true;
            lobby.selectedCharacterId = "dam";
            var other = PlayingAt(10, lobby, true); other.Attack(); other.Tick(.2f); FinishTransitions(other);
            Assert.That(other.Phase, Is.EqualTo(SessionPhase.StoryDialogue));
            other.AdvanceStory(); other.AdvanceStory();
            Assert.That(other.Story.Text, Is.EqualTo("하하, 그 정도야 쉽지. 길 안내는 부탁한다!"));
            Assert.That(other.Story.PortraitId, Is.EqualTo("dam"));
            Assert.That(other.SkipStory(), Is.True);FinishTransitions(other);
            Assert.That(other.Phase, Is.EqualTo(SessionPhase.Combat));Assert.That(other.PendingRewardCount,Is.EqualTo(1));
            Assert.That(other.CheckpointStartStage, Is.EqualTo(11));
            Assert.That(other.ExportSave().relics, Is.Empty);
        }

        [TestCase(0.2f, TransitionKind.EnemyDefeat)]
        [TestCase(1.0f, TransitionKind.Travel)]
        [TestCase(2.9f, TransitionKind.Arrival)]
        public void EveryNormalTransitionRoundTripsWithoutDuplicateRewardOrLostElapsed(float elapsed, TransitionKind expected)
        {
            var session = PlayingAt(weakEnemy: true); session.Attack(); session.Tick(.2f); session.Tick(elapsed);
            var before = session.ExportSave();
            session = new GameSession(catalog, RoundTrip(before));
            Assert.That(session.Transition.Kind, Is.EqualTo(expected));
            // JsonUtility's decimal double round trip may differ by a few ULPs.
            // 1e-6 ms tolerates serialization precision, not a lost simulation tick.
            Assert.That(session.ExportSave().run.transition.elapsedMs, Is.EqualTo(before.run.transition.elapsedMs).Within(0.000001));
            FinishTransitions(session);
            Assert.That(session.Stage, Is.EqualTo(2));
            Assert.That(session.Gold, Is.EqualTo(20));
            Assert.That(session.EnemyIntent.Progress, Is.Zero);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
        }

        [Test]
        public void DeathTransitionSurvivesRestoreAndUnlocksResultOnlyAfterLantern()
        {
            var save = PlayingAt(9).ExportSave(); save.run.playerHpMilli = 1000;
            var session = new GameSession(catalog, save); session.SetPaused(false);
            session.Tick(session.EnemyIntent.SecondsToHit); session.Tick(.8f);
            session = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Death));
            Assert.That(session.PlayerHp, Is.Zero);
            Assert.That(session.StartRun(), Is.False);
            session.Tick(.999f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Transition));
            session.Tick(.001f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RunEnd));session.ContinueSettlement();
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.DeathResult));
            Assert.That(session.StartRun(), Is.False);
            Assert.That(session.RetryRun(), Is.True);
            Assert.That(session.Transition.Kind, Is.EqualTo(TransitionKind.Arrival));
            Assert.That(session.Transition.DurationSeconds, Is.EqualTo(.7f));
        }

        [Test]
        public void SchemaOnePendingBossKeepsCandidatesAndRestoresFirstStory()
        {
            var old = KillBoss(10).ExportSave();
            old.run.stage=10;old.run.stageRewardApplied=true;old.run.enemyHpMilli=0;old.run.ended=false;old.run.pendingRewards.Clear();
            AsLegacySave(old,1); old.characters[0].seenStoryIds.Clear(); old.unlockedStoryIds.Clear();
            var session = new GameSession(catalog, RoundTrip(old));
            Assert.That(session.ExportSave().schemaVersion, Is.EqualTo(9));
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.StoryDialogue));
            CollectionAssert.AreEqual(old.run.offer.relicIds, session.ExportSave().run.pendingRewards[0].relicIds);
            Assert.That(session.Gold, Is.EqualTo(old.gold));
            Assert.That(session.CheckpointStartStage, Is.EqualTo(11));
        }

        [Test]
        public void SchemaOnePromisedMinibossChoiceIsGrandfatheredOnce()
        {
            var old = KillBoss(10).ExportSave();
            old.run.pendingRewards.Clear();old.run.ended=false;old.run.stageRewardApplied=true;old.run.enemyHpMilli=0;
            AsLegacySave(old,1); old.run.stage = 5; old.run.offer.stage = 5;
            old.characters[0].highestStage=5;old.characters[0].checkpoint=0;
            old.run.offer.rewardId = old.run.runId + ":0:5";
            var session = new GameSession(catalog, RoundTrip(old));
            Assert.That(session.ExportSave().run.offer.legacyMidbossReward, Is.True);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RewardChoice));
            string id = session.RelicChoices[0];
            Assert.That(session.ChooseRelic(id), Is.True);
            Assert.That(session.ChooseRelic(id), Is.False);
            Assert.That(session.CheckpointStartStage, Is.EqualTo(1));
            Assert.That(session.Stage, Is.EqualTo(5));
        }

        [TestCase("negative")]
        [TestCase("nan")]
        [TestCase("missing")]
        [TestCase("wrong-stage")]
        public void InvalidTransitionSavesAreRejected(string corruption)
        {
            var session = PlayingAt(weakEnemy: true); session.Attack(); session.Tick(.2f);
            var save = session.ExportSave();
            if (corruption == "negative") save.run.transition.elapsedMs = -1;
            if (corruption == "nan") save.run.transition.elapsedMs = double.NaN;
            if (corruption == "missing") save.run.transition = null;
            if (corruption == "wrong-stage") save.run.transition.fromStage = 2;
            Assert.Throws<InvalidOperationException>(() => new GameSession(catalog, save));
        }

        [Test]
        public void JsonUtilityEmptyOptionalRecordsNormalizeWithoutMutatingTheInputSnapshot()
        {
            // Explicitly emulate Unity inline-null materialization on the .NET runner.
            // Actual JsonUtility RoundTrip coverage remains in the restore tests above.
            var save = PlayingAt().ExportSave();
            save.run.offer = new RelicOfferData();
            save.run.story = new StoryStateData();
            save.run.transition = new TransitionData();
            var session = new GameSession(catalog, save);
            var canonical = session.ExportSave();
            Assert.That(canonical.run.offer, Is.Null);
            Assert.That(canonical.run.story, Is.Null);
            Assert.That(canonical.run.transition, Is.Null);
            Assert.That(save.run.offer, Is.Not.Null, "The caller's snapshot must remain detached.");
            Assert.That(save.run.story, Is.Not.Null);
            Assert.That(save.run.transition, Is.Not.Null);
            Assert.That(canonical.gold, Is.EqualTo(save.gold));
            Assert.That(canonical.randomState, Is.EqualTo(save.randomState));
            Assert.That(session.IsPaused, Is.True);
        }

        [TestCase("offer")]
        [TestCase("story")]
        [TestCase("transition")]
        public void PartiallyPopulatedOptionalRecordsAreNotSilentlyDiscarded(string record)
        {
            var save = PlayingAt().ExportSave();
            if (record == "offer") save.run.offer = new RelicOfferData { maxGold = 1 };
            if (record == "story") save.run.story = new StoryStateData { rewardId = "partial" };
            if (record == "transition") save.run.transition = new TransitionData { durationMs = 1 };
            Assert.Throws<InvalidOperationException>(() => new GameSession(catalog, save));
        }

        [TestCase(SessionPhase.RewardChoice)]
        [TestCase(SessionPhase.StoryDialogue)]
        [TestCase(SessionPhase.Transition)]
        public void AnEmptyRecordNeverSatisfiesAPhaseThatRequiresPendingState(SessionPhase phase)
        {
            var save = PlayingAt().ExportSave();
            save.phase = phase;
            save.run.offer = new RelicOfferData();
            save.run.story = new StoryStateData();
            save.run.transition = new TransitionData();
            Assert.Throws<InvalidOperationException>(() => new GameSession(catalog, save));
        }

        [Test]
        public void ConfirmExchangeOnlyGrantsGoldThenUpgradeRequiresAnotherPress()
        {
            var save = LobbySave();
            save.characters[0].statLevels[0] = 10;
            save.fragments = 3;
            var session = new GameSession(catalog, save);
            session.Upgrade(0);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.ExchangeConfirmation));
            Assert.That(session.PendingExchange.fragments, Is.EqualTo(3));
            Assert.That(session.PendingExchange.gold, Is.EqualTo(300));
            Assert.That(session.GetStatLevel(0), Is.EqualTo(10));
            Assert.That(session.ConfirmExchange(), Is.True);
            Assert.That(session.Gold, Is.EqualTo(300));
            Assert.That(session.Fragments, Is.Zero);
            Assert.That(session.GetStatLevel(0), Is.EqualTo(10));
            Assert.That(session.ConfirmExchange(), Is.False);
            Assert.That(session.Upgrade(0), Is.True);
            Assert.That(session.GetStatLevel(0), Is.EqualTo(11));
            Assert.That(session.Gold, Is.EqualTo(75));
        }

        [Test]
        public void CancelInsufficientAndStaleExchangeNeverSpend()
        {
            var save = LobbySave();
            save.characters[0].statLevels[0] = 10;
            save.fragments = 1;
            var session = new GameSession(catalog, save);
            session.Upgrade(0);
            Assert.That(session.ConfirmExchange(), Is.False);
            session.CancelExchange();
            Assert.That(session.Gold, Is.Zero);
            Assert.That(session.Fragments, Is.EqualTo(1));
            session.Upgrade(0);
            var changed = session.ExportSave();
            changed.gold = 1;
            changed.walletVersion++;
            var restored = new GameSession(catalog, changed);
            Assert.That(restored.Phase, Is.EqualTo(SessionPhase.Lobby));
            Assert.That(restored.ConfirmExchange(), Is.False);
            Assert.That(restored.Gold, Is.EqualTo(1));
            Assert.That(restored.Fragments, Is.EqualTo(1));
        }

        [Test]
        public void EconomyMigrationPreservesWalletGrowthAndPromisedMaxReward()
        {
            var save=AsLegacySave(LobbySave(),2);save.gold=2147483647;save.characters[0].statLevels[0]=30;save.characters[0].checkpoint=10;
            var restored=new GameSession(catalog,RoundTrip(save));
            Assert.That(restored.Gold,Is.EqualTo(2147483647L));Assert.That(restored.GetStatLevel(0),Is.EqualTo(30));
            Assert.That(restored.ExchangeRate,Is.EqualTo(150));Assert.That(restored.ExportSave().schemaVersion,Is.EqualTo(9));
            save=LobbySave();save.storageCapacity=10;foreach(var r in catalog.relics)save.relics.Add(new OwnedRelicData{id=r.id,level=5});
            var old=AsLegacySave(KillBoss(10,save).ExportSave(),2);old.run.stage=10;old.run.stageRewardApplied=true;old.run.pendingRewards.Clear();old.run.offer.maxGold=20;
            restored=new GameSession(catalog,RoundTrip(old));long before=restored.Gold;
            Assert.That(restored.PendingMaxRelicGold,Is.EqualTo(20));
            restored.ChooseRelic(restored.RelicChoices[0]);Assert.That(restored.Gold,Is.EqualTo(before+20));
        }

        [TestCase(0,100)] [TestCase(9,100)] [TestCase(10,150)] [TestCase(19,150)]
        [TestCase(20,1920)] [TestCase(30,75047)] [TestCase(40,5168490)] [TestCase(50,5168490)]
        public void ExchangeTierUsesHighestCompletedStage(int completed,long expected)
        {
            var save=LobbySave();save.highestClearedStage=completed;
            Assert.That(new GameSession(catalog,save).ExchangeRate,Is.EqualTo(expected));
        }

        [Test]
        public void LargeWalletAndFreshQuoteRoundTripWithoutTruncation()
        {
            var save=LobbySave();save.gold=3000000000L;save.characters[0].statLevels[0]=49;
            var session=new GameSession(catalog,RoundTrip(save));session.Upgrade(0);
            Assert.That(session.Gold,Is.EqualTo(1681991581L));
            session=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(session.Gold,Is.EqualTo(1681991581L));
        }

        [TestCase(8)] [TestCase(9)]
        public void EmptyLobbyRunSurvivesJsonUtilityWithoutInventingCombat(int schema)
        {
            var save=LobbySave();save.gold=3000000000L;
            if(schema==8)AsLegacySave(save,8);
            var session=new GameSession(catalog,RoundTrip(save));
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Lobby));Assert.That(session.Gold,Is.EqualTo(3000000000L));
            Assert.That(session.ExportSave().run,Is.Null);Assert.That(session.ExportSave().schemaVersion,Is.EqualTo(9));
            CollectionAssert.AreEqual(new int[5],session.ExportSave().characters[0].statLevels);
        }

        [TestCase("hp")] [TestCase("money")] [TestCase("skill")] [TestCase("recovery")] [TestCase("stat")]
        public void PartiallyPopulatedLobbyRunIsRejectedInsteadOfDiscarded(string field)
        {
            var save=LobbySave();save.run=new RunStateData();
            if(field=="hp")save.run.playerHpMilli=1000;
            if(field=="money")save.run.earnedGold=1;
            if(field=="skill")save.run.skillCooldownRemainingMs=1;
            if(field=="recovery")save.run.combatStats.recovery=1;
            if(field=="stat")save.run.statLevels[4]=1;
            Assert.Throws<InvalidOperationException>(()=>new GameSession(catalog,RoundTrip(save)));
        }

        [TestCase(0,false,40)] [TestCase(80,false,20)] [TestCase(80,true,0)]
        public void PerfectEventReportsOnlyActualStaggerGainAtImpact(int before,bool immune,int gain)
        {
            var save=PlayingAt().ExportSave();save.run.stagger=before;save.run.immunityUntilMs=immune?10000:-1;
            var session=new GameSession(catalog,save);session.SetPaused(false);SessionEvent result=null;
            session.EventRaised+=e=>{if(e.Kind==SessionEventKind.PerfectDodge)result=e;};
            Assert.That(session.EnemyIntent.TelegraphMs,Is.EqualTo(900));
            session.Tick(.85f);session.Dodge(DodgeDirection.Up);Assert.That(result,Is.Null);
            session.Tick(.05f);Assert.That(result,Is.Not.Null);
            Assert.That(result.StaggerBefore,Is.EqualTo(before));Assert.That(result.StaggerGained,Is.EqualTo(gain));
        }

        [Test]
        public void IntegerStatsUseTheSameValuesForDisplayAndDamage()
        {
            var lobby=LobbySave();lobby.characters[0].statLevels=new[]{9,9,9,9,0};
            var session=PlayingAt(1,lobby);var stats=session.PlayerStats;
            Assert.That(stats.attack,Is.EqualTo(46));Assert.That(stats.maxHp,Is.EqualTo(316));
            Assert.That(stats.critChance,Is.EqualTo(stats.critChancePercent/100d));
            Assert.That(stats.goldMultiplier,Is.EqualTo(1+stats.goldBonusPercent/100d));
            Assert.That(typeof(CombatStatsData).GetFields().All(f=>f.FieldType==typeof(int)),Is.True);
            session.Attack();session.Tick(.2f);
            Assert.That(session.ExportSave().run.enemyHpMilli%1000,Is.Zero);
            session.Tick(.45f);
            Assert.That(session.ExportSave().run.playerHpMilli%1000,Is.Zero);
        }

        [Test]
        public void LegacyFractionalRunMigratesWithoutLosingWalletOrProgress()
        {
            var save=AsLegacySave(PlayingAt(9).ExportSave(),3);save.gold=3000000000L;
            save.run.statLevels=new[]{9,9,9,9};save.run.stats=new LegacyCombatStatsData{attack=39.7,maxHpMilli=298000};
            save.run.combatStats=null;save.run.playerHpMilli=82550;save.run.enemyHpMilli=99450;
            var session=new GameSession(catalog,RoundTrip(save));
            Assert.That(session.PlayerStats.attack,Is.EqualTo(46));Assert.That(session.PlayerHp,Is.EqualTo(83));
            Assert.That(session.EnemyHp,Is.EqualTo(99));Assert.That(session.Gold,Is.EqualTo(3000000000L));
            Assert.That(session.Stage,Is.EqualTo(9));Assert.That(session.ExportSave().schemaVersion,Is.EqualTo(9));
            CollectionAssert.AreEqual(new[]{9,9,9,9,0},session.ExportSave().run.statLevels);
            Assert.That(session.PlayerStats.recovery,Is.EqualTo(5));
            Assert.That(session.ExportSave().run.earningsSinceUpdate,Is.True);
            Assert.That(session.ExportSave().run.earnedGold,Is.Zero);
        }

        [Test]
        public void ResultEarningsExcludeStartingWalletAndSurviveReloadAndRetry()
        {
            var lobby=LobbySave();lobby.gold=3000000000L;lobby.fragments=7;
            var session=PlayingAt(5,lobby,true);session.Attack();session.Tick(.2f);FinishTransitions(session);
            var save=session.ExportSave();long earned=session.Gold-lobby.gold;int fragments=session.Fragments-lobby.fragments;
            Assert.That(earned,Is.EqualTo(150));Assert.That(fragments,Is.EqualTo(0));
            save.run.playerHpMilli=1000;session=new GameSession(catalog,RoundTrip(save));session.SetPaused(false);
            session.Tick(session.EnemyIntent.SecondsToHit);FinishTransitions(session);
            Assert.That(session.Result.DefeatedStage,Is.EqualTo(6));Assert.That(session.Result.RetryStage,Is.EqualTo(1));
            Assert.That(session.Result.Gold,Is.EqualTo(earned));Assert.That(session.Result.Fragments,Is.EqualTo(fragments));
            long wallet=session.Gold;long id=session.ExportSave().run.runId;
            session=new GameSession(catalog,RoundTrip(session.ExportSave()));session.Tick(100);
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.DeathResult));Assert.That(session.Gold,Is.EqualTo(wallet));
            Assert.That(session.Result.Gold,Is.EqualTo(earned));Assert.That(session.StartRun(),Is.False);
            Assert.That(session.RetryRun(),Is.True);Assert.That(session.RetryRun(),Is.False);
            Assert.That(session.Gold,Is.EqualTo(wallet));Assert.That(session.ExportSave().run.runId,Is.Not.EqualTo(id));
            Assert.That(session.ExportSave().run.earnedGold,Is.Zero);Assert.That(session.ExportSave().run.earnedFragments,Is.Zero);
            Assert.That(session.ExportSave().run.earningsSinceUpdate,Is.False);Assert.That(session.PlayerHp,Is.EqualTo(session.PlayerMaxHp));
        }

        [Test]
        public void ResultIncludesMaxDuplicateGoldOnceAndLobbyDoesNotCreditAgain()
        {
            var lobby=LobbySave();lobby.gold=500;lobby.storageCapacity=10;foreach(var r in catalog.relics)lobby.relics.Add(new OwnedRelicData{id=r.id,level=5});
            var session=KillBoss(20,lobby);string relic=session.RelicChoices[0];session.ChooseRelic(relic);session.ChooseRelic(relic);FinishTransitions(session);
            session=new GameSession(catalog,session.ExportSave());Assert.That(session.Result.Cleared,Is.True);
            Assert.That(session.Result.Gold,Is.EqualTo(catalog.GetBaseGold(catalog.GetStage(20))+catalog.GetMaxRelicGold(20)));Assert.That(session.Result.Fragments,Is.EqualTo(4));
            Assert.That(session.Result.RetryStage,Is.EqualTo(11));long wallet=session.Gold;
            Assert.That(Home(session),Is.True);Assert.That(Home(session),Is.False);
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Lobby));Assert.That(session.Gold,Is.EqualTo(wallet));
            Assert.That(session.Result,Is.Null);
        }

        [Test]
        public void StorageAndAutomaticRelicsAreSharedButGrowthIsCharacterSpecific()
        {
            catalog.GetCharacter("dam").availableInMvp = true;
            var save = LobbySave();
            save.highestClearedStage = 10;
            save.fragments = 4;
            save.gold = 10;
            save.relics.Add(new OwnedRelicData { id = "relic_05", level = 1 });
            var session = new GameSession(catalog, save);
            Assert.That(session.BuyNextSlot(), Is.True);
            Assert.That(session.EquipRelic("relic_05"), Is.False);
            session.Upgrade(0);
            session.SelectCharacter("dam");
            Assert.That(session.StorageCapacity, Is.EqualTo(4));
            Assert.That(session.GetRelicLevel("relic_05"), Is.EqualTo(1));
            Assert.That(session.EquippedRelicIds.Count, Is.EqualTo(1));
            Assert.That(session.GetStatLevel(0), Is.Zero);
            session.SelectCharacter("on");
            Assert.That(session.GetStatLevel(0), Is.EqualTo(1));
            Assert.That(session.EquippedRelicIds.Count, Is.EqualTo(1));
            session.StartRun();
            Assert.That(session.Upgrade(0), Is.False);
            Assert.That(session.UnequipRelic("relic_05"), Is.False);
            Assert.That(session.SelectCharacter("dam"), Is.False);
        }
    }
}
