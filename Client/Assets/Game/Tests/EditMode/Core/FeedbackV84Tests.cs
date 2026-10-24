using System;
using System.Linq;
using NUnit.Framework;

namespace LastBell.Tests
{
    public sealed partial class GameSessionTests
    {
        GameSession ReadyEnemy(bool weak = false)
        {
            var save = PlayingAt(5, weakEnemy: weak).ExportSave();
            save.run.stagger = catalog.combat.staggerThreshold;
            var s = new GameSession(catalog, RoundTrip(save)); s.SetPaused(false); return s;
        }

        GameSession FullReward(int fragments = 4, bool duplicate = false)
        {
            var save = KillBoss(10).ExportSave();
            var ids = catalog.relics.Take(3).Select(r => r.id).ToArray();
            foreach (string id in ids) save.relics.Add(new OwnedRelicData { id = id, level = 1 });
            save.storageCapacity = 3; save.fragments = fragments;
            var offered = duplicate ? ids : catalog.relics.Skip(3).Take(3).Select(r => r.id).ToArray();
            save.run.offer.relicIds = offered;
            save.run.offer.offeredLevels = offered.Select(id => duplicate ? 1 : 0).ToArray();
            save.run.pendingRewards[0] = save.run.offer;
            return new GameSession(catalog, RoundTrip(save));
        }

        [Test] public void FullStaggerWaitsForHitAndCancelledAttackDoesNotConsumeIt()
        {
            var s = ReadyEnemy(); int stuns = 0;
            s.EventRaised += e => { if (e.Kind == SessionEventKind.EnemyStunned) stuns++; };
            Assert.IsTrue(s.IsStunReady); Assert.IsFalse(s.IsEnemyStunned);
            double impact = s.EnemyThreats[0].ImpactAtMs;
            s.Attack(); s.Tick(.1f); s.Dodge(DodgeDirection.Up); s.Tick(.1f);
            Assert.IsTrue(s.IsStunReady); Assert.AreEqual(0, stuns);
            Assert.AreEqual(impact, s.EnemyThreats[0].ImpactAtMs);
            s.Tick(.31f); Assert.IsTrue(s.Attack()); s.Tick(.199f);
            Assert.IsFalse(s.IsEnemyStunned); s.Tick(.001f);
            Assert.IsTrue(s.IsEnemyStunned); Assert.IsFalse(s.IsStunReady);
            Assert.AreEqual(1, stuns); Assert.IsEmpty(s.EnemyThreats);
            Assert.AreEqual(0, s.ExportSave().run.stunExtensionMs);
        }

        [Test] public void CompletedStackSurvivesDamageAndReloadButPartialStackLosesPoints()
        {
            var s = ReadyEnemy(); s.Tick(s.EnemyIntent.SecondsToHit);
            Assert.IsTrue(s.IsStunReady); Assert.Less(s.PlayerHp, s.PlayerMaxHp);
            var restored = new GameSession(catalog, RoundTrip(s.ExportSave()));
            Assert.IsTrue(restored.IsStunReady); Assert.IsTrue(restored.IsPaused);
            var save = restored.ExportSave(); save.run.stagger = 80;
            s = new GameSession(catalog, save); s.SetPaused(false); s.Tick(s.EnemyIntent.SecondsToHit);
            Assert.AreEqual(60, s.Stagger);
        }

        [Test] public void StunTriggerHitUsesNormalDamageThenSubsequentHitUsesStunMultiplier()
        {
            var s = ReadyEnemy(); var save = s.ExportSave(); save.run.combatStats.critChancePercent = 0;
            s = new GameSession(catalog, save); s.SetPaused(false);
            float hp = s.EnemyHp; int attack = s.PlayerStats.attack;
            s.Attack(); s.Tick(.2f); Assert.AreEqual(attack, hp - s.EnemyHp);
            Assert.IsTrue(s.IsEnemyStunned); s.Tick(.3f); hp = s.EnemyHp;
            s.Attack(); s.Tick(.167f);
            Assert.AreEqual(Math.Round(attack * s.PlayerStats.stunDamageMultiplier, MidpointRounding.AwayFromZero), hp - s.EnemyHp);
        }

        [Test] public void LethalReadyHitDoesNotStartStunAndNextStageStartsEmpty()
        {
            var s = ReadyEnemy(true); int stuns = 0;
            s.EventRaised += e => { if (e.Kind == SessionEventKind.EnemyStunned) stuns++; };
            s.Attack(); s.Tick(.2f); Assert.AreEqual(0, stuns);
            FinishTransitions(s); Assert.AreEqual(6, s.Stage); Assert.IsFalse(s.IsStunReady); Assert.AreEqual(0, s.Stagger);
        }

