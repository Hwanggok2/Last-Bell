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

        [Test] public void VersionOneStillLoadsForCoreMigration()
        {
            repository.Save(new GameSaveData{schemaVersion=1,gold=44,phase=SessionPhase.RewardChoice});
            var restored=repository.Load(out _);
            Assert.AreEqual(1,restored.schemaVersion);Assert.AreEqual(44,restored.gold);
        }

        [Test] public void VersionTwoTransitionAndStorySurviveJsonUtility()
        {
            var original=new GameSaveData{schemaVersion=2,phase=SessionPhase.Transition,run=new RunStateData
            {
                transition=new TransitionData{kind=TransitionKind.EnemyDefeat,fromStage=10,toStage=11,elapsedMs=321.5,durationMs=550},
                story=new StoryStateData{sceneId="story_boss_10",lineIndex=1,rewardId="1:0:10"}
            }};
            original.unlockedStoryIds.Add("story_boss_10");
            original.characters.Add(new CharacterProgressData{id="on",seenBossIntros=new System.Collections.Generic.List<string>{"0:bell_knight"}});
            repository.Save(original);var restored=repository.Load(out _);
            Assert.AreEqual(2,restored.schemaVersion);
            Assert.AreEqual(SessionPhase.Transition,restored.phase);
            Assert.AreEqual(TransitionKind.EnemyDefeat,restored.run.transition.kind);
            Assert.AreEqual(321.5,restored.run.transition.elapsedMs);
            Assert.AreEqual("story_boss_10",restored.run.story.sceneId);
            Assert.AreEqual(1,restored.run.story.lineIndex);
            CollectionAssert.AreEqual(original.unlockedStoryIds,restored.unlockedStoryIds);
            CollectionAssert.AreEqual(original.characters[0].seenBossIntros,restored.characters[0].seenBossIntros);
            original.phase=SessionPhase.StoryDialogue;original.run.transition=null;
            repository.Save(original);restored=repository.Load(out _);
            Assert.AreEqual(SessionPhase.StoryDialogue,restored.phase);Assert.IsNull(restored.run.transition);
            Assert.AreEqual("1:0:10",restored.run.story.rewardId);
        }

        [Test] public void InlineNullRecordsAreCanonicalizedInMemoryWithoutRewritingTheEnvelope()
        {
            repository.Save(new GameSaveData{phase=SessionPhase.Combat,run=new RunStateData{stage=1}});
            string bytesBefore=File.ReadAllText(repository.FilePath);
            var restored=repository.Load(out bool recovered);
            Assert.IsNull(restored.run.offer);Assert.IsNull(restored.run.story);Assert.IsNull(restored.run.transition);
            Assert.IsFalse(recovered);Assert.AreEqual(bytesBefore,File.ReadAllText(repository.FilePath));
        }

        [Test] public void PartiallyPopulatedInlineRecordsRemainAvailableForStrictCoreValidation()
        {
            repository.Save(new GameSaveData{run=new RunStateData
            {
                offer=new RelicOfferData{maxGold=1},story=new StoryStateData{rewardId="partial"},
                transition=new TransitionData{durationMs=1}
            }});
            string bytesBefore=File.ReadAllText(repository.FilePath);
            var restored=repository.Load(out _);
            Assert.AreEqual(1,restored.run.offer.maxGold);
            Assert.AreEqual("partial",restored.run.story.rewardId);
            Assert.AreEqual(1,restored.run.transition.durationMs);
            Assert.AreEqual(bytesBefore,File.ReadAllText(repository.FilePath));
        }
    }
}
