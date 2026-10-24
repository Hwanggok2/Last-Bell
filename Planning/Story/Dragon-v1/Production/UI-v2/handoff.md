# HP UI v2 — READY for bundled Unity integration

2026-10-24. This handoff covers HP styling only; player retargeting/art is owned by the coordinator's art work.

The code-native comparison was created and visually inspected at both target bar sizes before editing runtime code. `index.html`, `compare-1280.png/.svg`, `compare-640.png/.svg`, `render-hp.cjs` and `spec.json` contain the design. HTML preserves native image size and scrolls horizontally on narrow screens instead of shrinking the comparison.

## Implemented scope

- `HealthBarGraphic.cs`: DragonTheme branch only. Nine native rows, stepped shoulders (3/1/0 pixel insets), dark one-pixel outline, gray fixed caps, four fill columns then one dark separator. Highlight is a single flat pixel row. Five colors per active bar; no gradient, numeric label or new decorative symbol.
- `DragonUiTheme.cs`: HP height **24 → 36** source pixels only. Width remains player **160**, enemy **184**, boss **280**. All home projection, center positions, 64px health offset, names, shelf and input layout remain unchanged.
- 1280×720: **160×36 / 184×36 / 280×36**, native cell **4px**. 640×360: **80×18 / 92×18 / 140×18**, cell **2px**. Center unchanged means 6px expansion above/below at 720p, 3px at 360p.
- HP channel excludes caps and separator columns. Capacities are **26 / 31 / 50 drawable columns**. Existing `FilledCells` ceiling rule remains: zero is empty, any positive HP shows at least one column, 100% fills all health columns. Session HP, SetValue, input and gameplay code are untouched. Legacy non-Dragon branch is untouched.

## Inspection / static checks

- Viewed both generated PNGs at native size: caps/steps and block gaps remain visible at 640; tiny HP is distinguishable from zero; no extra symbols or numerals inside the bars. Palette remains consistent with Dragon paper UI.
- `raster-checks.json`: **30 samples** (3 widths × 5 HP states × 2 scales). Zero has no HP-colored pixels; tiny positive has exactly one native column; 25/50/100 increase monotonically; no more than five bar colors.
- Standalone Runtime + Editor compilation passed after the two C# edits, using `Tools/check-dragon-ui-compile.ps1`. Six existing JSON DTO field warnings only. Logs remain in `Planning/Production/DragonUiValidation/`.
- **No Unity, MCP, Apply, scene/prefab persistence or build performed.** Coordinator can include these two code edits with Player-v2 and run the final bundled QA once. Verify actual HP raster sharpness, center position and enemy-name spacing at 1280/640 with 0/tiny/25/50/100 values. Preview raster checks are not a claim of Unity render validation.

## Candidate-01 HP pixel-alignment correction — READY / FROZEN

- Actual 640 player bounds were `(210,84.3870926), 80×18` (bottom-left screen convention); enemy had the same fractional Y. The matching 1280 bounds were `(420,168.7741852), 160×36`. Although each native cell is an integer 2px/4px, the projected origin was not. At 640, recorded boundary colors such as `(126,165,146)` / `(75,151,132)` sit between intended cap/highlight/fill colors. Canvas is Overlay with pixelPerfect off; project current quality is 5 with 2× MSAA. This is fractional-edge raster coverage, not an extra color in the HP mesh.
- **Only DragonUiTheme.cs changed in this correction.** Immediately after positioning each of the two HP roots, `SnapHealthToScreenPixels` projects its bottom-left edge to actual screen pixels, rounds it, converts the translation back to the parent space, and shifts the bar. Each axis moves at most 0.5 physical pixels. 640 sample becomes y=84 (−0.3870926px); 1280 becomes y=169 (+0.2258148px). Width, height, colors, capacities, HP value and all other layout are unchanged. No global Canvas/MSAA changes.
- Physical-screen rounding is intentional: rounding `anchoredPosition` directly would round Canvas units, which are scaled by 2/3 at 640 and would still leave fractional screen edges. Scale-factor changes also invalidate the cached HP placement, covering 1280→640 resize even when the logical canvas stays 960×540.
- Static evidence `hp-pixel-alignment.json` checks all 30 recorded HP bounds; max translation remains under half a pixel and sizes/cells stay unchanged. Runtime/Editor standalone compile PASS after this patch. **No Unity/MCP/Apply/build performed here**; coordinator should rebuild and repeat only the HP render captures/palette check, including resize if convenient. New Unity render output is still pending.
