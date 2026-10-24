"""Enlarge reviewed contact frames with coordinates for integration QA only."""
import pathlib, json
from PIL import Image, ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v84/outskirts"
jobs=json.loads((ART/"jobs.json").read_text(encoding="utf-8"))["jobs"]
for key,frame in [("wild_dog",8),("grey_wolf",11),("brown_bear",7)]:
    source=next(row for row in jobs if row["id"]==key).get("attackDir","attack")
    im=Image.open(ART/key/source/f"{frame:02}.png").convert("RGBA")
    sheet=Image.new("RGB",(1080,830),"#20343d")
    zoom=im.resize((1024,768),Image.Resampling.NEAREST)
    sheet.paste(zoom,(40,40),zoom)
    d=ImageDraw.Draw(sheet)
    for x in range(0,129,8):
        d.line((40+x*8,40,40+x*8,808),fill="#29404a")
        d.text((40+x*8,20),str(x),fill="#e4d9bb")
    for y in range(0,97,8):
        d.line((40,40+y*8,1064,40+y*8),fill="#29404a")
        d.text((10,40+y*8),str(y),fill="#e4d9bb")
    d.text((10,812),key+f" contact frame{frame} / top-left pixel coordinates",fill="white")
    sheet.save(ART/(key+"-contact-grid.png"))

