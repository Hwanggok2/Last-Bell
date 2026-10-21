using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LastBell.Tests
{
    public sealed class GameSessionTests
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
            save.tutorialStep = 2;
            save.run = null;
            save.randomState = 0x12345678;
            return save;
        }

        private GameSession PlayingAt(int stage = 1, GameSaveData lobby = null, bool weakEnemy = false)
        {
            var session = new GameSession(catalog, lobby ?? LobbySave());
            Assert.That(session.StartRun(), Is.True);
            var save = session.ExportSave();
            save.run.stage = stage;
            save.run.patternStep = 0;
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
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RewardChoice));
            return session;
        }

        private static void EvadeNext(GameSession session, float leadMs = 30)
        {
            session.Tick(Math.Max(0, session.EnemyIntent.SecondsToHit - leadMs / 1000f));
            Assert.That(session.Dodge(session.EnemyIntent.Direction), Is.True);
            session.Tick(leadMs / 1000f);
        }

        [Test]
        public void FirstLaunchRequiresCorrectDodgeThenActualAttack()
        {
            var session = new GameSession(catalog);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.TutorialDodge));
            session.Tick(100);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Dodge(DodgeDirection.Back), Is.False);
            Assert.That(session.Attack(), Is.False);
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.TutorialAttack));
            Assert.That(session.Attack(), Is.True);
            session.Tick(.099f);
            Assert.That(session.TutorialStep, Is.EqualTo(1));
            session.Tick(.001f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Combat));
            Assert.That(session.TutorialStep, Is.EqualTo(2));
            Assert.That(session.EnemyHp, Is.EqualTo(110));
        }

        [TestCase(59, true, true)]
        [TestCase(60, true, true)]
        [TestCase(61, true, false)]
        [TestCase(160, true, false)]
        [TestCase(161, false, false)]
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
        public void WrongDirectionInsidePerfectWindowStillHurts()
        {
            var session = PlayingAt();
            session.Tick(.62f);
            session.Dodge(DodgeDirection.Forward);
            session.Tick(.03f);
            Assert.That(session.PlayerHp, Is.EqualTo(92));
            Assert.That(session.Stagger, Is.Zero);
        }

        [TestCase(1, DodgeDirection.Up)]
        [TestCase(2, DodgeDirection.Back)]
        [TestCase(3, DodgeDirection.Down)]
        [TestCase(4, DodgeDirection.Forward)]
        public void FirstFourStagesUseAllFourActualDangerDirections(int stage, DodgeDirection expected)
        {
            var session = PlayingAt(stage);
            Assert.That(session.EnemyIntent.Direction, Is.EqualTo(expected));
            EvadeNext(session);
            Assert.That(session.PlayerHp, Is.EqualTo(100));
            Assert.That(session.Stagger, Is.EqualTo(40));
        }

        [Test]
        public void BossComboKeeps450MsGapAnd250MsFollowupCue()
        {
            var session = PlayingAt(10);
            Assert.That(session.EnemyIntent.SecondsToHit, Is.EqualTo(.9f).Within(.001));
            Assert.That(session.EnemyIntent.HasNext, Is.True);
            EvadeNext(session);
            Assert.That(session.EnemyIntent.Direction, Is.EqualTo(DodgeDirection.Down));
            Assert.That(session.EnemyIntent.SecondsToHit, Is.EqualTo(.45f).Within(.001));
            Assert.That(session.EnemyIntent.Visible, Is.False);
            session.Tick(.2f);
            Assert.That(session.EnemyIntent.Visible, Is.True);
            Assert.That(session.EnemyIntent.SecondsToHit, Is.EqualTo(.25f).Within(.001));
            EvadeNext(session);
            Assert.That(session.Stagger, Is.EqualTo(80));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
        }

        [Test]
        public void TutorialAttackStepIsRestoredWithoutRepeatingTheDodge()
        {
            var session = new GameSession(catalog);
            session.Dodge(DodgeDirection.Up);
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.Phase, Is.EqualTo(SessionPhase.TutorialAttack));
            restored.Attack(); restored.Tick(.1f);
            Assert.That(restored.TutorialStep, Is.EqualTo(2));
            Assert.That(restored.EnemyHp, Is.EqualTo(110));
        }

        [Test]
        public void DodgeReuseIs260MsAndAttackIsAllowedAfter160Ms()
        {
            var session = PlayingAt();
            Assert.That(session.Dodge(DodgeDirection.Up), Is.True);
            session.Tick(.159f);
            Assert.That(session.Attack(), Is.False);
            session.Tick(.001f);
            Assert.That(session.Attack(), Is.True);
            session.Tick(.099f);
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
            session.Tick(.1f);
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
        public void SimultaneousLethalEnemyHitCancelsPlayerStrike()
        {
            var save = PlayingAt().ExportSave();
            save.run.playerHpMilli = 1000;
            var session = new GameSession(catalog, save);
            session.SetPaused(false);
            session.Tick(.55f);
            session.Attack();
            session.Tick(.1f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Lobby));
            Assert.That(session.EnemyHp, Is.EqualTo(120));
            Assert.That(session.Gold, Is.Zero);
        }

        [Test]
        public void StunCancelsHitsAndBlocksGainForTwoSecondsAfterward()
        {
            var session = PlayingAt();
            EvadeNext(session); EvadeNext(session); EvadeNext(session);
            Assert.That(session.IsEnemyStunned, Is.True);
            Assert.That(session.Stagger, Is.Zero);
            session.Tick(1.5f);
            Assert.That(session.IsEnemyStunned, Is.False);
            EvadeNext(session);
            Assert.That(session.Stagger, Is.Zero);
            EvadeNext(session);
            Assert.That(session.Stagger, Is.EqualTo(40));
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
            session.Attack(); session.Tick(.1f);
            Assert.That(damage.Critical, Is.True);
            Assert.That(damage.DamageMilli, Is.EqualTo(21000));
            Assert.That(session.EnemyHp, Is.EqualTo(99));
        }

        [Test]
        public void FractionalDamageAndDetachedSnapshotSurviveSerialization()
        {
            var lobby = LobbySave();
            lobby.relics.Add(new OwnedRelicData { id = "relic_05", level = 1 });
            lobby.characters[0].equippedRelicIds = new[] { "relic_05" };
            var session = PlayingAt(1, lobby);
            session.Attack(); session.Tick(.1f);
            var save = RoundTrip(session.ExportSave());
            Assert.That(save.run.enemyHpMilli, Is.EqualTo(109600));
            save.run.enemyHpMilli = 1;
            Assert.That(session.ExportSave().run.enemyHpMilli, Is.EqualTo(109600));
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.ExportSave().run.enemyHpMilli, Is.EqualTo(109600));
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
            Assert.That(restored.EnemyIntent.SecondsToHit, Is.EqualTo(.65f).Within(.001f));
            Assert.That(restored.ExportSave().randomState, Is.EqualTo(before.randomState));
        }

        [Test]
        public void FiveRewardIsThreeDistinctChoicesAndSurvivesInterruption()
        {
            var session = KillBoss(5);
            Assert.That(session.Fragments, Is.EqualTo(1));
            Assert.That(session.Gold, Is.EqualTo(120));
            Assert.That(session.RelicChoices.Count, Is.EqualTo(3));
            Assert.That(session.RelicChoices.Distinct().Count(), Is.EqualTo(3));
            var before = session.ExportSave();
            var restored = new GameSession(catalog, RoundTrip(before));
            CollectionAssert.AreEqual(session.RelicChoices, restored.RelicChoices);
            Assert.That(restored.Gold, Is.EqualTo(120));
            Assert.That(restored.ExportSave().randomState, Is.EqualTo(before.randomState));
            string id = restored.RelicChoices[0];
            Assert.That(restored.ChooseRelic(id), Is.True);
            Assert.That(restored.Stage, Is.EqualTo(6));
            Assert.That(restored.GetRelicLevel(id), Is.EqualTo(1));
            Assert.That(restored.EquippedRelicIds.Count, Is.Zero);
            Assert.That(restored.ChooseRelic(id), Is.False);
            Assert.That(restored.GetRelicLevel(id), Is.EqualTo(1));
            Assert.That(restored.Gold, Is.EqualTo(120));
        }

        [Test]
        public void OrdinaryDuplicateLevelsWithoutGoldAndMaxOnlyConvertsGold()
        {
            foreach (int level in new[] { 2, 5 })
            {
                var lobby = LobbySave();
                foreach (var relic in catalog.relics) lobby.relics.Add(new OwnedRelicData { id = relic.id, level = level });
                var session = KillBoss(5, lobby);
                string id = session.RelicChoices[0];
                session.ChooseRelic(id);
                Assert.That(session.GetRelicLevel(id), Is.EqualTo(level == 5 ? 5 : 3));
                Assert.That(session.Gold, Is.EqualTo(level == 5 ? 140 : 120));
            }
        }

        [Test]
        public void EquippedHpGrowthAppliesDeltaOnceWithoutFullHeal()
        {
            var lobby = LobbySave();
            lobby.relics.Add(new OwnedRelicData { id = "relic_10", level = 1 });
            lobby.characters[0].equippedRelicIds = new[] { "relic_10" };
            var save = KillBoss(5, lobby).ExportSave();
            save.run.playerHpMilli = 50000;
            save.run.offer.relicIds = new[] { "relic_10", "relic_05", "relic_15" };
            save.run.offer.offeredLevels = new[] { 1, 0, 0 };
            var session = new GameSession(catalog, RoundTrip(save));
            session.ChooseRelic("relic_10");
            Assert.That(session.PlayerMaxHp, Is.EqualTo(108));
            Assert.That(session.PlayerHp, Is.EqualTo(52));
            var restored = new GameSession(catalog, RoundTrip(session.ExportSave()));
            Assert.That(restored.PlayerHp, Is.EqualTo(52));
            Assert.That(restored.ChooseRelic("relic_10"), Is.False);
            Assert.That(restored.PlayerHp, Is.EqualTo(52));
        }

        [Test]
        public void TenChoiceCommitsCheckpointHealsAndAutomaticallyEntersEleven()
        {
            var save = KillBoss(10).ExportSave();
            save.run.playerHpMilli = 30000;
            Assert.That(save.characters[0].checkpoint, Is.Zero);
            var session = new GameSession(catalog, RoundTrip(save));
            Assert.That(session.Fragments, Is.EqualTo(3));
            Assert.That(session.ReturnToLobby(), Is.False);
            session.ChooseRelic(session.RelicChoices[0]);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Combat));
            Assert.That(session.Stage, Is.EqualTo(11));
            Assert.That(session.PlayerHp, Is.EqualTo(session.PlayerMaxHp));
            Assert.That(session.CheckpointStartStage, Is.EqualTo(11));
            Assert.That(session.ExportSave().characters[0].checkpoint, Is.EqualTo(10));
            session.ReturnToLobby(); session.StartRun();
            Assert.That(session.Stage, Is.EqualTo(11));
        }

        [Test]
        public void DeathAtNineReturnsToFirstCheckpoint()
        {
            var save = PlayingAt(9).ExportSave();
            save.run.playerHpMilli = 1000;
            var session = new GameSession(catalog, save);
            session.SetPaused(false);
            session.Tick(session.EnemyIntent.SecondsToHit);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.Lobby));
            Assert.That(session.CheckpointStartStage, Is.EqualTo(1));
            session.StartRun();
            Assert.That(session.Stage, Is.EqualTo(1));
            Assert.That(session.PlayerHp, Is.EqualTo(100));
        }

        [Test]
        public void TransitionEventsKeepTheFloorWhereTheActionOccurred()
        {
            var reward = KillBoss(5);
            int rewardStage = 0;
            reward.EventRaised += e => { if (e.Kind == SessionEventKind.RelicChosen) rewardStage = e.Stage; };
            reward.ChooseRelic(reward.RelicChoices[0]);
            Assert.That(reward.Stage, Is.EqualTo(6));
            Assert.That(rewardStage, Is.EqualTo(5));
            var save = PlayingAt(9).ExportSave();
            save.run.playerHpMilli = 1000;
            var death = new GameSession(catalog, save);
            int deathStage = 0;
            death.EventRaised += e => { if (e.Kind == SessionEventKind.PlayerDefeated) deathStage = e.Stage; };
            death.SetPaused(false);
            death.Tick(death.EnemyIntent.SecondsToHit);
            Assert.That(death.Stage, Is.EqualTo(1));
            Assert.That(deathStage, Is.EqualTo(9));
        }

        [Test]
        public void BoundaryCompletionIsMvpCompleteNotFakeFiftyClear()
        {
            var session = PlayingAt(11, weakEnemy: true);
            session.Attack(); session.Tick(.1f);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.MvpComplete));
            Assert.That(session.Cycle, Is.Zero);
            Assert.That(session.Stage, Is.EqualTo(11));
            Assert.That(session.ExportSave().highestClearedStage, Is.EqualTo(11));
            Assert.That(session.LastMessage, Does.Contain("MVP"));
        }

        [Test]
        public void ConfirmExchangeOnlyGrantsGoldThenUpgradeRequiresAnotherPress()
        {
            var save = LobbySave();
            save.characters[0].statLevels[0] = 40;
            save.fragments = 2;
            var session = new GameSession(catalog, save);
            session.Upgrade(0);
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.ExchangeConfirmation));
            Assert.That(session.PendingExchange.fragments, Is.EqualTo(2));
            Assert.That(session.PendingExchange.gold, Is.EqualTo(200));
            Assert.That(session.GetStatLevel(0), Is.EqualTo(40));
            Assert.That(session.ConfirmExchange(), Is.True);
            Assert.That(session.Gold, Is.EqualTo(200));
            Assert.That(session.Fragments, Is.Zero);
            Assert.That(session.GetStatLevel(0), Is.EqualTo(40));
            Assert.That(session.ConfirmExchange(), Is.False);
            Assert.That(session.Upgrade(0), Is.True);
            Assert.That(session.GetStatLevel(0), Is.EqualTo(41));
            Assert.That(session.Gold, Is.EqualTo(50));
        }

        [Test]
        public void CancelInsufficientAndStaleExchangeNeverSpend()
        {
            var save = LobbySave();
            save.characters[0].statLevels[0] = 40;
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
        public void SlotsAndInventoryAreSharedButEquipmentAndGrowthAreCharacterSpecific()
        {
            catalog.GetCharacter("dam").availableInMvp = true;
            var save = LobbySave();
            save.highestClearedStage = 10;
            save.fragments = 4;
            save.gold = 10;
            save.relics.Add(new OwnedRelicData { id = "relic_05", level = 1 });
            var session = new GameSession(catalog, save);
            Assert.That(session.BuyNextSlot(), Is.True);
            session.EquipRelic("relic_05");
            session.Upgrade(0);
            session.SelectCharacter("dam");
            Assert.That(session.UnlockedSlots, Is.EqualTo(2));
            Assert.That(session.GetRelicLevel("relic_05"), Is.EqualTo(1));
            Assert.That(session.EquippedRelicIds.Count, Is.Zero);
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
