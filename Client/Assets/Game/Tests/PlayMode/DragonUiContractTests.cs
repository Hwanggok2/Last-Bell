#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
namespace LastBell.Tests
{
    public sealed class DragonUiContractTests
    {
        [TestCase(0f,0,0,0)]
        [TestCase(.000001f,1,1,1)]
        [TestCase(.25f,7,8,13)]
        [TestCase(.5f,13,16,25)]
        [TestCase(1f,26,31,50)]
        public void HealthSkinCannotShowALivingEnemyAsEmpty(float hpFraction,int playerCells,int enemyCells,int bossCells)
        {
            Assert.That(HealthBarGraphic.FilledCells(hpFraction,26),Is.EqualTo(playerCells));
            Assert.That(HealthBarGraphic.FilledCells(hpFraction,31),Is.EqualTo(enemyCells));
            Assert.That(HealthBarGraphic.FilledCells(hpFraction,50),Is.EqualTo(bossCells));
        }
        [TestCase(999L,3000L,false)]
        [TestCase(1000L,3000L,true)]
        [TestCase(1001L,3000L,true)]
        [TestCase(1000L,3001L,false)]
        [TestCase(1001L,3001L,true)]
        [TestCase(0L,3000L,false)]
        [TestCase(long.MaxValue,long.MaxValue,true)]
        public void HeavyHitThresholdUsesSingleActualDamage(long damage,long maxHp,bool expected)
            =>Assert.That(HealthBarGraphic.IsHeavyDamage(damage,maxHp),Is.EqualTo(expected));

        [Test]
        public void HeavyHitImmediatelyChangesHealthThenLinearlyDrainsTrail()
        {
            var go=new GameObject("HP trail");var bar=go.AddComponent<HealthBarGraphic>();
            try
            {
                bar.ResetValue(1);bar.ApplyDamage(.5f,1500,3000);
                Assert.That(bar.Value,Is.EqualTo(.5f));Assert.That(bar.TrailValue,Is.EqualTo(1));
                bar.Sample(HealthBarGraphic.TrailHoldSeconds);Assert.That(bar.TrailValue,Is.EqualTo(1));
                bar.Sample(HealthBarGraphic.TrailLerpSeconds*.5f);Assert.That(bar.TrailValue,Is.EqualTo(.75f).Within(.00001));
                bar.Sample(HealthBarGraphic.TrailLerpSeconds*.5f);Assert.That(bar.TrailValue,Is.EqualTo(.5f));Assert.That(bar.TrailActive,Is.False);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test]
        public void SmallHitsDoNotAggregateIntoHeavyHitAndCannotDelayActiveTrail()
        {
            var go=new GameObject("HP trail");var bar=go.AddComponent<HealthBarGraphic>();
            try
            {
                bar.ResetValue(1);bar.ApplyDamage(.8f,600,3000);bar.ApplyDamage(.6f,600,3000);
                Assert.That(bar.TrailActive,Is.False);Assert.That(bar.TrailValue,Is.EqualTo(.6f));
                bar.ResetValue(1);bar.ApplyDamage(.5f,1500,3000);bar.Sample(.12f+.275f);
                bar.ApplyDamage(.4f,300,3000);Assert.That(bar.TrailValue,Is.EqualTo(.75f).Within(.00001));
                bar.Sample(.275f);Assert.That(bar.TrailActive,Is.False);Assert.That(bar.TrailValue,Is.EqualTo(.4f));
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test]
        public void HealAndNewTargetClearTrailWhileLargeFollowupStartsFromVisibleTrail()
        {
            var go=new GameObject("HP trail");var bar=go.AddComponent<HealthBarGraphic>();
            try
            {
                bar.ResetValue(1);bar.ApplyDamage(.6f,1200,3000);bar.Sample(.395f);
                float visible=bar.TrailValue;bar.ApplyDamage(.2f,1200,3000);
                Assert.That(bar.Value,Is.EqualTo(.2f));Assert.That(bar.TrailValue,Is.EqualTo(visible));
                bar.SetValue(.3f);Assert.That(bar.TrailActive,Is.False);Assert.That(bar.TrailValue,Is.EqualTo(.3f));
                bar.ResetValue(1);bar.ApplyDamage(0,3000,3000);Assert.That(bar.Value,Is.Zero);Assert.That(bar.TrailActive,Is.True);
                bar.ResetValue(1);Assert.That(bar.TrailActive,Is.False);Assert.That(bar.TrailValue,Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
#endif
