# Dragon UI client handoff — READY

Owner: UI client chat. 2026-10-24 KST. Code and assets are ready for the coordinator's integrated Unity apply/build/visual QA. This client has not launched Unity, imported through Unity, applied a migration, saved a scene/prefab, or built Dragon-v1.

## Integration

1. Use the existing loaded v8.6 saved view/presenter. `LastBell.Editor.DragonUiMigration.Apply(MvpPresenter presenter)` applies UI plus telegraph, damage-number and story-overlay skins. `Apply(MvpView view)` is also available for the prefab alone; it does not reach the separate world effects.
2. `ConfirmedThirdIcon` defaults to `"down"`. The user explicitly confirmed on 2026-10-24: “현재 Unity 조작 유지: 위·뒤 회피 + 아래 쳐내기”. Up/Back dodge, Down parry, Space attack and the hidden fourth Forward button remain unchanged. Common `UiSymbol.Down` also resolves to down. The coordinator generated the three down PNGs from the native v4 grid; all **45 PNGs** are copied byte-for-byte. This control contract is confirmed; no input-choice question remains pending.
3. Assign approved inn/prologue background sprites through `view.Dragon.LobbyBackdrop` and `PrologueBackdrop` **after Apply**. Initial migration removes the legacy blue-castle backdrop and supplies a paper fallback. Repeat Apply preserves already-themed background sprites. New Dragon-v1 sprites already assigned before first Apply are preserved as well.
4. Call `Validate(presenter)` (or `Validate(view)` for the prefab), then save through the coordinator's existing scene/prefab workflow. The helper only changes/marks the supplied loaded objects and imports its UI icons; it never opens/saves scenes, loads/saves prefabs, starts Play mode, replaces actor art or builds.
5. Use the coordinator's independent Dragon build path. Do not run legacy full migrations after this partial UI pass; legacy UI-name/typography assertions may intentionally differ.

## UI contract

- Direction hit centers, top-left normalized: up `(122/1084,380/540)`, back `(60/1084,460/540)`, down/parry `(161/1084,481/540)`. At 1280×720 they are `(144.1,506.7)`, `(70.8,613.3)`, `(190.1,641.3)`. Hit areas are 104×104 at 720p (78×78 at 540p); the old overlapping 78×84 boxes are not reused in Dragon mode.
- Attack, pause and stage panel use v4 positions. No directional diamond plates; source icons handle normal, pressed and disabled states. Pressed shifts 4 source pixels, without the legacy scale squash. Pointer listeners and command acceptance rules are unchanged.
- HP follows projected **canonical home feet** (`PlanPoint(500,590)` / `(900,590)`), with world-shake offset removed, not the staged montage actor coordinates or attack lunges. HP gap 64 source pixels; shelf starts 32 source pixels below feet. Source bars: player 160×24, enemy 184×24, boss 280×24. Positive HP retains at least one cell; only zero HP is empty. Enemy name remains below its bar.
- Warm paper panels, stepped outlines, shared pixel palette, selected check, disabled/pressed states, and 보석/기세 presentation copy. Internal Fragments/resonance identifiers, stats, prices, saves, rewards and automatic relic rules stay compatible.
- Attack-count indicator uses the approved shield glyph. Incoming timing arcs and perfect-dodge full-screen centered wave are rasterized to the same pixel style; timing, contact geometry and success rules are unchanged.
- Story dialogue hides stage panel, command shelf, gold, enemy name and pause decorations while using a paper dialogue surface. Other phase visibility remains owned by the current presenter.
- Existing hierarchy is reused; only named decorative UI children/components are added. Lobby/prologue background artwork is owned by the coordinator.

## Files owned by this client

- New runtime: `DragonUiPalette.cs`, `DragonButtonFeedback.cs`, `DragonUiTheme.cs`.
- Runtime changes: `SymbolGraphic.cs`, `PointerCommand.cs`, `ReferencePanelGraphic.cs`, `HealthBarGraphic.cs`, `MvpView.cs`, `FeedbackLayout.cs`, `MvpPresenter.cs`, `ReferenceLobbyView.cs`, `GoldPickupGraphic.cs`, `RelicIconGraphic.cs`, `CombatRhythmGraphic.cs`, `DodgeFeedback.cs`, `EnemyTelegraphGraphic.cs`, `PixelNumber.cs`, `PresentationOverlay.cs` under `Client/Assets/Game/Runtime/Presentation/`.
- Editor helper: `Client/Assets/Game/Editor/DragonUiMigration.cs`.
- Assets: `Client/Assets/Game/Art/UI/Dragon-v1/` (45 PNGs + SHA-256 source manifest).
- Validation: `Client/Assets/Game/Tests/PlayMode/DragonUiContractTests.cs`, `Tools/check-dragon-ui-compile.ps1`, `Planning/Production/DragonUiValidation/`.
- No edits to Core rules, GameData, MvpBuild, DragonWorldMigration, ParallaxWorld, actor manifest, BattlePresentation, EnemyAttackEffects, scenes or prefabs in this UI task.

