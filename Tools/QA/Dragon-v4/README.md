# Dragon-v4 QA tools

Completed: candidate03 ran1280 dash target,1280 resume,640 fresh/resume; all4 processes exit0,1037 checks PASS,Console0. See `Planning/QA/dragon-v4-result-20261024.md` for adopted earlier evidence and explicit input/mobile/audio-listening limits. The following preparation/continuation notes are history; do not rerun without a new requested scope.

Latest authorized continuation after DQA4-003: `-Resume1280From candidate02` now runs1280 `v4-dash` only,1280 resume,640 fresh/resume (4 processes). It excludes candidate02 Stage11 visual motion from adopted evidence. Dash observations require actual root advance of250–350 plan pixels (about296 expected), scheduled forward contact, intermediate return positions, home return and grounded feet. The earlier three-process note below is superseded. Product code is Client-owned; wait for the final combined observed build/hash.

**Prepared; native QA, Unity and builds NOT_RUN.** Runtime source compile with player defines passed once; syntax checks do not execute the runner or analysis. Final source/candidate and execution ownership must be handed over by root.

The new `v4` / `v4-resume` entry points use the existing saved scene and explicit isolated CLI save path. The V3 campaign body is unchanged; its buffered frame collector is reused by the V4 partial class. Output is `v4-evidence.json`, `v4-frame-evidence.json`, `v4-state.jsonl`, indexed PNGs, events and result files. Source-filter WAVs are optional QA engine captures; they do not certify device output, exact concurrent voices or direct listening.

After the coordinator hands over the observed Runtime hash, execute one four-process batch:

```powershell
& ./Tools/QA/Dragon-v4/run-campaign.ps1 `
  -Executable 'C:/Github/Test2/Builds/Dragon-v4-QA/LastBellMvp.exe' `
  -CandidateId 'dragon-v4-candidate-01' `
  -ExpectedRuntimeSha256 '<64-hex hash from observed-build handoff>' `
  -CoreEvidence 'C:/Github/Test2/Planning/Production/DragonV093ClientValidation/core-tests.txt' `
  -Run
```

The runner requires generated `mvp1.16.0`, three available20s skills, five encounter stages, the frozen0.9.2 timing baseline and `Client/Logs/dragon-v4-operation.json`. It refuses existing evidence directories and stops after a failed process. It restores only scoped game/Editor PlayerPrefs, checks a synthetic legacy sentinel, and never hashes or opens the real default save. Existing092 save namespace/define is retained by the product; this campaign uses only the override.

Core246 assertions are reused from Client evidence; this tool does not run that suite. Stage11's authorized `frontal_dash` replacement is permitted while its timing and balance remain checked. Story UI compares resolved `session.Story` values; all25 pages are observed for each selected character. Unknown names use per-scene portrait IDs. Daren shield snapshots cover activation, file restore, one-contact absorption and a declared last100ms expiry fixture. Full timer/effect contracts remain covered by Client's Core tests.

After candidate01's QA exact-double assertion failure, the approved continuation uses `-Continue1280From 'C:/Github/Test2/Planning/Evidence/Dragon-v4/dragon-v4-candidate-01'` and a new candidate ID/hash. The runner verifies the old failure, unchanged catalog/art and saved payload checksum, then copies only that isolated QA save into the new candidate. The1280 `v4-tail` mode starts at Daren's file-restore/shield boundary, then observes Miel and remaining HUD/motion/story/audio. It omits already-passed Luka and on/dam selection segments.1280 resume and640 fresh/resume were not previously run. JSON-restored duration comparisons use only0.000001ms absolute tolerance with expected/actual/delta records; same-instance freeze and integer state remain exact.

After candidate02 completed all900 checks with only the QA multi-AudioSource tap Console error, use `-Resume1280From 'C:/Github/Test2/Planning/Evidence/Dragon-v4/dragon-v4-candidate-02'` with a new candidate/hash. It validates completed PASS checks and that specific error, unchanged catalog/art, the isolated save checksum and expectation hash, then runs only1280 resume and640 fresh/resume (3 processes). It never rewrites prior results. The corrected probe skips optional PCM capture on multi-AudioSource objects and records PCM_CAPTURE_UNAVAILABLE/MULTIPLE_AUDIO_SOURCES; the actual trigger and mute checks still run. Candidate02 PCM is not accepted as attributable Sfx or listening evidence.

After the batch completes, analyze existing files only:

```powershell
& 'C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  ./Tools/QA/Dragon-v4/analyze.py `
  --candidate 'C:/Github/Test2/Planning/Evidence/Dragon-v4/dragon-v4-candidate-01'
```

The analysis produces hashes, gap/ground metrics, contact coordinates, story/contact sheets and optional PCM amplitude metrics. It leaves visual review, listening and voice-overlap acceptance unclaimed until independently reviewed. OS input and mobile remain deferred. Defects require a new candidate ID and a targeted follow-up scope; do not rerun the full batch automatically.
