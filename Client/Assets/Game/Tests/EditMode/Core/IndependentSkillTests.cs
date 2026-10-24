using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LastBell.Tests
{
    public sealed partial class GameSessionTests
    {
        [Test]
        public void IndependentSkillKeepsOrdinaryContactAndHasItsOwnEvents()
        {
            var s=PlayingAt();var events=new List<SessionEvent>();s.EventRaised+=events.Add;
            Assert.IsTrue(s.Attack());s.Tick(.05f);var before=s.ExportSave().run;
            Assert.IsTrue(s.UseSkill());var after=s.ExportSave().run;
            Assert.AreEqual(before.attackHitAtMs,after.attackHitAtMs);Assert.AreEqual(before.attackReadyAtMs,after.attackReadyAtMs);
            Assert.AreEqual(20000,s.SkillCooldownRemainingMs);Assert.AreEqual(200,s.SkillContactRemainingMs);
            Assert.IsFalse(s.UseSkill());s.Tick(.1501f);
            Assert.AreEqual(1,events.Count(e=>e.Kind==SessionEventKind.EnemyDamaged));Assert.AreEqual(0,events.Count(e=>e.Kind==SessionEventKind.PlayerSkillContact));
            s.Tick(.05f);Assert.AreEqual(1,events.Count(e=>e.Kind==SessionEventKind.PlayerSkillContact));
            Assert.AreEqual(1,events.Count(e=>e.Kind==SessionEventKind.AttackStarted));Assert.AreEqual(1,events.Count(e=>e.Kind==SessionEventKind.PlayerSkillStarted));
            Assert.AreEqual(s.CurrentSkill.id,events.Single(e=>e.Kind==SessionEventKind.PlayerSkillContact).SkillId);
        }

        [Test]
        public void IndependentSkillSurvivesDodgeAndJsonRestoreWithoutReplay()
        {
            var s=PlayingAt();Assert.IsTrue(s.Dodge(DodgeDirection.Up));double dodge=s.ExportSave().run.dodgeStartMs;
            Assert.IsTrue(s.UseSkill());Assert.AreEqual(dodge,s.ExportSave().run.dodgeStartMs);s.Tick(.08f);
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));s.SetPaused(false);Assert.AreEqual(120,s.SkillContactRemainingMs,.001);
            var events=new List<SessionEvent>();s.EventRaised+=events.Add;s.Tick(.1201f);
            Assert.AreEqual(1,events.Count(e=>e.Kind==SessionEventKind.PlayerSkillContact));Assert.IsFalse(s.SkillContactPending);
            float hp=s.EnemyHp;s=new GameSession(catalog,RoundTrip(s.ExportSave()));s.SetPaused(false);s.Tick(0);Assert.AreEqual(hp,s.EnemyHp);
        }

        [Test]
        public void CompanionSpecialReservationSurvivesJsonAndReplacesOneBasicHit()
        {
            var lobby=LobbySave();lobby.companions.Add(new CompanionProgressData{id="tori",level=1});lobby.characters[0].companionIds.Add("tori");
            var s=PlayingAt(1,lobby);var save=s.ExportSave();var companion=save.run.companions[0];companion.remainingMs=400;companion.randomState=1;
            s=new GameSession(catalog,RoundTrip(save));s.SetPaused(false);s.Tick(.2201f);Assert.AreEqual("companion_tori_double_shot",s.RunCompanions[0].PendingSkillId);
            int rng=s.ExportSave().run.companions[0].randomState;double raw=companion.damageMilli+companion.damageFraction;
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));s.SetPaused(false);Assert.AreEqual(rng,s.ExportSave().run.companions[0].randomState);
            var events=new List<SessionEvent>();s.EventRaised+=events.Add;s.Tick(.18f);
            var hits=events.Where(e=>e.Kind==SessionEventKind.CompanionDamaged).ToArray();Assert.AreEqual(1,hits.Length);
            Assert.AreEqual((long)Math.Floor(raw*1.8),hits[0].DamageMilli);Assert.AreEqual("companion_tori_double_shot",hits[0].SkillId);
            Assert.IsFalse(events.Any(e=>e.Kind==SessionEventKind.EnemyDamaged));
        }

        [Test]
        public void LegacySchemaNineSkillMigrationKeepsCooldownAndDoesNotReplayCoupledAttack()
        {
            var save=PlayingAt().ExportSave();save.run.skillRulesVersion=0;save.run.pendingSkillAttack=true;save.run.attackHitAtMs=200;save.run.skillCooldownRemainingMs=19900;
            var s=new GameSession(catalog,RoundTrip(save));s.SetPaused(false);
            Assert.AreEqual(9,s.ExportSave().schemaVersion);Assert.AreEqual(1,s.ExportSave().run.skillRulesVersion);
            Assert.AreEqual(19900,s.SkillCooldownRemainingMs);Assert.IsFalse(s.SkillContactPending);Assert.IsFalse(s.ExportSave().run.pendingSkillAttack);
            float hp=s.EnemyHp;s.Tick(.2f);Assert.AreEqual(hp,s.EnemyHp);
        }
    }
}
