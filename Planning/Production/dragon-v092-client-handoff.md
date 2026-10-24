# Dragon 0.9.2 client integration — READY / FROZEN

## Coordinator build/scene contract (names fixed)

- **Windows playtest build only**: add `LASTBELL_PLAYTEST_092` to `BuildPlayerOptions.extraScriptingDefines` for both the observed candidate and shipped test exe. Do not persist it in project-wide scripting defines. The coordinator owns DragonThemeMigration and its 0.9.2/version/data/output changes; this client does not edit that file.
- GameBootstrap enables the profile only under `UNITY_STANDALONE_WIN && LASTBELL_PLAYTEST_092 && !UNITY_EDITOR`. It selects `Application.persistentDataPath/Playtests/dragon-0.9.2`. Existing `-lastbell-save-dir` has final priority; regular builds, Editor and WebGL retain their original save scope. No QA fixture is injected and no existing file is migrated/deleted/reset.
- No save: existing prologue → selection/upgrade → Stage 1 tutorial. Existing 0.9.2 profile: continue it, including checkpoint 11. Manual repeated fresh play can use the existing `-lastbell-save-dir <new-empty-folder>` option; there is no progress-reset button. Restart/retry follows checkpoint, not an implicit reset.
- Default actor scene placement should now call **`ParallaxWorld.GroundPoint(x, stage)`** instead of `PlanPoint(x,590)`; x remains 500/900. For data/QA use **`ParallaxWorld.GroundForStage(stage)`** (FirstStage, LastStage, Region, Kind, PlanY, FloorTint). Both APIs are fixed.
- DragonWorldMigration.Configure now assigns the seven floor renderers to saved **`ParallaxWorld.GroundTiles`**. Reapply it to prefab/scene during the coordinator's integration; Runtime region tint uses this binding. No new background art is provided. Stages 11–20 distinguish only the existing floor color, not a completed forest backdrop.

## Ground contract

| Stages | Region / GroundKind | Plan contact Y | Scope |
| --- | --- | --- | --- |
| 1–10 | 바람들판 / Meadow | 590 | MVP; original meadow floor |
| 11–20 | 솔바람숲 / Forest | 590 | MVP; same slice/geometry, subdued green-brown tint |
| 21–30 | 햇돌광산 (`Region=mine`) / Quarry | 590 | Planning contract only |
| 31–40 | 구름고개 / Ridge | 590 | Planning contract only |
| 41–50 | 붉은바위둥지 / DragonNest | 590 | Planning contract only |

`GroundPlanY=590` is the common actor-root plane in top-left 1280×720 plan space. The far/floor image uses **source contact row 634**, not the tall grass-fringe start row 602. Editor placement derives pictureTop from the same GroundPlanY. BattlePresentation uses GroundPoint for neutral stance, travel, player attack, dodge base, parry and creature approach. Existing dodge-Y offset, lunge-X, frame timing and source pivots are preserved. No per-frame alpha-bounds normalization.

`ActorView.FeetWorldPosition` exposes the imported sprite-origin/feet pivot for QA; it is not the current lowest opaque pixel. Rendering already places each sprite at its authored custom pivot. The added accessor does not change pose rendering.

With the current camera y=−1.04, orthographic size=3.1 and unshifted WorldRoot: world contact y=−1.9805556; actual screen top-origin contact is **469.225806px at 1280×720**, **234.612903px at 640×360**. This projection is not a new world offset. Geometric pivot-to-plane target is 0 (allow 0.00001 world units numeric tolerance); grounded opaque-foot edge versus visible terrain seam may differ by **at most 1 physical raster pixel** from sampling/AA at each size. Check the visible source seam as well as the computed line. Authored airborne frames/root offsets are assessed separately and must not be forced onto the plane.

Current v2 source audit: player Idle/Attack/Down-parry/Back/Hit bottoms equal their supplied pivots; Run frames 2/6 lift by 2 native pixels and Up-dodge lifts by up to 3 native pixels intentionally. All currently integrated Dragon 1–10 Idle frames have opaque bottom equal to pivot. New Player-v3/11–20 source should retain the same contact-frame contract; it is owned by the art coordinator.

