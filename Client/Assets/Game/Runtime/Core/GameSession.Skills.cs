using System;

namespace LastBell
{
    public sealed partial class GameSession
    {
        public SkillDefinition CurrentSkill => catalog.FindSkill(CurrentCharacterId);
        public double SkillCooldownRemainingMs => state.run?.skillCooldownRemainingMs ?? 0;
        public double SkillShieldRemainingMs => state.run?.skillShieldRemainingMs ?? 0;
        public long SkillShieldMilli => state.run?.skillShieldMilli ?? 0;
        public bool SkillContactPending => state.run?.skillContactPending ?? false;
        public double SkillContactRemainingMs => state.run?.skillContactRemainingMs ?? 0;
        public bool CanUseSkill => state.phase == SessionPhase.Combat && CurrentSkill != null && !paused &&
            !GuidedPractice && practiceBackupState==null && state.run!=null && !state.run.ended &&
            state.run.playerHpMilli>0 && state.run.enemyHpMilli>0 && !SkillContactPending && SkillCooldownRemainingMs <= TimeEpsilonMs;

        private bool BeginSkill()
        {
            if(!CanUseSkill)return false;
            var run=state.run;run.skillCooldownRemainingMs=CurrentSkill.cooldownMs;
            run.skillContactPending=true;run.skillContactRemainingMs=CurrentSkill.contactDelayMs;
            Emit(SessionEventKind.PlayerSkillStarted,skillId:CurrentSkill.id);Publish();return true;
        }
        private void CancelSkillContact()
        {if(state.run!=null){state.run.skillContactPending=false;state.run.skillContactRemainingMs=0;}}
        private void ResolveSkillContact()
        {
            var run=state.run;
            if(Phase!=SessionPhase.Combat||!run.skillContactPending||run.skillContactRemainingMs>TimeEpsilonMs)return;
            CancelSkillContact();
            if(run.playerHpMilli<=0||run.enemyHpMilli<=0||run.ended||GuidedPractice||practiceBackupState!=null)return;
            var skill=CurrentSkill;long damage=Math.Min(run.enemyHpMilli,Milli(Math.Max(1,Rounded(run.combatStats.attack*skill.damagePercent/100d))));
            run.enemyHpMilli-=damage;double before=run.stagger;
            if(damage>0)ApplySkillContact(skill);
            Emit(SessionEventKind.PlayerSkillContact,damage,skillId:skill.id,staggerBefore:before,staggerGained:run.stagger-before);
            if(run.enemyHpMilli<=0)DefeatEnemy();
        }

        private bool CanBeginAttack => !paused && !GuidedPractice && practiceBackupState==null && state.run != null &&
            (state.phase == SessionPhase.Combat || state.phase == SessionPhase.TutorialAttack) &&
            state.run.attackHitAtMs < 0 && ClockMs + TimeEpsilonMs >= state.run.attackReadyAtMs &&
            (state.phase != SessionPhase.Combat || CanPerfectCounter ||
                ClockMs - state.run.dodgeStartMs + TimeEpsilonMs >= catalog.combat.dodgeActiveMs);

        // Tick at every chronological combat deadline so a shield created on contact
        // cannot lose time from before it existed. Visual transitions never call this.
        private void MoveCombatClock(double target)
        {
            var run = state.run;
            double elapsed = Math.Max(0, target - run.clockMs);
            AdvanceCompanionClocks(elapsed);
            if (state.phase == SessionPhase.Combat && !GuidedPractice && practiceBackupState==null)
            {
                run.skillCooldownRemainingMs = Math.Max(0, run.skillCooldownRemainingMs - elapsed);
                if(run.skillContactPending)run.skillContactRemainingMs=Math.Max(0,run.skillContactRemainingMs-elapsed);
                run.skillShieldRemainingMs = Math.Max(0, run.skillShieldRemainingMs - elapsed);
                if (run.skillShieldRemainingMs <= TimeEpsilonMs)
                {
                    run.skillShieldRemainingMs = 0;
                    run.skillShieldMilli = 0;
                }
            }
            run.clockMs += elapsed;
        }

        private void ApplySkillContact(SkillDefinition skill)
        {
            var run = state.run;
            if (skill == null || run.playerHpMilli <= 0) return;
            if (skill.shieldMaxHpPercent > 0)
            {
                run.skillShieldMilli = Milli(Math.Max(1, Rounded(run.combatStats.maxHp * skill.shieldMaxHpPercent / 100d)));
                run.skillShieldRemainingMs = skill.shieldDurationMs;
            }
            if (skill.staggerGain > 0 && run.enemyHpMilli > 0 && !IsEnemyStunned && !IsStaggerImmune)
            {
                string kind = catalog.GetStage(run.stage).kind;
                double resistance = kind == "boss" ? Number(catalog.combat.bossStaggerMultiplier) :
                    kind == "miniboss" ? Number(catalog.combat.minibossStaggerMultiplier) : 1;
                run.stagger = Math.Min(catalog.combat.staggerThreshold, run.stagger + skill.staggerGain * resistance);
            }
        }

        private long AbsorbSkillShield(long incoming, DodgeDirection direction)
        {
            var run = state.run;
            if (run.skillShieldMilli <= 0 || run.skillShieldRemainingMs <= TimeEpsilonMs) return incoming;
            long absorbed = Math.Min(incoming, run.skillShieldMilli);
            run.skillShieldMilli = 0;
            run.skillShieldRemainingMs = 0;
            Emit(SessionEventKind.SkillShieldAbsorbed, direction: direction, skillId: CurrentSkill?.id, absorbedMilli: absorbed);
            return incoming - absorbed;
        }

        private void ValidateSkillSave(RunStateData run)
        {
            var skill = catalog.FindSkill(run.characterId);
            if (!FiniteDuration(run.skillCooldownRemainingMs) || !FiniteDuration(run.skillShieldRemainingMs) || run.skillShieldMilli < 0 ||
                run.skillCooldownRemainingMs > (skill?.cooldownMs ?? 0) || run.skillShieldRemainingMs > (skill?.shieldDurationMs ?? 0) ||
                (run.skillShieldMilli > 0) != (run.skillShieldRemainingMs > 0) ||
                run.skillShieldMilli > (skill == null || skill.shieldMaxHpPercent == 0 ? 0 : Milli(Math.Max(1, Rounded(run.combatStats.maxHp * skill.shieldMaxHpPercent / 100d)))) ||
                run.pendingSkillAttack || !FiniteDuration(run.skillContactRemainingMs) || run.skillContactRemainingMs>(skill?.contactDelayMs??0) ||
                (!run.skillContactPending&&run.skillContactRemainingMs!=0) ||
                run.skillContactPending&&(skill==null||Phase!=SessionPhase.Combat||run.playerHpMilli<=0||run.enemyHpMilli<=0||run.ended||GuidedPractice||run.skillCooldownRemainingMs<=0))
                throw new InvalidOperationException("Invalid saved skill cooldown, contact or shield.");
        }

        private static bool FiniteDuration(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }
}
