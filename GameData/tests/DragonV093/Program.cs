using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LastBell;

internal static class Program
{
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };
    static string source;
    static int assertions;
    static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
    static void Near(double value, double expected, string message) { Check(Math.Abs(value-expected)<.01, message+": "+value+" != "+expected); }
    static GameCatalog Catalog() => JsonSerializer.Deserialize<GameCatalog>(source, Json);
    static GameSaveData RoundTrip(GameSaveData value) => JsonSerializer.Deserialize<GameSaveData>(JsonSerializer.Serialize(value,Json),Json);
    static GameSession Restore(GameSession s)
    {
        var restored = new GameSession(s.Catalog,RoundTrip(s.ExportSave()));
        restored.SetPaused(false);return restored;
    }
    static GameSession Fighter(string character="on", int stage=1, long enemyHp=10000000, int incoming=30, Action<GameSaveData> setup=null, Action<GameCatalog> configure=null)
    {
        var data=Catalog();configure?.Invoke(data);
        data.GetStage(stage).damage=incoming;
        var seed=new GameSession(data).ExportSave();seed.phase=SessionPhase.Lobby;seed.tutorialStep=2;seed.prologueSeen=seed.comboHintSeen=true;
        seed.selectedCharacterId=character;
        var session=new GameSession(data,seed);Check(session.StartRun(),"start "+character);session.Tick(10);
        var save=session.ExportSave();var run=save.run;
        run.stage=stage;run.enemyHpMilli=run.enemyMaxHpMilli=enemyHp;run.enemyHitAtMs=-1;run.attackHitAtMs=-1;
        run.clockMs=0;run.attackReadyAtMs=0;run.randomSequence=new string[0];run.sequenceCursor=0;run.patternIndex=0;
        run.combatStats.attack=100;run.combatStats.maxHp=1000;run.playerHpMilli=1000000;
        run.combatStats.critChancePercent=0;run.combatStats.damageReductionPercent=0;
        setup?.Invoke(save);
        session=new GameSession(data,RoundTrip(save));session.SetPaused(false);return session;
    }
    static void ToNextHit(GameSession s, bool dodge=false)
    {
        var threat=s.EnemyThreats[0];double lead=threat.ImpactAtMs-s.ClockMs;
        if(dodge)
        {
            s.Tick((float)Math.Max(0,(lead-70)/1000));
            Check(s.Dodge(threat.Direction),"timed defense accepted");
            s.Tick(.071f);
        }
        else s.Tick((float)(lead/1000+.00001));
    }
    static void EffectsAndAcceptance()
    {
        foreach(var pair in new[]{new {Id="on",Damage=160},new {Id="dam",Damage=115},new {Id="rin",Damage=125}})
        {
            var s=Fighter(pair.Id);var events=new List<SessionEvent>();s.EventRaised+=events.Add;
            double hp=s.EnemyHp;Check(s.CanUseSkill&&s.UseSkill(),"ready skill "+pair.Id);
            Near(s.SkillCooldownRemainingMs,20000,"accepted cooldown");
            Check(!s.UseSkill()&&!s.Attack(),"pending strike rejects double input");
            s.Tick(.199f);Near(s.EnemyHp,hp,"no precontact damage");Near(s.SkillShieldMilli,0,"no precontact shield");Near(s.Stagger,0,"no precontact stagger");
            s.Tick(.0011f);Near(s.EnemyHp,hp-pair.Damage,"integer skill damage "+pair.Id);
            Near(s.SkillCooldownRemainingMs,19799.9,"combat cooldown");
            Check(events.Count(e=>e.Kind==SessionEventKind.AttackStarted&&e.SkillId==s.CurrentSkill.id)==1,"accepted event once");
            Check(events.Count(e=>e.Kind==SessionEventKind.EnemyDamaged&&e.SkillId==s.CurrentSkill.id)==1,"contact event once");
            s.Tick(0);Check(events.Count(e=>e.Kind==SessionEventKind.EnemyDamaged)==1,"zero tick no duplicate");
            s.SetPaused(true);double remaining=s.SkillCooldownRemainingMs;s.Tick(60);Check(!s.UseSkill(),"paused reject");Near(s.SkillCooldownRemainingMs,remaining,"pause freeze");s.SetPaused(false);
            s.Tick(.3f);Check(!s.UseSkill(),"cooldown refusal");Check(s.Attack(),"ordinary attack remains manual");s.Tick(.2f);
            Near(s.EnemyHp,hp-pair.Damage-100,"ordinary damage unchanged");
        }
        var cancelled=Fighter("dam");Check(cancelled.UseSkill()&&cancelled.Dodge(DodgeDirection.Back),"dodge cancels skill");cancelled.Tick(.25f);
        Near(cancelled.EnemyHp,10000,"cancel no hit");Near(cancelled.SkillShieldMilli,0,"cancel no shield");Near(cancelled.SkillCooldownRemainingMs,19750,"cancel no refund");
        var blocked=Fighter();Check(blocked.Attack(),"normal pending");Check(!blocked.UseSkill(),"skill rejected during normal");Near(blocked.SkillCooldownRemainingMs,0,"refusal free");
        var immune=Fighter("rin",setup:x=>x.run.immunityUntilMs=10000);immune.UseSkill();immune.Tick(.2f);Near(immune.Stagger,0,"immune no gain");
    }
    static void Shield()
    {
        foreach(int damage in new[]{30,120})
        {
            var s=Fighter("dam",incoming:damage);var events=new List<SessionEvent>();s.EventRaised+=events.Add;
            s.UseSkill();s.Tick(.2f);Near(s.SkillShieldMilli,80000,"8% HP shield");Near(s.SkillShieldRemainingMs,3000,"shield starts at contact");
            s=Restore(s);events.Clear();s.EventRaised+=events.Add;Near(s.SkillShieldMilli,80000,"shield save amount");
            ToNextHit(s);Near(s.PlayerHp,1000-Math.Max(0,damage-80),"residual damage");Near(s.SkillShieldMilli,0,"one-hit consumes whole shield");
            Check(events.Count(e=>e.Kind==SessionEventKind.SkillShieldAbsorbed)==1,"absorb event once");
            Near(events.Single(e=>e.Kind==SessionEventKind.SkillShieldAbsorbed).AbsorbedMilli,Math.Min(80,damage)*1000,"absorbed amount");
            Check(events.Count(e=>e.Kind==SessionEventKind.PlayerDamaged)==(damage>80?1:0),"only residual emits damage");
            double hp=s.PlayerHp;ToNextHit(s);Near(s.PlayerHp,hp-damage,"next hit unshielded");
        }
        var dodge=Fighter("dam");dodge.UseSkill();dodge.Tick(.2f);ToNextHit(dodge,true);Near(dodge.SkillShieldMilli,80000,"dodge preserves shield");
        var expiry=Fighter("dam",configure:c=>c.combat.singleTelegraphMs=3600);expiry.UseSkill();expiry.Tick(.2f);expiry.Tick(3);
        Near(expiry.SkillShieldMilli,0,"shield expires");Near(expiry.SkillShieldRemainingMs,0,"expiry timer zero");ToNextHit(expiry);Near(expiry.PlayerHp,970,"expired shield cannot absorb");
    }
    static void Stagger()
    {
        foreach(var pair in new[]{new {Stage=1,Gain=50},new {Stage=5,Gain=30},new {Stage=10,Gain=20}})
        {
            var s=Fighter("rin",pair.Stage);s.UseSkill();s.Tick(.2f);Near(s.Stagger,pair.Gain,"skill resistance "+pair.Stage);
        }
        var full=Fighter("rin",setup:x=>x.run.stagger=50);full.UseSkill();full.Tick(.2f);
        Check(full.IsStunReady&&!full.IsEnemyStunned,"skill fills stack for next hit");
        full.Tick(.3f);Check(full.Attack(),"next hit accepted");full.Tick(.2f);Check(full.IsEnemyStunned,"next hit triggers stun");
        var ready=Fighter("rin",setup:x=>x.run.stagger=100);ready.UseSkill();ready.Tick(.2f);Check(ready.IsEnemyStunned,"already full skill can trigger stun");
        var stunned=Fighter("rin",setup:x=>{x.run.stunUntilMs=5000;x.run.immunityUntilMs=6000;});stunned.UseSkill();stunned.Tick(.2f);Near(stunned.Stagger,0,"stunned no gain");
    }
    static void TimersAndRestore()
    {
        foreach(string id in new[]{"on","dam","rin"})
        {
            var s=Fighter(id);s.UseSkill();s.Tick(.1f);double hp=s.EnemyHp;
            s=Restore(s);Check(!s.ExportSave().run.pendingSkillAttack,"restore cancels pending marker");Near(s.SkillCooldownRemainingMs,19900,"restore retains spend");
            s.Tick(.2f);Near(s.EnemyHp,hp,"no replayed hit");Near(s.SkillShieldMilli,0,"no replayed shield");Near(s.Stagger,0,"no replayed gain");
        }
        var stage=Fighter(enemyHp:1000);stage.UseSkill();stage.Tick(.2f);Check(stage.Phase==SessionPhase.Transition,"killing contact transition");
        double remaining=stage.SkillCooldownRemainingMs;stage.Tick(10);Check(stage.Stage==2&&stage.Phase==SessionPhase.Combat,"advance stage");Near(stage.SkillCooldownRemainingMs,remaining,"travel/arrival freeze and carry");
        Check(stage.ReturnToLobby(),"retire");Check(stage.ContinueSettlement(),"settle");Check(stage.RetryRun(),"retry");Near(stage.SkillCooldownRemainingMs,0,"fresh retry ready");
        var old=Fighter();var save=old.ExportSave();save.run.skillCooldownRemainingMs=0;save.run.skillShieldRemainingMs=0;save.run.skillShieldMilli=0;
        var restored=new GameSession(old.Catalog,RoundTrip(save));restored.SetPaused(false);Check(restored.CanUseSkill,"old schema8 ready");
        foreach(double invalid in new[]{-1,double.NaN,double.PositiveInfinity,20001})
        {
            save=old.ExportSave();save.run.skillCooldownRemainingMs=invalid;bool rejected=false;
            try{new GameSession(old.Catalog,save);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"invalid duration rejected");
        }
        var paused=Fighter();paused.SetPaused(true);Check(!paused.UseSkill(),"pause free refusal");Near(paused.SkillCooldownRemainingMs,0,"pause refusal spends nothing");
        var cooldown=Fighter();cooldown.UseSkill();cooldown.Tick(.2f);
        // A harmless waiting opponent lets all20 combat seconds elapse without another attack.
        cooldown.Catalog.combat.singleTelegraphMs=30000;cooldown=Restore(cooldown);cooldown.Tick(19.8001f);
        Near(cooldown.SkillCooldownRemainingMs,0,"20-second cooldown ends");Check(cooldown.CanUseSkill,"ready after cooldown");
    }
    static void Story()
    {
        foreach(int stage in new[]{2,7,10,15,20})
        {
            var s=Fighter("dam",stage,1000);s.UseSkill();s.Tick(.2f);if(s.Phase==SessionPhase.Transition)s.Tick(10);
            Check(s.Phase==SessionPhase.StoryDialogue,"story after stage "+stage);
            Check(s.PendingRewardCount==(stage%10==0?1:0),"boss-only reward "+stage);
            long gold=s.Gold;double remaining=s.SkillCooldownRemainingMs;s.Tick(30);Near(s.SkillCooldownRemainingMs,remaining,"story freezes cooldown");
            Check(s.AdvanceStory(),"next page");int page=s.Story.LineIndex;s=Restore(s);Check(s.Story.LineIndex==page,"mid-story restore");Near(s.Gold,gold,"restore no duplicate gold");
            var scene=s.Catalog.FindStory(stage);
            while(s.Phase==SessionPhase.StoryDialogue)
            {
                var line=scene.lines[s.Story.LineIndex];
                if(line.speaker=="player")
                {Check(s.Story.Speaker==s.Catalog.GetCharacter("dam").displayName&&s.Story.PortraitId=="dam","player identity/portrait");Check(s.Story.Text==line.responses.Single(x=>x.characterId=="dam").text,"character response");}
                else Check(s.Story.PortraitId==line.portraitId,"npc portrait");
                s.AdvanceStory();
            }
            if(s.Phase==SessionPhase.Transition)s.Tick(10);
            Check(s.ExportSave().characters.Single(x=>x.id=="dam").seenStoryIds.Contains(scene.id),"character story recorded");
            if(stage==20)Check(s.Phase==SessionPhase.RunEnd&&s.PendingRewardCount==1,"final story then existing settlement");
            else Check(s.Stage==stage+1&&s.Phase==SessionPhase.Combat,"story continues next stage");
        }
        var seen=Fighter("on",2,1000,setup:x=>x.characters.Single(c=>c.id=="on").seenStoryIds.Add("story_encounter_2"));seen.Attack();seen.Tick(.2f);seen.Tick(10);
        Check(seen.Stage==3&&seen.Phase==SessionPhase.Combat,"seen encounter skipped");
    }
    static void FrontalDash()
    {
        var s=Fighter(stage:11);Check(s.Catalog.GetStage(11).randomPatterns.SequenceEqual(new[]{"frontal_dash"}),"single dash pool");
        Check(GameCatalog.RequiredDodge("frontal_dash")=="back","dash back direction");
        var threat=CombatGeometry.Threat("frontal_dash");Check(threat.Overlaps(CombatGeometry.Body(DodgeDirection.Back,false)),"dash hits home");
        Check(!threat.Overlaps(CombatGeometry.Body(DodgeDirection.Back,true)),"back exits dash");
        for(int i=0;i<12;i++){Check(s.EnemyThreats[0].PatternId=="frontal_dash","repeated dash "+i);ToNextHit(s,true);}
        Near(s.PlayerHp,1000,"all repeated dashes avoided");
        var data=Catalog();data.GetStage(1).randomPatterns=new[]{"frontal_dash"};Check(data.Validate().Any(x=>x.Contains("random pool")),"single-pool exception stage11 only");
    }
    static int Main(string[] args)
    {
        source=File.ReadAllText(args.Length>0?args[0]:"GameData/mvp-catalog.json");
        var tests=new Action[]{EffectsAndAcceptance,Shield,Stagger,TimersAndRestore,Story,FrontalDash};
        foreach(var test in tests){test();Console.WriteLine("PASS "+test.Method.Name);}
        Console.WriteLine("PASS "+assertions+" assertions; catalog="+Catalog().dataVersion);return 0;
    }
}
