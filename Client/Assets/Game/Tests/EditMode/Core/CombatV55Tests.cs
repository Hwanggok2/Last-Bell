using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
namespace LastBell.Tests
{
    public sealed class CombatV55Tests
    {
        GameCatalog catalog;
        [SetUp] public void Setup(){catalog=UnityEngine.JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Game/Data/Generated/mvp-catalog.json")));}
        GameSession Playing(int stage=1,int stagger=0,bool immune=false)
        {
            var save=new GameSession(catalog).ExportSave();save.tutorialStep=2;save.prologueSeen=true;save.comboHintSeen=true;save.comboPracticeCompleted=true;save.phase=SessionPhase.Lobby;save.run=null;
            var s=new GameSession(catalog,save);s.StartRun();save=s.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;
            save.run.stage=stage;save.run.randomSequence=catalog.GetStage(stage).patternSteps.Select(x=>x.danger).ToArray();save.run.sequenceCursor=0;save.run.stagger=stagger;save.run.immunityUntilMs=immune?100000:-1;
            save.run.enemyHpMilli=save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000;
            s=new GameSession(catalog,save);s.SetPaused(false);return s;
        }
        static void Perfect(GameSession s){s.Tick(s.EnemyIntent.SecondsToHit-.05f);Assert.That(s.Dodge(s.EnemyIntent.Direction),Is.True);s.Tick(.050001f);}
        [TestCase("low_sweep",DodgeDirection.Up)] [TestCase("thrust",DodgeDirection.Back)]
        [TestCase("outer_arc",DodgeDirection.Forward)]
        public void SharedFootprintHasExactlyOneSafeDodge(string pattern,DodgeDirection safe)
        {
            var box=CombatGeometry.Threat(pattern);Assert.That(box.Overlaps(CombatGeometry.Body(safe,false)),Is.True);
            foreach(DodgeDirection d in Enum.GetValues(typeof(DodgeDirection)))Assert.That(box.Overlaps(CombatGeometry.Body(d,true)),Is.EqualTo(d!=safe),pattern+" / "+d);
        }
        [TestCase(1,780,900)] [TestCase(2,780,900)]
        public void SingleHitsOnTheFourthBeatAfterThreeBlinks(int stage,int release,int impact)
        {
            var s=Playing(stage);var t=s.EnemyThreats.Single();Assert.That(t.CueAtMs,Is.Zero);Assert.That(t.CueEndMs,Is.EqualTo(impact));
            Assert.That(t.ReleaseAtMs,Is.EqualTo(release));Assert.That(t.ImpactAtMs,Is.EqualTo(impact));
            var events=new List<SessionEvent>();s.EventRaised+=events.Add;s.Tick((impact-1)/1000f);
            Assert.That(events.Select(e=>e.Kind),Is.EqualTo(new[]{SessionEventKind.EnemyCueStarted,SessionEventKind.EnemyReleased}));
            Assert.That(s.PlayerHp,Is.EqualTo(100));s.Tick(.001f);Assert.That(s.PlayerHp,Is.LessThan(100));
            Assert.That(events.Last(e=>e.Kind==SessionEventKind.PlayerDamaged).TimeMs,Is.EqualTo(impact));
        }
        [TestCase(5,900,1800,2550)] [TestCase(10,900,1800,2550)]
        public void FullQueuePrecedesBothHitsAndNeverCuesAgainBetweenHits(int stage,int cue,int hit1,int hit2)
        {
            var s=Playing(stage,0,true);var ts=s.EnemyThreats;
            Assert.That(ts.Select(t=>t.CueAtMs),Is.EqualTo(new double[]{0,cue}));
            Assert.That(ts.Select(t=>t.ImpactAtMs),Is.EqualTo(new double[]{hit1,hit2}));
            var events=new List<SessionEvent>();s.EventRaised+=events.Add;Perfect(s);Perfect(s);
            Assert.That(events.Where(e=>e.Kind==SessionEventKind.EnemyCueStarted).Select(e=>e.TimeMs),Is.EqualTo(new double[]{0,cue}));
            Assert.That(events.Where(e=>e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.PerfectParry).Select(e=>e.TimeMs),Is.EqualTo(new double[]{hit1,hit2}));
            Assert.That(s.PlayerHp,Is.EqualTo(100));
        }
        [Test] public void CounterConsumesAcceptedInputButPreservesBothCooldownsAndContactDeadline()
        {
            var s=Playing();Perfect(s);double dodgeReady=s.ClockMs+s.DodgeRecoverySeconds*1000;
            SessionEvent attack=null;int damage=0;s.EventRaised+=e=>{if(e.Kind==SessionEventKind.AttackStarted)attack=e;if(e.Kind==SessionEventKind.EnemyDamaged)damage++;};
            Assert.That(s.CanPerfectCounter,Is.True);Assert.That(s.AttackRecoverySeconds,Is.Zero);Assert.That(s.Attack(),Is.True);
            Assert.That(attack.PerfectCounter,Is.True);Assert.That(s.CanPerfectCounter,Is.False);
            Assert.That(s.ClockMs+s.DodgeRecoverySeconds*1000,Is.EqualTo(dodgeReady).Within(.01));
            Assert.That(s.Attack(),Is.False);s.Tick(.199f);Assert.That(damage,Is.Zero);s.Tick(.001f);Assert.That(damage,Is.EqualTo(1));
            Assert.That(s.Attack(),Is.False);s.Tick(.3f);Assert.That(s.Attack(),Is.True);Assert.That(attack.PerfectCounter,Is.False);
        }
        [Test] public void CounterCannotRefundCooldownAndRejectedInputRetainsOpportunity()
        {
            var s=Playing();s.Tick(.65f);s.Attack();s.Tick(.2f);s.Dodge(DodgeDirection.Up);s.Tick(.05f);
            Assert.That(s.CanPerfectCounter,Is.True);Assert.That(s.Attack(),Is.False);Assert.That(s.CanPerfectCounter,Is.True);
            s.Tick(.25f);Assert.That(s.Attack(),Is.True);Assert.That(s.CanPerfectCounter,Is.False);
        }
        [Test] public void CounterExpiresClearsOnNewDodgeAndNeverCancelsTheNextThreat()
        {
            var s=Playing(5);Perfect(s);double next=s.EnemyThreats[0].ImpactAtMs;s.Attack();
            Assert.That(s.EnemyThreats[0].ImpactAtMs,Is.EqualTo(next));s.Tick(.75f);Assert.That(s.PlayerHp,Is.EqualTo(100-catalog.GetStage(5).damage));
            s=Playing();Perfect(s);s.Tick(1.201f);Assert.That(s.CanPerfectCounter,Is.False);
            s=Playing();Perfect(s);s.Tick(.31f);s.Dodge(DodgeDirection.Back);Assert.That(s.CanPerfectCounter,Is.False);
        }
        [Test] public void StunCancelsRestOfComboAndRestoredFollowupReplaysFullWarning()
        {
            var s=Playing(5,80);Perfect(s);Assert.That(s.IsStunReady,Is.True);Assert.That(s.EnemyThreats.Length,Is.EqualTo(1));s.Attack();s.Tick(.2f);Assert.That(s.IsEnemyStunned,Is.True);Assert.That(s.EnemyThreats,Is.Empty);
            float hp=s.PlayerHp;s.Tick(.4f);Assert.That(s.PlayerHp,Is.EqualTo(hp));
            s=Playing(5);Perfect(s);var saved=s.ExportSave();s=new GameSession(catalog,saved);
            Assert.That(s.IsPaused,Is.True);Assert.That(s.CanPerfectCounter,Is.False);Assert.That(s.EnemyThreats.Length,Is.EqualTo(1));
            Assert.That(s.EnemyThreats[0].PatternId,Is.EqualTo("high_sweep"));Assert.That(s.EnemyIntent.SecondsToHit,Is.EqualTo(.9f).Within(.001));
        }
        [Test] public void PausedCueAndParticlesHaveNoExtraCoreEventsOnResume()
        {
            var s=Playing(5);var events=new List<SessionEvent>();s.EventRaised+=events.Add;s.Tick(.9f);int count=events.Count;
            s.SetPaused(true);s.Tick(10);Assert.That(s.ClockMs,Is.EqualTo(900).Within(.001));Assert.That(events.Count,Is.EqualTo(count));
            s.SetPaused(false);s.Tick(0);Assert.That(events.Count,Is.EqualTo(count));
            s.Tick(.78f);Assert.That(events.Last().Kind,Is.EqualTo(SessionEventKind.EnemyReleased));Assert.That(events.Last().TimeMs,Is.EqualTo(1680).Within(.001));
        }
        [Test] public void LeavingRearFootprintByAnAttackNeverCausesAnInvisibleDirectionFailure()
        {
            var s=Playing(6);Perfect(s);s.Tick(.22f);s.Attack();s.Tick(.26f);
            Assert.That(CombatGeometry.Threat("outer_arc").Overlaps(s.PlayerCombatBody),Is.False);
            Assert.That(s.PlayerHp,Is.EqualTo(100));Assert.That(s.Stagger,Is.EqualTo(40));
        }
    }
}
