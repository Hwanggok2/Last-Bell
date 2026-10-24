"""Download and inspect native PixelLab source frames; no image synthesis or resampling."""
import concurrent.futures, hashlib, json, pathlib, urllib.request, sys
from PIL import Image, ImageDraw

ROOT = pathlib.Path(__file__).resolve().parents[1]
ART = ROOT / "Planning/Story/Monster-v84/outskirts"
data = json.loads((ART / "jobs.json").read_text(encoding="utf-8"))
if len(sys.argv)>1:
    data["jobs"]=[row for row in data["jobs"] if row["id"] in sys.argv[1:]]
tasks = []
for row in data["jobs"]:
    tasks.append((row["baseJob"], 0, ART / row["id"] / "base.png"))
    for motion in ("attack", "stun"):
        if motion + "Job" in row:
            count = row.get(motion + "OutputFrames", 17 if motion == "attack" else 9)
            for index in range(count):
                tasks.append((row[motion + "Job"], index, ART / row["id"] / row.get(motion+"Dir",motion) / f"{index:02}.png"))

def download(task):
    job, index, target = task
    if target.exists():
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    url = f"https://api.pixellab.ai/mcp/images/{job}/download?index={index}"
    raw = urllib.request.urlopen(url, timeout=45).read()
    assert raw.startswith(b"\x89PNG\r\n\x1a\n"), url
    target.write_bytes(raw)

with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    list(pool.map(download, tasks))

metrics = {}
for row in data["jobs"]:
    key = row["id"]
    base = Image.open(ART / key / "base.png").convert("RGBA")
    bbox = base.getbbox()
    padded = Image.new("RGBA", (128, 96))
    padded.paste(base, (32, 80-bbox[3]))
    padded.save(ART / key / "animation-input.png")
    records = []
    paths = [ART / key / "animation-input.png"]
    paths += sorted((ART / key / row.get("attackDir","attack")).glob("*.png"))
    paths += sorted((ART / key / row.get("stunDir","stun")).glob("*.png"))
    # Large nearest-neighbour display is evidence only; source PNGs are unaltered.
    sheet = Image.new("RGB", (6*260, 5*224+40), "#20343d")
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 10), key + "  native grid unchanged / padded 128x96 / nearest x2", fill="#f4e4bd")
    for idx, path in enumerate(paths):
        im = Image.open(path).convert("RGBA")
        alpha = list(im.getchannel("A").getdata())
        colors = {pixel[:3] for pixel in im.getdata() if pixel[3]}
        box = im.getbbox()
        record = {
            "path": path.relative_to(ROOT).as_posix(), "size": list(im.size),
            "bounds": box, "colors": len(colors),
            "fractionalAlphaPixels": sum(0 < x < 255 for x in alpha),
            "touchesCanvasEdge": box is not None and (box[0] == 0 or box[1] == 0 or box[2] == im.width or box[3] == im.height),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        }
        records.append(record)
        x, y = idx%6*260, 40+idx//6*224
        label = "base" if path.name == "animation-input.png" else path.parent.name+" "+path.stem
        draw.text((x+3,y+3), label, fill="white")
        preview = im.resize((256,192), Image.Resampling.NEAREST)
        sheet.paste(preview,(x+2,y+22),preview)
    sheet.save(ART / (key+"-all-frames.png"))
    metrics[key] = records
(ART / "frame-metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
print(f"Downloaded and measured {sum(map(len, metrics.values()))} frames")

