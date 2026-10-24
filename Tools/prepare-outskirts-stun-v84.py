"""Fetch the revised dazed poses and add transparent motion margins without resampling."""
import concurrent.futures, json, pathlib, urllib.request
from PIL import Image, ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/"Planning/Story/Monster-v84/outskirts"
jobs={"grey_wolf":"e6e0714f-8af7-43b9-a819-b29798b672fb","wild_dog":"87606503-5a43-467a-be88-74046a41457f","brown_bear":"ce90fd7d-8034-4649-8336-f4e74b25dafd"}
def fetch(pair):
    key,job=pair
    target=ART/key/"stun-pose.png"
    if not target.exists():
        raw=urllib.request.urlopen(f"https://api.pixellab.ai/mcp/images/{job}/download",timeout=45).read()
        assert raw.startswith(b"\x89PNG\r\n\x1a\n")
        target.write_bytes(raw)
    pose=Image.open(target).convert("RGBA")
    box=pose.getbbox()
    padded=Image.new("RGBA",(128,96))
    padded.paste(pose,(32,80-box[3]))
    padded.save(ART/key/"stun-input.png")
    return key,box
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
    result=list(pool.map(fetch,jobs.items()))
sheet=Image.new("RGB",(3*384,2*310),"#20343d")
draw=ImageDraw.Draw(sheet)
for i,(key,box) in enumerate(result):
    for row,name in enumerate(["animation-input.png","stun-input.png"]):
        im=Image.open(ART/key/name).convert("RGBA")
        zoom=im.resize((384,288),Image.Resampling.NEAREST)
        sheet.paste(zoom,(i*384,row*310+22),zoom)
        draw.text((i*384+5,row*310+3),key+" "+name,fill="white")
sheet.save(ART/"revised-stun-poses.png")
print(json.dumps(result))

