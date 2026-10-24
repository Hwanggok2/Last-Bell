"""Package approved v85 source frames without modifying Unity assets."""
import hashlib,json,pathlib
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v85/outskirts"
jobs=json.loads((ART/"jobs.json").read_text(encoding="utf-8"))
reviews=json.loads((ART/"review-decisions.json").read_text(encoding="utf-8"))
textures,actors=[],[]
textureByPath={}
for row in jobs["jobs"]:
    key=row["id"];decision=reviews[key]
    base=Image.open(ART/key/"animation-input.png").convert("RGBA");b=base.getbbox()
    def texture(source,target):
        im=Image.open(source)
        t={"path":f"Assets/Game/Art/Presentation/Clips/monster-v85/{key}/{target}.png","source":source.relative_to(ROOT).as_posix(),
           "sha256":hashlib.sha256(source.read_bytes()).hexdigest(),"width":im.width,"height":im.height,"pivot":{"x":64,"y":80}}
        if t['path'] not in textureByPath:
            textures.append(t);textureByPath[t['path']]=t
        else:assert textureByPath[t['path']]['sha256']==t['sha256']
        return t["path"]
    basePath=texture(ART/key/"animation-input.png","base")
    def clip(motion,frames,indices,holds,job,loop=False,contact=-1,fallback=False):
        return {"motion":motion,"sourceId":f"v85-{key}-{motion.lower()}","sourceJob":job,
            "note":"Original hero used directly for style. Reviewed native PixelLab frames; transparent padding only. Hit uses runtime white flash. No death.",
            "frames":frames,"sourceFrames":indices,"frameDurationsMs":holds,"fps":1000/(sum(holds)/len(holds)),
            "loop":loop,"contactFrame":contact,"referenceHeightPixels":0,"lowPoseFrames":[],"recoverFrames":[],"faceAnchors":[],
            "effectOrigin":decision["attack"]["contactPoint"] if motion=="Attack" else {"x":64,"y":48},
            "staticFallback":fallback,"legacyStun":False}
    clips=[]
    for motion in ["idle","attack","stun"]:
        d=decision[motion];indices=d["sourceFrames"]
        frames=[texture(ART/key/row.get(motion+"Dir",motion)/f"{i:02}.png",f"{motion}/{i:02}") for i in indices]
        clips.append(clip(motion.title(),frames,indices,d["holdsMs"],row[motion+"Job"],motion!="attack",d.get("contactFrame",-1)))
    clips.append(clip("Hit",[basePath],[0],[120],row["baseSelectedJob"],fallback=True))
    actors.append({"id":key,"facesRight":row["facesRight"],"height":row["height"],"referenceHeightPixels":b[3]-b[1],"clips":clips})
result={"version":"v85","status":"reviewed-source-fragment-not-yet-Unity-imported",
    "ownership":"Parent copies textures and merges these actors; preserve original hero and unrelated actors.",
    "textures":textures,"actors":actors,"qa":"review-decisions.json","provenance":"jobs.json"}
(ART/"manifest-fragment.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(f"Prepared {len(actors)} actors / {len(textures)} textures; no Unity edits.")

