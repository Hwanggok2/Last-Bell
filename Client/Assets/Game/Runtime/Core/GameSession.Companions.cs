using System;
using System.Collections.Generic;

namespace LastBell
{
    public sealed partial class GameSession
    {
        public int CompanionSlots=>state.companionSlots;
        public int CompanionLevelCap {get{int i=Array.FindLastIndex(catalog.adventure.levelCapBosses,x=>state.highestClearedStage>=x);return catalog.adventure.levelCaps[Math.Max(0,i)];}}
        public int NextCompanionSlotCost=>CompanionSlots>=3?0:catalog.adventure.slotGemCosts[CompanionSlots-1];
        public bool CanExpandCompanionSlots=>Phase==SessionPhase.Lobby&&CompanionSlots<3&&CompanionUnlocked("barun")&&state.highestClearedStage>=catalog.adventure.slotBossRequirements[CompanionSlots-1]&&Fragments>=NextCompanionSlotCost;
        public string CompanionSlotLockReason=>CompanionSlots>=3?"최대 자리입니다.":!CompanionUnlocked("barun")?"바룬을 만나면 열려요":state.highestClearedStage<catalog.adventure.slotBossRequirements[CompanionSlots-1]?"Stage "+catalog.adventure.slotBossRequirements[CompanionSlots-1]+" 보스 처치 필요":Fragments<NextCompanionSlotCost?"보석이 부족해요":Phase!=SessionPhase.Lobby?"여관에서 변경할 수 있어요":"";
        public int WeaponRank=>CurrentCharacter.weaponRank;
        public long NextWeaponCost=>WeaponRank>=3?0:catalog.adventure.weaponGoldCosts[WeaponRank];
        public bool CanCraftWeapon=>Phase==SessionPhase.Lobby&&CompanionUnlocked("doto")&&WeaponRank<3&&state.highestClearedStage>=catalog.adventure.weaponBossRequirements[WeaponRank]&&Gold>=NextWeaponCost;
        public bool CanCraftWithoutGold=>Phase==SessionPhase.Lobby&&CompanionUnlocked("doto")&&WeaponRank<3&&state.highestClearedStage>=catalog.adventure.weaponBossRequirements[WeaponRank];
        public string WeaponLockReason=>WeaponRank>=3?"최대 강화입니다.":!CompanionUnlocked("doto")?"도토를 만나면 열려요":state.highestClearedStage<catalog.adventure.weaponBossRequirements[WeaponRank]?"Stage "+catalog.adventure.weaponBossRequirements[WeaponRank]+" 보스 처치 필요":Gold<NextWeaponCost?"골드가 부족해요":Phase!=SessionPhase.Lobby?"여관에서 제작할 수 있어요":"";
        public IReadOnlyList<string> EquippedCompanionIds=>CurrentCharacter.companionIds.AsReadOnly();
        public IReadOnlyList<int> VisitedRegionStarts
        {
            get{var result=new List<int>();if(CompanionUnlocked("tori")){int highest=Math.Max(state.highestClearedStage,state.run?.stage??0);for(int start=1;start<=Math.Min(highest,catalog.mvpLastStage);start+=10)result.Add(start);}return result.AsReadOnly();}
        }
        public IReadOnlyList<CompanionView> Companions
        {
            get
            {
                var list=new List<CompanionView>();var stats=BuildLobbyStats();
                foreach(var d in catalog.companions)
                {
                    int level=CompanionLevel(d.id);long cost=level>0&&level<CompanionLevelCap?catalog.GetCompanionUpgradeCost(level+1):0;
                    int previewLevel=Math.Max(1,level),nextLevel=Math.Min(CompanionLevelCap,previewLevel+1);
                    list.Add(new CompanionView{Id=d.id,Name=d.displayName,ServiceId=d.serviceId,AttackName=d.attackName,Level=level,LevelCap=CompanionLevelCap,JoinStage=d.joinStage,EquippedSlot=CurrentCharacter.companionIds.IndexOf(d.id),PeriodMs=CompanionPeriod(d.id,previewLevel),
                        SkillId=d.skillId,SkillName=d.skillName,SkillDescription=CompanionSkillDescription(d),SkillChancePercent=CompanionSkillChance(previewLevel),NextSkillChancePercent=CompanionSkillChance(nextLevel),SkillDamagePercent=d.skillDamagePercent,NextPeriodMs=CompanionPeriod(d.id,nextLevel),
                        ExpectedDpsMilli=level>0?PreviewCompanionDps(d.id,level,stats.baseAttack):0,NextExpectedDpsMilli=level>0?PreviewCompanionDps(d.id,nextLevel,stats.baseAttack):0,
                        DamageMilli=level>0?PreviewCompanionDamage(d.id,level,stats.baseAttack):0,NextDamageMilli=level>0?PreviewCompanionDamage(d.id,Math.Min(CompanionLevelCap,level+1),stats.baseAttack):0,UpgradeCost=cost,CanUpgrade=Phase==SessionPhase.Lobby&&cost>0&&Gold>=cost});
                }
                return list.AsReadOnly();
            }
        }
        public IReadOnlyList<RunCompanionView> RunCompanions
        {
            get
            {
                var list=new List<RunCompanionView>();if(state.run!=null)for(int i=0;i<state.run.companions.Count;i++)
                {var c=state.run.companions[i];var d=catalog.FindCompanion(c.id);list.Add(new RunCompanionView{Id=c.id,Level=c.level,Slot=i,DamageMilli=c.damageMilli,PeriodMs=c.periodMs,RemainingMs=c.remainingMs,IsWindingUp=c.windingUp,SkillId=d.skillId,SkillChancePercent=CompanionSkillChance(c.level),PendingSkillId=c.skillReserved&&c.reservedSkill?d.skillId:null,ExpectedDpsMilli=CompanionExpectedDps(c)});}
                return list.AsReadOnly();
            }
        }
        public int CompanionLevel(string id)=>state.companions.Find(x=>x.id==id)?.level??0;
        public bool CompanionUnlocked(string id)=>CompanionLevel(id)>0;
        double CompanionGrowth(int level)=>(1+catalog.adventure.companionGrowthPercent/100d*(level-1))*Math.Pow(1+catalog.adventure.companionBreakPercent/100d,(level-1)/10);
        long PreviewCompanionDamage(string id,int level,int attack)
        {
            return (long)Math.Floor(PreviewCompanionBaseMilli(id,level,attack));
        }

