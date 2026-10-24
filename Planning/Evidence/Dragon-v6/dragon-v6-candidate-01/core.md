# Dragon v6 / v0.9.5 Client — implementation complete / SOURCE FROZEN

Core/Data/GameData and related test sources are ready for root integration. No Presentation/Editor/Bootstrap/probe/scene/prefab changes, Unity execution, product build or QA run by this client. Save schema9 and LASTBELL_PLAYTEST_092 profile remain unchanged. Data is **mvp1.18.0**.

## Player skill integration

`UseSkill()` is fully separate from BeginAttack. `CanUseSkill` checks ordinary live Combat, player/enemy alive, unpaused, no guided/help practice, cooldown ready, and no existing skill reservation. It is valid during normal windup/recovery and during dodge/parry. It never rewrites normal attack contact/cooldown, pose, dodge or counter state.

- New **PlayerSkillStarted** event: accepted input, SkillId, TimeMs, Stage. Cooldown starts at20000ms; contact is independently reserved200ms later.
- New **PlayerSkillContact** event: one actual contact, SkillId, DamageMilli, StaggerBefore/Gained, TimeMs, Stage. It does **not** also emit AttackStarted/EnemyDamaged. Root owns skill VFX/audio/hit popup from this event.
- `SkillContactPending` and `SkillContactRemainingMs` are available; existing CurrentSkill/cooldown/shield APIs remain. CurrentSkill now includes `contactDelayMs` from data.
- Stable IDs: Luka/on `skill_luka_gale_slash`160%; Daren/dam `skill_daren_iron_counter`115%, shield8%/3000ms, display name 방패 충격; Miel/rin `skill_miel_star_seal`125%, stagger50 with enemy resistance, display name 별빛 벼락. Root renders independent slash/shield shock/sky lightning. IDs were not renamed.
- No crit, stun multiplier/extension or direct consumption of full stagger from the skill. Miel can fill the stack; the next ordinary attack consumes it. Daren shield begins on contact, not input, and existing one-hit absorption logic remains.
- Remaining-time clock advances only ordinary combat. Pause, transition and guided/help practice freeze combat timers; transition/death/defeat/end-run cancel pending player contact without refund. Same-time ordering is enemy/death → ordinary hit → independent skill → companions. Original ordinary attack trade logic remains.
- New independent pending contact resumes once after save/load; completed contact cannot replay. Ordinary attack restore behavior is unchanged. Older coupled pendingSkillAttack is migration-only and canceled, retaining spent cooldown.

## Companion integration / growth UI

Existing **CompanionAttackStarted** and **CompanionDamaged** remain the only start/contact pair. `SkillId` is null for ordinary attack or the special ID below. No extra generic skill-start event for companions and no duplicated basic+special damage. CompanionId, actual DamageMilli and StaggerBefore/Gained are authoritative. Momo additionally emits **PlayerRecovered** with CompanionId, SkillId and actual clamped RecoveryMilli.

| Companion | SkillId | Name | Replacement damage | Effect |
|---|---|---|---:|---|
| tori | companion_tori_double_shot | 쌍쏘기 |180%|one gameplay hit; optional two visual arrows|
| barun | companion_barun_ground_slam | 쿵 내려찍기 |170%|stagger8, enemy resistance/immunity applies|
| doto | companion_doto_flame_hammer | 불꽃 망치 |200%|damage only|
| rina | companion_rina_lightning | 번개 줄기 |180%|stagger10, enemy resistance/immunity applies|
| momo | companion_momo_seed_bomb | 씨앗 폭탄 |170%|living player heal2% run maxHP, clamped|

`CompanionView` adds **SkillId, SkillName, SkillDescription, SkillChancePercent, NextSkillChancePercent, SkillDamagePercent, NextPeriodMs, ExpectedDpsMilli, NextExpectedDpsMilli**. SkillDescription is composed from catalog values, including stagger8/10 and heal2%; UI must not duplicate these constants. Existing PeriodMs now reflects the member's current level. ExpectedDpsMilli/NextExpectedDpsMilli are doubles in milli-HP per second; divide by1000 for HP/s display. Existing DamageMilli/NextDamageMilli remain integer base-hit previews and are not expected DPS.

`RunCompanionView` adds **SkillId** (definition ID), **PendingSkillId** (reserved special, otherwise null), **SkillChancePercent**, **ExpectedDpsMilli**. Existing RemainingMs, IsWindingUp, PeriodMs and DamageMilli remain available. Presentation should use the event SkillId for the contact that just happened; the reservation is cleared before contact dispatch, so reading PendingSkillId at that instant would describe the next cycle instead.

