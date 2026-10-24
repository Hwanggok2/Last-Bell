"""Summarize existing evidence only; never launches Unity or changes source assets."""
import csv
import json
import sys
from collections import defaultdict
from pathlib import Path

root = Path(sys.argv[1]).resolve()
groups = defaultdict(list)
with (root / "full-1280x720/timeline.csv").open(encoding="utf-8-sig", newline="") as stream:
    for row in csv.DictReader(stream):
        groups[row["scenario"]].append(row)

summary = {"campaign": json.loads((root / "campaign.json").read_text(encoding="utf-8-sig")), "motion": {}, "cases": {}, "boundsOutsideScreen": []}
for name, rows in groups.items():
    motion = {}
    for actor in ("player", "enemy"):
        frames = defaultdict(set)
        for row in rows:
            frames[row[actor + "Motion"]].add(int(row[actor + "Frame"]))
        motion[actor] = {key: sorted(value) for key, value in frames.items()}
    travel = [row for row in rows if row["transition"] == "Travel"]
    item = {"rows": len(rows), "clockMode": rows[0]["clockMode"], "seconds": round(float(rows[-1]["realtime"]) - float(rows[0]["realtime"]), 4), "motions": motion}
    if travel and rows[0]["clockMode"] == "real-frame-fixture":
        first, last = travel[0], travel[-1]
        item["travel"] = {"pixelsFirst": float(first["travel"]), "pixelsLast": float(last["travel"]), "layer0Delta": round(float(last["layer0X"]) - float(first["layer0X"]), 5), "layer1Delta": round(float(last["layer1X"]) - float(first["layer1X"]), 5)}
        delta = item["travel"]["layer1Delta"]
        item["travel"]["layerDeltaRatio"] = round(item["travel"]["layer0Delta"] / delta, 5) if delta else None
    summary["motion"][name] = item

for directory in sorted(root.iterdir()):
    result = directory / "result.json"
    if not result.is_file():
        continue
    data = json.loads(result.read_text(encoding="utf-8-sig"))
    summary["cases"][directory.name] = {"passed": data["passed"], "completed": data["completed"], "errors": data["errors"], "error": data["error"], "checks": len(data["checks"]), "pngCount": len(list(directory.glob("*.png"))), "resolution": [data["width"], data["height"]]}
    for layout in directory.glob("*-layout.json"):
        data = json.loads(layout.read_text(encoding="utf-8-sig"))
        for element in data["elements"]:
            box = element["bounds"]
            # Rect JSON uses x/y/width/height; geometric overflow is a review hint,
            # not a clipping verdict (a large centered text box can be intentional).
            x, y, w, h = (box.get(key, 0) for key in ("x", "y", "width", "height"))
            if x < -1 or y < -1 or x + w > data["width"] + 1 or y + h > data["height"] + 1:
                summary["boundsOutsideScreen"].append({"file": str(layout.relative_to(root)), "name": element["name"], "text": element.get("text"), "bounds": box})

(root / "analysis.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({"cases": summary["cases"], "motion": summary["motion"], "boundsOutsideScreen": len(summary["boundsOutsideScreen"])}, ensure_ascii=False, indent=2))