        public CompanionUpgradeQuote QuoteCompanionUpgrade(string id)
        {
            if(catalog.FindCompanion(id)==null)return null;int level=CompanionLevel(id);long cost=level>0&&level<CompanionLevelCap?catalog.GetCompanionUpgradeCost(level+1):0;
            return new CompanionUpgradeQuote{Id=id,FromLevel=level,ToLevel=level+1,Cost=cost,WalletVersion=state.walletVersion,CanPurchase=Phase==SessionPhase.Lobby&&cost>0&&Gold>=cost};
        }
        public bool UpgradeCompanion(string id)=>PurchaseCompanionUpgrade(QuoteCompanionUpgrade(id));
        public bool PurchaseCompanionUpgrade(CompanionUpgradeQuote quote)
        {
            if(Phase!=SessionPhase.Lobby||quote==null||quote.WalletVersion!=state.walletVersion)return false;
            var current=QuoteCompanionUpgrade(quote.Id);
            if(current==null||!current.CanPurchase||current.FromLevel!=quote.FromLevel||current.ToLevel!=quote.ToLevel||current.Cost!=quote.Cost)return false;
            state.gold-=current.Cost;state.walletVersion++;state.companions.Find(x=>x.id==quote.Id).level++;Publish();return true;
        }
        public bool EquipCompanion(string id,int slot)
        {
            if(Phase!=SessionPhase.Lobby||!CompanionUnlocked(id)||slot<0||slot>=CompanionSlots||slot>CurrentCharacter.companionIds.Count)return false;
            var ids=CurrentCharacter.companionIds;int old=ids.IndexOf(id);if(old==slot)return false;
            if(old>=0){if(slot==ids.Count)return false;string other=ids[slot];ids[slot]=id;ids[old]=other;}
            else if(slot==ids.Count)ids.Add(id);else ids[slot]=id;
            Publish();return true;
        }
        public bool RemoveCompanion(int slot)
        {if(Phase!=SessionPhase.Lobby||slot<0||slot>=CurrentCharacter.companionIds.Count)return false;CurrentCharacter.companionIds.RemoveAt(slot);Publish();return true;}
        public bool UnequipCompanion(int slot)=>RemoveCompanion(slot);
        public CompanionSlotQuote QuoteCompanionSlot()=>new CompanionSlotQuote{TargetSlots=CompanionSlots+1,Cost=NextCompanionSlotCost,RequiredBossStage=CompanionSlots>=3?0:catalog.adventure.slotBossRequirements[CompanionSlots-1],WalletVersion=state.walletVersion,CanPurchase=CanExpandCompanionSlots};
        public bool ExpandCompanionSlots()=>PurchaseCompanionSlot(QuoteCompanionSlot());
        public bool PurchaseCompanionSlot(CompanionSlotQuote quote)
        {if(quote==null||quote.TargetSlots!=CompanionSlots+1||quote.Cost!=NextCompanionSlotCost||quote.WalletVersion!=state.walletVersion||!CanExpandCompanionSlots)return false;state.fragments-=NextCompanionSlotCost;state.companionSlots++;state.walletVersion++;Publish();return true;}

