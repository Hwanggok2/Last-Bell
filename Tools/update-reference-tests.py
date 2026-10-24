from pathlib import Path
base=Path('Client/Assets/Game/Tests/EditMode/Core')
def method(s,name,body):
 start=s.index('{',s.index('void '+name+'('));end=start;depth=1
 while depth:
  end+=1
  if s[end]=='{':depth+=1
  if s[end]=='}':depth-=1
 return s[:start+1]+'\n'+body+'\n        '+s[end:]
p=base/'GameSessionTests.cs';s=p.read_text(encoding='utf-8')
s=s.replace('session.FinishPrologue();','session.FinishPrologue();session.StartRun();')
s=s.replace('            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RewardChoice));\n            return session;', '''            if(session.Phase==SessionPhase.Combat)session.ReturnToLobby();
            if(session.Phase==SessionPhase.RunEnd)session.ContinueSettlement();
            if(session.Phase==SessionPhase.RewardAnnouncement)session.ContinueSettlement();
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RewardChoice));
            return session;''')
s=s.replace('for (int i = 0; i < 8 && session.Phase == SessionPhase.Transition; i++) session.Tick(10);', '''for (int i = 0; i < 8 && session.Phase == SessionPhase.Transition; i++) session.Tick(10);
            if(session.Phase==SessionPhase.RunEnd&&session.PendingRewardCount==0)session.ContinueSettlement();''')
helper='''        private static void Settle(GameSession s)
        {
            while(s.Phase==SessionPhase.RunEnd||s.Phase==SessionPhase.RewardAnnouncement||s.Phase==SessionPhase.RewardChoice)
            {if(s.Phase==SessionPhase.RewardChoice)s.ChooseRelic(s.RelicChoices[0]);else s.ContinueSettlement();}
        }
        private static bool Home(GameSession s)
        {
            bool result=s.ReturnToLobby();
            if(s.Phase==SessionPhase.RunEnd){Settle(s);s.ReturnToLobby();}
            return result;
        }
'''
# Do not alter the newly added helper itself.
import re
s=re.sub(r'\b(session|s|restored|reward|committed|other|replay)\.ReturnToLobby\(\)',r'Home(\1)',s)
s=s.replace('        private static void EvadeNext',helper+'\n        private static void EvadeNext')
s=s.replace('[TestCase(4, DodgeDirection.Forward)]','[TestCase(4, DodgeDirection.Down)]').replace('session.Dodge(DodgeDirection.Forward)','session.Dodge(DodgeDirection.Back)')
s=s.replace('session.Tick(.159f);','session.Tick(.239f);').replace('session.Tick(.099f);','session.Tick(.119f);')
s=s.replace('Is.EqualTo(820)','Is.EqualTo(1000)')
s=s.replace('session.Fragments, Is.EqualTo(1)','session.Fragments, Is.EqualTo(0)').replace('fragments,Is.EqualTo(1)','fragments,Is.EqualTo(0)')
s=s.replace('Fragments, Is.EqualTo(3)','Fragments, Is.EqualTo(4)').replace('Fragments,Is.EqualTo(3)','Fragments,Is.EqualTo(4)')
s=s.replace('[TestCase(20,2550)]','[TestCase(20,1920)]')
s=method(s,'LargeInputFrameKeepsRemainderAndExposesEachImpactBeforeDamage','''            var session=PlayingAt(10);session.TickInputFrame(3);
            Assert.That(session.ClockMs,Is.EqualTo(1800));Assert.That(session.PlayerHp,Is.EqualTo(100));
            session.Dodge(session.EnemyIntent.Direction);session.TickInputFrame(0);
            Assert.That(session.ClockMs,Is.EqualTo(2550));Assert.That(session.IsEnemyImpactPending,Is.True);Assert.That(session.PlayerHp,Is.EqualTo(100));
            session.TickInputFrame(0);Assert.That(session.ClockMs,Is.EqualTo(3000));Assert.That(session.PlayerHp,Is.EqualTo(56));Assert.That(session.IsEnemyImpactPending,Is.False);''')
