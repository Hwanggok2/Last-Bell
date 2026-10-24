using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace LastBell.Tests
{
    public sealed class CombatV59Tests
    {
        GameCatalog catalog;
        [SetUp] public void Setup(){catalog=UnityEngine.JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Game/Data/Generated/mvp-catalog.json")));}
        GameSession Playing(int stage=3)
        {
            var save=new GameSession(catalog).ExportSave();save.tutorialStep=2;save.prologueSeen=true;save.comboHintSeen=true;save.comboPracticeCompleted=true;save.phase=SessionPhase.Lobby;save.run=null;
            var s=new GameSession(catalog,save);s.StartRun();save=s.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;
            save.run.stage=stage;save.run.randomSequence=catalog.GetStage(stage).patternSteps.Select(x=>x.danger).ToArray();save.run.sequenceCursor=0;save.run.immunityUntilMs=100000;save.run.enemyHpMilli=save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;
            s=new GameSession(catalog,save);s.SetPaused(false);return s;
        }
        [TestCase(0,true,true)] [TestCase(90,true,true)] [TestCase(91,true,false)] [TestCase(240,true,false)] [TestCase(241,false,false)]
        public void DownParryUsesIndependentDefenseWindowAndDealsNoDamage(int lead,bool success,bool perfect)
        {
            var s=Playing();var events=new List<SessionEvent>();s.EventRaised+=events.Add;float enemy=s.EnemyHp;
            s.TickInputFrame((900-lead)/1000f);Assert.That(s.Dodge(DodgeDirection.Down),Is.True);s.TickInputFrame(lead/1000f);s.TickInputFrame(0);
            Assert.That(s.PlayerHp,Is.EqualTo(success?100:100-catalog.GetStage(3).damage));
            Assert.That(s.EnemyHp,Is.EqualTo(enemy));
            Assert.That(s.CanPerfectCounter,Is.EqualTo(perfect));
            Assert.That(events.Count(e=>e.Kind==SessionEventKind.Parried||e.Kind==SessionEventKind.PerfectParry),Is.EqualTo(success?1:0));
            if(success)Assert.That(events.Any(e=>e.Kind==SessionEventKind.PerfectParry),Is.EqualTo(perfect));
        }
        [Test] public void ParryInterruptsPendingAttackWithoutRefundingItsCooldown()
        {
            var s=Playing();s.Tick(.69f);Assert.That(s.Attack(),Is.True);double ready=s.ExportSave().run.attackReadyAtMs;float hp=s.EnemyHp;
            s.Tick(.16f);Assert.That(s.CanParry,Is.True);Assert.That(s.Dodge(DodgeDirection.Down),Is.True);Assert.That(s.PlayerCombatBody.X,Is.EqualTo(353));
            Assert.That(s.ExportSave().run.attackReadyAtMs,Is.EqualTo(ready));Assert.That(s.ExportSave().run.attackHitAtMs,Is.EqualTo(-1));
            s.Tick(.05f);Assert.That(s.CanPerfectCounter,Is.True);Assert.That(s.EnemyHp,Is.EqualTo(hp));Assert.That(s.Attack(),Is.False);
            s.Tick(.29f);Assert.That(s.Attack(),Is.True);s.Tick(.2f);Assert.That(s.EnemyHp,Is.LessThan(hp));
        }
        [Test] public void ParryPreservesAnAlreadyLandedAttackAndCannotRepeatOnHeldInput()
        {
            var s=Playing();s.Tick(.61f);s.Attack();s.Tick(.24f);float hp=s.EnemyHp;
            Assert.That(hp,Is.LessThan(s.EnemyMaxHp));Assert.That(s.Dodge(DodgeDirection.Down),Is.True);Assert.That(s.Dodge(DodgeDirection.Down),Is.False);
            s.Tick(.05f);Assert.That(s.EnemyHp,Is.EqualTo(hp));Assert.That(s.Dodge(DodgeDirection.Back),Is.False);
        }
        [TestCase(DodgeDirection.Up)] [TestCase(DodgeDirection.Back)]
        public void HighAttackRequiresDownParryInsteadOfMovement(DodgeDirection direction)
        {
            var s=Playing();s.Tick(.85f);Assert.That(s.Dodge(direction),Is.True);s.Tick(.05f);
            Assert.That(s.PlayerHp,Is.LessThan(100));Assert.That(s.CanPerfectCounter,Is.False);Assert.That(s.Dodge(DodgeDirection.Down),Is.False);
        }
        [Test] public void DoubleParryAndMixedDefensesPreserve750MsHitSpacing()
        {
            var steps=catalog.GetStage(5).patternSteps;
            foreach(var step in steps.Take(2)){step.danger="high_sweep";step.requiredDodge="parry";}
            var s=Playing(5);var times=s.EnemyThreats.Select(t=>t.ImpactAtMs).ToArray();Assert.That(times,Is.EqualTo(new double[]{1800,2550}));
            s.Tick(1.75f);Assert.That(s.Dodge(DodgeDirection.Down),Is.True);s.Tick(.05f);Assert.That(s.EnemyThreats.Length,Is.EqualTo(1));
            s.Tick(.70f);Assert.That(s.Dodge(DodgeDirection.Down),Is.True);s.Tick(.05f);Assert.That(s.PlayerHp,Is.EqualTo(100));
            steps[1].danger="low_sweep";steps[1].requiredDodge="up";s=Playing(5);s.Tick(1.75f);s.Dodge(DodgeDirection.Down);s.Tick(.75f);
            Assert.That(s.Dodge(DodgeDirection.Up),Is.True);Assert.That(s.CanPerfectCounter,Is.False);s.Tick(.05f);Assert.That(s.PlayerHp,Is.EqualTo(100));
        }
        [Test] public void RhythmUsesThreeBlinksThenContactAndEqualVisibleWaveSpeed()
        {
            foreach(int stage in new[]{1,5,6,9,10})
            {
                var s=Playing(stage);var q=s.EnemyThreats;double beat=q[0].BeatMs;
                Assert.That(q[0].ImpactAtMs,Is.EqualTo(q.Length*3*beat));
                Assert.That(q[0].ReleaseAtMs,Is.GreaterThanOrEqualTo(q.Last().CueAtMs+2.5*beat));
                foreach(var t in q){Assert.That(t.ImpactAtMs-t.WaveStartMs,Is.EqualTo(2.5*beat));Assert.That(t.ImpactAtMs-t.ReleaseAtMs,Is.EqualTo(catalog.combat.attackFlightMs));}
                for(int i=1;i<q.Length;i++)Assert.That(q[i].ImpactAtMs-q[i-1].ImpactAtMs,Is.EqualTo(catalog.combat.comboDefaultGapMs));
            }
        }
        [Test] public void DodgeBodyMatchesEntryHoldAndReturnAndAttackMoves165Pixels()
        {
            var s=Playing(2);s.Tick(.85f);s.Dodge(DodgeDirection.Back);Assert.That(s.DodgeVisualAmount,Is.Zero);
            s.Tick(.025f);Assert.That(s.DodgeVisualAmount,Is.EqualTo(.875f).Within(.001));
            Assert.That(s.PlayerCombatBody.X,Is.EqualTo(353-90*.875f).Within(.01));
            s.Tick(.025f);Assert.That(s.PlayerHp,Is.EqualTo(100));s.Tick(.1f);Assert.That(s.DodgeVisualAmount,Is.EqualTo(1).Within(.0001));
            s.Tick(.065f);Assert.That(s.DodgeVisualAmount,Is.EqualTo(.125f).Within(.001));s.Tick(.065f);Assert.That(s.DodgeVisualAmount,Is.Zero.Within(.0001));
            Assert.That(s.Attack(),Is.True);s.Tick(.12f);Assert.That(s.PlayerCombatBody.X,Is.EqualTo(518));
            s.Tick(.38f);Assert.That(s.PlayerCombatBody.X,Is.EqualTo(353));
        }
        [Test] public void PauseAndRestoreNeverReplayAStaleParry()
        {
            var s=Playing();s.Tick(.85f);s.Dodge(DodgeDirection.Down);s.SetPaused(true);s.Tick(5);Assert.That(s.ClockMs,Is.EqualTo(850).Within(.01));
            var restored=new GameSession(catalog,s.ExportSave());restored.SetPaused(false);restored.Tick(.9f);Assert.That(restored.PlayerHp,Is.LessThan(100));
            s.SetPaused(false);s.Tick(.05f);Assert.That(s.PlayerHp,Is.EqualTo(100));
        }
        [Test] public void AttackButtonNeverBecomesAParryAtHighContact()
        {
            var s=Playing();int parries=0;s.EventRaised+=e=>{if(e.Kind==SessionEventKind.ParryStarted)parries++;};
            s.TickInputFrame(.9f);Assert.That(s.Attack(),Is.True);s.TickInputFrame(0);
            Assert.That(parries,Is.Zero);Assert.That(s.PlayerHp,Is.LessThan(100));Assert.That(s.CanPerfectCounter,Is.False);
            Assert.That(s.AttackRecoverySeconds,Is.GreaterThan(.49f));
        }
        [Test] public void EarlyDownSpendsDefenseReuseAndNeverCrouchesOrDealsDamage()
        {
            var s=Playing();float hp=s.EnemyHp;
            s.Tick(.6f);Assert.That(s.Dodge(DodgeDirection.Down),Is.True);
            Assert.That(s.PlayerCombatBody.X,Is.EqualTo(353));Assert.That(s.PlayerCombatBody.Y,Is.EqualTo(340));
            Assert.That(s.DodgeVisualAmount,Is.Zero);Assert.That(s.AttackRecoverySeconds,Is.EqualTo(.24f).Within(.001));
            s.Tick(.25f);Assert.That(s.Dodge(DodgeDirection.Down),Is.False);s.Tick(.05f);
            Assert.That(s.PlayerHp,Is.LessThan(100));Assert.That(s.EnemyHp,Is.EqualTo(hp));Assert.That(s.CanPerfectCounter,Is.False);
        }
        [Test] public void DownCannotParryALowAttackOrCancelItsQueue()
        {
            var s=Playing(1);s.Tick(.85f);Assert.That(s.Dodge(DodgeDirection.Down),Is.True);s.Tick(.05f);
            Assert.That(s.PlayerHp,Is.LessThan(100));Assert.That(s.Stagger,Is.Zero);Assert.That(s.CanPerfectCounter,Is.False);
            Assert.That(s.EnemyThreats.Length,Is.EqualTo(1));
        }
    }
}