        public long ServicePrice(string id)=>id=="weapon_craft"?NextWeaponCost:id=="travel_blessing"||id=="herbal_tea"?checked((catalog.economy.normalGoldPerStage[CheckpointStartStage]*catalog.adventure.servicePricePercent+99)/100):0;
        public bool IsServiceReserved(string id)=>CurrentCharacter.reservations.Exists(x=>x.serviceId==id);
        public long ReservedServicePaidGold(string id)=>CurrentCharacter.reservations.Find(x=>x.serviceId==id)?.paidGold??0;
        public string ServiceName(string id)=>id=="route_guide"?"길 안내":id=="pack_cart"?"짐마차":id=="weapon_craft"?"무기 제작":id=="travel_blessing"?"여행의 축복":id=="herbal_tea"?"약초차":"";
        public string ServiceEffect(string id)
        {
            var a=catalog.adventure;
            switch(id)
            {
                case "route_guide":return "클리어 골드 +"+a.guideGoldPercent+"% · 지역 정보";
                case "pack_cart":return "동료 자리 "+CompanionSlots+" / 3";
                case "weapon_craft":return "공격력 +"+(WeaponRank>0?a.weaponAttackPercents[WeaponRank-1]:0)+"%"+(WeaponRank<3?" → +"+a.weaponAttackPercents[WeaponRank]+"%":"");
                case "travel_blessing":return "최대 HP +"+a.blessingHpPercent+"%";
                case "herbal_tea":return "클리어 회복량 +"+a.teaRecoveryPercent+"%";
                default:return "";
            }
        }
        public string ServiceDuration(string id)=>id=="travel_blessing"||id=="herbal_tea"?"다음 출전부터 원정 종료까지":id=="weapon_craft"?"캐릭터 영구":"계정 영구";
        string ServiceCompanion(string id)=>id=="weapon_craft"?"doto":id=="travel_blessing"?"rina":id=="herbal_tea"?"momo":id=="route_guide"?"tori":id=="pack_cart"?"barun":null;
        public bool CanReserveService(string id)=>Phase==SessionPhase.Lobby&&(id=="travel_blessing"||id=="herbal_tea")&&CompanionUnlocked(ServiceCompanion(id))&&!IsServiceReserved(id)&&Gold>=ServicePrice(id);
        public ServiceQuote QuoteService(string id)
        {
            if(ServiceCompanion(id)==null)return null;
            return new ServiceQuote{Id=id,CharacterId=CurrentCharacterId,StartStage=CheckpointStartStage,Rank=WeaponRank,Cost=ServicePrice(id),WalletVersion=state.walletVersion,Unlocked=CompanionUnlocked(ServiceCompanion(id)),Reserved=IsServiceReserved(id),CanPurchase=id=="weapon_craft"?CanCraftWeapon:CanReserveService(id)};
        }
        public bool CraftWeapon()=>PurchaseService(QuoteService("weapon_craft"));
        public bool ReserveService(string id)=>PurchaseService(QuoteService(id));
        public bool PurchaseService(ServiceQuote quote)
        {
            if(Phase!=SessionPhase.Lobby||quote==null||quote.CharacterId!=CurrentCharacterId||quote.WalletVersion!=state.walletVersion)return false;
            var current=QuoteService(quote.Id);
            if(current==null||!current.CanPurchase||current.Cost!=quote.Cost||current.Rank!=quote.Rank||current.StartStage!=quote.StartStage)return false;
            state.gold-=current.Cost;state.walletVersion++;
            if(quote.Id=="weapon_craft")CurrentCharacter.weaponRank++;
            else CurrentCharacter.reservations.Add(new ServiceReservationData{serviceId=quote.Id,characterId=CurrentCharacterId,startStage=CheckpointStartStage,paidGold=current.Cost});
            Emit(SessionEventKind.ServicePurchased,serviceId:quote.Id);Publish();return true;
        }
        public bool CancelService(string id)
        {
            if(Phase!=SessionPhase.Lobby)return false;var reservation=CurrentCharacter.reservations.Find(x=>x.serviceId==id);if(reservation==null)return false;
            state.gold=checked(state.gold+reservation.paidGold);state.walletVersion++;CurrentCharacter.reservations.Remove(reservation);Publish();return true;
        }
        public bool CancelReservation(string id)=>CancelService(id);
        void RefundMovedReservations()
        {
            foreach(var c in state.characters)
            {
                int start=c.checkpoint>=10&&catalog.mvpLastStage>=11?11:1;
                foreach(var r in c.reservations.ToArray())if(r.startStage!=start){state.gold=checked(state.gold+r.paidGold);state.walletVersion++;c.reservations.Remove(r);}
            }
        }
        bool UnlockCompanion(string id,bool emit=true)
        {
            if(string.IsNullOrEmpty(id)||catalog.FindCompanion(id)==null||CompanionUnlocked(id))return false;
            state.companions.Add(new CompanionProgressData{id=id,level=1});if(id=="tori")ApplyToriGuide();
            if(emit)Emit(SessionEventKind.CompanionUnlocked,companionId:id);return true;
        }
        void ApplyToriGuide()
        {
            var run=state.run;if(run==null||run.ended||run.guideApplied)return;
            run.guideApplied=true;run.combatStats.goldBonusPercent=Math.Min(Rounded(Number(catalog.combat.goldLuckBonusCap)*100),run.combatStats.goldBonusPercent+catalog.adventure.guideGoldPercent);
        }
        void SnapshotCompanion(string id)
        {
            var run=state.run;var d=catalog.FindCompanion(id);int slot=run.companions.Count;
            int level=CompanionLevel(id),period=CompanionPeriod(id,level);
            run.companions.Add(new RunCompanionData{id=id,level=level,periodMs=period,remainingMs=period+slot*catalog.adventure.slotPhaseMs,randomState=CompanionSeed(id)});RecalculateCompanionDamage();
        }
        void RecalculateCompanionDamage()
        {
            var run=state.run;double total=0;
            foreach(var c in run.companions)total+=CompanionRawDps(c.id,c.level);
            double q=total<=0?1:Math.Min(1,catalog.adventure.partyDpsCapPercent/100d/total);
            foreach(var c in run.companions){double raw=run.combatStats.baseAttack*1000d*catalog.FindCompanion(c.id).damagePercent/100d*CompanionGrowth(c.level)*q;c.damageMilli=(long)Math.Floor(raw);c.damageFraction=raw-c.damageMilli;}
        }
        void PrepareCompanionCombat()
        {foreach(var c in state.run.companions){c.windingUp=false;c.remainingMs=Math.Max(c.remainingMs,catalog.adventure.newEnemyGraceMs);}}
        void CancelCompanionWindups(){if(state.run!=null)foreach(var c in state.run.companions)c.windingUp=false;}
        bool CompanionsTicking=>Phase==SessionPhase.Combat&&!GuidedPractice&&practiceBackupState==null&&state.run.playerHpMilli>0;
        double NextCompanionEvent()
        {
            if(!CompanionsTicking)return double.PositiveInfinity;double next=double.PositiveInfinity;
            foreach(var c in state.run.companions)next=Math.Min(next,ClockMs+Math.Max(0,c.remainingMs-(c.windingUp?0:catalog.adventure.companionWindupMs)));
            return next;
        }
        void AdvanceCompanionClocks(double elapsed){if(CompanionsTicking)foreach(var c in state.run.companions)c.remainingMs=Math.Max(0,c.remainingMs-elapsed);}
        void ResolveCompanionEvents()
        {
            if(!CompanionsTicking)return;
            foreach(var c in state.run.companions)
            {
                if(!CompanionsTicking||state.run.enemyHpMilli<=0)break;
                var definition=catalog.FindCompanion(c.id);
                if(!c.windingUp&&c.remainingMs<=catalog.adventure.companionWindupMs+TimeEpsilonMs){ReserveCompanionSkill(c);c.windingUp=true;Emit(SessionEventKind.CompanionAttackStarted,companionId:c.id,skillId:c.reservedSkill?definition.skillId:null);}
                if(c.remainingMs>TimeEpsilonMs)continue;
                bool special=c.reservedSkill;c.windingUp=false;c.remainingMs=c.periodMs;c.skillReserved=c.reservedSkill=false;
                double raw=(c.damageMilli+c.damageFraction)*(special?definition.skillDamagePercent/100d:1)+c.damageRemainder;
                long rounded=(long)Math.Floor(raw);c.damageRemainder=raw-rounded;
                long damage=Math.Min(rounded,state.run.enemyHpMilli);state.run.enemyHpMilli-=damage;double before=state.run.stagger;
                if(special&&damage>0)ApplyCompanionSkill(definition);
                Emit(SessionEventKind.CompanionDamaged,damage,companionId:c.id,skillId:special?definition.skillId:null,staggerBefore:before,staggerGained:state.run.stagger-before);
                if(state.run.enemyHpMilli<=0){DefeatEnemy();break;}
            }
        }
    }
}
