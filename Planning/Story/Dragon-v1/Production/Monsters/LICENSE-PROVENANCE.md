# Dragon-v1 monster production provenance

Generated for this project with the user's authorization to use PixelLab. Original game images are stored unchanged in `canonical.png` and each clip's `raw/` folder. Production selections and any later correction are separately identified in `manifest.json`; selection does not overwrite a generated source.

## Sources

- Approved project concept: `Planning/Story/Dragon-v1/images/monster-pixel-v2.png`.
- Combat proportions: `Planning/Story/Dragon-v1/images/battle-normal-clean-v4.png` and `battle-boss-clean-v4.png`.
- First-ten-stage species and names: `Planning/Story/Dragon-v1/story.json`.
- Three normal canonicals: PixelLab `create_image_pro_flash`, text descriptions and explicit 6–9-color palettes. Complete requests are in `canonical-requests.json`; IDs/seeds are in `jobs.json`.
- Two boss canonicals: the normal rabbit/boar canonical as owned style reference, new ProFlash image geometry. Full requests, reference IDs and resulting IDs are in `boss-jobs.json`.
- Motion: PixelLab `animate_image_pixminimax`, actual regenerated anatomy from each canonical. Full requests, seeds, job IDs and provider receipts are in `jobs.json` and `additional-jobs.json`.
- A frame's original output URL is `https://api.pixellab.ai/mcp/images/JOB_ID/download?index=SOURCE_INDEX`. Input index 0 is included in the provider's output; it is not an additional generated pose.

## Provider terms checked 2026-10-24

PixelLab [Terms of Service](https://www.pixellab.ai/termsofservice), last updated 2025-11-23, sections 1.3 and 3.3 permit commercial/noncommercial use, modification and distribution of generated content; model training requires written permission. Section 4.1 incorporates Open RAIL-M. The [provider FAQ](https://www.pixellab.ai/docs/faq) likewise states commercial use is allowed.

This is a record of the provider's permission, not a claim that every generated pixel has exclusive copyright protection. No third-party game sprite was uploaded as a generation input. Existing project concept art is the visual brief; normal source PNGs and derived bosses are generated for this project.

## Scope

This package covers three normal base forms and two boss body variants for the 1–10-stage MVP. Stage aliases may share a base form. It is not fifty unique monster designs. White damage flash, particles and environmental attack VFX remain runtime responsibilities; they must not be baked into these body clips.
