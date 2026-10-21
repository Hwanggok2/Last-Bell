using System;
using System.IO;
using NUnit.Framework;

namespace LastBell.Tests
{
    public sealed class JsonSaveRepositoryTests
    {
        string directory;
        JsonSaveRepository repository;
        [SetUp] public void SetUp()
        {
            directory=Path.Combine(Path.GetTempPath(),"lastbell-save-test-"+Guid.NewGuid().ToString("N"));
            repository=new JsonSaveRepository(directory);
        }
        [TearDown] public void TearDown(){if(Directory.Exists(directory))Directory.Delete(directory,true);}

        [Test] public void FreshProfileHasNoSave()
        {Assert.IsNull(repository.Load(out bool recovered));Assert.IsFalse(recovered);}

        [Test] public void RoundtripKeepsRewardOfferAndWallet()
        {
            var original=new GameSaveData{gold=123,fragments=4,phase=SessionPhase.RewardChoice,
                run=new RunStateData{stage=10,offer=new RelicOfferData{rewardId="1:0:10",stage=10,relicIds=new[]{"a","b","c"},offeredLevels=new[]{1,2,0}}}};
            repository.Save(original);var restored=repository.Load(out bool recovered);
            Assert.AreEqual(123,restored.gold);Assert.AreEqual(4,restored.fragments);
            Assert.AreEqual("1:0:10",restored.run.offer.rewardId);
            CollectionAssert.AreEqual(original.run.offer.relicIds,restored.run.offer.relicIds);
            Assert.IsFalse(recovered);Assert.IsFalse(File.Exists(repository.FilePath+".tmp"));
        }

        [Test] public void InterruptedTemporaryWriteDoesNotReplacePrimary()
        {
            repository.Save(new GameSaveData{gold=21});
            File.WriteAllText(repository.FilePath+".tmp","{unfinished");
            Assert.AreEqual(21,repository.Load(out _).gold);
        }

        [Test] public void CorruptPrimaryUsesVerifiedPreviousSave()
        {
            repository.Save(new GameSaveData{gold=21});repository.Save(new GameSaveData{gold=32});
            File.WriteAllText(repository.FilePath,"{broken");
            Assert.AreEqual(21,repository.Load(out bool recovered).gold);Assert.IsTrue(recovered);
            Assert.AreEqual("{broken",File.ReadAllText(repository.FilePath));
        }

        [Test] public void InvalidSaveIsPreservedWhenBothCopiesFail()
        {
            File.WriteAllText(repository.FilePath,"broken primary");File.WriteAllText(repository.FilePath+".bak","broken backup");
            Assert.Throws<InvalidDataException>(()=>repository.Load(out _));
            Assert.AreEqual("broken primary",File.ReadAllText(repository.FilePath));
            Assert.AreEqual("broken backup",File.ReadAllText(repository.FilePath+".bak"));
        }

        [Test] public void UnsupportedVersionCannotOverwriteExistingSave()
        {
            repository.Save(new GameSaveData{gold=21});string before=File.ReadAllText(repository.FilePath);
            Assert.Throws<InvalidDataException>(()=>repository.Save(new GameSaveData{schemaVersion=99}));
            Assert.AreEqual(before,File.ReadAllText(repository.FilePath));
        }
    }
}
