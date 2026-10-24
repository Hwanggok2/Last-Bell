using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LastBell.Tests
{
    public sealed class BalanceV7Tests
    {
        GameCatalog catalog;
        [SetUp] public void Setup(){catalog=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(Application.dataPath,"Game/Data/Generated/mvp-catalog.json")));}
        GameSession Hero(int stars,int level)
        {
            catalog.GetCharacter("on").stars=new[]{stars,stars,stars,stars,stars};
            var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.run=null;save.prologueSeen=true;save.tutorialStep=2;
            save.characters[0].statLevels=new[]{level,level,level,level,level};save.gold=20000000000L;
            return new GameSession(catalog,save);
        }
        [Test] public void CostsJumpOnElevenAndAccelerateInsideEachTier()
        {
            int[] checkpoints={11,21,31,41};decimal[] jumps={3m,3.5m,4m,4.5m};
            for(int i=0;i<4;i++)Assert.AreEqual((long)decimal.Ceiling(catalog.GetUpgradeCost(checkpoints[i]-1)*jumps[i]),catalog.GetUpgradeCost(checkpoints[i]));
            for(int l=12;l<=50;l++)if(l%10!=1)
            {decimal rate=1m+(25+(l-1)/10*5)/100m;Assert.AreEqual((long)decimal.Ceiling(catalog.GetUpgradeCost(l-1)*rate),catalog.GetUpgradeCost(l));}
        }
        [Test] public void EveryStarAndEveryUpgradeHasAWholeObservableGain()
        {
            for(int star=1;star<=5;star++)for(int l=0;l<50;l++)
            {
                var before=Hero(star,l).PlayerStats;var after=Hero(star,l+1).PlayerStats;
                Assert.Greater(after.attack,before.attack);Assert.Greater(after.maxHp,before.maxHp);
                Assert.Greater(after.goldBonusPercent,before.goldBonusPercent,"luck must improve at "+star+" stars / "+l);
                Assert.Greater(after.stunDamagePercent,before.stunDamagePercent);
            }
            for(int star=2;star<=5;star++)
            {var low=Hero(star-1,20).PlayerStats;var high=Hero(star,20).PlayerStats;Assert.Greater(high.attack,low.attack);Assert.Greater(high.maxHp,low.maxHp);Assert.Greater(high.critChancePercent,low.critChancePercent);}
        }
        [Test] public void UpgradedHeroStillNeedsMoreHitsAndHasFewerMistakesLate()
        {
            var early=Hero(3,10).PlayerStats;var late=Hero(3,20).PlayerStats;
            Assert.AreEqual(50,early.attack);Assert.AreEqual(340,early.maxHp);Assert.AreEqual(216,late.attack);Assert.AreEqual(1044,late.maxHp);
            Assert.Greater(catalog.GetStage(11).maxHp,catalog.GetStage(10).maxHp);
            Assert.Greater(catalog.GetStage(21).maxHp,catalog.GetStage(20).maxHp);
            Assert.AreEqual(18,Math.Ceiling(catalog.GetStage(10).maxHp/(double)early.attack));
            Assert.AreEqual(41,Math.Ceiling(catalog.GetStage(20).maxHp/(double)late.attack));
            Assert.AreEqual(2,Math.Ceiling(late.maxHp/(double)catalog.GetStage(20).damage));
        }
        [Test] public void LargeTotalUpgradeCostDoesNotOverflowAndDebitsOnce()
        {
            var s=Hero(3,0);long paid=0;
            for(int level=1;level<=50;level++)for(int stat=0;stat<5;stat++){paid+=s.GetUpgradeCost(stat);Assert.IsTrue(s.Upgrade(stat));}
            // Recovery costs half the other stats, rounded up separately at each level.
            Assert.AreEqual(18805244702L,paid);Assert.AreEqual(20000000000L-paid,s.Gold);
            for(int stat=0;stat<5;stat++){Assert.AreEqual(50,s.GetStatLevel(stat));Assert.IsFalse(s.Upgrade(stat));}
        }
    }
}
