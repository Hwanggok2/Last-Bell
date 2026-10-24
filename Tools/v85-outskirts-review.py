"""Render source-frame QA and measure grounded Idle loops; game assets are never painted."""
import json,pathlib,hashlib
from PIL import Image,ImageDraw,ImageChops
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v85/outskirts"
data=json.loads((ART/"jobs.json").read_text(encoding="utf-8"))
hero=Image.open(ROOT/data["style"]["reference"]).convert("RGBA")
comparison=Image.new("RGB",(3*420,300),"#1b2d3d");draw=ImageDraw.Draw(comparison)
metrics={}
for slot,row in enumerate(data["jobs"]):
    key=row["id"];metrics[key]={}
    base=Image.open(ART/key/"animation-input.png").convert("RGBA")
    box=base.getbbox();ref=box[3]-box[1]
    hscale=1.1*540/6.2/84;mscale=row["height"]*540/6.2/ref
    hp=hero.resize((round(128*hscale),round(128*hscale)),Image.Resampling.NEAREST)
    mp=base.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if row["facesRight"] else base
    mp=mp.resize((round(128*mscale),round(96*mscale)),Image.Resampling.NEAREST)
    baseline=235;x=slot*420
    comparison.paste(hp,(x+10,baseline-round(114*hscale)),hp)
    comparison.paste(mp,(x+270-round(64*mscale),baseline-round(80*mscale)),mp)
    draw.text((x+8,15),key+" / ORIGINAL hero / actual enemy LEFT",fill="#f9d296")
    draw.text((x+8,36),f"height {row['height']} / ref {ref}px / original hero unchanged",fill="white")
    for motion in ("idle","attack","stun"):
        folder=ART/key/row.get(motion+"Dir",motion)
        paths=sorted(folder.glob("*.png"))
        if not paths:continue
        rows=(len(paths)+5)//6
        sheet=Image.new("RGB",(6*260,rows*224+30),"#20343d");sd=ImageDraw.Draw(sheet)
        sd.text((8,7),key+" "+motion+" / source orientation / nearest x2",fill="white")
        records=[];first=Image.open(paths[0]).convert("RGBA")
        for i,path in enumerate(paths):
            im=Image.open(path).convert("RGBA");b=im.getbbox()
            d=ImageChops.difference(first,im)
            foot=im.crop((0,76,128,82))
            records.append({"frame":i,"path":path.relative_to(ROOT).as_posix(),"bounds":b,"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
              "footBandSha256":hashlib.sha256(foot.tobytes()).hexdigest(),"pixelSha256":hashlib.sha256(im.tobytes()).hexdigest(),"changedFromFirst":sum(1 for pixel in d.getdata() if any(pixel)),
              "groundXsAt79":[x for x in range(128) if im.getpixel((x,79))[3]],
              "uniqueColors":len({p[:3] for p in im.getdata() if p[3]}),
              "fractionalAlpha":sum(0<p[3]<255 for p in im.getdata()),
              "edgeTouch":b[0]==0 or b[1]==0 or b[2]==128 or b[3]==96})
            x2,y2=i%6*260,30+i//6*224;zoom=im.resize((256,192),Image.Resampling.NEAREST)
            sheet.paste(zoom,(x2+2,y2+22),zoom);sd.text((x2+5,y2+3),f"{i:02}",fill="white")
        sheet.save(ART/f"{key}-{motion}-review.png")
        metrics[key][motion]=records
comparison.save(ART/"selected-bases-game-scale.png")
(ART/"frame-metrics.json").write_text(json.dumps(metrics,indent=2),encoding="utf-8")
print("Saved source-motion sheets, foot-band hashes and actual-game-scale original-hero comparison.")