s=method(s,'EquippedHpGrowthAndBossFullHealApplyOnce','''            var lobby=LobbySave();lobby.relics.Add(new OwnedRelicData{id="relic_10",level=1});lobby.characters[0].equippedRelicIds=new[]{"relic_10"};
            var s=KillBoss(10,lobby);Assert.That(s.PlayerHp,Is.EqualTo(106));
            var save=s.ExportSave();save.run.offer.relicIds=new[]{"relic_10","relic_05","relic_15"};save.run.offer.offeredLevels=new[]{1,0,0};
            save.run.pendingRewards[0].relicIds=(string[])save.run.offer.relicIds.Clone();s=new GameSession(catalog,RoundTrip(save));
            Assert.That(s.ChooseRelic("relic_10"),Is.True);Assert.That(s.PlayerHp,Is.EqualTo(106));Assert.That(s.PlayerMaxHp,Is.EqualTo(106));
            s=new GameSession(catalog,RoundTrip(s.ExportSave()));Assert.That(s.ChooseRelic("relic_10"),Is.False);
            Assert.That(s.RetryRun(),Is.True);Assert.That(s.PlayerMaxHp,Is.EqualTo(108));Assert.That(s.PlayerHp,Is.EqualTo(108));''')
s=s.replace('Assert.That(session.PlayerHp,Is.EqualTo(session.PlayerMaxHp));Assert.That(session.CheckpointStartStage','Assert.That(session.PlayerHp,Is.EqualTo(30));Assert.That(session.CheckpointStartStage')
s=s.replace('s.SkipStory();FinishTransitions(s);s.ChooseRelic(s.RelicChoices[0]);FinishTransitions(s);','s.SkipStory();FinishTransitions(s);')
s=s.replace('s.Tick(s.EnemyIntent.SecondsToHit);FinishTransitions(s);\n            Assert.That(s.Phase', 's.Tick(s.EnemyIntent.SecondsToHit);FinishTransitions(s);Settle(s);\n            Assert.That(s.Phase')
s=s.replace('            session.Tick(.001f);\n            Assert.That(session.Phase, Is.EqualTo(SessionPhase.DeathResult));','            session.Tick(.001f);\n            Assert.That(session.Phase, Is.EqualTo(SessionPhase.RunEnd));session.ContinueSettlement();\n            Assert.That(session.Phase, Is.EqualTo(SessionPhase.DeathResult));')
s=method(s,'BossRewardIsFixedBeforeCollapseThenStoryResumesBeforeRelicChoice','''            var session=PlayingAt(10,weakEnemy:true);session.Attack();session.Tick(.2f);
            var choices=session.ExportSave().run.pendingRewards[0].relicIds;
            Assert.That(choices.Distinct().Count(),Is.EqualTo(3));Assert.That(session.RelicChoices,Is.Empty);
            Assert.That(session.Gold,Is.EqualTo(750));Assert.That(session.Fragments,Is.EqualTo(4));Assert.That(session.CheckpointStartStage,Is.EqualTo(11));
            session=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(session.Phase,Is.EqualTo(SessionPhase.StoryDialogue));
            session.AdvanceStory();session=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(session.Story.LineIndex,Is.EqualTo(1));
            CollectionAssert.AreEqual(choices,session.ExportSave().run.pendingRewards[0].relicIds);
            session.Tick(100);Assert.That(session.Story.LineIndex,Is.EqualTo(1));session.SkipStory();FinishTransitions(session);
            Assert.That(session.Stage,Is.EqualTo(11));Assert.That(session.Phase,Is.EqualTo(SessionPhase.Combat));Assert.That(session.ChooseRelic(choices[0]),Is.False);
            session.ReturnToLobby();session.ContinueSettlement();session.ContinueSettlement();
            CollectionAssert.AreEqual(choices,session.RelicChoices);Assert.That(session.ChooseRelic(choices[0]),Is.True);
            var committed=new GameSession(catalog,RoundTrip(session.ExportSave()));Assert.That(committed.ChooseRelic(choices[0]),Is.False);
            Assert.That(committed.GetRelicLevel(choices[0]),Is.EqualTo(1));Assert.That(committed.Gold,Is.EqualTo(750));Assert.That(committed.Fragments,Is.EqualTo(4));''')
