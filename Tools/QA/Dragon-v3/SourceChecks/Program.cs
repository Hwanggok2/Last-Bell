using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LastBell;

static class Program
{
    static GameCatalog catalog;
    static int checks;
    static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    static GameSession Battle(int stage,bool weak=false)
    {
        var save=new GameSession(catalog).ExportSave();save.phase=SessionPhase.Lobby;save.prologueSeen=save.comboHintSeen=true;save.tutorialStep=2;save.gold=12345;
        save.characters.Single(x=>x.id==catalog.defaultCharacterId).statLevels[1]=catalog.growth.maxLevel;
        var temp=new GameSession(catalog,save);temp.StartRun();save=temp.ExportSave();save.phase=SessionPhase.Combat;save.run.transition=null;
        save.run.stage=stage;save.run.patternIndex=save.run.patternStep=0;save.run.randomSequence=new string[0];save.run.sequenceCursor=0;
        save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;save.run.enemyHpMilli=weak?1000:save.run.enemyMaxHpMilli;
        var session=new GameSession(catalog,save);session.SetPaused(false);return session;
    }
    static void Main(string[] args)
    {
        catalog=JsonSerializer.Deserialize<GameCatalog>(File.ReadAllText(args[0]),new JsonSerializerOptions{IncludeFields=true});catalog.ValidateOrThrow();
        foreach(int stage in Enumerable.Range(11,10))
        {
            var hit=Battle(stage);float hp=hit.PlayerHp;hit.Tick((float)((hit.EnemyThreats.First().ImpactAtMs-hit.ClockMs)/1000)+.05f);
            Check(hit.PlayerHp>0&&hit.PlayerHp<hp,"forest Hit fixture survives actual stage damage "+stage);
            var s=Battle(stage);var save=s.ExportSave();save.run.stagger=catalog.combat.staggerThreshold;s=new GameSession(catalog,save);s.SetPaused(false);
            Check(s.Attack(),"Stun fixture attack "+stage);s.Tick(.2f);Check(s.IsEnemyStunned,"Stun fixture entered "+stage);
            s.Tick(catalog.GetStunDurationMs(catalog.GetStage(stage).kind)/1000f+.1f);Check(!s.IsEnemyStunned,"Stun fixture recovered "+stage);
        }
        foreach(int stage in new[]{15,20})
        {
            var s=Battle(stage);var save=s.ExportSave();save.phase=SessionPhase.Transition;
            save.run.enemyHitAtMs=save.run.attackHitAtMs=-1;
            save.run.transition=new TransitionData{kind=TransitionKind.BossIntro,fromStage=stage-1,toStage=stage,durationMs=stage==20?catalog.transitions.bossIntroMs:catalog.transitions.minibossIntroMs};
            s=new GameSession(catalog,save);s.SetPaused(false);Check(!s.Attack()&&!s.Dodge(DodgeDirection.Up),"boss intro blocks commands");
            s.Tick(save.run.transition.durationMs/1000f+.1f);Check(s.Phase==SessionPhase.Combat,"boss intro fixture ends");
        }
        var story=Battle(20,true);Check(story.Attack(),"story fixture kill");story.Tick(.2f);story.Tick(catalog.transitions.enemyDefeatMs/1000f);
        Check(story.Phase==SessionPhase.StoryDialogue,"story reached");long gold=story.Gold;int fragments=story.Fragments;var skipSave=story.ExportSave();
        while(story.Phase==SessionPhase.StoryDialogue)Check(story.AdvanceStory(),"story next");
        story.Tick(10);Check(story.Phase==SessionPhase.RunEnd&&story.Stage==20&&story.Gold==gold&&story.Fragments==fragments,"story boundary/reward stable");
        Check(story.ContinueSettlement()&&story.ContinueSettlement()&&story.Phase==SessionPhase.RewardChoice,"explicit settlement");
        var choice=story.RelicChoices[0];Check(story.ChooseRelic(choice),"relic choice");story.Tick(10);
        Check(story.Phase==SessionPhase.MvpComplete&&!story.ChooseRelic(choice)&&story.Stage==20,"MVP complete no duplicate");
        story=new GameSession(catalog,skipSave);story.SetPaused(false);Check(story.SkipStory(),"skip accepted");story.Tick(10);
        Check(story.Phase==SessionPhase.RunEnd&&story.PendingRewardCount==1&&story.Gold==gold&&story.Fragments==fragments,"skip boundary");
        Console.WriteLine("PASS "+checks+" V3 fixture source checks; no Unity, player or user save accessed.");
    }
}
