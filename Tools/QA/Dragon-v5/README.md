# Dragon-v5 observation tools — prepared, not executed

Target: Windows Development `0.9.4`, catalog `mvp1.17.0`, save schema9, `LASTBELL_PLAYTEST_092`, Unity6000.3.15f1. Root owns Apply, Unity compilation, source freeze, observed build and execution handoff. Do not launch while companion art or sources are still changing. OS real-input checks remain deferred by the user.

The probe is `Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V5.cs`. The existing probe entry adds only `v5`, `v5-legacy`, `v5-resume` dispatch/preflight and cleanup. All v4 branches remain. The entire probe is under `UNITY_EDITOR || DEVELOPMENT_BUILD`; ordinary launches without `-dragon-theme-smoke` install nothing. The native observation requires the Windows playtest define. No production Core/Presentation code is edited by this package.

## What the prepared campaign observes

| Process | Input / output | Scope |
| --- | --- | --- |
|1280 fresh (`v5-legacy`) |Copied old QA schema8 at native startup; then explicitly new/derived DTO fixtures |Native legacy preservation, synthetic legacy7/15/20 file restoration, new prologue, inn/growth/services, guided2-input sequence, recovery3/10/20,3 skills/shield/stun particles,5 solo companion periods, current story2/12/10/20 for3 characters. Saves interrupted guided checkpoint |
|1280 resume |Separate copy of the preceding checkpoint |Actual new process; HP/skill time preserved; guided restarts first sequence and reaches first hold |
|640 fresh (`v5`) |Empty isolated boot save, copied old QA source under `legacy-source` |Same representative changed scope at small size; prepares Daren shield plus a pending Tori windup |
|640 resume |Separate copy of the preceding checkpoint |Actual new process; same-enemy remaining time/windup preserved; current shield displayed; old bursts absent; one pending contact occurs |

The final Client contract preserves a pending contact on **same-enemy reload**. The350ms grace belongs to a **new enemy**. The native assertion does not incorrectly clamp an existing100ms remainder to350ms. New-enemy grace permutations remain in the Client evidence and broader acceptance plan.

`V5Prepare` uses detached save DTOs, high player HP via legitimate HP growth, and labelled high enemy HP to keep observations alive. Solo future NPC and all-five inn fixtures are rule/UI fixtures, not evidence of playing stages21–50. Inn scrolling is an explicit ScrollRect viewport setup followed by a real EventSystem raycast click; it does not certify a physical drag gesture. Button and PointerCommand paths are synthetic Unity events, never OS mouse/keyboard control.

Long timer gaps use labelled50ms manual Core ticks. Contact windows use the existing buffered V3 rendered-frame collector, with state and events attached. This does not run the V3/V4 campaigns. The inherited `v3-frame-*.png` prefix identifies the collector, not the product version. Repeated6/10/8/7/6-second periods are checked in authoritative combat time across three contacts per solo NPC, with real render windows around each contact. Capture readback is not60fps/performance certification.

Four real ParticleSystems, Point RT/camera/RawImage bindings, particle positions/count/colors, current actor roots/feet, threat cues/impact times and current state are recorded. The three skill contact events must produce actual particles. Pause preserves combat/skill/shield time. Miel ready state is distinct from actual stunned state: the latter has15 particles and no duplicate old UI stars, then clears at stun end. Daren shield consumes on one confirmed absorption, even if the incoming hit is less than its capacity. A labelled last180ms fixture isolates natural expiry from absorption; the full3000ms rule is also covered by the handed-over Client tests. The640 checkpoint explicitly shortens the initial Tori remainder to800ms so its pending windup coexists with a live shield before the first incoming hit.

The probe does **not** automatically certify all60 acceptance IDs, all retreat archetypes, exact dialogue typography/overflow, held/prebuffer input, all economic edge cases, native stunned-state restore, complete old-profile corruption/fallback behavior, RT leaks, audio listening, physical input or device performance. Service/recovery particle samples are collected; their visibility and correct palette still need PNG review. A machine observation PASS leaves `acceptance=NOT_CERTIFIED`, `visualReview=NOT_RUN` and `audioListening=NOT_RUN`.

## Required handoff and execution

`run-campaign.ps1` refuses to run without explicit `-Run`. The coordinator supplies:

- Executable inside `Builds/Dragon-v5-QA` and exact `LastBell.Runtime.dll` SHA256.
- Core evidence and saved Apply/integration evidence paths inside this workspace.
- Legacy save inside **`Planning/Evidence/Dragon-v4` only**, with a valid schema8 envelope/checksum. No default user profile is read, copied or hashed.
- A frozen source manifest inside the workspace. It contains `sourceFrozen: true`, `version: "0.9.4"`, `dataVersion: "mvp1.17.0"`, `runtimeSha256`, and `files: [{"path": "workspace/relative/file", "sha256": "..."}]`. Paths use `/`. Include the final Runtime/Editor sources, saved scene/prefab/art manifests and generated catalog; at minimum the runner requires the base probe, V5 partial and generated catalog entries. This manifest is supplied only after Root freezes its build inputs, not created optimistically by the campaign.

Example shape (placeholders deliberately cannot pass preflight):

```powershell
& .\Tools\QA\Dragon-v5\run-campaign.ps1 `
  -Executable '<absolute Builds/Dragon-v5-QA executable>' `
  -CandidateId 'dragon-v5-candidate-01' `
  -ExpectedRuntimeSha256 '<64 hex characters>' `
  -FreezeManifest '<absolute frozen manifest>' `
  -CoreEvidence '<absolute handed-over Client evidence>' `
  -IntegrationEvidence '<absolute Apply/integration evidence>' `
  -LegacySave '<absolute prior Dragon-v4 QA save>' -Run
```

Output is `Planning/Evidence/Dragon-v5/<candidate>/`. Existing candidates are rejected. Each process receives a strict child save override. Links/junctions and source/hash mismatches are rejected before launch. Native processes launch hidden. The runner stops on first failure, records it, restores only scoped game/Editor PlayerPrefs in `finally`, and checks the synthetic sentinel plus original old-QA seed. Fresh checkpoint files are retained unchanged when the resume process uses a separate copy. No retries or broad repeat campaign are automatic.

Each case emits `result.json`, `v5-evidence.json`, `v5-frame-evidence.json`, `v5-state.jsonl`, `v5-input.jsonl`, `frames.jsonl`, PNG/layout files and the native log. The fresh case also emits derived migration DTOs/files and `v5-resume-expected.json`. Evidence records Core events separately from synthetic input edges and private particle simulation clocks are not inspected through reflection.

After an authorized campaign, use the standard-library-only analyzer:

```powershell
python .\Tools\QA\Dragon-v5\analyze.py '<absolute candidate directory>'
```

It prints a read-only integrity summary and never rewrites candidate evidence or grants visual acceptance. Review original PNGs with the state timeline and record the inspected range separately. On failure, preserve the candidate and determine whether the problem is product behavior, fixture/assertion logic or insufficient sampling before planning a targeted rerun.

## Preparation validation

Source preparation checks are reported in `Planning/QA/dragon-v5-probe-preparation-20261024.md`. No Unity Apply/build, game process, campaign or real input was executed to prepare this package. Source syntax checking is not a Unity compilation or runtime PASS.
