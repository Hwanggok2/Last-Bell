using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LastBell;

static class Program
{
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string source;static int count;
    static GameCatalog Catalog()=>JsonSerializer.Deserialize<GameCatalog>(source,Json);
    static GameSaveData Copy(GameSaveData x)=>JsonSerializer.Deserialize<GameSaveData>(JsonSerializer.Serialize(x,Json),Json);
    static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
    static void Near(double actual,double expected,string message,double epsilon=.002)=>Check(Math.Abs(actual-expected)<=epsilon,message+": "+actual+" vs "+expected);
    static GameSaveData Lobby(GameCatalog c)
    {
        var x=new GameSession(c).ExportSave();x.phase=SessionPhase.Lobby;x.prologueSeen=true;x.tutorialStep=2;x.comboPracticeCompleted=true;x.gold=100000000;x.fragments=100;x.highestClearedStage=40;x.companionSlots=3;
        foreach(var d in c.companions)x.companions.Add(new CompanionProgressData{id=d.id,level=1});return x;
    }
    static GameSession Restore(GameSession x){var s=new GameSession(x.Catalog,Copy(x.ExportSave()));s.SetPaused(false);return s;}
    static GameSession Fight(string hero="on",string companion=null,int stage=1,Action<GameSaveData> edit=null)
    {
        var c=Catalog();var x=Lobby(c);x.selectedCharacterId=hero;
        if(companion!=null)x.characters.Find(z=>z.id==hero).companionIds.Add(companion);
        var s=new GameSession(c,x);s.StartRun();s.Tick(10);x=s.ExportSave();
        var r=x.run;r.stage=stage;r.combatStats.attack=r.combatStats.baseAttack=100;r.combatStats.maxHp=1000;r.combatStats.critChancePercent=0;r.playerHpMilli=500000;r.enemyHpMilli=r.enemyMaxHpMilli=10000000;
        r.clockMs=0;r.randomSequence=new string[0];r.sequenceCursor=0;r.attackReadyAtMs=0;r.attackHitAtMs=-1;
        foreach(var a in r.companions){a.damageMilli=24000;a.damageFraction=a.damageRemainder=0;a.remainingMs=400;a.randomState=1;}
        edit?.Invoke(x);s=new GameSession(c,Copy(x));s.SetPaused(false);return s;
    }
    static void Travel(GameSession s){for(int i=0;i<8&&s.Phase==SessionPhase.Transition;i++)s.Tick(10);}
    static void PlayerIndependence()
    {
        foreach(var hero in new[]{"on","dam","rin"})
        {
            var s=Fight(hero);var events=new List<SessionEvent>();s.EventRaised+=events.Add;
            Check(s.Attack(),"ordinary start");s.Tick(.05f);var before=s.ExportSave().run;double pose=s.PlayerCombatBody.X;
            Check(s.UseSkill(),"skill accepted during ordinary windup "+hero);var after=s.ExportSave().run;
            Near(after.attackHitAtMs,before.attackHitAtMs,"normal contact unchanged");Near(after.attackReadyAtMs,before.attackReadyAtMs,"normal cooldown unchanged");Near(s.PlayerCombatBody.X,pose,"ordinary pose unchanged");
            Check(!s.UseSkill(),"duplicate rejected");s.Tick(.1501f);Check(events.Count(e=>e.Kind==SessionEventKind.EnemyDamaged)==1,"ordinary one contact");Check(!events.Any(e=>e.Kind==SessionEventKind.PlayerSkillContact),"independent later deadline");
            s.Tick(.05f);Near(s.EnemyHp,10000-100-CurrentDamage(hero),"separate damage");
            Check(events.Count(e=>e.Kind==SessionEventKind.AttackStarted)==1&&events.Count(e=>e.Kind==SessionEventKind.PlayerSkillStarted)==1&&events.Count(e=>e.Kind==SessionEventKind.PlayerSkillContact)==1,"separate event ownership");
            var contact=events.Single(e=>e.Kind==SessionEventKind.PlayerSkillContact);Check(contact.SkillId==s.CurrentSkill.id&&!contact.Critical,"skill ID and noncritical");
            var dodge=Fight(hero);Check(dodge.Dodge(DodgeDirection.Up),"dodge start");dodge.Tick(.05f);var dodgeSave=dodge.ExportSave().run;Check(dodge.UseSkill(),"skill accepted while dodging");Near(dodge.ExportSave().run.dodgeStartMs,dodgeSave.dodgeStartMs,"dodge not canceled");dodge.Tick(.2f);Near(dodge.EnemyHp,10000-CurrentDamage(hero),"dodge does not cancel independent hit");
            var pending=Fight(hero);pending.UseSkill();pending.Tick(.08f);pending=Restore(pending);Near(pending.SkillContactRemainingMs,120,"save contact remainder");double cooldown=pending.SkillCooldownRemainingMs;pending.SetPaused(true);pending.Tick(10);Near(pending.SkillCooldownRemainingMs,cooldown,"paused timer");Check(!pending.UseSkill(),"paused use refused");pending.SetPaused(false);events.Clear();pending.EventRaised+=events.Add;pending.Tick(.1201f);Check(events.Count(e=>e.Kind==SessionEventKind.PlayerSkillContact)==1,"restored one contact");pending=Restore(pending);double enemy=pending.EnemyHp;pending.Tick(0);Near(pending.EnemyHp,enemy,"no restored replay");
        }
        var first=Fight();first.UseSkill();Check(first.Attack(),"normal start during skill");first.Tick(.2f);Near(first.EnemyHp,9740,"same-frame normal and skill each once");
        var ready=Fight("rin",edit:x=>x.run.stagger=100);ready.UseSkill();ready.Tick(.2f);Check(ready.IsStunReady&&!ready.IsEnemyStunned,"skill cannot consume full stack");ready.Attack();ready.Tick(.2f);Check(ready.IsEnemyStunned,"next normal consumes stack");
        var shield=Fight("dam");shield.UseSkill();shield.Tick(.2f);Near(shield.SkillShieldMilli,80000,"shield on independent contact");Near(shield.SkillShieldRemainingMs,3000,"shield lifetime starts at contact");
        foreach(int stage in new[]{1,5,10}){var s=Fight("rin",stage:stage);s.UseSkill();s.Tick(.2f);Near(s.Stagger,stage==1?50:stage==5?30:20,"player stagger resistance");}
        var immune=Fight("rin",edit:x=>x.run.immunityUntilMs=10000);immune.UseSkill();immune.Tick(.2f);Near(immune.Stagger,0,"immunity applies");
        var guide=Fight(stage:5,edit:x=>{x.comboPracticeCompleted=false;x.phase=SessionPhase.ComboHint;x.run.enemyHitAtMs=-1;});Check(!guide.UseSkill()&&guide.ConfirmComboHint()&&!guide.UseSkill(),"guided skill locked");
    }
    static int CurrentDamage(string id)=>id=="on"?160:id=="dam"?115:125;
    static void CompanionProcs()
    {
        foreach(string id in new[]{"tori","barun","doto","rina","momo"})
        {
            var s=Fight(companion:id);var events=new List<SessionEvent>();s.EventRaised+=events.Add;s.Tick(.2201f);
            var started=events.Single(e=>e.Kind==SessionEventKind.CompanionAttackStarted);Check(started.SkillId==s.Catalog.FindCompanion(id).skillId,"seed1 forces special "+id);
            var x=s.ExportSave();int rng=x.run.companions[0].randomState;s=Restore(s);Check(s.ExportSave().run.companions[0].randomState==rng,"reload does not reroll");events.Clear();s.EventRaised+=events.Add;s.Tick(.18f);
            var hit=events.Single(e=>e.Kind==SessionEventKind.CompanionDamaged);Near(hit.DamageMilli,24000*s.Catalog.FindCompanion(id).skillDamagePercent/100d,"replacement damage "+id);
            Check(hit.SkillId==started.SkillId&&!events.Any(e=>e.Kind==SessionEventKind.EnemyDamaged||e.Kind==SessionEventKind.AttackStarted),"no duplicate basic/player contact");
            Near(s.Stagger,id=="barun"?8:id=="rina"?10:0,"companion stagger "+id);Near(s.PlayerHp,id=="momo"?520:500,"momo living heal");
            s=Restore(s);double hp=s.EnemyHp;s.Tick(0);Near(s.EnemyHp,hp,"no replay after proc contact");
            var basic=Fight(companion:id,edit:z=>{var a=z.run.companions[0];a.skillReserved=true;a.reservedSkill=false;});events.Clear();basic.EventRaised+=events.Add;basic.Tick(.401f);Near(basic.EnemyHp,9976,"basic not multiplied");Check(string.IsNullOrEmpty(events.Single(e=>e.Kind==SessionEventKind.CompanionDamaged).SkillId),"basic empty skillId");
        }
        foreach(int stage in new[]{5,10})foreach(string id in new[]{"barun","rina"})
        {var s=Fight(companion:id,stage:stage);s.Tick(.401f);Near(s.Stagger,(id=="barun"?8:10)*(stage==5?.6:.4),"companion resistance");}
        var cap=Fight(companion:"momo",edit:x=>x.run.playerHpMilli=995000);cap.Tick(.401f);Near(cap.PlayerHp,1000,"heal clamps max");
        var immunity=Fight(companion:"rina",edit:x=>x.run.immunityUntilMs=10000);immunity.Tick(.401f);Near(immunity.Stagger,0,"companion immunity");
        var full=Fight(companion:"barun",edit:x=>x.run.stagger=100);full.Tick(.401f);Check(full.IsStunReady&&!full.IsEnemyStunned,"companion cannot consume full stack");
    }
    static void AtomicityAndMigration()
    {
        var dead=Fight(companion:"momo",edit:x=>{x.run.playerHpMilli=1000;x.run.companions[0].remainingMs=900;});dead.Tick(.7f);Check(dead.UseSkill(),"predeath skill");var events=new List<SessionEvent>();dead.EventRaised+=events.Add;dead.Tick(.201f);Check(dead.PlayerHp==0,"enemy death deadline wins");Check(!events.Any(e=>e.Kind==SessionEventKind.PlayerSkillContact||e.Kind==SessionEventKind.CompanionDamaged||e.Kind==SessionEventKind.PlayerRecovered),"no postdeath damage/heal/revive");
        var last=Fight(companion:"tori",edit:x=>{x.run.enemyHpMilli=1000;x.run.companions[0].remainingMs=200;});events.Clear();last.EventRaised+=events.Add;last.UseSkill();last.Attack();last.Tick(.201f);Check(events.Count(e=>e.Kind==SessionEventKind.EnemyDefeated)==1,"simultaneous kill exactly once");Check(!events.Any(e=>e.Kind==SessionEventKind.PlayerSkillContact||e.Kind==SessionEventKind.CompanionDamaged),"normal kill cancels later contacts");long gold=last.Gold;Travel(last);Check(last.Gold==gold&&!last.SkillContactPending,"no next-enemy inherited player skill");
        var saved=Fight(companion:"tori",edit:x=>{x.run.enemyHpMilli=1000;x.run.companions[0].remainingMs=600;});saved.Tick(.3f);saved.Attack();saved.Tick(.13f);var reservation=saved.ExportSave().run.companions[0];saved.Tick(.071f);Travel(saved);var carried=saved.ExportSave().run.companions[0];Check(carried.skillReserved&&carried.reservedSkill==reservation.reservedSkill&&carried.randomState==reservation.randomState,"stage transition retains reserved choice");
        var old=Fight(companion:"tori").ExportSave();old.run.skillRulesVersion=0;old.run.companions[0].randomState=0;old.run.pendingSkillAttack=true;old.run.attackHitAtMs=200;old.run.skillCooldownRemainingMs=19900;old.run.companions[0].windingUp=true;old.run.companions[0].remainingMs=100;
        var migrated=new GameSession(Catalog(),Copy(old));Check(migrated.ExportSave().schemaVersion==9&&migrated.ExportSave().run.skillRulesVersion==1,"schema9 optional revision migration");Near(migrated.PlayerHp,500,"legacy HP unchanged");Near(migrated.SkillCooldownRemainingMs,19900,"legacy spent cooldown kept");Check(!migrated.SkillContactPending&&!migrated.ExportSave().run.pendingSkillAttack,"legacy coupled skill not replayed");Check(migrated.ExportSave().run.companions[0].skillReserved&&!migrated.ExportSave().run.companions[0].reservedSkill,"legacy windup completes basic once");
        foreach(var corrupt in new Action<GameSaveData>[] {x=>x.run.skillRulesVersion=2,x=>x.run.skillContactRemainingMs=1,x=>x.run.companions[0].randomState=0,x=>x.run.companions[0].reservedSkill=true,x=>x.run.companions[0].damageRemainder=1,x=>x.run.companions[0].damageFraction=double.NaN})
        {var s=Fight(companion:"tori").ExportSave();corrupt(s);bool rejected=false;try{new GameSession(Catalog(),s);}catch(InvalidOperationException){rejected=true;}Check(rejected,"malformed skill snapshot rejected");}
    }
    static void GrowthAndCap()
    {
        var c=Catalog();string[] ids=c.companions.Select(x=>x.id).ToArray();
        foreach(string upgraded in ids)foreach(int otherLevel in new[]{1,10,20,30})foreach(int size in new[]{1,2,3})
        {
            var party=new[]{upgraded}.Concat(ids.Where(x=>x!=upgraded)).Take(size).ToArray();double previous=-1,previousOwn=-1;
            for(int level=1;level<=30;level++)
            {
                var x=Lobby(c);foreach(var p in x.companions)p.level=p.id==upgraded?level:otherLevel;x.characters[0].companionIds.AddRange(party);
                var s=new GameSession(c,x);var views=s.Companions.Where(v=>party.Contains(v.Id)).ToArray();double total=views.Sum(v=>v.ExpectedDpsMilli),own=views.Single(v=>v.Id==upgraded).ExpectedDpsMilli;
                Check(total<=s.PlayerStats.baseAttack*240+.000001,"weighted cap");Check(total+.000001>=previous&&own+.000001>=previousOwn,"upgrade monotonic weighted DPS");previous=total;previousOwn=own;
                var upgradedView=views.Single(z=>z.Id==upgraded);Check(upgradedView.SkillChancePercent==11+level&&upgradedView.PeriodMs==c.FindCompanion(upgraded).periodMs*(101-level)/100,"chance/period growth");
                Check(upgradedView.NextExpectedDpsMilli+.000001>=own,"next DPS monotonic");
                s.StartRun();Near(s.RunCompanions.Sum(z=>z.ExpectedDpsMilli),total,"live snapshot matches preview",.00001);
            }
        }
    }
    static void DeterminismAndFractionalDamage()
    {
        var a=Fight(companion:"tori",edit:x=>{x.run.stunUntilMs=100000000;x.run.immunityUntilMs=200000000;x.run.enemyHitAtMs=-1;x.run.enemyHpMilli=x.run.enemyMaxHpMilli=1000000000000;});
        var b=Restore(a);var ea=new List<SessionEvent>();var eb=new List<SessionEvent>();a.EventRaised+=ea.Add;b.EventRaised+=eb.Add;
        for(int i=0;i<200;i++)
        {
            a.Tick(.22f);b.Tick(.22f);
            if(i%3==0){b=Restore(b);b.EventRaised+=eb.Add;}
            a.Tick(5.78f);b.Tick(5.78f);
            var ca=a.ExportSave().run.companions[0];var cb=b.ExportSave().run.companions[0];
            Check(ca.randomState==cb.randomState&&ca.skillReserved==cb.skillReserved&&ca.reservedSkill==cb.reservedSkill,"reload preserves proc stream");
            Near(ca.remainingMs,cb.remainingMs,"same independent timer");
        }
        var ha=ea.Where(e=>e.Kind==SessionEventKind.CompanionDamaged).ToArray();var hb=eb.Where(e=>e.Kind==SessionEventKind.CompanionDamaged).ToArray();
        Check(ha.Length==200&&hb.Length==200,"one replacement hit per period");Check(ha.Select(e=>e.SkillId??"").SequenceEqual(hb.Select(e=>e.SkillId??"")),"all saved choices identical");
        Check(ha.Sum(e=>e.DamageMilli)==hb.Sum(e=>e.DamageMilli),"save cannot duplicate or change damage");
        Check(ha.Any(e=>e.SkillId!=null)&&ha.Any(e=>e.SkillId==null),"both proc and ordinary outcomes exercised");
        // Fractional internal milli-damage must be carried, not rounded down each hit.
        var f=Fight(companion:"tori",edit:x=>{var c=x.run.companions[0];c.damageMilli=101;c.damageFraction=.37;x.run.stunUntilMs=100000000;x.run.immunityUntilMs=200000000;x.run.enemyHitAtMs=-1;});
        double intended=0;long actual=0;f.EventRaised+=e=>{if(e.Kind==SessionEventKind.CompanionDamaged){intended+=101.37*(e.SkillId==null?1:1.8);actual+=e.DamageMilli;}};
        for(int i=0;i<100;i++)f.Tick(6);
        Check(actual<=intended+.000001&&intended-actual<1.000001,"fraction carry prevents cap rounding drift");
        var x0=Fight(companion:"tori").ExportSave();int pattern=x0.run.patternRandomState,random=x0.randomState;
        var independent=new GameSession(Catalog(),x0);independent.SetPaused(false);independent.Tick(.23f);
        Check(independent.ExportSave().randomState==random,"proc does not spend player crit/reward RNG");
    }
    static int Main(string[] args)
    {
        source=File.ReadAllText(args.Length>0?args[0]:"GameData/mvp-catalog.json");
        foreach(var test in new Action[]{PlayerIndependence,CompanionProcs,AtomicityAndMigration,GrowthAndCap,DeterminismAndFractionalDamage}){test();Console.WriteLine("PASS "+test.Method.Name);}
        Console.WriteLine("PASS "+count+" assertions; "+Catalog().dataVersion);return 0;
    }
}