Chance=min(45,12+level-1)%; L1/L10/L20/L30=12/21/31/41%. Period=base×max(70,100-(level-1))/100; respective percentages100/91/81/71. Current level caps10/20/30 remain. The45% and70% guards are not reachable at current max30. Windup180ms stays included in the period, slot offsets0/400/800 and new-enemy grace350ms remain.

## Expected DPS and rounding

Shared party expected-DPS cap remains24% of unmodified player baseAttack per second. The weighted sum includes the new period and `1+p×(specialMultiplier-1)`. Upgrade previews recompute the whole equipped formation with the selected member at the next level; unequipped members preview alone. Tests cover every level1–30 for all five members, party sizes1–3 and teammate levels1/10/20/30: total and promoted-member expected DPS never decrease. At cap total plateaus; basic-hit size or unchanged teammates' shares may fall, so UI uses expected DPS/period/chance as agreed.

Run snapshots store integer damageMilli plus internal fractional-milli remainder fields (`damageFraction`, `damageRemainder`). Fractional damage is carried into subsequent contacts instead of discarded per hit. This retains the weighted cap/upgrade monotonicity through milli-HP rounding without changing whole-number stat data. Skill multiplier applies once to the reserved replacement hit; overkill is capped to enemy remaining HP.

## Save compatibility / reservation safety

Schema remains9. `RunStateData.skillRulesVersion=1` distinguishes new snapshots from existing9 snapshots(default0). Migration occurs after the previous schema1–8 chain and before validation. Old player coupled contact is canceled; cooldown, wallet, HP, reward IDs, checkpoints and dialogue remain. Existing companion timers are scaled to the level-adjusted period; an already winding-up old attack keeps its remaining time and finishes once as a basic attack. New expected damage is recalculated once for the current formation. No heal/reward is applied by migration.

Current snapshots store `skillContactPending/skillContactRemainingMs`, per-companion `randomState`, `skillReserved/reservedSkill`, and damage fractions. A nonzero deterministic companion RNG is separate from enemy-pattern and player crit/reward RNG. The roll occurs only at initial windup reservation. Reload and interrupted stage-transition windups reuse that choice; after actual contact the reservation clears and the next period starts. A different enemy receives at least350ms grace. All new run fields participate in strict empty-inline-record normalization. Invalid version, timers, RNG, pending choice and fraction fields are rejected.

Manual QA fixtures that inject current RunCompanionData into a version1 run must set a nonzero randomState, use the level-adjusted period, and mark skillReserved when windingUp. Use actual StartRun snapshots where possible. To deliberately test an old9 snapshot, set run.skillRulesVersion=0, leave new fields zero, and use the previous base period; the migration initializes them. No Bootstrap/probe sources were changed here.

## Verification and evidence

1. `dotnet run --project GameData/tests/DragonV095/DragonV095.csproj -- GameData/mvp-catalog.json`: **9547 assertions passed**. Independent skill ownership/input/pause/restore; all five replacement effects; resistance/immunity/heal clamp; simultaneous lethal contact; no revival/reward duplication; reserved-choice carry; schema9 migration; malformed state; all-level mixed-party monotonicity/cap;200 contacts with repeated save/restore and100 fractional contacts without damage drift.
2. Existing `GameData/tests/DragonV094/DragonV094.csproj`: **217 assertions passed** against mvp1.18.0. Fixtures now provide explicit current companion RNG/reservation fields; level30 timer expectation is4260ms and cap assertion uses skill-weighted ExpectedDpsMilli. Other recovery, companion services, progression, tutorial, malformed-save and inline-null assertions remain.
3. Python exporter tests: **30 passed** (previous28 plus two tests validating independent contact/cooldown and companion skill/growth contracts).
4. Source export then `--check`: passed, schema2, mvp1.18.0, sourcePlanningVersion `Adventure-v6 / independent player skills and level-scaled companion proc skills`, final source SHA256 **e720fbba2f1f909cee0f3b1860333b381a4adf8d78bd975a2d097b8d1836ac56**. The final change after the test runs was this planning-label metadata only; Core, effects and numeric data stayed frozen.
5. `GameData/tests/DragonV095/check-client-compile.ps1`: Core/Data/JsonSaveRepository standalone compilation passed without starting Unity. `git diff --check` passed for owned source changes.

Unity-source regressions added in `Client/Assets/Game/Tests/EditMode/Core/IndependentSkillTests.cs`: four cases for ordinary+skill independent deadlines/events, dodge+JsonUtility restore, companion replacement reservation restore, and old9 coupled-skill migration. **Not executed in Unity here**; root owns execution plus presentation/mobile/manual QA. Evidence/hashes are in `Planning/Production/DragonV095ClientValidation/`. No product build was produced here.