# Story repeats now continue directly into the next fight; no immediate choice.
s=s.replace('Assert.That(replay.Phase, Is.EqualTo(SessionPhase.RewardChoice));','Assert.That(replay.Phase, Is.EqualTo(SessionPhase.Combat));Assert.That(replay.PendingRewardCount,Is.EqualTo(1));')
s=s.replace('Assert.That(other.Phase, Is.EqualTo(SessionPhase.RewardChoice));','Assert.That(other.Phase, Is.EqualTo(SessionPhase.Combat));Assert.That(other.PendingRewardCount,Is.EqualTo(1));')
# Real legacy saves had only offer, no settlement queue, and remained at the killed stage.
s=s.replace('old.schemaVersion = 1; old.characters', 'old.run.stage=10;old.run.stageRewardApplied=true;old.run.enemyHpMilli=0;old.run.ended=false;old.run.pendingRewards.Clear();\n            old.schemaVersion = 1; old.characters')
s=s.replace('old.schemaVersion = 1; old.run.stage = 5;', 'old.run.pendingRewards.Clear();old.run.ended=false;old.run.stageRewardApplied=true;old.run.enemyHpMilli=0;\n            old.schemaVersion = 1; old.run.stage = 5;')
s=s.replace('CollectionAssert.AreEqual(old.run.offer.relicIds, session.RelicChoices);','CollectionAssert.AreEqual(old.run.offer.relicIds, session.ExportSave().run.pendingRewards[0].relicIds);')
s=s.replace('Assert.That(session.Stage, Is.EqualTo(6));\n        }\n\n        [TestCase("negative")]', 'Assert.That(session.Stage, Is.EqualTo(5));\n        }\n\n        [TestCase("negative")]')
s=s.replace('old.schemaVersion=2;old.run.offer.maxGold=20;', 'old.schemaVersion=2;old.run.pendingRewards.Clear();old.run.offer.maxGold=20;')
# Same-deadline impacts are now both resolved; no enemy strike cancellation.
s=s.replace('Assert.That(session.EnemyHp, Is.EqualTo(1));\n            Assert.That(session.Gold, Is.Zero);','Assert.That(session.EnemyHp, Is.Zero);\n            Assert.That(session.Gold, Is.EqualTo(20));')
s=s.replace('Assert.That(session.EnemyHp, Is.EqualTo(120));\n            Assert.That(session.Gold, Is.Zero);','Assert.That(session.EnemyHp, Is.EqualTo(110));\n            Assert.That(session.Gold, Is.Zero);')
p.write_text(s,encoding='utf-8')
# Remaining fixture setup must explicitly start a run after selection.
p=base/'FeedbackV6Tests.cs';s=p.read_text(encoding='utf-8').replace('var save=new GameSession(catalog).ExportSave();save.prologueSeen', 'var initial=new GameSession(catalog);initial.FinishPrologue();initial.StartRun();var save=initial.ExportSave();save.prologueSeen')
s=s.replace('"outer_arc"','"high_sweep"')
s=s.replace('s.Tick(5);Assert.AreEqual(SessionPhase.RewardChoice,s.Phase);\n            s.ChooseRelic(s.RelicChoices[0]);','s.Tick(5);Assert.AreEqual(SessionPhase.Combat,s.Phase);Assert.AreEqual(1,s.PendingRewardCount);\n            ')
s=s.replace('            Assert.IsTrue(s.ChooseRelic(s.RelicChoices[0]));', '            s.ContinueSettlement();s.ContinueSettlement();Assert.IsTrue(s.ChooseRelic(s.RelicChoices[0]));')
p.write_text(s,encoding='utf-8')
p=base/'RouteOnboardingTests.cs';s=p.read_text(encoding='utf-8').replace('SessionPhase.TutorialDodge,restored.Phase','SessionPhase.Lobby,restored.Phase')
s=method(s,'ManualReadingAndSkipBothContinueExistingTutorial','''            var s=new GameSession(catalog);s.SetPrologueManual(true);s.Tick(18);Assert.AreEqual(SessionPhase.Lobby,s.Phase);
            Assert.IsTrue(s.StartRun());Assert.AreEqual(SessionPhase.TutorialDodge,s.Phase);
            s=new GameSession(catalog);Assert.IsTrue(s.FinishPrologue());Assert.AreEqual(SessionPhase.Lobby,s.Phase);
            Assert.IsTrue(s.StartRun());Assert.AreEqual(SessionPhase.TutorialDodge,s.Phase);''')
