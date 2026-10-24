"""Produce a reviewable manifest fragment; the parent integration owns Unity mutations."""
import hashlib, json, pathlib
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[1]
ART = ROOT / "Planning/Story/Monster-v84/outskirts"
jobs = json.loads((ART / "jobs.json").read_text(encoding="utf-8"))
review = json.loads((ART / "review-decisions.json").read_text(encoding="utf-8"))
textures, actors = [], []
for row in jobs["jobs"]:
    key = row["id"]
    decision = review[key]
    base = Image.open(ART / key / "animation-input.png")
    box = base.getbbox()
    def texture(src, target):
        image = Image.open(src)
        target = f"Assets/Game/Art/Presentation/Clips/monster-v84/{key}/{target}.png"
        textures.append({
            "path": target, "source": src.relative_to(ROOT).as_posix(),
            "sha256": hashlib.sha256(src.read_bytes()).hexdigest(),
            "width": image.width, "height": image.height, "pivot": {"x":64, "y":80}
        })
        return target
    idle = texture(ART/key/"animation-input.png", "idle")
    def clip(motion, frames, indices, holds, source_job, contact=-1, loop=False, fallback=False):
        return {
            "motion":motion, "sourceId":f"v84-{key}-{motion.lower()}",
            "sourceJob":source_job,
            "note":"Reviewed PixelLab pixel-grid animation. Transparent padding without resampling; palette drift corrected by PixelLab to shared16. Stun has a dedicated slumped pose. Hit uses runtime white flash. No death.",
            "frames":frames, "sourceFrames":indices, "frameDurationsMs":holds,
            "fps":25, "loop":loop, "contactFrame":contact, "referenceHeightPixels":0,
            "lowPoseFrames":[], "recoverFrames":[], "faceAnchors":[],
            "effectOrigin":decision["attack"]["contactPoint"] if motion=="Attack" else {"x":32,"y":60},
            "staticFallback":fallback, "legacyStun":False
        }
    clips = [
        clip("Idle",[idle],[0],[1000],row["baseJob"],loop=True,fallback=True),
        clip("Hit",[idle],[0],[120],row["baseJob"],fallback=True)
    ]
    for motion in ("attack", "stun"):
        indices = decision[motion]["sourceFrames"]
        frames = [texture(ART/key/row.get(motion+"Dir",motion)/f"{i:02}.png",f"{motion}/{i:02}") for i in indices]
        clips.append(clip(motion.title(),frames,indices,decision[motion]["holdsMs"],
                          row[motion+"Job"],decision[motion].get("contactFrame",-1),motion=="stun"))
    actors.append({
        "id":key, "facesRight":False,"height":decision["worldHeight"],
        "referenceHeightPixels":box[3]-box[1],"clips":clips
    })
out = {
    "version":"v84", "status":"reviewed-source-fragment-not-yet-Unity-imported",
    "ownership":"Parent integration copies textures and merges these actors into the existing art manifest; preserve all unrelated actors.",
    "textures":textures, "actors":actors,
    "qa":"review-decisions.json", "provenance":"jobs.json"
}
(ART/"manifest-fragment.json").write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(f"Prepared {len(actors)} actors / {len(textures)} textures; did not modify Unity.")

