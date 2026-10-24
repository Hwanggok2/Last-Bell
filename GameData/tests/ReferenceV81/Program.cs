using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LastBell;
class Program
{
    static JsonSerializerOptions options=new JsonSerializerOptions{IncludeFields=true};
    static GameCatalog catalog;static int n;
    static void Check(bool ok,string name){n++;if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
    static GameSaveData Round(GameSaveData s)=>JsonSerializer.Deserialize<GameSaveData>(JsonSerializer.Serialize(s,options),options);
    static GameSaveData Lobby(){var s=new GameSession(catalog).ExportSave();s.phase=SessionPhase.Lobby;s.prologueSeen=s.comboHintSeen=true;s.tutorialStep=2;s.run=null;return s;}
    static GameSession Playing(int stage=1,string[] sequence=null,bool weak=false)
    {
        var save=Lobby();save.characters[0].statLevels=new[]{10,50,0,0};var s=new GameSession(catalog,save);s.StartRun();save=s.ExportSave();save.run.transition=null;save.phase=SessionPhase.Combat;
        save.run.stage=stage;save.run.enemyHpMilli=weak?1000:catalog.GetStage(stage).maxHp*1000L;save.run.enemyMaxHpMilli=catalog.GetStage(stage).maxHp*1000L;
        save.run.randomSequence=sequence??new string[0];save.run.sequenceCursor=0;save.run.immunityUntilMs=1000000;
        s=new GameSession(catalog,Round(save));s.SetPaused(false);return s;
    }
    static GameSession Reload(GameSession s){var next=new GameSession(catalog,Round(s.ExportSave()));next.SetPaused(false);return next;}
    static void Settle(GameSession s)
    {
        for(int i=0;i<50;i++)
        {
            if(s.Phase==SessionPhase.RunEnd||s.Phase==SessionPhase.RewardAnnouncement)s.ContinueSettlement();
            else if(s.Phase==SessionPhase.RewardChoice)s.ChooseRelic(s.RelicChoices[0]);
            else return;
        }
        throw new Exception("settlement stuck");
    }
    static void Main()
    {
        catalog=JsonSerializer.Deserialize<GameCatalog>(File.ReadAllText("GameData/mvp-catalog.json"),options);catalog.ValidateOrThrow();
        var fresh=new GameSession(catalog);Check(fresh.Phase==SessionPhase.Prologue&&fresh.ExportSave().run==null,"first launch prologue has no active combat run");fresh.Tick(18);
        Check(fresh.Phase==SessionPhase.Lobby&&fresh.ExportSave().prologueSeen,"auto prologue finishes in selection lobby");
        var save=Lobby();save.gold=12345;save.characters[0].statLevels[0]=10;var s=new GameSession(catalog,save);var q=s.QuoteUpgrade(0,true);
        Check(q.ToLevel==20&&q.Cost==9633,"MAX counts each sequential upgrade and stops before breakthrough");
        Check(s.PurchaseUpgrade(q)&&s.Gold==2712&&s.GetStatLevel(0)==20,"MAX pays and applies exact quote atomically");
        Check(!s.PurchaseUpgrade(q)&&s.GetStatLevel(0)==20,"double click cannot reuse quote");
        q=s.QuoteUpgrade(0,false);Check(q.Cost==8397&&!s.PurchaseUpgrade(q)&&s.Fragments==0,"insufficient gold does not spend fragments");
        for(int count=1;count<=3;count++)
        {
            s=Playing(count==1?1:20,Enumerable.Repeat("low_sweep",count).ToArray());var ts=s.EnemyThreats;
            Check(ts.Length==count&&ts[0].ImpactAtMs==count*900,"all warnings precede "+count+" attacks");
            for(int i=0;i<count;i++)Check(ts[i].CueAtMs==i*900&&ts[i].BeatMs==300&&ts[i].ImpactAtMs==count*900+i*750&&ts[i].WaveStartMs==ts[i].ImpactAtMs-750,"cue/wave/contact "+count+":"+i);
        }
        foreach(int lead in new[]{0,90,91,240,241})
        {
            s=Playing(3,new[]{"high_sweep"});float hp=s.PlayerHp;s.TickInputFrame((900-lead)/1000f);s.Dodge(DodgeDirection.Down);s.TickInputFrame(lead/1000f);s.TickInputFrame(0);
            Check((s.PlayerHp==hp)==(lead<=240),"Down parry active boundary "+lead);
            Check(s.CanPerfectCounter==(lead<=90),"perfect boundary "+lead);
        }
        s=Playing(1,new[]{"low_sweep"});Check(!s.Dodge(DodgeDirection.Forward),"retired forward command rejected");
        s.Tick(.85f);s.Dodge(DodgeDirection.Up);s.Tick(.05f);Check(s.CanPerfectCounter,"perfect dodge gives counter");
        s.Tick(1.19f);Check(s.CanPerfectCounter&&s.Attack()&&!s.CanPerfectCounter,"counter lasts 1200ms and consumes once");
        foreach(int stage in new[]{1,5,10})
        {
            s=Playing(stage,new[]{catalog.GetStage(stage).randomPatterns[0]});s.Tick(.9f);Console.WriteLine($"REST {stage} clock={s.ClockMs} cue={s.EnemyThreats[0].CueAtMs} expected={900+catalog.GetStage(stage).patternSteps[0].intervalMs} HP={s.PlayerHp}");
            Check(Math.Abs(s.EnemyThreats[0].CueAtMs-(900+catalog.GetStage(stage).patternSteps[0].intervalMs))<.001,"rest starts at last actual contact stage "+stage);
        }
        s=Playing(5,null,true);s.Attack();s.Tick(.2f);Check(s.Fragments==0&&s.PendingRewardCount==0,"miniboss no fragments or relics");
        s=Playing(10,null,true);float old=s.PlayerHp;s.Attack();s.Tick(.2f);
        Check(s.Fragments==4&&s.PendingRewardCount==1&&s.CheckpointStartStage==11,"boss pays four fragments and queues reward immediately");
        var promised=s.ExportSave().run.pendingRewards[0].relicIds.ToArray();s=Reload(s);if(s.Phase==SessionPhase.StoryDialogue)s.SkipStory();s.Tick(10);
        Check(s.Stage==11&&s.Phase==SessionPhase.Combat&&s.PendingRewardCount==1&&s.PlayerHp==s.PlayerMaxHp,"boss heals and travels without opening reward panel");
        s.ReturnToLobby();Check(s.Phase==SessionPhase.RunEnd&&!s.StartRun(),"manual end settles before allowing a new run");s.ContinueSettlement();
        Check(s.Phase==SessionPhase.RewardAnnouncement&&s.RewardStage==10,"stage clear announcement before relic choice");s.ContinueSettlement();s=Reload(s);
        Check(s.RelicChoices.SequenceEqual(promised),"save reload retains fixed candidates");var id=s.RelicChoices[0];long gold=s.Gold;Check(s.ChooseRelic(id)&&s.GetRelicLevel(id)==1&&!s.ChooseRelic(id)&&s.Gold==gold,"reward confirmed exactly once");
        Check(s.RetryRun()&&s.Stage==11&&s.PendingRewardCount==0,"retry checkpoint 11 has a fresh reward ledger");
        // Full run to 20, two queued offers, each level quote uses current ownership.
        save=Lobby();save.characters[0].statLevels=new[]{50,50,0,0};s=new GameSession(catalog,save);s.StartRun();
        for(int i=0;i<500&&s.Phase!=SessionPhase.RunEnd;i++)
        {
            if(s.Phase==SessionPhase.Transition)s.Tick(10);
            else if(s.Phase==SessionPhase.StoryDialogue)s.SkipStory();
            else if(s.Phase==SessionPhase.ComboHint)s.ConfirmComboHint();
            else if(s.Phase==SessionPhase.Combat){s.Attack();s.Tick(.5f);}
            else throw new Exception("unexpected run phase "+s.Phase);
        }
        Check(s.Phase==SessionPhase.RunEnd&&s.Result.Cleared&&s.PendingRewardCount==2&&s.Fragments==8,"complete 1..20 queues only boss10/20 rewards");
        save=s.ExportSave();var candidate=save.run.pendingRewards[0].relicIds[0];save.run.pendingRewards[1].relicIds=(string[])save.run.pendingRewards[0].relicIds.Clone();
        save.relics.Add(new OwnedRelicData{id=candidate,level=2});s=new GameSession(catalog,Round(save));
        s.ContinueSettlement();s.ContinueSettlement();Check(s.ChooseRelic(candidate)&&s.GetRelicLevel(candidate)==3,"first queued relic goes 2 to 3");s=Reload(s);s.ContinueSettlement();
        Check(s.ChooseRelic(candidate)&&s.GetRelicLevel(candidate)==4&&s.Phase==SessionPhase.MvpComplete,"second offer recomputes level 3 to 4 after reload");
        Check(s.RetryRun()&&s.Stage==11,"completed prototype restarts at 11");
        // Concurrent boss/player death still pays the kill and never revives player.
        s=Playing(10,new[]{"thrust"},true);save=s.ExportSave();save.run.playerHpMilli=1000;s=new GameSession(catalog,save);s.SetPaused(false);s.Tick(.7f);s.Attack();s.Tick(.2f);
        Check(s.PlayerHp==0&&s.EnemyHp==0&&s.PendingRewardCount==1&&s.Fragments==4&&s.Transition.Kind==TransitionKind.Death,"simultaneous death preserves boss reward without healing");
        s.Tick(10);s=Reload(s);Check(s.Phase==SessionPhase.RunEnd&&!s.Result.Cleared,"death settlement survives reload");Settle(s);Check(s.RetryRun()&&s.Stage==11,"simultaneous kill still unlocks checkpoint");
        // Legacy directional save migration keeps wallet and pending promise.
        save=Playing(4,new[]{"high_sweep"}).ExportSave();save.schemaVersion=6;save.run.randomSequence=new[]{"outer_arc"};save.run.patternId="outer_arc";
        s=new GameSession(catalog,Round(save));Check(s.EnemyThreats.All(t=>t.Direction!=DodgeDirection.Forward),"legacy forward sequence migrates before validation");
        s=Playing(1);save=s.ExportSave();save.run.immunityUntilMs=-1;save.run.enemyHpMilli=1000000;s=new GameSession(catalog,save);s.SetPaused(false);
        for(int i=0;i<3;i++){s.Tick(Math.Max(0,s.EnemyIntent.SecondsToHit-.05f));Check(s.Dodge(s.EnemyIntent.Direction),"defense after full rest "+i);s.Tick(.050001f);}
        Check(s.IsStunReady&&!s.IsEnemyStunned,"three perfect defenses prepare stun without cancelling attacks");s.Attack();s.Tick(.2f);Check(s.IsEnemyStunned,"ready stack stuns only on confirmed hit");
        Console.WriteLine("REFERENCE_V81_CONTRACT_PASS "+n);
    }
}
