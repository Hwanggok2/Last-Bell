"""Retain all PixelLab base candidates and render actual-game-size style comparisons."""
import concurrent.futures, json, pathlib, urllib.request, sys
from PIL import Image,ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v85/outskirts"
data=json.loads((ART/"jobs.json").read_text(encoding="utf-8"))
rows=[x for x in data["jobs"] if len(sys.argv)==1 or x["id"] in sys.argv[1:]]
def fetch(task):
    row,index=task
    target=ART/row["id"]/"candidates"/f"{index:02}.png"
    if target.exists():return
    target.parent.mkdir(parents=True,exist_ok=True)
    raw=urllib.request.urlopen(f'https://api.pixellab.ai/mcp/images/{row["baseJob"]}/download?index={index}',timeout=60).read()
    assert raw.startswith(b"\x89PNG\r\n\x1a\n")
    target.write_bytes(raw)
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    list(pool.map(fetch,[(r,i) for r in rows for i in range(r["candidates"])]))
hero=Image.open(ROOT/data["style"]["reference"]).convert("RGBA")
heroBounds=hero.getbbox()
for row in rows:
    key=row["id"]
    sheet=Image.new("RGB",(4*352,4*224+30),"#1b2d3d")
    draw=ImageDraw.Draw(sheet)
    draw.text((8,8),key+" / ORIGINAL hero + native64 candidates / at approximate 960x540 gameplay sizes",fill="white")
    for i in range(row["candidates"]):
        x,y=(i%4)*352,30+(i//4)*224
        im=Image.open(ART/key/"candidates"/f"{i:02}.png").convert("RGBA")
        bounds=im.getbbox();ref=bounds[3]-bounds[1]
        hero_scale=1.1*540/6.2/84
        monster_scale=row["height"]*540/6.2/ref
        hero_zoom=hero.resize((round(hero.width*hero_scale),round(hero.height*hero_scale)),Image.Resampling.NEAREST)
        monster_zoom=im.resize((round(im.width*monster_scale),round(im.height*monster_scale)),Image.Resampling.NEAREST)
        baseline=y+195
        sheet.paste(hero_zoom,(x+6,baseline-round(heroBounds[3]*hero_scale)),hero_zoom)
        sheet.paste(monster_zoom,(x+170-round(bounds[0]*monster_scale),baseline-round(bounds[3]*monster_scale)),monster_zoom)
        draw.text((x+5,y+5),f"candidate {i:02} / opaque {bounds[2]-bounds[0]}x{ref}",fill="#f9d296")
    sheet.save(ART/(key+"-candidates-game-scale.png"))
print("Downloaded candidate originals and game-scale comparison sheets.")

