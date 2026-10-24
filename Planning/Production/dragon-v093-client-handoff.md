# v0.9.3 client — complete / source frozen

Source frozen for coordinator integration. No Unity, Apply, scene/prefab saves or builds run by this client. Data validator is ready for root export. `Configure(MvpView)` matches the root migration call.

## Exact integration API (ready for root / QA)

- `DragonV093GameplayMigration.Configure(MvpView)` (alias of `Apply(MvpView)`); `Apply(MvpPresenter)` and `Validate(MvpView)` also exist. Only modifies supplied loaded view. Root saves/applies/builds.
- `GameSession.UseSkill()` returns acceptance; `CurrentSkill` (`SkillDefinition`), `CanUseSkill`, `SkillCooldownRemainingMs` (double), `SkillShieldRemainingMs` (double), `SkillShieldMilli` (long).
- `MvpView.Skill` (`PointerCommand`), `SkillLabel`, `SkillCooldown` (`Text`), `SkillFace` (`ReferencePanelGraphic`). Saved node `BattlePanel/UniqueSkill`; lower-center anchor, size224×112 and bottom20 at720p, scales with safe-root height. E shortcut. No automatic normal-attack activation.
- `MvpView.SkillShieldIcon` (`Image`), saved `UniqueSkill/ShieldActive`, existing shield-normal.png. Native36×44 at720p, centered8px above skill control; non-raycast. Visible only inCombat with shield amount/time positive. Absorption/expiry clears it on the next render. Configure requires the existing Dragon UI sprite already imported. Natural sprite dimensions preserve the existing UI pixel scale.
- Run schema8 optional fields: `pendingSkillAttack` bool, `skillCooldownRemainingMs`/`skillShieldRemainingMs` double, `skillShieldMilli` long. New run ready; same-run stage carry; only combat clock decrements. Load cancels pending contact but retains spent cooldown; shield state resumes.
- `SessionEvent.SkillId` on accepted `AttackStarted` and actual `EnemyDamaged`; null normal. Contact includes `StaggerBefore`/`StaggerGained`. `SessionEventKind.SkillShieldAbsorbed` exposes `AbsorbedMilli`, `SkillId`, direction, pattern and time. Fully blocked hit does not emit zero-damage `PlayerDamaged`; partial block emits shield consumption then residual damage.
- Miel50 uses only stage resistance: normal50/miniboss30/boss20. Stun/immunity prevent gain. Pre-hit full stack triggers stun; filling via this skill prepares the next hit.
- `MvpPresenter.PhysicalAttackInput` is the only normal physical attack input route to `BattleAudio.PlayAttackTouch()`. `Issue(Session.Attack)` and hit events never synthesize touch. Existing `BattlePresentation.Audio.Swing` remains actual-contact sound.
- `StoryView.PortraitId`, speaker/player identity resolution; `PrepareStory` after all enemy kinds; Overlay receives portrait. Existing boss-only queued settlement rewards preserved.
- `GameCatalog.RequiredDodge("frontal_dash") == "back"`; same core bounds as thrust. Stage11 alone accepts single pool `["frontal_dash"]`. Repetition filter falls back to authored pool when it would be empty. Presentation uses AttackThrust with existing authored approach/contact/return; DQA3-003 attack > recovery > early cue priority preserved.
- Daren/Miel now use the same authored player motion sampling path as Luka. Skill slash tint/size follows accepted skill, still uses the same200ms contact and no extra damage event.

## Verification completed

- `dotnet run --project GameData/tests/DragonV093/DragonV093.csproj -- GameData/mvp-catalog.json`: PASS246 assertions, mvp1.16.0. Six groups cover acceptance/contact/cancellation, shield, stagger resistance/next-hit, timers/save, stories, repeated dash.
- Standalone Runtime and Editor C# compilation passed, including final saved button-face hierarchy and shield indicator. Existing JSON DTO CS0649 warnings only.
- Evidence: `Planning/Production/DragonV093ClientValidation/core-tests.txt`, `Runtime.log`, `Editor.log`.
- Existing `GameSessionTests` and `CombatPresentationTests` stage10 dialogue expectations updated as requested, including selected-character portrait assertion. They were not executed in Unity here.
- DQA3-003 `SampleEnemy` actual-attack → previous-recovery → early-ready ordering and contact/return geometry preserved. No QA probes, capture scripts or QA evidence edited.
- Saved control uses one Image raycast surface on the command root and a separate child Face graphic; avoids two graphics sharing one CanvasRenderer. Default224×112 at720p gives112×56 at640×360, with20px safe-root bottom margin at720p. Final overlap/touch/render verification belongs to root QA.

## Root integration notes

Call `DragonV093GameplayMigration.Configure(view)` when patching the existing loaded view, then persist with the other authorized changes. Export mvp1.16.0. Existing save namespace/define remains092; this client did not touch storage selection.

`SkillShieldAbsorbed` is a confirmed enemy contact. BattlePresentation already records that contact for authored enemy recovery, including fully blocked hits. If root EnemyAttackEffects should emit impact particles for shields, add this event there; do not play player hurt audio for a fully blocked hit. Existing actual-contact Swing remains once per EnemyDamaged. Skill button has visual press/cooldown feedback; normal attack touch audio is physical-input-only.

Skills reuse the authored normal attack clip and timing with a larger gold/blue/green slash for Luka/Daren/Miel. No new attack frames or altered gameplay contact timing introduced. Final rendered/mobile/audio validation and build are coordinator-owned.
