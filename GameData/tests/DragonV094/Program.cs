using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LastBell;

internal static class Program
{
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string source;static int assertions;
    static GameCatalog Catalog()=>JsonSerializer.Deserialize<GameCatalog>(source,Json);
    static string Encode(GameSaveData value)=>JsonSerializer.Serialize(value,Json);
    static GameSaveData Copy(GameSaveData value)=>JsonSerializer.Deserialize<GameSaveData>(Encode(value),Json);
    static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
    static void Near(double value,double expected,string message){Check(Math.Abs(value-expected)<.02,message+": "+value+" != "+expected);}
    static GameSaveData Lobby(GameCatalog c,bool rich=false)
    {
        var s=new GameSession(c).ExportSave();s.phase=SessionPhase.Lobby;s.tutorialStep=2;s.prologueSeen=true;s.comboPracticeCompleted=true;
        if(rich){s.gold=500000000;s.fragments=100;s.highestClearedStage=40;foreach(var d in c.companions)s.companions.Add(new CompanionProgressData{id=d.id,level=1});}
        return s;
    }
    static GameSession Resume(GameSession s){var result=new GameSession(s.Catalog,Copy(s.ExportSave()));result.SetPaused(false);return result;}
    static GameSession Fight(int stage=1,string id="on",Action<GameSaveData> edit=null,Action<GameCatalog> configure=null)
    {
        var c=Catalog();configure?.Invoke(c);var save=Lobby(c);save.selectedCharacterId=id;var s=new GameSession(c,save);s.StartRun();s.Tick(10);
        save=s.ExportSave();var run=save.run;run.stage=stage;run.enemyHpMilli=run.enemyMaxHpMilli=10000000;run.combatStats.maxHp=1000;run.playerHpMilli=500000;
        run.combatStats.attack=run.combatStats.baseAttack=100;run.combatStats.critChancePercent=0;run.combatStats.recovery=25;
        run.randomSequence=new string[0];run.sequenceCursor=0;run.patternIndex=0;run.clockMs=0;run.attackHitAtMs=-1;run.attackReadyAtMs=0;run.enemyHitAtMs=-1;
        edit?.Invoke(save);
        // Explicit current-format companions in these legacy feature fixtures.
        foreach(var companion in run.companions){if(companion.randomState==0)companion.randomState=1;if(companion.windingUp)companion.skillReserved=true;}
        s=new GameSession(c,Copy(save));s.SetPaused(false);return s;
    }
    static void FinishTravel(GameSession s){for(int i=0;i<8&&s.Phase==SessionPhase.Transition;i++)s.Tick(10);}
    static void Kill(GameSession s){Check(s.Attack(),"kill attack accepted");s.Tick(.201f);FinishTravel(s);}
    static void CloseStory(GameSession s,bool join=false)
    {
        if(s.Phase!=SessionPhase.StoryDialogue)return;
        s.SkipStory();if(s.Story!=null&&s.Story.AwaitingCompanionChoice)s.ChooseCompanionJoin(join);FinishTravel(s);
    }
    static GameSaveData AsLegacy(GameSaveData s)
    {
        s.schemaVersion=8;foreach(var c in s.characters)c.statLevels=c.statLevels.Take(4).ToArray();
        if(s.run!=null)s.run.statLevels=s.run.statLevels.Take(4).ToArray();return s;
    }
    static void RecoveryAndQuotes()
    {
        int[] levels={0,1,10,11,20,21,40,50};int[][] expected={new[]{5,7,25,49,81,152,496,1102},new[]{5,8,30,60,102,197,665,1523},new[]{5,7,20,38,61,112,348,750}};
        string[] ids={"on","dam","rin"};
        for(int c=0;c<3;c++)for(int i=0;i<levels.Length;i++)
        {
            var data=Catalog();var save=Lobby(data);save.selectedCharacterId=ids[c];save.characters.Single(x=>x.id==ids[c]).statLevels[4]=levels[i];
            var s=new GameSession(data,save);Near(s.PlayerStats.recovery,expected[c][i],"recovery "+ids[c]+" "+levels[i]);
        }
        var cat=Catalog();var rich=Lobby(cat,true);var shop=new GameSession(cat,rich);
        var quote=shop.QuoteUpgrade(4,false);Check(quote.Cost==5&&quote.After.recovery==7,"recovery quote cost/result");long before=shop.Gold;
        Check(shop.PurchaseUpgrade(quote)&&!shop.PurchaseUpgrade(quote),"upgrade confirmation idempotent");Near(shop.Gold,before-5,"debit once");
        Check(shop.GetStatLevel(4)==1&&shop.GetStatLevel(3)==0,"fifth stat separate");
        foreach(int stage in new[]{1,5,10,15,20})
        {
            var s=Fight(stage,edit:x=>x.run.enemyHpMilli=1000);var events=new List<SessionEvent>();s.EventRaised+=events.Add;
            Kill(s);Near(s.PlayerHp,525,"same integer heal for all kinds "+stage);Check(events.Count(e=>e.Kind==SessionEventKind.PlayerRecovered)==1,"one healing event");
            s=Resume(s);CloseStory(s);Near(s.PlayerHp,525,"no load/dialogue heal replay");
        }
        var clamp=Fight(edit:x=>{x.run.enemyHpMilli=1000;x.run.playerHpMilli=990000;});Kill(clamp);Near(clamp.PlayerHp,1000,"heal clamp");
        var trade=Fight(10,edit:x=>{x.run.enemyHpMilli=1000;x.run.playerHpMilli=1000;});
        double impact=trade.EnemyThreats[0].ImpactAtMs;trade.Tick((float)((impact-200)/1000));trade.Attack();trade.Tick(.201f);
        Near(trade.PlayerHp,0,"trade cannot revive");
    }
    static void Migration()
    {
        var cat=Catalog();var old=AsLegacy(Lobby(cat));old.gold=54321;old.fragments=7;
        old.characters[0].seenStoryIds.AddRange(new[]{"story_encounter_2","story_encounter_7"});old.characters[1].seenStoryIds.AddRange(new[]{"story_encounter_15","story_boss_20"});
        var s=new GameSession(cat,Copy(old));Check(s.ExportSave().schemaVersion==9,"schema9");Near(s.Gold,54321,"migration wallet");Check(s.Companions.Count(x=>x.Unlocked)==4&&!s.CompanionUnlocked("momo"),"union retroactive four");
        Check(s.CompanionSlots==1&&s.EquippedCompanionIds.Count==0&&s.WeaponRank==0,"migration no surprise formation/service");
        Check(s.ExportSave().characters.All(x=>x.statLevels.Length==5&&x.statLevels[4]==0),"append zero fifth stat");
        Check(!s.ExportSave().comboPracticeCompleted,"old read flag not completion");
        s=Resume(s);Check(s.Companions.Count(x=>x.Unlocked)==4&&s.Companions.All(x=>x.Level<=1),"migration not repeated");
        var merelySeen=AsLegacy(Lobby(cat));merelySeen.unlockedStoryIds.Add("story_encounter_7");var noGrant=new GameSession(cat,merelySeen);Check(!noGrant.CompanionUnlocked("rina"),"uncompleted record is not unlock");
        foreach(var pair in new[]{new{Stage=7,Id="rina"},new{Stage=15,Id="barun"},new{Stage=20,Id="doto"}})
        {
            var live=Fight(pair.Stage);var save=AsLegacy(live.ExportSave());save.highestClearedStage=pair.Stage;save.run.stageRewardApplied=true;save.run.enemyHpMilli=0;save.run.enemyHitAtMs=-1;save.phase=SessionPhase.StoryDialogue;
            string scene="story_"+(pair.Stage==20?"boss_":"encounter_")+pair.Stage;
            save.run.story=new StoryStateData{sceneId=scene,rewardId=save.run.runId+":0:"+pair.Stage,lineIndex=1};save.run.skillCooldownRemainingMs=12345;
            var migrated=new GameSession(cat,Copy(save));Check(!migrated.CompanionUnlocked(pair.Id),"active legacy waits "+pair.Id);Check(migrated.Story.Text==cat.FindStory(scene,true).lines[1].text,"legacy text/page preserved");
            double hp=migrated.PlayerHp;long gold=migrated.Gold;migrated.SkipStory();Check(migrated.CompanionUnlocked(pair.Id),"legacy completion unlocks");
            if(migrated.Story?.AwaitingCompanionChoice==true)migrated.ChooseCompanionJoin(false);FinishTravel(migrated);
            Near(migrated.PlayerHp,hp,"legacy completion no additional heal");Near(migrated.Gold,gold,"legacy completion no duplicated payout");Near(migrated.SkillCooldownRemainingMs,12345,"legacy cooldown preserved");
        }
        var checkpoint=AsLegacy(Lobby(cat));checkpoint.highestClearedStage=12;var backfill=new GameSession(cat,checkpoint);Check(backfill.CompanionUnlocked("tori")&&backfill.CompanionUnlocked("barun"),"progress backfill");Check(backfill.CheckpointStartStage==1,"account NPC does not unlock character checkpoint");
        var oldFive=AsLegacy(Fight(5).ExportSave());oldFive.comboHintSeen=true;oldFive.run.skillCooldownRemainingMs=1234;
        var newFive=new GameSession(cat,oldFive);Check(newFive.Phase==SessionPhase.ComboHint&&!newFive.ExportSave().comboPracticeCompleted,"active legacy stage5 offers action tutorial");Near(newFive.SkillCooldownRemainingMs,1234,"legacy tutorial migration retains cooldown");Near(newFive.PlayerHp,500,"legacy tutorial migration retains HP");
    }
    static void EncountersAndTransactions()
    {
        var tori=Fight(2,edit:x=>x.run.enemyHpMilli=1000);Kill(tori);Check(tori.Story!=null&&!tori.CompanionUnlocked("tori"),"unlock deferred until dialogue complete");
        long stage2gold=tori.Gold;tori.SkipStory();FinishTravel(tori);Check(tori.Stage==3&&tori.RunCompanions.Count==1&&tori.RunCompanions[0].Id=="tori","first Tori joins stage3");Near(tori.Gold,stage2gold,"no retrospective guide gold");Check(tori.PlayerStats.goldBonusPercent==3,"guide applies next reward");
        Check(!tori.UpgradeCompanion("tori")&&!tori.RemoveCompanion(0)&&!tori.EquipCompanion("tori",0),"inn only changes");
        var b=Fight(12,edit:x=>{x.run.enemyHpMilli=1000;x.adventureNewAccount=false;});Kill(b);b.SkipStory();Check(b.Story.AwaitingCompanionChoice&&b.Story.CanInviteCompanion,"later empty-slot choice");
        b=Resume(b);Check(b.Story.AwaitingCompanionChoice,"invitation resumes");Check(b.ChooseCompanionJoin(true)&&!b.ChooseCompanionJoin(true),"join once");FinishTravel(b);Check(b.RunCompanions.Count==1&&b.RunCompanions[0].Id=="barun","joined snapshot");
        var cat=Catalog();var save=Lobby(cat,true);var s=new GameSession(cat,save);long wallet=s.Gold;var cq=s.QuoteCompanionUpgrade("tori");Check(cq.Cost==20,"companion minimum cost");Check(s.PurchaseCompanionUpgrade(cq)&&!s.PurchaseCompanionUpgrade(cq),"companion quote stale repeat");Near(s.Gold,wallet-20,"companion debit once");
        var sq=s.QuoteCompanionSlot();Check(s.PurchaseCompanionSlot(sq)&&!s.PurchaseCompanionSlot(sq),"slot quote once");Near(s.Fragments,96,"second slot four gems");Check(s.ExpandCompanionSlots(),"third slot");Near(s.Fragments,88,"third slot eight gems");
        Check(s.EquipCompanion("tori",0)&&s.EquipCompanion("barun",1)&&s.EquipCompanion("doto",2),"three slots equip");Check(s.EquipCompanion("tori",1),"reorder swaps");Check(s.EquippedCompanionIds.Distinct().Count()==3,"no duplicated companion");
        s.SelectCharacter("dam");Check(s.EquippedCompanionIds.Count==0&&s.CompanionLevel("tori")==2&&s.CompanionSlots==3,"character formation/account progression separate");s.SelectCharacter("on");
        foreach(string id in new[]{"weapon_craft","travel_blessing","herbal_tea"})
        {var q=s.QuoteService(id);wallet=s.Gold;Check(s.PurchaseService(q)&&!s.PurchaseService(q),"service once "+id);Near(s.Gold,wallet-q.Cost,"service cost "+id);}
        long paid=s.ReservedServicePaidGold("herbal_tea");wallet=s.Gold;Check(s.CancelService("herbal_tea")&&!s.CancelService("herbal_tea"),"cancel once");Near(s.Gold,wallet+paid,"exact refund");
        s.ReserveService("herbal_tea");int preview=s.PlayerStats.recovery;int maxHp=s.PlayerStats.maxHp;int baseAttack=s.PlayerStats.baseAttack;
        Check(s.StartRun(),"depart with services");Check(!s.IsServiceReserved("herbal_tea")&&!s.IsServiceReserved("travel_blessing"),"consumed reservation");Check(s.PlayerStats.recovery==preview&&s.PlayerHp==maxHp,"snapshot services");Check(s.PlayerStats.baseAttack==baseAttack,"craft excluded from companion base");
        Check(!s.CancelService("travel_blessing")&&!s.CraftWeapon(),"no mid-run service");
        var empty=Lobby(cat);empty.companions.Add(new CompanionProgressData{id="tori",level=1});empty.gold=0;empty.fragments=1;var exchange=new GameSession(cat,empty);
        Check(!exchange.UpgradeCompanion("tori")&&exchange.CompanionLevel("tori")==1,"insufficient purchase no mutation");Check(exchange.RequestPurchaseExchange(20)&&exchange.Fragments==1,"explicit exchange quote");Check(exchange.ConfirmExchange()&&!exchange.ConfirmExchange(),"exchange once");Check(exchange.CompanionLevel("tori")==1,"exchange does not buy companion upgrade");
    }
    static void CompanionClockAndDamage()
    {
        var cat=Catalog();cat.combat.singleTelegraphMs=30000;var save=Lobby(cat,true);save.companionSlots=3;var c=save.characters[0];c.companionIds.AddRange(new[]{"tori","barun","doto"});
        foreach(var p in save.companions)p.level=30;var s=new GameSession(cat,save);s.StartRun();s.Tick(10);save=s.ExportSave();save.run.combatStats.attack=save.run.combatStats.baseAttack=100;save.run.enemyHpMilli=save.run.enemyMaxHpMilli=10000000;
        // Actual production snapshots with A100; use join recalculation by choosing a known level producing A from the actual run below.
        var actual=s.RunCompanions;double a=s.PlayerStats.baseAttack;Near(actual.Sum(x=>x.ExpectedDpsMilli),a*240,"party expected DPS cap milli");
        var events=new List<SessionEvent>();s.EventRaised+=events.Add;s.Tick(4.079f);Check(events.Count(e=>e.Kind==SessionEventKind.CompanionAttackStarted)==0,"level30 windup not early");s.Tick(.002f);Check(events.Count(e=>e.Kind==SessionEventKind.CompanionAttackStarted)==1,"windup180 before contact");
        s.SetPaused(true);double remaining=s.RunCompanions[0].RemainingMs;s.Tick(99);Near(s.RunCompanions[0].RemainingMs,remaining,"pause companion clock");s.SetPaused(false);s.Tick(.18f);
        Check(events.Count(e=>e.Kind==SessionEventKind.CompanionDamaged)==1,"first contact once");Check(!events.Any(e=>e.Kind==SessionEventKind.EnemyDamaged||e.Kind==SessionEventKind.AttackStarted),"no player events");
        Near(s.RunCompanions[0].RemainingMs,4259,"level30 period includes windup");s=Resume(s);s.Tick(0);Near(s.RunCompanions[0].RemainingMs,4259,"restore no replay");
        var lastHit=Fight(edit:x=>{x.companions.Add(new CompanionProgressData{id="tori",level=1});x.characters[0].companionIds.Add("tori");x.run.companions.Add(new RunCompanionData{id="tori",level=1,periodMs=6000,damageMilli=24000,remainingMs=400});x.run.enemyHpMilli=1000;});
        events.Clear();lastHit.EventRaised+=events.Add;lastHit.Tick(.401f);Check(lastHit.Phase==SessionPhase.Transition,"companion last hit advances");Near(lastHit.PlayerHp,525,"companion last hit heals once");long gold=lastHit.Gold;FinishTravel(lastHit);Near(lastHit.Gold,gold,"transition no reward replay");Check(events.Count(e=>e.Kind==SessionEventKind.EnemyDefeated)==1,"one defeat event");Near(lastHit.RunCompanions[0].RemainingMs,6000,"travel freeze carry");
        var fullStack=Fight(edit:x=>{x.companions.Add(new CompanionProgressData{id="tori",level=1});x.run.companions.Add(new RunCompanionData{id="tori",level=1,periodMs=6000,damageMilli=24000,remainingMs=400});x.run.stagger=100;});fullStack.Tick(.401f);Check(fullStack.IsStunReady&&!fullStack.IsEnemyStunned,"companion cannot trigger full stagger");
        var death=Fight(edit:x=>{x.companions.Add(new CompanionProgressData{id="tori",level=1});x.run.companions.Add(new RunCompanionData{id="tori",level=1,periodMs=6000,damageMilli=24000,remainingMs=900});x.run.playerHpMilli=1000;x.run.enemyHpMilli=1000;});events.Clear();death.EventRaised+=events.Add;death.Tick(.901f);Check(death.PlayerHp==0&&!events.Any(e=>e.Kind==SessionEventKind.CompanionDamaged||e.Kind==SessionEventKind.PlayerRecovered),"player death wins simultaneous companion contact");
    }
    static void GuidedPractice()
    {
        var s=Fight(5,edit:x=>{x.comboPracticeCompleted=false;x.comboHintSeen=true;x.phase=SessionPhase.ComboHint;x.run.enemyHitAtMs=-1;x.run.skillCooldownRemainingMs=12000;});
        Check(s.Phase==SessionPhase.ComboHint,"old read flag does not skip tutorial");double hp=s.PlayerHp;long gold=s.Gold;var events=new List<SessionEvent>();s.EventRaised+=events.Add;
        Check(s.ConfirmComboHint(),"start guided");Near(s.EnemyThreats[0].ImpactAtMs,1800,"first real time");Near(s.EnemyThreats[1].ImpactAtMs,2550,"second real time");
        Check(!s.Dodge(DodgeDirection.Up)&&!s.Attack()&&!s.UseSkill(),"no prebuffer/attack/skill");s.Tick(30);Near(s.ClockMs,1800,"first hold exact");Check(s.ComboPractice.AwaitingInput&&s.CombatSuspended,"hold view");s.Tick(30);Near(s.ClockMs,1800,"hold no wall time");Check(!s.Dodge(DodgeDirection.Down),"wrong edge rejected");
        var interrupted=Resume(s);Check(!interrupted.ComboPractice.AwaitingInput&&interrupted.ComboPractice.Step==0,"interrupted starts guided first sequence");Near(interrupted.SkillCooldownRemainingMs,12000,"restore cooldown");
        Check(s.Dodge(DodgeDirection.Up),"fresh first edge");Check(!s.Dodge(DodgeDirection.Up),"held duplicate not second completion");s.Tick(10);Near(s.ClockMs,2550,"second exact hold");Check(s.Dodge(DodgeDirection.Down),"second parry edge");
        Check(s.ExportSave().comboPracticeCompleted,"complete only after both inputs");Near(s.PlayerHp,hp,"guided HP unchanged");Near(s.Gold,gold,"guided no gold");Near(s.Stagger,0,"guided no stagger");Near(s.SkillCooldownRemainingMs,12000,"guided cooldown frozen");
        Check(!events.Any(e=>e.Kind==SessionEventKind.PerfectDodge||e.Kind==SessionEventKind.PerfectParry||e.Kind==SessionEventKind.PlayerRecovered),"normal grade no bonus");
        s.Tick(.36f);Check(s.ComboPractice.IsRealTrial&&!s.ComboPractice.Active,"release to real trial");Check(s.EnemyThreats.Select(x=>x.PatternId).SequenceEqual(new[]{"low_sweep","high_sweep"}),"same real sequence");s.Tick(1.801f);Check(s.PlayerHp<hp,"real trial damage normal");
        var skip=Fight(5,edit:x=>{x.comboPracticeCompleted=false;x.phase=SessionPhase.ComboHint;x.run.enemyHitAtMs=-1;});Check(skip.SkipComboPractice()&&skip.ExportSave().comboPracticeSkipped&&!skip.ExportSave().comboPracticeCompleted,"explicit skipped marker");
        var live=Fight(edit:x=>x.run.skillCooldownRemainingMs=5432);live.Tick(.3f);live.SetPaused(true);string before=Encode(live.ExportSave());int saves=0;live.SaveRequested+=()=>saves++;
        Check(live.BeginComboPractice()&&live.ConfirmComboHint(),"help sandbox");live.Tick(3);Check(Encode(live.ExportSave())==before&&saves==0,"sandbox never persists tutorial rewards/state");Check(live.Dodge(DodgeDirection.Up),"sandbox action");Check(live.EndComboPractice(),"exit help");Check(Encode(live.ExportSave())==before&&live.IsPaused,"exact live save/timing/pause restored");
        Check(live.BeginComboPractice()&&live.ConfirmComboHint(),"replay again");live.Tick(3);live.Dodge(DodgeDirection.Up);live.Tick(1);live.Dodge(DodgeDirection.Down);live.Tick(.36f);
        Check(!live.ComboPractice.IsReplay&&Encode(live.ExportSave())==before&&saves==0,"completed help restores without save or progression");
    }
    static void RiskStates()
    {
        var cat=Catalog();var seed=Lobby(cat,true);var s=new GameSession(cat,seed);
        var cq=s.QuoteCompanionUpgrade("tori");s.ReserveService("travel_blessing");long wallet=s.Gold;Check(!s.PurchaseCompanionUpgrade(cq)&&s.Gold==wallet,"unrelated purchase invalidates upgrade quote");
        var service=s.QuoteService("herbal_tea");s.SelectCharacter("dam");Check(!s.PurchaseService(service),"service quote character binding");s.SelectCharacter("on");
        long paid=s.ReservedServicePaidGold("travel_blessing");var moved=s.ExportSave();moved.characters.Single(x=>x.id=="on").checkpoint=10;
        s=new GameSession(cat,Copy(moved));Check(!s.IsServiceReserved("travel_blessing"),"changed checkpoint cancels old reservation");Near(s.Gold,wallet+paid,"changed checkpoint exact refund");wallet=s.Gold;s=Resume(s);Near(s.Gold,wallet,"refund not duplicated on load");
        var preview=s.Companions.Single(x=>x.Id=="tori");Check(preview.NextDamageMilli>=preview.DamageMilli&&preview.PeriodMs==6000,"UI next damage/period from rules");
        foreach(var corrupt in new Action<GameSaveData>[]
        {
            x=>x.companions.Add(new CompanionProgressData{id="tori",level=1}),
            x=>x.characters[0].companionIds.AddRange(new[]{"tori","tori"}),
            x=>x.companions[0].level=31,
            x=>x.companionSlots=4,
            x=>x.characters[0].statLevels=new int[4],
            x=>x.characters[0].reservations.Add(new ServiceReservationData{serviceId="travel_blessing",characterId="dam",startStage=1,paidGold=2})
        })
        {
            var bad=Copy(seed);corrupt(bad);bool rejected=false;try{new GameSession(cat,bad);}catch(InvalidOperationException){rejected=true;}Check(rejected,"invalid save rejected without writer");
        }
        var capSeed=Lobby(cat);capSeed.companions.Add(new CompanionProgressData{id="tori",level=10});capSeed.gold=999999;
        var cap=new GameSession(cat,capSeed);Check(!cap.UpgradeCompanion("tori")&&cap.CompanionLevelCap==10,"level10 locked until boss10");
        var weak=Fight(edit:x=>{x.companions.Add(new CompanionProgressData{id="tori",level=1});x.run.companions.Add(new RunCompanionData{id="tori",level=1,periodMs=6000,damageMilli=24000,remainingMs=400});x.run.enemyHpMilli=1000;});
        weak.Attack();weak.Tick(.201f);double carry=weak.RunCompanions[0].RemainingMs;FinishTravel(weak);Near(weak.RunCompanions[0].RemainingMs,350,"new enemy grace delays pending contact");Check(carry<350,"actual short carry exercised");
        var guided=Fight(5,edit:x=>{x.comboPracticeCompleted=false;x.phase=SessionPhase.ComboHint;x.run.enemyHitAtMs=-1;x.companions.Add(new CompanionProgressData{id="tori",level=1});x.run.companions.Add(new RunCompanionData{id="tori",level=1,periodMs=6000,damageMilli=24000,remainingMs=400});});
        guided.ConfirmComboHint();double remaining=guided.RunCompanions[0].RemainingMs;guided.Tick(30);Near(guided.RunCompanions[0].RemainingMs,remaining,"guided companion lock includes moving cue");
        var resumedComplete=Fight(5,edit:x=>{x.comboPracticeCompleted=false;x.phase=SessionPhase.ComboHint;x.run.enemyHitAtMs=-1;});resumedComplete.ConfirmComboHint();resumedComplete.Tick(2);resumedComplete.Dodge(DodgeDirection.Up);resumedComplete.Tick(1);resumedComplete.Dodge(DodgeDirection.Down);
        resumedComplete=Resume(resumedComplete);Check(!resumedComplete.ComboPractice.AwaitingInput&&resumedComplete.ExportSave().comboPracticeCompleted,"saved completion does not restart guided input");resumedComplete.Tick(.36f);Check(resumedComplete.ComboPractice.IsRealTrial,"saved recovery continues real trial");
        var pending=Fight(edit:x=>{x.companions.Add(new CompanionProgressData{id="tori",level=1});x.run.companions.Add(new RunCompanionData{id="tori",level=1,periodMs=6000,damageMilli=24000,remainingMs=50,windingUp=true});});
        var hits=new List<SessionEvent>();pending.EventRaised+=hits.Add;Near(pending.RunCompanions[0].RemainingMs,50,"same-enemy reload preserves pending companion time");pending.Tick(.051f);Check(hits.Count(x=>x.Kind==SessionEventKind.CompanionDamaged)==1,"pending contact resumes once");
        pending=Resume(pending);hits.Clear();pending.EventRaised+=hits.Add;pending.Tick(0);Check(!hits.Any(x=>x.Kind==SessionEventKind.CompanionDamaged||x.Kind==SessionEventKind.CompanionAttackStarted),"completed contact not replayed after reload");
    }
    static void InlineNullRunRecovery()
    {
        var cat=Catalog();
        foreach(int schema in new[]{2,7,8,9})
        {
            var save=Lobby(cat);save.schemaVersion=schema;save.gold=3000000000L;
            save.characters[0].checkpoint=10;save.characters[0].highestStage=10;
            save.run=new RunStateData{stats=new LegacyCombatStatsData(),offer=new RelicOfferData(),story=new StoryStateData(),transition=new TransitionData()};
            if(schema<9){foreach(var c in save.characters)Array.Resize(ref c.statLevels,4);Array.Resize(ref save.run.statLevels,4);}
            var restored=new GameSession(cat,Copy(save));
            Check(restored.Phase==SessionPhase.Lobby&&restored.ExportSave().run==null,"empty inline run canonicalized before schema "+schema+" migration/validation");
            Check(restored.Gold==3000000000L&&restored.CheckpointStartStage==11&&restored.ExportSave().schemaVersion==9,"wallet/checkpoint/schema preserved "+schema);
        }
        var zero=Lobby(cat);zero.run=new RunStateData{dodgeStartMs=0,attackHitAtMs=0,enemyHitAtMs=0,attackSpeed=0,stunUntilMs=0,immunityUntilMs=0,statLevels=new int[0]};
        Check(new GameSession(cat,Copy(zero)).ExportSave().run==null,"all-zero Unity inline run shape canonicalized");
        foreach(var corrupt in new Action<RunStateData>[]
        {
            r=>r.playerHpMilli=1000,r=>r.earnedGold=1,r=>r.skillCooldownRemainingMs=1,r=>r.combatStats.recovery=1,
            r=>r.statLevels[4]=1,r=>r.characterId="on",r=>r.story=new StoryStateData{reunion=true},
            r=>r.companions.Add(new RunCompanionData{id="tori"}),r=>r.statLevels=new int[6]
        })
        {
            var save=Lobby(cat);save.run=new RunStateData();corrupt(save.run);save.NormalizeOptionalRecords();
            Check(save.run!=null,"partial run retained for strict validation");
            bool rejected=false;try{new GameSession(cat,Copy(save));}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"partial inactive run cannot be silently lost");
        }
        var active=Lobby(cat);active.phase=SessionPhase.Combat;active.run=new RunStateData();
        bool missingRejected=false;try{new GameSession(cat,Copy(active));}catch(InvalidOperationException){missingRejected=true;}
        Check(missingRejected,"empty run cannot satisfy active combat");
    }
    static int Main(string[] args)
    {
        source=File.ReadAllText(args.Length>0?args[0]:"GameData/mvp-catalog.json");
        foreach(var test in new Action[]{RecoveryAndQuotes,Migration,EncountersAndTransactions,CompanionClockAndDamage,GuidedPractice,RiskStates,InlineNullRunRecovery}){test();Console.WriteLine("PASS "+test.Method.Name);}
        Console.WriteLine("PASS "+assertions+" assertions; "+Catalog().dataVersion);return 0;
    }
}