s=s.replace('s.FinishPrologue();var save=s.ExportSave();save.tutorialStep=2;','s.FinishPrologue();s.StartRun();var save=s.ExportSave();save.tutorialStep=2;')
s=s.replace('clock+1440','clock+1800').replace('s.ReturnToLobby();s.StartRun();','s.ReturnToLobby();s.ContinueSettlement();s.RetryRun();')
p.write_text(s,encoding='utf-8')
# Update v5 combat invariants without dropping their regression coverage.
p=base/'CombatV55Tests.cs';s=p.read_text(encoding='utf-8').replace('TestCase(1,600,900)','TestCase(1,780,900)').replace('TestCase(2,600,900)','TestCase(2,780,900)')
s=s.replace('s.Tick(.47f)','s.Tick(.65f)').replace('s.Tick(.501f)','s.Tick(1.201f)').replace('s.Tick(.21f)','s.Tick(.31f)')
s=s.replace('s.Tick(.6f);Assert.That(events.Last().Kind','s.Tick(.78f);Assert.That(events.Last().Kind').replace('Is.EqualTo(1320)','Is.EqualTo(1680)')
p.write_text(s,encoding='utf-8')
p=base/'CombatV59Tests.cs';s=p.read_text(encoding='utf-8').replace('s.Tick(.51f)','s.Tick(.69f)').replace('var s=Playing();s.Tick(.43f)','var s=Playing();s.Tick(.61f)')
s=s.replace(' [TestCase(DodgeDirection.Forward)]','').replace('s.Tick(1.39f)','s.Tick(1.75f)').replace('s.Tick(.43f);Assert.That(s.Dodge','s.Tick(.70f);Assert.That(s.Dodge')
s=s.replace('Is.EqualTo(beat*.5)','Is.EqualTo(catalog.combat.attackFlightMs)').replace('Is.EqualTo(2*beat)','Is.EqualTo(catalog.combat.comboDefaultGapMs)')
s=s.replace('Is.EqualTo(670)','Is.EqualTo(850)')
s=s.replace('s.Tick(.5f);Assert.That(s.Dodge','s.Tick(.6f);Assert.That(s.Dodge').replace('Is.EqualTo(.16f)','Is.EqualTo(.24f)').replace('s.Tick(.17f);Assert.That(s.Dodge','s.Tick(.25f);Assert.That(s.Dodge')
p.write_text(s,encoding='utf-8')
p=Path('Client/Assets/Game/Runtime/Bootstrap/ReferenceV81SmokeProbe.cs');s=p.read_text(encoding='utf-8').replace('Playing(5,new[]{"thrust","low_sweep"})','Playing(5,new[]{"low_sweep","high_sweep"})').replace('s.Dodge(DodgeDirection.Back);s.Tick(.05f);yield return Capture("10-perfect-wave")','s.Dodge(DodgeDirection.Up);s.Tick(.05f);yield return Capture("10-perfect-wave")');p.write_text(s,encoding='utf-8')
