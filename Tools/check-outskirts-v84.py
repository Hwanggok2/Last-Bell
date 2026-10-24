"""Validate delivered original frames and packing metadata, with no Unity mutation."""
import hashlib,json,pathlib
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v84/outskirts"
report={"checks":[], "failures":[]}
def check(name,ok):
    report["checks"].append({"name":name,"ok":bool(ok)})
    if not ok: report["failures"].append(name)
for key in ["wild_dog","grey_wolf","brown_bear"]:
    for original,padded in [("base.png","animation-input.png"),("stun-pose.png","stun-input.png")]:
        source=Image.open(ART/key/original).convert("RGBA")
        out=Image.open(ART/key/padded).convert("RGBA")
        offset=80-source.getbbox()[3]
        check(key+" "+padded+" contains exact original pixel bytes",out.crop((32,offset,96,offset+64)).tobytes()==source.tobytes())
manifest=json.loads((ART/"manifest-fragment.json").read_text(encoding="utf-8"))
palette={tuple(bytes.fromhex(s[1:])) for s in json.loads((ART/"jobs.json").read_text(encoding="utf-8"))["style"]["palette"]}
for tex in manifest["textures"]:
    path=ROOT/tex["source"]
    check(tex["source"]+" SHA256",hashlib.sha256(path.read_bytes()).hexdigest()==tex["sha256"])
    im=Image.open(path).convert("RGBA")
    check(tex["source"]+" dimensions",im.size==(128,96)==(tex["width"],tex["height"]))
    check(tex["source"]+" binary alpha",set(im.getchannel("A").getdata())<={0,255})
    check(tex["source"]+" shared palette",{p[:3] for p in im.getdata() if p[3]}<=palette)
    b=im.getbbox()
    check(tex["source"]+" not clipped",b[0]>0 and b[1]>0 and b[2]<128 and b[3]<96)
for actor in manifest["actors"]:
    check(actor["id"]+" allowed clips",set(c["motion"] for c in actor["clips"])=={"Idle","Hit","Attack","Stun"})
    for clip in actor["clips"]:
        check(actor["id"]+" "+clip["motion"]+" frame holds",len(clip["frames"])==len(clip["sourceFrames"])==len(clip["frameDurationsMs"]))
        if clip["motion"]=="Attack":
            check(actor["id"]+" contact frame valid",0<=clip["contactFrame"]<len(clip["frames"]))
report["passed"]=sum(c["ok"] for c in report["checks"])
report["total"]=len(report["checks"])
(ART/"validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
print(f'{report["passed"]}/{report["total"]} source checks pass')
assert not report["failures"], report["failures"]

