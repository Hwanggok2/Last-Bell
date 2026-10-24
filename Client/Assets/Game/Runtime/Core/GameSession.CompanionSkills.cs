using System;
using System.Collections.Generic;

namespace LastBell
{
    public sealed partial class GameSession
    {
        string CompanionSkillDescription(CompanionDefinition d)=>"일반 공격의 "+d.skillDamagePercent+"% 피해"+
            (d.skillStaggerGain>0?" · 기절 누적 +"+d.skillStaggerGain+" (적 저항 적용)":"")+
            (d.skillHealMaxHpPercent>0?" · 최대 HP "+d.skillHealMaxHpPercent+"% 회복":"");
        int CompanionSkillChance(int level)=>Math.Min(catalog.adventure.companionSkillChanceCapPercent,
            catalog.adventure.companionSkillChanceBasePercent+catalog.adventure.companionSkillChancePerLevel*(level-1));
        int CompanionPeriod(string id,int level)=>catalog.FindCompanion(id).periodMs*
            Math.Max(catalog.adventure.companionPeriodFloorPercent,100-catalog.adventure.companionPeriodReductionPerLevel*(level-1))/100;
        double CompanionSkillWeight(string id,int level)=>1+CompanionSkillChance(level)/100d*(catalog.FindCompanion(id).skillDamagePercent/100d-1);
        double CompanionExpectedDps(RunCompanionData c)=>(c.damageMilli+c.damageFraction)*CompanionSkillWeight(c.id,c.level)*1000/c.periodMs;
        double CompanionRawDps(string id,int level)=>catalog.FindCompanion(id).damagePercent/100d*CompanionGrowth(level)*CompanionSkillWeight(id,level)*1000/CompanionPeriod(id,level);
        double PreviewCompanionBaseMilli(string id,int level,int attack)
        {
            var party=CurrentCharacter.companionIds.Contains(id)?CurrentCharacter.companionIds:new List<string>{id};double sum=0;
            foreach(string member in party)sum+=CompanionRawDps(member,member==id?level:CompanionLevel(member));
            double q=sum<=0?1:Math.Min(1,catalog.adventure.partyDpsCapPercent/100d/sum);
            return attack*1000d*catalog.FindCompanion(id).damagePercent/100d*CompanionGrowth(level)*q;
        }
        double PreviewCompanionDps(string id,int level,int attack)=>PreviewCompanionBaseMilli(id,level,attack)*CompanionSkillWeight(id,level)*1000/CompanionPeriod(id,level);
        int CompanionSeed(string id)
        {
            uint seed=unchecked((uint)(state.run.runId*747796405L+state.randomState));
            foreach(char c in state.run.characterId+":"+id)seed=unchecked((seed^c)*16777619u);
            return unchecked((int)(seed==0?362436069u:seed));
        }
        void ReserveCompanionSkill(RunCompanionData c)
        {
            if(c.skillReserved)return;
            uint value=unchecked((uint)c.randomState);
            value^=value<<13;value^=value>>17;value^=value<<5;c.randomState=unchecked((int)value);
            c.reservedSkill=value/4294967296d<CompanionSkillChance(c.level)/100d;c.skillReserved=true;
        }
        void ApplyCompanionSkill(CompanionDefinition d)
        {
            var run=state.run;if(run.playerHpMilli<=0)return;
            if(d.skillStaggerGain>0&&run.enemyHpMilli>0&&!IsEnemyStunned&&!IsStaggerImmune)
            {
                string kind=catalog.GetStage(run.stage).kind;
                double resistance=kind=="boss"?Number(catalog.combat.bossStaggerMultiplier):kind=="miniboss"?Number(catalog.combat.minibossStaggerMultiplier):1;
                run.stagger=Math.Min(catalog.combat.staggerThreshold,run.stagger+d.skillStaggerGain*resistance);
            }
            if(d.skillHealMaxHpPercent>0)
            {
                long heal=Math.Min(Milli(Math.Max(1,Rounded(run.combatStats.maxHp*d.skillHealMaxHpPercent/100d))),Math.Max(0,run.combatStats.maxHpMilli-run.playerHpMilli));
                run.playerHpMilli+=heal;
                if(heal>0)Emit(SessionEventKind.PlayerRecovered,companionId:d.id,skillId:d.skillId,recoveryMilli:heal);
            }
        }
        void MigrateIndependentSkillSave()
        {
            var run=state.run;if(run==null||run.skillRulesVersion!=0)return;
            if(run.skillContactPending||run.skillContactRemainingMs!=0)throw new InvalidOperationException("Legacy run contains new skill reservation.");
            foreach(var c in run.companions)
            {
                var d=catalog.FindCompanion(c.id);
                if(d==null||c.periodMs!=d.periodMs||c.level<1||c.level>catalog.adventure.companionMaxLevel||c.damageMilli<0||
                    !FiniteDuration(c.remainingMs)||c.remainingMs>c.periodMs+catalog.adventure.slotPhaseMs*2||c.windingUp&&c.remainingMs>catalog.adventure.companionWindupMs+TimeEpsilonMs||
                    c.randomState!=0||c.skillReserved||c.reservedSkill||c.damageFraction!=0||c.damageRemainder!=0)
                    throw new InvalidOperationException("Invalid legacy companion snapshot.");
                int period=CompanionPeriod(c.id,c.level);
                if(!c.windingUp)c.remainingMs=c.remainingMs*period/c.periodMs;
                c.periodMs=period;c.randomState=CompanionSeed(c.id);c.skillReserved=c.windingUp;
            }
            // Old skill was the ordinary attack reservation; keep its spent cooldown,
            // cancel its contact as previous saves did, and never turn it into two hits.
            if(run.pendingSkillAttack)run.attackHitAtMs=-1;
            run.pendingSkillAttack=false;run.skillRulesVersion=1;RecalculateCompanionDamage();
        }
    }
}
