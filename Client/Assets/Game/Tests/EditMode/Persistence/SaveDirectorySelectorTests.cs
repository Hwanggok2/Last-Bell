using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LastBell.Tests
{
    public sealed class SaveDirectorySelectorTests
    {
        string directory;
        GameCatalog catalog;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"lastbell-profile-test-"+Guid.NewGuid().ToString("N"));
            catalog=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(Application.dataPath,"Game/Data/Generated/mvp-catalog.json")));
        }
        [TearDown] public void TearDown(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        GameSaveData CheckpointSave()
        {
            var session=new GameSession(catalog);session.FinishPrologue();var save=session.ExportSave();
            var character=save.characters.Find(x=>x.id==save.selectedCharacterId);
            character.checkpoint=10;character.highestStage=10;save.highestClearedStage=10;save.tutorialStep=2;
            save.gold=12345;return save;
        }
        [Test] public void ProductionKeepsExistingNamespaceAndSelectionCreatesNothing()
        {
            Assert.AreEqual(directory,SaveDirectorySelector.Select(directory,null,false));
            Assert.AreEqual(Path.Combine(directory,"Playtests","dragon-0.9.2"),SaveDirectorySelector.Select(directory,null,true));
            Assert.IsFalse(Directory.Exists(directory));
        }
        [TestCase(false)] [TestCase(true)]
        public void ExplicitDirectoryHasPriority(bool playtest)
        {
            string custom=Path.Combine(directory,"explicit-profile");
            Assert.AreEqual(Path.GetFullPath(custom),SaveDirectorySelector.Select(directory,new[]{"game.exe","-lastbell-save-dir",custom},playtest));
            Assert.IsFalse(Directory.Exists(directory));
        }
        [Test] public void FirstPlaytestStartsStageOneWithoutChangingLegacyPrimaryOrBackup()
        {
            var legacy=new JsonSaveRepository(directory);var saved=CheckpointSave();legacy.Save(saved);saved.gold++;legacy.Save(saved);
            string primary=File.ReadAllText(legacy.FilePath),backup=File.ReadAllText(legacy.FilePath+".bak");
            var playtest=new JsonSaveRepository(SaveDirectorySelector.Select(directory,null,true));
            var fresh=new GameSession(catalog,playtest.Load(out bool recovered));
            Assert.IsFalse(recovered);Assert.AreEqual(SessionPhase.Prologue,fresh.Phase);
            fresh.FinishPrologue();Assert.IsTrue(fresh.StartRun());Assert.AreEqual(1,fresh.Stage);
            playtest.Save(fresh.ExportSave());
            Assert.AreEqual(primary,File.ReadAllText(legacy.FilePath));Assert.AreEqual(backup,File.ReadAllText(legacy.FilePath+".bak"));
        }
        [Test] public void ReopeningTheSamePlaytestPreservesItsCheckpointAndWallet()
        {
            string path=SaveDirectorySelector.Select(directory,null,true);
            new JsonSaveRepository(path).Save(CheckpointSave());
            var restored=new GameSession(catalog,new JsonSaveRepository(SaveDirectorySelector.Select(directory,null,true)).Load(out _));
            Assert.AreEqual(12345,restored.Gold);Assert.AreEqual(11,restored.CheckpointStartStage);
            Assert.IsTrue(restored.StartRun());Assert.AreEqual(11,restored.Stage);
        }
    }
}
