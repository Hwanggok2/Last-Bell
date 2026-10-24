using System;
using System.Collections.Generic;
namespace LastBell
{
    public enum ComboPracticeMode { None, Guided, AwaitingInput, Recovery, RealTrial }
    [Serializable] public sealed class CompanionProgressData
    { public string id;public int level;internal CompanionProgressData Copy()=>(CompanionProgressData)MemberwiseClone(); }
    [Serializable] public sealed class ServiceReservationData
    { public string serviceId,characterId;public int startStage;public long paidGold;internal ServiceReservationData Copy()=>(ServiceReservationData)MemberwiseClone(); }
    [Serializable] public sealed class RunCompanionData
    { public string id;public int level,periodMs;public long damageMilli;public double remainingMs,damageFraction,damageRemainder;public bool windingUp;public int randomState;public bool skillReserved, reservedSkill;internal RunCompanionData Copy()=>(RunCompanionData)MemberwiseClone(); }
    public sealed class CompanionView
    {
        public string Id {get;internal set;} public string Name {get;internal set;} public string ServiceId {get;internal set;} public string AttackName {get;internal set;}
        public int Level {get;internal set;} public int LevelCap {get;internal set;} public int JoinStage {get;internal set;}
        public int EquippedSlot {get;internal set;} public int PeriodMs {get;internal set;} public long DamageMilli {get;internal set;}
        public long UpgradeCost {get;internal set;} public bool CanUpgrade {get;internal set;} public bool Unlocked=>Level>0;
        public long NextDamageMilli {get;internal set;}
        public string SkillId {get;internal set;} public string SkillName {get;internal set;} public string SkillDescription {get;internal set;}
        public int SkillChancePercent {get;internal set;} public int NextSkillChancePercent {get;internal set;}
        public int SkillDamagePercent {get;internal set;} public int NextPeriodMs {get;internal set;}
        public double ExpectedDpsMilli {get;internal set;} public double NextExpectedDpsMilli {get;internal set;}
    }
    public sealed class RunCompanionView
    {
        public string Id {get;internal set;} public int Level {get;internal set;} public int Slot {get;internal set;}
        public int PeriodMs {get;internal set;} public long DamageMilli {get;internal set;} public double RemainingMs {get;internal set;}
        public bool IsWindingUp {get;internal set;}
        public string SkillId {get;internal set;} public string PendingSkillId {get;internal set;}
        public int SkillChancePercent {get;internal set;} public double ExpectedDpsMilli {get;internal set;}
    }
    public sealed class CompanionUpgradeQuote
    {public string Id {get;internal set;} public int FromLevel {get;internal set;} public int ToLevel {get;internal set;} public long Cost {get;internal set;} public long WalletVersion {get;internal set;} public bool CanPurchase {get;internal set;} }
    public sealed class ServiceQuote
    {public string Id {get;internal set;} public string CharacterId {get;internal set;} public int Rank {get;internal set;} public int StartStage {get;internal set;} public long Cost {get;internal set;} public long WalletVersion {get;internal set;} public bool Unlocked {get;internal set;} public bool Reserved {get;internal set;} public bool CanPurchase {get;internal set;} }
    public sealed class CompanionSlotQuote
    {public int TargetSlots {get;internal set;} public int Cost {get;internal set;} public int RequiredBossStage {get;internal set;} public long WalletVersion {get;internal set;} public bool CanPurchase {get;internal set;} }
    public sealed class ComboPracticeView
    {
        public bool Active {get;internal set;} public bool IsReplay {get;internal set;} public bool AwaitingInput {get;internal set;}
        public bool IsRealTrial {get;internal set;} public int Step {get;internal set;} public DodgeDirection RequiredDirection {get;internal set;} public string Message {get;internal set;}
    }
}
