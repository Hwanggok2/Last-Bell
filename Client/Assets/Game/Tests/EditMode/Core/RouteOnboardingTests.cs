using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LastBell.Tests
{
    public sealed class RouteOnboardingTests
    {
        GameCatalog catalog;
        [SetUp] public void Setup(){catalog=JsonUtility.FromJson<GameCatalog>(File.ReadAllText(Path.Combine(Application.dataPath,"Game/Data/Generated/mvp-catalog.json")));}
        [Test] public void FirstPrologueFreezesCombatAndPersistsOnlyOnCompletion()
        {
            var s=new GameSession(catalog);Assert.AreEqual(SessionPhase.Prologue,s.Phase);
            double clock=s.ClockMs;float hp=s.PlayerHp;
            s.Tick(10);Assert.AreEqual(clock,s.ClockMs);Assert.AreEqual(hp,s.PlayerHp);Assert.IsFalse(s.Attack());Assert.IsFalse(s.Dodge(DodgeDirection.Up));
            var restored=new GameSession(catalog,s.ExportSave());Assert.AreEqual(0,restored.PrologueProgress);Assert.IsFalse(restored.ExportSave().prologueSeen);
            restored.ToggleProloguePause();restored.Tick(100);Assert.AreEqual(SessionPhase.Prologue,restored.Phase);
            restored.ToggleProloguePause();restored.Tick(28);Assert.AreEqual(SessionPhase.Lobby,restored.Phase);Assert.IsTrue(restored.ExportSave().prologueSeen);
            Assert.AreEqual(clock,restored.ClockMs);
        }
        [Test] public void ReducedMotionAndSkipReturnToSelectionBeforeTutorial()
        {
            var s=new GameSession(catalog);s.SetPrologueManual(true);s.Tick(18);Assert.AreEqual(SessionPhase.Lobby,s.Phase);
            Assert.IsTrue(s.StartRun());Assert.AreEqual(SessionPhase.TutorialDodge,s.Phase);
            s=new GameSession(catalog);Assert.IsTrue(s.FinishPrologue());Assert.AreEqual(SessionPhase.Lobby,s.Phase);
            Assert.IsTrue(s.StartRun());Assert.AreEqual(SessionPhase.TutorialDodge,s.Phase);
        }
        GameSession AtFive()
        {
            var s=new GameSession(catalog);s.FinishPrologue();s.StartRun();var save=s.ExportSave();save.tutorialStep=2;
            save.comboHintSeen=save.comboPracticeCompleted=save.comboPracticeSkipped=false;
            save.run.stage=5;save.run.enemyHpMilli=save.run.enemyMaxHpMilli=catalog.GetStage(5).maxHp*1000L;
            save.phase=SessionPhase.Transition;save.run.transition=new TransitionData{kind=TransitionKind.Arrival,fromStage=4,toStage=5,durationMs=700};
            s=new GameSession(catalog,save);s.Tick(10);return s;
        }
        [Test] public void FiveRequiresBothGuidedInputsBeforeRealTrialAndDoesNotRepeat()
        {
            var s=AtFive();Assert.AreEqual(SessionPhase.ComboHint,s.Phase);double clock=s.ClockMs;float hp=s.PlayerHp;
            s.Tick(60);Assert.AreEqual(clock,s.ClockMs);Assert.AreEqual(hp,s.PlayerHp);Assert.IsEmpty(s.EnemyThreats);
            Assert.IsFalse(s.Attack());Assert.IsFalse(s.Dodge(DodgeDirection.Up));
            s=new GameSession(catalog,s.ExportSave());Assert.AreEqual(SessionPhase.ComboHint,s.Phase);Assert.IsFalse(s.ExportSave().comboHintSeen);
            Assert.IsTrue(s.ConfirmComboHint());Assert.AreEqual(SessionPhase.Combat,s.Phase);Assert.AreEqual(2,s.EnemyThreats.Length);
            Assert.AreEqual(clock,s.EnemyThreats[0].CueAtMs);Assert.AreEqual(clock+1800,s.EnemyThreats[0].ImpactAtMs);
            Assert.IsTrue(s.ExportSave().comboHintSeen);Assert.IsFalse(s.ConfirmComboHint());
            Assert.IsFalse(s.ExportSave().comboPracticeCompleted);Assert.IsFalse(s.Dodge(DodgeDirection.Up));Assert.IsFalse(s.Attack());
            s.Tick(60);Assert.AreEqual(clock+1800,s.ClockMs);Assert.IsTrue(s.ComboPractice.AwaitingInput);
            Assert.AreEqual(DodgeDirection.Up,s.ComboPractice.RequiredDirection);Assert.IsFalse(s.Dodge(DodgeDirection.Down));
            s.Tick(60);Assert.AreEqual(clock+1800,s.ClockMs);Assert.AreEqual(hp,s.PlayerHp);
            Assert.IsTrue(s.Dodge(DodgeDirection.Up));Assert.IsFalse(s.ExportSave().comboPracticeCompleted);
            s.Tick(.75f);Assert.AreEqual(clock+2550,s.ClockMs);Assert.IsTrue(s.ComboPractice.AwaitingInput);
            Assert.AreEqual(DodgeDirection.Down,s.ComboPractice.RequiredDirection);Assert.IsFalse(s.Dodge(DodgeDirection.Up));
            Assert.IsTrue(s.Dodge(DodgeDirection.Down));Assert.IsTrue(s.ExportSave().comboPracticeCompleted);
            Assert.AreEqual(ComboPracticeMode.Recovery,s.ExportSave().run.comboPractice);Assert.AreEqual(hp,s.PlayerHp);
            s.Tick(.36f);Assert.IsTrue(s.ComboPractice.IsRealTrial);Assert.IsFalse(s.ComboPractice.Active);
            Assert.AreEqual(clock+2910,s.ClockMs,.001);Assert.AreEqual(2,s.EnemyThreats.Length);
            s.ReturnToLobby();s.ContinueSettlement();s.RetryRun();Assert.AreEqual(1,s.Stage);Assert.AreNotEqual(SessionPhase.Prologue,s.Phase);
            var save=s.ExportSave();save.phase=SessionPhase.Transition;save.run.stage=5;save.run.transition=new TransitionData{kind=TransitionKind.Arrival,fromStage=4,toStage=5,durationMs=700};
            s=new GameSession(catalog,save);s.Tick(10);Assert.AreEqual(SessionPhase.Combat,s.Phase);
        }
        [Test] public void VersionFourKeepsWalletAndDoesNotInterruptExistingPlayers()
        {
            var s=new GameSession(catalog);s.FinishPrologue();var save=s.ExportSave();save.schemaVersion=4;save.prologueSeen=false;save.gold=3000000000L;
            foreach(var character in save.characters)System.Array.Resize(ref character.statLevels,4);
            s=new GameSession(catalog,save);Assert.AreEqual(9,s.ExportSave().schemaVersion);Assert.AreEqual(3000000000L,s.Gold);Assert.IsTrue(s.ExportSave().prologueSeen);
            Assert.AreEqual(20,s.Catalog.mvpLastStage);Assert.AreEqual(1,s.CheckpointStartStage);
        }
        [Test] public void ReplayCanRestoreAndReturnWithoutStartingAnotherRun()
        {
            var s=new GameSession(catalog);s.FinishPrologue();s.Dodge(DodgeDirection.Up);s.Attack();s.Tick(.2f);s.ReturnToLobby();
            var before=s.ExportSave();Assert.IsTrue(s.ReplayPrologue());
            s=new GameSession(catalog,s.ExportSave());Assert.AreEqual(SessionPhase.Prologue,s.Phase);s.FinishPrologue();
            Assert.AreEqual(SessionPhase.Lobby,s.Phase);Assert.AreEqual(before.runSequence,s.ExportSave().runSequence);Assert.AreEqual(before.gold,s.Gold);
        }
    }
}