        [Test] public void FullStorageRequiresExplicitExpansionAndChargesExactlyOnce()
        {
            var s = FullReward(); string id = s.RelicChoices[0];
            Assert.IsFalse(s.ChooseRelic(id)); Assert.AreEqual(4, s.Fragments); Assert.AreEqual(0, s.GetRelicLevel(id));
            s = new GameSession(catalog, RoundTrip(s.ExportSave()));
            Assert.IsTrue(s.ChooseRelic(id, true)); Assert.AreEqual(4, s.StorageCapacity);
            Assert.AreEqual(0, s.Fragments); Assert.AreEqual(1, s.GetRelicLevel(id));
            Assert.IsFalse(s.ChooseRelic(id, true)); Assert.AreEqual(4, s.OwnedRelicCount);
            Assert.IsTrue(s.RetryRun()); Assert.AreEqual(4, s.ExportSave().run.equippedRelicIds.Length);
        }

        [Test] public void InsufficientFragmentsNeverSpendOrLosePromisedRewardAndGoldAlternativeIsExplicit()
        {
            var s = FullReward(3); string id = s.RelicChoices[0]; long gold = s.Gold, earned = s.Result.Gold, replacement = s.PendingMaxRelicGold;
            Assert.IsFalse(s.ChooseRelic(id, true)); Assert.AreEqual(3, s.Fragments); Assert.AreEqual(1, s.PendingRewardCount);
            Assert.AreEqual(gold, s.Gold); Assert.AreEqual(3, s.StorageCapacity);
            Assert.IsFalse(s.ConvertRelicRewardToGold("not-offered"));
            Assert.IsTrue(s.ConvertRelicRewardToGold(id)); Assert.AreEqual(gold + replacement, s.Gold);
            Assert.AreEqual(earned + replacement, s.Result.Gold); Assert.AreEqual(0, s.GetRelicLevel(id));
            Assert.AreEqual(3, s.Fragments); Assert.AreEqual(3, s.StorageCapacity);
            Assert.IsFalse(s.ConvertRelicRewardToGold(id)); Assert.AreEqual(0, s.PendingRewardCount);
        }

        [Test] public void DuplicateUpgradeNeedsNoSlotOrFragmentsAndCannotUseFullStorageGoldAlternative()
        {
            var s = FullReward(0, true); string id = s.RelicChoices[0];
            Assert.IsFalse(s.NeedsRelicStorage(id)); Assert.IsFalse(s.ConvertRelicRewardToGold(id));
            Assert.IsTrue(s.ChooseRelic(id)); Assert.AreEqual(2, s.GetRelicLevel(id));
            Assert.AreEqual(3, s.StorageCapacity); Assert.AreEqual(3, s.OwnedRelicCount); Assert.AreEqual(0, s.Fragments);
        }

        [Test] public void AutomaticRelicsAffectEveryHeroWithoutManualEquipmentAndSnapshotAtRunStart()
        {
            var save = LobbySave(); save.relics.Add(new OwnedRelicData { id = "relic_10", level = 1 });
            catalog.GetCharacter("dam").availableInMvp = true;
            var s = new GameSession(catalog, save); Assert.AreEqual(106, s.PlayerStats.maxHp);
            Assert.IsFalse(s.UnequipRelic("relic_10")); s.SelectCharacter("dam"); Assert.AreEqual(106, s.PlayerStats.maxHp);
            s.StartRun(); Assert.AreEqual(106, s.PlayerMaxHp); Assert.That(s.ExportSave().run.equippedRelicIds, Is.EqualTo(new[] { "relic_10" }));
        }

        [TestCase(1, 0, 3)] [TestCase(2, 1, 4)] [TestCase(3, 2, 5)] [TestCase(1, 8, 8)]
        public void LegacyStorageMigrationKeepsPaidExpansionsAndAllOwnedRelics(int oldSlots, int owned, int expected)
        {
            var save = AsLegacySave(LobbySave(),7); save.unlockedSlots = oldSlots; save.gold = 1234; save.fragments = 17;
            foreach (var r in catalog.relics.Take(owned)) save.relics.Add(new OwnedRelicData { id = r.id, level = 2 });
            var s = new GameSession(catalog, RoundTrip(save)); Assert.AreEqual(expected, s.StorageCapacity);
            Assert.AreEqual(owned, s.OwnedRelicCount); Assert.AreEqual(1234, s.Gold); Assert.AreEqual(17, s.Fragments);
            s = new GameSession(catalog, RoundTrip(s.ExportSave())); Assert.AreEqual(expected, s.StorageCapacity); Assert.AreEqual(9, s.ExportSave().schemaVersion);
        }

        [Test] public void StorageExpansionStopsAtCatalogMaximumAndRejectsOverCapacitySave()
        {
            var save = LobbySave(); save.fragments = 1000; var s = new GameSession(catalog, save);
            foreach (int cost in catalog.economy.storageExpansionCosts)
            { Assert.AreEqual(cost, s.NextStorageExpansionCost); int before = s.Fragments; Assert.IsTrue(s.ExpandRelicStorage()); Assert.AreEqual(before - cost, s.Fragments); }
            Assert.AreEqual(10, s.StorageCapacity); Assert.IsFalse(s.ExpandRelicStorage());
            save = LobbySave(); foreach (var r in catalog.relics.Take(4)) save.relics.Add(new OwnedRelicData { id = r.id, level = 1 });
            Assert.Throws<InvalidOperationException>(() => new GameSession(catalog, save));
        }
    }
}
