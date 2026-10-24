# v0.9.3 Client contract — implementation validated / frozen

## File ownership

Client: Runtime/Core skill/action/state work; Runtime/Data/GameCatalog.cs skill definitions/validation plus `RequiredDodge("frontal_dash")`; MvpPresenter/MvpView skill fields/input/event connection; BattlePresentation skill motion and frontal dash presentation; new Editor/DragonV093GameplayMigration.cs adding only the saved skill control. StoryDefinition, PrologueView and PresentationOverlay belong to the story agent. Root owns existing HUD/telegraph/stun presentation, audio assets/service and all Unity apply/build/QA. Data source/export/catalog payload belongs to the data owner. Client will not run Unity, Apply, migration or build, and will preserve DQA3-003 recovery ordering.

## Data owner: requested exact catalog fields

Add a top-level **`skills` array** (C# `SkillDefinition[]`). Each record:

```json
{
  "id": "unique_stable_skill_id",
  "characterId": "on",
  "displayName": "data-owner-approved skill name",
  "description": "short player-facing effect description",
  "cooldownMs": 20000,
  "damagePercent": 160,
  "shieldMaxHpPercent": 0,
  "shieldDurationMs": 0,
  "staggerGain": 0
}
```

All numeric fields above are integers. Exactly one record per available character; no multipliers duplicated in client code or UI. IDs/names/copy are data-owned; code resolves by `characterId`. Please set characters `on`, `dam`, `rin` available; art IDs: existing Luka ID, `player_dragon_daren`, `player_dragon_miel`.

| characterId | damagePercent | cooldownMs | shieldMaxHpPercent | shieldDurationMs | staggerGain |
| --- | ---: | ---: | ---: | ---: | ---: |
| on | 160 | 20000 | 0 | 0 | 0 |
| dam | 115 | 20000 | 8 | 3000 | 0 |
| rin | 125 | 20000 | 0 | 0 | 50 |

No 3-charge/automatic-on-normal-attack rule. The Skill input is separate and reuses normal attack eligibility/contact scheduling. Accepted input spends cooldown once; refusal spends nothing. Damage modifier uses the existing integer damage/critical/stun rules, applied once on actual player contact. Cancellation before contact grants no contact bonus and does not refund an already accepted skill.

Daren: on actual contact grant a 3-combat-second shield. The next non-dodged hit consumes it once and absorbs up to 8% integer maximum HP; remaining damage applies normally. Successful dodges do not consume the shield. Miel: on actual contact add50 stagger multiplied once by existing stage resistance (normal50/miniboss30/boss20), capped by threshold and respecting stun/immunity; no perfect-streak or relic multiplier. Reaching full stagger via this hit prepares the **next** attack to stun; it does not change the existing pre-filled-stack attack trigger.

## Stage 11 frontal dash contract

- New pattern ID **`frontal_dash`**, requiredDodge **`back`**. Core collision bounds initially match the existing thrust rectangle. Catalog validator and CombatGeometry will recognize it; no global timing/balance change.
- Stage11 uses frontal_dash in every `patternSteps[].danger` and the single pool `["frontal_dash"]`, with teachDodge=back. Existing intervals/telegraph values remain. Validator permits a single-entry pool only for stage11/frontal_dash; random selection safely permits repeats when no alternative exists.
- Presentation maps this to the actor's Attack/AttackThrust clip and forward approach on its authored contact time, followed by existing recovery. The shared actual attack → previous recovery → future ready-pose priority remains intact.
- Root telegraph/audio can recognize the new ID; it remains a backward-dodge threat, not a fourth input direction.

## Runtime / save / UI contract

- Public command: **`GameSession.UseSkill()`**. Read API: `CurrentSkill`, `CanUseSkill`, `SkillCooldownRemainingMs`, `SkillShieldMilli`, `SkillShieldRemainingMs`.
- Cooldown and shield lifetime use **combat elapsed time only**. Pause, story, transition, lobby and results do not tick them. Cooldown persists across stages in the same run; a new departure/retry starts ready. Run-state remaining durations will be serialized, with zero defaults compatible with existing schema8 saves (no repository format change needed).
- Resume policy follows existing interrupted normal-attack behavior: pending contact is cancelled on restore while the already-spent skill cooldown remains. No replayed damage/shield/stagger bonus and no reload-based cooldown refund. Completed shield state resumes with its remaining combat duration and one-hit capacity.
- Existing `AttackStarted` and `EnemyDamaged` events carry **`SkillId`** (null for normal attacks), allowing root audio/VFX to distinguish accepted skill and actual skill contact without double-starting the attack. `SkillShieldAbsorbed` exposes `AbsorbedMilli`; partial absorption is followed by residual `PlayerDamaged`, full absorption emits no zero-damage player hit.
- Lower-center mobile touch control, within the safe area; separate from direction keys and normal attack. Skill name and remaining whole seconds/ready state are shown; disabled states follow actual command eligibility. Desktop shortcut planned **E**. Editor helper modifies the supplied loaded saved view only and does not open/save/apply/build it.

## Verification plan

Focused pure-core execution passed246 assertions: 3 character effects, accepted/refused cooldown, actual-contact once, pre-contact cancellation, pause/transition/story freeze, stage carry/new-run ready, save/reload without duplicate effect, shield one-hit/expiry, resistance and full-stagger next-hit behavior, encounter/boss dialogue with portrait/resume, and repeated dash direction/contact. Runtime and Editor standalone C# compilation passed. DQA3-003 presentation priority remains unchanged. Final rendered/mobile/audio QA belongs to root after integration freeze; no Unity launch, Apply or saved-scene/prefab mutation was performed here.
