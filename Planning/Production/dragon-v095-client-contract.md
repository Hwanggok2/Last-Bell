# Dragon v6 / v0.9.5 Client contract

Status: implementation starting; Core/Data/GameData/tests owned here. Root owns Presentation, Unity, Apply, builds and QA. Data target mvp1.18.0; profile/LASTBELL_PLAYTEST_092 unchanged.

## Independent player skill

- `UseSkill()` no longer calls BeginAttack. `CanUseSkill` requires live, unpaused ordinary Combat, enemy/player alive, no guided/help practice, cooldown ready and no pending skill. An ordinary attack windup/recovery or dodge does not block it.
- 20000ms cooldown starts on acceptance. Independent fixed **200ms contact delay**, unaffected by normal attack speed, counter or dodge. It never clears/rewrites normal attack deadlines, pose, dodge/parry or counter state.
- New events **PlayerSkillStarted** and **PlayerSkillContact**, carrying existing `SkillId`, `TimeMs`, `Stage`, `DamageMilli`, `StaggerBefore/Gained` fields. No AttackStarted/EnemyDamaged duplicate for skill. Root renders Luka independent slash, Daren shield shock and Miel sky lightning from these events. Existing SkillShieldAbsorbed remains.
- Public `SkillContactPending`, `SkillContactRemainingMs` supplement cooldown/shield APIs. Effects use current existing damage values160/115/125%, Daren shield8% maxHP/3000ms and Miel stagger50. Skill contact has no crit, stun damage bonus or stun-extension proc; it can fill stagger but only the next normal player attack consumes a full stack to stun.
- Independent remaining-time clock advances only live Combat and pauses with combat. Death/defeat/travel/end-run cancels pending contact without refund; a dead player's skill cannot deal damage or create a shield. At equal deadlines: enemy impact/death, normal player hit, independent skill, companions. Normal-vs-enemy trade behavior remains; no second reward when an earlier hit kills the enemy.
- On save/reload the newly reserved independent contact resumes once with its remaining time; no replay after completion. Legacy `pendingSkillAttack` is migration-only and is canceled under the previous restore policy, retaining spent cooldown.

## Companion skills and growth

| Id | SkillId | Name | Replacement damage | Additional effect |
|---|---|---|---:|---|
| tori | companion_tori_double_shot | 쌍쏘기 |180%|one gameplay hit; root may draw two arrows|
| barun | companion_barun_ground_slam | 쿵 내려찍기 |170%|stagger+8 before enemy-kind resistance|
| doto | companion_doto_flame_hammer | 불꽃 망치 |200%|none|
| rina | companion_rina_lightning | 번개 줄기 |180%|stagger+10 before enemy-kind resistance|
| momo | companion_momo_seed_bomb | 씨앗 폭탄 |170%|heal2% of player run maxHP, living-only/clamped|

- Chance=min(45,12+level-1)%: L1=12%, L10=21%, L20=31%, L30=41%. Existing caps10/20/30 remain;45% is a future guard, unreachable at current cap.
- Period=basePeriod×max(70,100-(level-1))/100, integer milliseconds. L1/L10/L20/L30=100/91/81/71%;70% is likewise a future guard. Windup180ms stays included in the period.
- A proc replaces the scheduled basic contact. **Existing CompanionAttackStarted/CompanionDamaged events remain the sole start/contact pair**, now with `SkillId` empty for normal or the table ID for special; no duplicate special event. CompanionId, damage and actual stagger/heal remain authoritative. Momo also emits PlayerRecovered with CompanionId/SkillId and actual RecoveryMilli.
- Roll once at initial windup reservation using a separate saved per-companion RNG, independent of enemy-pattern/player-crit RNG. Save pending choice and remaining time; reload and stage transitions cannot reroll the same reservation. Clear reservation only after actual contact; a canceled visual windup resumes its saved choice after new-enemy grace.
- `CompanionView`: add SkillId, SkillName, SkillDescription (one-line text composed from catalog damage/stagger/heal values), SkillChancePercent, NextSkillChancePercent, SkillDamagePercent, NextPeriodMs, ExpectedDpsMilli, NextExpectedDpsMilli; existing PeriodMs becomes the level-adjusted value. `RunCompanionView`: add SkillId (definition), SkillChancePercent, PendingSkillId and ExpectedDpsMilli. ExpectedDpsMilli values use double precision; divide by1000 for HP/s display.

## DPS constraint / tradeoff

Keep the existing party cap: expected companion DPS <=24% of unmodified player baseAttack per second. Include `1+p×(skillMultiplier-1)` and adjusted period in the shared scale. Upgrading a fixed formation never lowers total expected DPS or the upgraded member's expected DPS; at the cap total plateaus. Individual basic-hit numbers or an unchanged teammate's share can fall as speed/proc allocation grows—UI should show expected DPS plus chance/period rather than claiming every raw hit always increases. Requiring every member's raw hit to rise while retaining a hard party cap and faster periods is incompatible. Test all1–30 levels and mixed formations for monotonic expected DPS, including milli-HP rounding.

## Persistence and integration

Keep schema9 unless implementation proves a breaking container change necessary. Add explicit run `skillRulesVersion=1` to distinguish schema9 old snapshots (default0); migrate those once before strict validation. Add independent player-skill reservation and per-companion proc/RNG fields with safe zero defaults. Existing schema1–8 chain still reaches9 then performs this run migration. Preserve wallet/HP/rewards/checkpoints/cooldown/active dialogue and saved filenames. Current9 reservations must round-trip without re-roll or duplicate contact. Empty-run canonicalization must include every new field.

Root should switch player skill visuals/audio/damage popup to the two new PlayerSkill events and branch companion visuals by SkillId on existing Companion events. UI growth values come from Core views, no duplicated formulas. Final implementation/verification and any contract adjustments will be recorded in dragon-v095-client-handoff.md.
