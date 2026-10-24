using System;
using System.Collections.Generic;

namespace LastBell
{
    [Serializable] public sealed class CompanionDefinition
    {
        public string id, displayName, speciesId, jobId, attackName, serviceId, returnLine;
        public int joinStage, periodMs, damagePercent;
        public string skillId, skillName;
        public int skillDamagePercent, skillStaggerGain, skillHealMaxHpPercent;
    }
    [Serializable] public sealed class AdventureConfig
    {
        public float recoveryBase, recoveryLinear, recoveryBreakBase, recoveryBreakPerStar;
        public int recoveryCostPercent, companionMaxLevel, companionGrowthPercent, companionBreakPercent, partyDpsCapPercent;
        public int companionWindupMs, newEnemyGraceMs, slotPhaseMs, guideGoldPercent, blessingHpPercent, teaRecoveryPercent, servicePricePercent;
        public int[] slotGemCosts, slotBossRequirements, levelCapBosses, levelCaps, weaponAttackPercents, weaponBossRequirements;
        public long[] weaponGoldCosts;
        public int companionSkillChanceBasePercent, companionSkillChancePerLevel, companionSkillChanceCapPercent;
        public int companionPeriodReductionPerLevel, companionPeriodFloorPercent;
    }
    public sealed partial class GameCatalog
    {
        public CompanionDefinition FindCompanion(string id) => Array.Find(companions ?? new CompanionDefinition[0],x=>x.id==id);
        public long GetStatUpgradeCost(int index,int targetLevel) => index==4 ? checked((GetUpgradeCost(targetLevel)*adventure.recoveryCostPercent+99)/100) : GetUpgradeCost(targetLevel);
        public long GetCompanionUpgradeCost(int targetLevel) => targetLevel<2||targetLevel>adventure.companionMaxLevel?0:Math.Max(20,(GetUpgradeCost(targetLevel)+1)/2);
        internal void ValidateAdventure(List<string> errors,HashSet<string> characterIds)
        {
            Require(errors, adventure!=null,"adventure: required");
            if(adventure==null)return;
            var a=adventure;
            Require(errors,Positive(a.recoveryBase)&&Positive(a.recoveryLinear)&&Positive(a.recoveryBreakBase)&&Positive(a.recoveryBreakPerStar)&&a.recoveryCostPercent==50,"adventure: invalid recovery");
            Require(errors,a.companionMaxLevel==30&&a.companionGrowthPercent==3&&a.companionBreakPercent==25&&a.partyDpsCapPercent==24&&a.companionWindupMs==180&&a.newEnemyGraceMs==350&&a.slotPhaseMs==400,"adventure: invalid companion timing/growth");
            Require(errors,a.guideGoldPercent==3&&a.blessingHpPercent==5&&a.teaRecoveryPercent==15&&a.servicePricePercent==10,"adventure: invalid services");
            Require(errors,a.companionSkillChanceBasePercent==12&&a.companionSkillChancePerLevel==1&&a.companionSkillChanceCapPercent==45&&a.companionPeriodReductionPerLevel==1&&a.companionPeriodFloorPercent==70,"adventure: invalid skill growth");
            Require(errors,Same(a.slotGemCosts,4,8)&&Same(a.slotBossRequirements,10,30)&&Same(a.levelCapBosses,0,10,20)&&Same(a.levelCaps,10,20,30)&&Same(a.weaponAttackPercents,4,7,10)&&Same(a.weaponBossRequirements,0,30,40)&&a.weaponGoldCosts!=null&&a.weaponGoldCosts.Length==3&&Array.TrueForAll(a.weaponGoldCosts,x=>x>0),"adventure: invalid tiers");
            var ids=CheckIds(errors,companions,x=>x.id,"companions");
            Require(errors,ids.SetEquals(new[]{"tori","barun","doto","rina","momo"}),"companions: expected five stable IDs");
            string[] order={"tori","barun","doto","rina","momo"};int[] periods={6000,10000,8000,7000,6000};int[] percents={24,40,32,28,24};
            string[] services={"route_guide","pack_cart","weapon_craft","travel_blessing","herbal_tea"};
            string[] skillIds={"companion_tori_double_shot","companion_barun_ground_slam","companion_doto_flame_hammer","companion_rina_lightning","companion_momo_seed_bomb"};
            int[] skillDamage={180,170,200,180,170},stagger={0,8,0,10,0},heal={0,0,0,0,2};
            if(companions!=null)foreach(var c in companions)
            {
                if(c==null)continue;int i=Array.IndexOf(order,c.id);
                Require(errors,i>=0&&c.joinStage==2+i*10&&c.periodMs==periods[i]&&c.damagePercent==percents[i]&&c.serviceId==services[i]&&!string.IsNullOrWhiteSpace(c.displayName)&&!string.IsNullOrWhiteSpace(c.returnLine),"companions: invalid definition "+c.id);
                Require(errors,i>=0&&c.skillId==skillIds[i]&&!string.IsNullOrWhiteSpace(c.skillName)&&c.skillDamagePercent==skillDamage[i]&&c.skillStaggerGain==stagger[i]&&c.skillHealMaxHpPercent==heal[i],"companions: invalid skill "+c.id);
            }
            ValidateStoryTable(errors,stories,new[]{2,10,12,20},characterIds,ids,"stories");
            ValidateStoryTable(errors,plannedStories,new[]{22,32,42},characterIds,ids,"plannedStories");
            ValidateStoryTable(errors,legacyStories,new[]{2,7,10,15,20},characterIds,ids,"legacyStories");
        }
        static bool Same(int[] values,params int[] expected) => values!=null&&values.Length==expected.Length&&Array.TrueForAll(expected,x=>Array.IndexOf(values,x)==Array.IndexOf(expected,x));
        void ValidateStoryTable(List<string> errors,StoryDefinition[] table,int[] stagesExpected,HashSet<string> characterIds,HashSet<string> companionIds,string name)
        {
            CheckIds(errors,table,x=>x.id,name);var seen=new HashSet<int>();
            var portraits=new HashSet<string>{"on","dam","rin","tori","rina","barun","doto","momo","gib","unknown","player"};
            if(table!=null)foreach(var s in table)
            {
                if(s==null)continue;
                Require(errors,seen.Add(s.stage)&&Array.IndexOf(stagesExpected,s.stage)>=0&&(s.kind=="boss"||s.kind=="encounter")&&s.id=="story_"+(s.kind=="boss"?"boss_":"encounter_")+s.stage,name+": invalid identity");
                Require(errors,string.IsNullOrEmpty(s.companionId)||companionIds.Contains(s.companionId),name+": unknown companion");
                bool boss=s.stage%10==0;
                Require(errors,s.kind==(boss?"boss":"encounter")&&(boss?s.bossId==(s.stage==10?"bell_knight":"chain_jailer"):string.IsNullOrEmpty(s.bossId)&&!string.IsNullOrEmpty(s.companionId)),name+": invalid boss/encounter binding");
                if(!boss&&name!="legacyStories")Require(errors,FindCompanion(s.companionId)?.joinStage==s.stage,name+": encounter companion stage mismatch");
                Require(errors,s.lines!=null&&s.lines.Length>=2&&s.lines.Length<=8,name+": invalid pages");
                if(s.lines==null)continue;
                foreach(var line in s.lines)
                {
                    Require(errors,line!=null&&!string.IsNullOrWhiteSpace(line.text)&&!string.IsNullOrWhiteSpace(line.speaker)&&portraits.Contains(line.portraitId??"")&&(line.speaker=="player")== (line.portraitId=="player"),name+": invalid text/portrait");
                    if(line?.responses==null){errors.Add(name+": responses required");continue;}
                    var responses=new HashSet<string>();
                    foreach(var r in line.responses)Require(errors,r!=null&&characterIds.Contains(r.characterId??"")&&responses.Add(r.characterId)&&!string.IsNullOrWhiteSpace(r.text),name+": invalid response");
                    Require(errors,line.speaker!="player"||responses.SetEquals(characterIds),name+": all player responses required");
                }
            }
            Require(errors,seen.SetEquals(stagesExpected),name+": missing scene");
        }
    }
}
