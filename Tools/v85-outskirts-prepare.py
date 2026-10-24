"""Download reviewed PixelLab sources and add transparent motion margins only."""
import concurrent.futures,json,pathlib,urllib.request,hashlib,sys
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v85/outskirts"
data=json.loads((ART/"jobs.json").read_text(encoding="utf-8"))
selected=set(sys.argv[1:])&{row["id"] for row in data["jobs"]}
if selected:data["jobs"]=[row for row in data["jobs"] if row["id"] in selected]
tasks=[]
for row in data["jobs"]:
    tasks.append((row["baseSelectedJob"],0,ART/row["id"]/"base.png"))
    if row.get("stunPoseJob"):tasks.append((row["stunPoseJob"],0,ART/row["id"]/"stun-pose.png"))
    for motion in ("idle","attack","stun"):
        if "--bases-only" in sys.argv:continue
        if "--skip-stun" in sys.argv and motion=="stun":continue
        if "--idle-only" in sys.argv and motion!="idle":continue
        if "--attack-only" in sys.argv and motion!="attack":continue
        if "--stun-only" in sys.argv and motion!="stun":continue
        if row.get(motion+"Job"):
            count=row.get(motion+"OutputFrames",17 if motion=="attack" else 9)
            for i in range(count):tasks.append((row[motion+"Job"],i,ART/row["id"]/row.get(motion+"Dir",motion)/f"{i:02}.png"))
def fetch(task):
    job,i,target=task
    if target.exists():return
    target.parent.mkdir(parents=True,exist_ok=True)
    raw=urllib.request.urlopen(f"https://api.pixellab.ai/mcp/images/{job}/download?index={i}",timeout=60).read()
    assert raw.startswith(b"\x89PNG\r\n\x1a\n")
    target.write_bytes(raw)
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:list(pool.map(fetch,tasks))
proof={}
for row in data["jobs"]:
    key=row["id"];proof[key]={}
    for source,target in [("base.png","animation-input.png"),("stun-pose.png","stun-input.png")]:
        p=ART/key/source
        if not p.exists():continue
        im=Image.open(p).convert("RGBA");b=im.getbbox();offset=(32,80-b[3])
        padded=Image.new("RGBA",(128,96));padded.paste(im,offset);padded.save(ART/key/target)
        assert padded.crop((offset[0],offset[1],offset[0]+64,offset[1]+64)).tobytes()==im.tobytes()
        proof[key][target]={"source":source,"sourceBounds":b,"offset":offset,"pixelPreserving":True,"referenceHeight":b[3]-b[1],"sha256":hashlib.sha256((ART/key/target).read_bytes()).hexdigest()}
(ART/"padding-proof.json").write_text(json.dumps(proof,indent=2),encoding="utf-8")
print("Downloaded sources and verified pixel-exact transparent padding.")

