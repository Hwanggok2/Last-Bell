using System;
using System.Collections.Generic;

namespace LastBell
{
    public sealed partial class GameSession
    {
        int CalculateRecovery(int stars,int level)
        {
            var a=catalog.adventure;double q=Number(catalog.growth.starScale[stars-1]);int b=Math.Max(0,(level-1)/10);
            return Rounded((Number(a.recoveryBase)+Number(a.recoveryLinear)*q*level)*Math.Pow(Number(a.recoveryBreakBase)+Number(a.recoveryBreakPerStar)*(stars-1),b));
        }
        static int[] FiveStats(int[] levels)
        {
            if(levels==null||levels.Length!=4)throw new InvalidOperationException("Legacy stat levels must contain four values.");
            var result=new int[5];Array.Copy(levels,result,4);return result;
        }
        void MigrateAdventureSave()
        {
            if(state.schemaVersion!=8)return;
            state.adventureNewAccount=false;state.companionSlots=1;state.comboPracticeCompleted=state.comboPracticeSkipped=false;
            state.companions=new List<CompanionProgressData>();
            foreach(var c in state.characters)
            {
                // Newly materialized characters already have the schema9 default array.
                if(c.statLevels.Length==4)c.statLevels=FiveStats(c.statLevels);
                else if(c.statLevels.Length!=5||c.statLevels[4]!=0)throw new InvalidOperationException("Invalid legacy stat array.");
                c.companionIds=new List<string>();c.reservations=new List<ServiceReservationData>();c.weaponRank=0;
            }
            if(state.run!=null)
            {
                var r=state.run;
                if(r.statLevels.Length==4)r.statLevels=FiveStats(r.statLevels);
                else if(r.statLevels.Length!=5||r.statLevels[4]!=0)throw new InvalidOperationException("Invalid legacy run stats.");
                r.companions=new List<RunCompanionData>();r.weaponRank=r.teaRecovery=0;r.blessing=r.guideApplied=false;r.comboPractice=ComboPracticeMode.None;
                r.skillRulesVersion=0;r.skillContactPending=false;r.skillContactRemainingMs=0;
                var computed=CalculateStats(r.characterId,r.statLevels,r.equippedRelicIds,r.appliedRelicLevels);
                r.combatStats.baseAttack=computed.baseAttack;r.combatStats.recovery=computed.recovery;
                if(r.story!=null)r.story.legacy=catalog.FindStory(r.story.sceneId,true)!=null;
            }
            var completed=new HashSet<string>();foreach(var c in state.characters)foreach(string id in c.seenStoryIds)completed.Add(id);
            var aliases=new Dictionary<string,string>{{"story_encounter_2","tori"},{"story_boss_10","tori"},{"story_encounter_7","rina"},{"story_encounter_15","barun"},{"story_boss_20","doto"}};
            foreach(var pair in aliases)if(completed.Contains(pair.Key))UnlockCompanion(pair.Value,false);
            string pending=state.run?.story==null?null:catalog.FindStory(state.run.story.sceneId,state.run.story.legacy)?.companionId;
            foreach(var d in catalog.companions)if(state.highestClearedStage>=d.joinStage&&d.id!=pending)UnlockCompanion(d.id,false);
            // An old read-only hint is not a completed action tutorial, including
            // saves made while already fighting Stage5. Preserve HP/time/cooldown.
            if(state.phase==SessionPhase.Combat&&state.run?.stage==5)
            {
                state.run.attackHitAtMs=state.run.enemyHitAtMs=-1;state.run.pendingSkillAttack=false;
                state.phase=SessionPhase.ComboHint;
            }
            state.schemaVersion=9;
        }
        void ValidateAdventureSave()
        {
            if(state.companionSlots<1||state.companionSlots>3)throw new InvalidOperationException("Invalid companion slots.");
            var unlocked=new HashSet<string>();
            foreach(var c in state.companions)
                if(c==null||catalog.FindCompanion(c.id)==null||!unlocked.Add(c.id)||c.level<1||c.level>CompanionLevelCap)throw new InvalidOperationException("Invalid companion progression.");
            if(CompanionSlots>1&&(!unlocked.Contains("barun")||state.highestClearedStage<catalog.adventure.slotBossRequirements[CompanionSlots-2]))throw new InvalidOperationException("Unmet companion slot requirements.");
            foreach(var c in state.characters)
            {
                if(c.weaponRank<0||c.weaponRank>3||c.companionIds.Count>CompanionSlots)throw new InvalidOperationException("Invalid weapon or formation capacity.");
                if(c.weaponRank>0&&(!unlocked.Contains("doto")||state.highestClearedStage<catalog.adventure.weaponBossRequirements[c.weaponRank-1]))throw new InvalidOperationException("Unmet weapon requirements.");
                var formation=new HashSet<string>();foreach(string id in c.companionIds)if(!unlocked.Contains(id)||!formation.Add(id))throw new InvalidOperationException("Invalid companion formation.");
                var services=new HashSet<string>();foreach(var r in c.reservations)
                    if(r==null||(r.serviceId!="travel_blessing"&&r.serviceId!="herbal_tea")||!services.Add(r.serviceId)||r.characterId!=c.id||(r.startStage!=1&&r.startStage!=11)||r.paidGold<0||!unlocked.Contains(ServiceCompanion(r.serviceId)))throw new InvalidOperationException("Invalid service reservation.");
            }
            var run=state.run;if(run==null)return;
            if(run.skillRulesVersion!=1||run.statLevels.Length!=5||run.combatStats.baseAttack<=0||run.combatStats.recovery<0||run.teaRecovery<0||run.weaponRank<0||run.weaponRank>3||run.companions.Count>CompanionSlots||!Enum.IsDefined(typeof(ComboPracticeMode),run.comboPractice)||!FiniteDuration(run.practiceRecoveryUntilMs))throw new InvalidOperationException("Invalid adventure run.");
            foreach(int level in run.statLevels)if(level<0||level>catalog.growth.maxLevel)throw new InvalidOperationException("Invalid run stat levels.");
            var live=new HashSet<string>();double dps=0;
            foreach(var c in run.companions)
            {
                var d=c==null?null:catalog.FindCompanion(c.id);
                if(d==null||!unlocked.Contains(c.id)||!live.Add(c.id)||c.level<1||c.level>CompanionLevel(c.id)||c.periodMs!=CompanionPeriod(c.id,c.level)||c.damageMilli<0||!FiniteDuration(c.remainingMs)||c.remainingMs>c.periodMs+catalog.adventure.slotPhaseMs*2||c.windingUp&&c.remainingMs>catalog.adventure.companionWindupMs+TimeEpsilonMs||
                    c.randomState==0||c.windingUp&&!c.skillReserved||c.reservedSkill&&!c.skillReserved||!FiniteDuration(c.damageFraction)||c.damageFraction>=1||!FiniteDuration(c.damageRemainder)||c.damageRemainder>=1)throw new InvalidOperationException("Invalid live companion.");
                dps+=CompanionExpectedDps(c);
            }
            if(dps>run.combatStats.baseAttack*1000d*catalog.adventure.partyDpsCapPercent/100+.5)throw new InvalidOperationException("Companion DPS exceeds cap.");
            if(run.comboPractice!=ComboPracticeMode.None&&(run.stage!=5||Phase!=SessionPhase.Combat||run.ended))throw new InvalidOperationException("Invalid guided practice state.");
            if(run.story!=null&&run.story.awaitingCompanionChoice)
            {
                var scene=catalog.FindStory(run.story.sceneId,run.story.legacy);
                if(scene==null||!CompanionUnlocked(scene.companionId)||run.story.reunion)throw new InvalidOperationException("Invalid companion invitation.");
            }
        }
    }
}
