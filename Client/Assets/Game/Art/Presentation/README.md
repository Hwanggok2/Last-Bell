# Selected presentation art

`PresentationArtCatalog.asset` and `PresentationActor.prefab` are saved Unity assets. `PresentationArtImporter.MigrateExisting()` explicitly validates the complete source manifest, imports the selected frames, updates these assets without replacing their GUIDs, and calls `Validate()`. Nothing is regenerated on Play or script reload.

The catalog uses the latest planning selection: On's approved `hero-approved-dash-20261021-02` source frames01–08 with authored40/40/60/60/40/40/60/60ms holds (400ms loop), v6 upward/downward slashes, v4 four-direction dodges, and v5 Idle/Hit/Death/Return. Enemy Attack and v6 clean Hit sequences retain the selected source order. A contact frame is an index in that selected order; `sourceFrames` records the original PNG indices.

All character PNGs keep their complete 128×128 cells and source foot pivot. Point sampling, no compression/mipmaps, full-rectangle sprite meshes and 100 pixels per unit preserve the source pixels. Canonical opaque heights are On 84, sentinel/chain 86, wisp 88, warden 92 and knight 94 pixels. World heights are 1.1 / 2 / 2 / 2 / 3.2 / 4. Legacy Stun poses use their own atlas reference heights so they do not jump in scale. The actor component never stretches the body or moves the root to simulate an animation.

The 271 copied source PNGs are byte-identical to Planning originals. Ten slash cells are integer rectangle extractions from the existing authored atlas; their decoded RGBA pixels match the source rectangles. `manifest.json` records source paths, hashes, selection, pivots and fallbacks. `prepare-sources.cjs` is an explicit offline maintenance utility, not a Unity/runtime hook; the source images are read-only. Its optional `LASTBELL_CANVAS_MODULE` variable locates the Canvas decoder for atlas extraction.

## Remaining authored-art gaps

- Monster Idle: five canonical single-frame poses, not new continuous animations.
- Stun: six existing single-frame prototype poses, not new continuous animations.
- Monster Death: selected existing recoil sequence under shader dissolve, not a newly drawn death clip.
- Boss Roar: the existing attack sequence played once, with face anchors; no separately authored roar frames.

`ActorView.SampleMotion` is presentation-only. The director supplies elapsed time, movement, contact timing, pause and transitions; Core owns hits and rewards. Manual sampling lasts one frame and supports `EndSample`/`ClearMotionOverride`. `SampleDodgeImpact` holds the down-dodge low poses through impact + 140 ms, then recovers by +220 ms. `_Flash` makes the visible silhouette entirely white for 45 ms and fades by 120 ms. `_Dissolve` clips stable 4-source-pixel blocks, reaching fully invisible at 1. No orange hit tint remains.

Source-copy/hash/RGBA checks and an isolated Roslyn compile against Unity 6000.3.15f1 assemblies passed. Unity import, shader rendering and integrated PlayMode verification belong to the root integration pass; this file does not claim those were performed by the art task.

Approved run integration (2026-10-21): all8 PNG bytes match their approved sources. The shared source pivot(60,111) preserves the airborne lift; scale remains1.1 world units /84 source pixels. Optional `frameDurationsMs` takes precedence over uniform fps; legacy contact remapping is unchanged. Unity104 EditMode/14 PlayMode tests and100 actual Windows travel captures passed. See `Planning/Evidence/ApprovedRun-20261021/verification.json`.
