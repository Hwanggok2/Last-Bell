using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastBell.Tests
{
    public sealed class FeedbackV6Tests
    {
        GameCatalog catalog;
        [SetUp] public void Setup(){catalog=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(Application.dataPath,"Game/Data/Generated/mvp-catalog.json")));}
        GameSession Playing(int stage=1)
        {
            var save=new GameSession(catalog).ExportSave();save.prologueSeen=save.comboHintSeen=true;save.tutorialStep=2;
            save.phase=SessionPhase.Combat;save.run.transition=null;save.run.stage=stage;
            save.run.enemyHpMilli=save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;
            return Restore(save);
        }
        GameSession Restore(GameSaveData save){var s=new GameSession(catalog,JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save)));s.SetPaused(false);return s;}
        void Perfect(GameSession s)
        {
            s.Tick(Math.Max(0,s.EnemyIntent.SecondsToHit-.05f));Assert.IsTrue(s.Dodge(s.EnemyIntent.Direction));s.Tick(.05f);
        }
        [Test] public void AllFifteenActorsHavePlayableAssignmentsAndTwentyHasBossStats()
        {
            catalog.ValidateOrThrow();Assert.AreEqual(20,catalog.mvpLastStage);
            Assert.AreEqual(15,catalog.stages.Take(20).Select(x=>catalog.GetEnemy(x.enemyId).artId).Distinct().Count());
            Assert.AreEqual(8652,catalog.GetStage(20).maxHp);Assert.AreEqual("chain_jailer",catalog.GetStage(20).enemyId);
        }
        [TestCase(0L)] [TestCase(1L)] [TestCase(42L)] [TestCase(750L)] [TestCase(1000000000L)] [TestCase(long.MaxValue)]
        public void DotAccountingFinishesInOnePointTwoSecondsWithoutOverflow(long reward)
        {
            var g=new GoldCollection(reward);Assert.AreEqual(Math.Min(reward,GoldCollection.MaxDots),g.Count);
            Assert.AreEqual(0,g.Collected(0));Assert.AreEqual(reward,g.Collected(1.2));Assert.AreEqual(reward,g.Collected(20));
            long previous=0;for(int i=0;i<=120;i++){long value=g.Collected(i/100d);Assert.GreaterOrEqual(value,previous);Assert.LessOrEqual(value,reward);previous=value;}
            if(g.Count>1)Assert.AreEqual(1.2,g.StartAt(g.Count-1)+GoldCollection.FlightSeconds,1e-9);
        }
        [Test] public void HigherRewardsIncreaseDensityAndRenderedDotsRemainBounded()
        {
            var small=new GoldCollection(10);var large=new GoldCollection(100);
            Assert.Greater(large.Count,small.Count);Assert.Less(large.StartAt(1),small.StartAt(1));
            Assert.AreEqual(GoldCollection.MaxDots,new GoldCollection(100000).Count);
            Assert.AreEqual(750,new GoldCollection(750).Count);
            Assert.Less((Math.Ceiling(GoldCollection.MaxDots*GoldCollection.FlightSeconds/GoldCollection.EmissionSeconds)+1)*4,65000);
        }
        [Test] public void GateWardenIsTheFifthStageMinibossAndGateLockMovesToNine()
        {
            var five=catalog.GetStage(5);var nine=catalog.GetStage(9);
            Assert.AreEqual("gate_warden",five.enemyId);Assert.AreEqual("miniboss",five.kind);Assert.AreEqual(293,five.maxHp);
            Assert.AreEqual("gate_lock",nine.enemyId);Assert.AreEqual("normal",nine.kind);Assert.AreEqual(319,nine.maxHp);
        }
        [Test] public void EarlyNormalEnemiesNeverComboAndThreeHitAttacksBeginAtFifteen()
        {
            var seenTriples=new HashSet<int>();var seenDoubles=new HashSet<int>();
            for(int stage=1;stage<=20;stage++)for(int seed=1;seed<=64;seed++)
            {
                var save=Playing(stage).ExportSave();save.run.randomSequence=new string[0];save.run.sequenceCursor=0;save.run.patternRandomState=seed;
                var s=Restore(save);int count=s.EnemyThreats.Length;
                if(stage<=10&&catalog.GetStage(stage).kind=="normal")Assert.AreEqual(1,count,"stage "+stage);
                if(stage<15)Assert.LessOrEqual(count,2,"stage "+stage);
                if(stage==10)Assert.AreEqual(2,count);
                if(count==2)seenDoubles.Add(stage);if(count==3)seenTriples.Add(stage);
            }
            Assert.IsTrue(seenDoubles.Contains(5));Assert.IsTrue(seenTriples.Contains(15));Assert.IsTrue(seenTriples.Contains(20));
        }
        [TestCase(6,1)] [TestCase(9,1)] [TestCase(10,2)]
        public void ExistingQueuedAttacksRespectTheUpdatedStageOnResume(int stage,int expected)
        {
            var save=Playing(stage).ExportSave();save.run.randomSequence=stage==10?new[]{"thrust","outer_arc","high_sweep"}:new[]{"low_sweep","high_sweep"};
            save.run.sequenceCursor=0;save.run.patternRandomState=77;save.gold=1234;
            var restored=Restore(save);Assert.AreEqual(expected,restored.EnemyThreats.Length);
            Assert.AreEqual(1234,restored.Gold);Assert.AreEqual(77,restored.ExportSave().run.patternRandomState);
            Assert.AreEqual(restored.ClockMs,restored.EnemyThreats[0].CueAtMs);
        }
        [Test] public void RandomQueueFreezesAndResumeKeepsOnlyUnresolvedDirections()
        {
            var s=Playing(20);var save=s.ExportSave();save.run.randomSequence=new[]{"outer_arc","high_sweep","low_sweep"};save.run.sequenceCursor=0;save.run.immunityUntilMs=100000;
            s=Restore(save);var original=s.EnemyThreats.Select(t=>t.PatternId).ToArray();int rng=s.ExportSave().run.patternRandomState;
            Perfect(s);Assert.AreEqual(2,s.EnemyThreats.Length);
            var restored=Restore(s.ExportSave());Assert.AreEqual(original.Skip(1),restored.EnemyThreats.Select(t=>t.PatternId));
            Assert.AreEqual(rng,restored.ExportSave().run.patternRandomState);Assert.AreEqual(restored.ClockMs,restored.EnemyThreats[0].CueAtMs);
            Assert.AreEqual(restored.ClockMs+1440,restored.EnemyThreats[0].ImpactAtMs,.001);
        }
        [Test] public void PatternRandomDoesNotConsumeRewardRandomAndNeverTriplesDirections()
        {
            var s=Playing(20);var save=s.ExportSave();save.run.immunityUntilMs=1000000;s=Restore(save);
            int rewardRandom=s.ExportSave().randomState;var patterns=new List<string>();var counts=new HashSet<int>();
            for(int i=0;i<120;i++)
            {
                var q=s.EnemyThreats;if(q[0].SequenceIndex==0)counts.Add(q.Length);
                patterns.Add(q[0].PatternId);Perfect(s);
            }
            Assert.AreEqual(rewardRandom,s.ExportSave().randomState);Assert.That(counts,Is.EquivalentTo(new[]{2,3}));
            for(int i=2;i<patterns.Count;i++)Assert.IsFalse(patterns[i]==patterns[i-1]&&patterns[i]==patterns[i-2]);
        }
        [TestCase(1,40d,48d)] [TestCase(5,24d,28.8d)] [TestCase(10,16d,19.2d)]
        public void BossResistanceAndConsecutivePerfectBonusAreApplied(int stage,double first,double second)
        {
            var s=Playing(stage);var events=new List<SessionEvent>();s.EventRaised+=events.Add;Perfect(s);Assert.AreEqual(first,s.Stagger,.001);
            Perfect(s);Assert.AreEqual(first+second,s.Stagger,.001);Assert.AreEqual(2,s.PerfectStreak);
            var restored=Restore(s.ExportSave());Assert.AreEqual(2,restored.PerfectStreak);
        }
        [Test] public void StunnedAttackSnapshotsSpeedAndExtendsOnlyUpToThreeHundredMs()
        {
            var s=Playing();var save=s.ExportSave();save.run.stunUntilMs=10000;save.run.immunityUntilMs=12000;save.run.enemyHitAtMs=-1;
            save.run.enemyHpMilli=1000000;s=Restore(save);
            for(int i=0;i<8;i++)
            {
                Assert.IsTrue(s.Attack());var attack=s.ExportSave().run;
                Assert.AreEqual(500/1.2,attack.attackReadyAtMs-s.ClockMs,.001);Assert.AreEqual(200/1.2,attack.attackHitAtMs-s.ClockMs,.001);
                s.Tick(.417f);
            }
            var result=s.ExportSave().run;Assert.AreEqual(300,result.stunExtensionMs);Assert.AreEqual(10300,result.stunUntilMs,.001);Assert.AreEqual(12300,result.immunityUntilMs,.001);Assert.AreEqual(8,s.HitCombo);
        }
        [Test] public void ExactStunEndExpiresBeforeHitAndLethalHitDoesNotExtend()
        {
            var s=Playing();var save=s.ExportSave();save.run.stunUntilMs=200/1.2;save.run.immunityUntilMs=2200;save.run.enemyHitAtMs=-1;
            s=Restore(save);s.Attack();s.Tick(.167f);Assert.IsFalse(s.IsEnemyStunned);Assert.AreEqual(0,s.ExportSave().run.stunExtensionMs);
            save=Playing().ExportSave();save.run.stunUntilMs=1500;save.run.immunityUntilMs=3500;save.run.enemyHpMilli=1000;save.run.enemyHitAtMs=-1;
            s=Restore(save);s.Attack();s.Tick(.167f);Assert.AreEqual(0,s.ExportSave().run.stunExtensionMs);Assert.AreEqual(1,s.HitCombo);
        }
        [Test] public void DamageResetsComboAndPerfectStreakButStageTravelPreservesHitCombo()
        {
            var s=Playing();var save=s.ExportSave();save.run.hitCombo=7;save.run.perfectStreak=3;s=Restore(save);
            s.Tick(s.EnemyIntent.SecondsToHit);Assert.AreEqual(0,s.HitCombo);Assert.AreEqual(0,s.PerfectStreak);
            save=Playing().ExportSave();save.run.hitCombo=7;save.run.enemyHpMilli=1000;s=Restore(save);s.Attack();s.Tick(.2f);s.Tick(10);
            Assert.AreEqual(2,s.Stage);Assert.AreEqual(8,s.HitCombo);Assert.AreEqual(0,s.PerfectStreak);
        }
        [Test] public void BossTenTalksBeforeDissolveAndRewardsRemainOnceAcrossReload()
        {
            var s=Playing(10);var save=s.ExportSave();save.run.enemyHpMilli=1000;s=Restore(save);long before=s.Gold;
            s.Attack();s.Tick(.2f);Assert.AreEqual(SessionPhase.StoryDialogue,s.Phase);Assert.AreEqual(0,s.EnemyHp);Assert.IsNull(s.Transition);
            Assert.AreEqual(before+750,s.Gold);s=Restore(s.ExportSave());Assert.AreEqual(before+750,s.Gold);
            s.SkipStory();Assert.AreEqual(TransitionKind.EnemyDefeat,s.Transition.Kind);s.Tick(5);Assert.AreEqual(SessionPhase.RewardChoice,s.Phase);
            s.ChooseRelic(s.RelicChoices[0]);Assert.AreEqual(11,s.Stage);Assert.AreEqual(s.PlayerMaxHp,s.PlayerHp);Assert.AreEqual(before+750,s.Gold);
        }
        [Test] public void TwentyCompletesAndOldCompletedTenSaveReturnsToLobby()
        {
            var s=Playing(20);var save=s.ExportSave();save.run.enemyHpMilli=1000;s=Restore(save);s.Attack();s.Tick(.2f);s.Tick(10);s.SkipStory();
            Assert.IsTrue(s.ChooseRelic(s.RelicChoices[0]));Assert.AreEqual(SessionPhase.MvpComplete,s.Phase);Assert.AreEqual(0,s.HitCombo);
            save=s.ExportSave();save.schemaVersion=5;save.run.stage=10;long gold=save.gold;s=Restore(save);
            Assert.AreEqual(SessionPhase.Lobby,s.Phase);Assert.AreEqual(gold,s.Gold);Assert.AreEqual(6,s.ExportSave().schemaVersion);
            Assert.IsTrue(s.StartRun());Assert.AreEqual(11,s.Stage);
        }
    }
}