## Verification and remaining integrated QA

- PASS: standalone Runtime and Editor C# compilation against installed Unity 6000.3.15f1 assemblies, including the final down-icon and story-visibility changes. `Tools/check-dragon-ui-compile.ps1` writes `Runtime.log` and `Editor.log`; six pre-existing JSON DTO unassigned-field warnings only. No Unity process launched by the checker.
- PASS: 45 PNGs byte-identical to approved source; binary alpha, 4-pixel dimensions, hashes recorded. Point/uncompressed/no-mipmap Sprite import is enforced by Apply.
- PASS: direction rectangles in bounds, non-overlapping, at least 48px at 960×540, 640×360, 1280×720 and 854×360. Evidence: `Planning/Production/DragonUiValidation/static-checks.json`.
- Prepared four HP boundary NUnit cases (0, tiny positive, 1%, 100%). **Not executed** here; coordinator owns Unity tests.
- Required coordinator checks: saved references/normal-pressed-disabled rendering, actual camera-projected HP/shelf position, safe area, long Korean labels, lobby/relic/upgrade/pause/prologue/story surfaces, Down parry and hidden Forward, current combat timing and final Windows build. Static compilation does not substitute for rendered Unity QA.

## Candidate-01 reward-card occlusion fix — READY

- Observed evidence: `Planning/Evidence/Dragon-v1/candidate-01/full-1280x720/route-relic-choice.png`. All three opaque button faces covered their sibling relic icon/name.
- Cause: `FeedbackV7Migration` deliberately put the formerly transparent `Receive` hit button at the last sibling index. In the saved candidate prefab each card has `Name=0`, `RelicIcon=6`, `Receive=7`. Dragon styling made the Receive subtree opaque, so its first-child background still rendered above those siblings.
- Minimal fix in **DragonUiMigration only**: after styling buttons, set each reward's `Receive` to the first sibling. Existing card parent, icon/title bindings, button bounds, listeners and click target are preserved. The icon/name already have `raycastTarget=false`, so they do not intercept the button. `Validate` now rejects a Receive layer above either sibling.
- Static related-screen review: lobby `PortraitPanel` already has its `ReferenceFrame` first, followed by `IdlePortrait`; no opaque covering button. UpgradeDetail likewise has its frame first; OneLevel/Maximum are separate small controls, and UpgradePortrait is hidden by existing layout. Collection icon and ordinary button captions use their existing background-first structure. Snapshot: `Planning/Production/DragonUiValidation/card-layer-audit-before.json`.
- Runtime/Editor standalone compile PASS after the fix; no Unity Apply, scene/prefab save, MCP or build performed here. Coordinator must **reapply the helper to both the saved view and scene instance**, then recheck reward choices before/after selecting all three cards and confirmation. The current candidate binary/prefab has not been repaired by this client.

## Candidate-02 reward-title containment fix — READY

- Confirmed in both `candidate-02-targeted/review-1280x720/review-relic-selected.png` and `review-640x360/review-relic-selected.png`: the old 172-wide title at x=-28 extended beyond the 116-wide card; its baseline also sat against the lower border.
- **FeedbackLayout.cs only** in this follow-up, conditional on Dragon mode: card y=80, height=144; title x=10, y=84, width=96, height=48; description y=232, height=80. Coordinates use that file's existing 1084×540 reference convention. Card x/width, icon, dialog, confirmation controls, font asset/font size and all reward logic are unchanged. The title wraps inside the card on up to two lines. Card border now has 10 horizontal / 12 bottom units of space around the title, and the description remains 8 units from the card and confirmation button.
- Measured against **actual candidate font size 21**, preserved at both screen sizes. NeoDunggeunmoPro TTF metrics: maximum current word width 63 canvas units, two line boxes 44 units, available title 85.02×48 canvas units. All 10 catalog names, including `생명의 목걸이`, fit within two lines.
- At **1280×720** the title area is **113.36×64px**, card **136.97×192px**. At **640×360** the title area is **56.68×32px**, card **68.49×96px**, effective font size 14px and two-line box height 29.33px. Static evidence: `Planning/Production/DragonUiValidation/reward-title-fit.json`.
- Runtime/Editor standalone compilation PASS after this change. No Unity/MCP, Apply, prefab/scene persistence or build performed here. Parent still needs to render 1280/640 reward choice/selected states and confirm actual Unity wrapping; TTF measurement is not a Unity screenshot claim.