## Verification / integration ownership

- PASS: Runtime + Editor standalone compilation against installed Unity assemblies; Windows player compilation with `UNITY_STANDALONE_WIN,LASTBELL_PLAYTEST_092` and without `UNITY_EDITOR`; focused persistence test assembly compilation. Logs: `Planning/Production/DragonUiValidation/{Runtime,Editor}.log` and `Planning/Production/DragonV092ClientValidation/{Player092,PersistenceTests}.log`. Six existing DTO warnings in Editor only.
- Five NUnit cases prepared in `SaveDirectorySelectorTests.cs`: production path unchanged/no filesystem side effect, explicit path precedence in both build variants, fresh Stage 1 while legacy primary AND backup bytes stay identical, and subsequent checkpoint11/wallet persistence. **Tests were compiled, not run**; coordinator owns the final Unity pass.
- `DragonV092ClientValidation/ground-contact-audit.json` records the source-manifest hash, 11 integrated actors' actual Idle contact pixels/pivots, intentional player lift frames, camera math and real candidate-01 capture pixels at 1280/640. Grounded player solid outline ends at screen row468 at 720p; row469 is the terrain transition; at 640 the sole transition includes partial coverage on row234 before soil on235. No visible multi-pixel gap or soil penetration in the inspected neutral player captures. This is existing-render evidence, not a new forest/v3 render claim.
- Changed runtime files: GameBootstrap, new SaveDirectorySelector, ParallaxWorld, ActorView (feet-pivot accessor only), BattlePresentation (ground-baseline call sites only). Changed editor file: DragonWorldMigration. New test: Persistence/SaveDirectorySelectorTests. No DragonThemeMigration, manifest/catalog, sprite, scene, prefab, camera or gameplay-rule changes by this client.
- **No Unity/MCP/Apply/build or scene/prefab save performed.** Coordinator must apply GroundTiles bindings and use the named build define; then perform the one targeted fresh/reload-save and stage1/10/11/20 ground/motion check with the new art. Verify both product default namespace and explicit QA save-dir override, and preserve the old production save file hash.

## DQA3-003 queued-cue recovery interruption — CODE READY / FROZEN

- Verified the candidate-02/03 frame records and the candidate-03 stage19 before/after PNGs: first impact at combat2700ms, then clip4 → clip0 with full home-root reset. At 1280-equivalent scale the jumps are 296px (stage19) and 211px (stage20). The next unresolved combo threat has already shown its cue but its authored attack has not started. The `elapsed<0` branch returned before the previous impact's recovery branch, leaving TickVisuals' neutral root untouched.
- **Only product edit: BattlePresentation.cs / SampleEnemy.** Save the earliest future ready-pose pattern while scanning; any actual authored windup/contact (`elapsed>=0`, within cue→impact) still wins immediately. If no actual attack is due, sample the prior impact pattern and SampleCreatureApproach throughout recovery. Adopt the future frame0 pose only after recovery ends. No changes to Core, clocks, impacts, telegraphs, assets, root movement curve, damage, audio, stage placement, QA source, editor migration or build code.
- Actual stage19/20 Attack clips: contact200ms, total500ms, recovery300ms. Current combo gap750ms means next authored start at **550ms after prior impact**. Existing root return takes280ms, finishing20ms before the recovery clip and270ms before next active start. There is **250ms headroom after full recovery**. No blend or extra timing delay is needed. A genuinely overlapping future active attack remains higher priority; do not change gameplay timing to guarantee completion in hypothetical faster data.
- Static arithmetic against recorded first post-contact ages predicts first return steps of ~7.55px / ~3.07px at 1280, instead of the prior 296px / 211px reset. These are predictions, not new Unity captures. `DragonV092ClientValidation/DQA3-003/timing-analysis.json` records timing, recovery samples, next-active boundary and next contact frame.
- PASS: Runtime and Editor standalone compilation; current V3 QA JSON DTO warnings and older migration DTO warnings only. No Unity/MCP/Apply/build, QA edit, commit or push. Final QA remains with coordinator: stage19/20 recovery frames and smooth root return complete, no single-frame home snap, and next real attack samples the correct pattern/contact on its original schedule at 1280/640.
