"""Read-only source/texture preflight. Does not import assets or launch Unity/player."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).parent / "verification.json"
read = lambda path: json.loads((ROOT / path).read_text(encoding="utf-8-sig"))
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
checks = []
def check(ok, label):
    checks.append({"label": label, "passed": bool(ok)})
    if not ok:
        raise AssertionError(label)

bindings = read("Planning/Production/dragon-v3-art-bindings.json")
manifest_path = "Client/Assets/Game/Art/Presentation/manifest.json"
runtime = read(manifest_path)
check(sha(ROOT / manifest_path) == bindings["manifestSha256"], "runtime manifest matches integration hash")
actors = {a["id"]: a for a in runtime["actors"]}
textures = {t["path"]: t for t in runtime["textures"]}
rows, packages, selected_ids = [], [], set()
source_paths = set()
for package_entry in bindings["packages"]:
    package = read(package_entry["path"])
    check(package["status"] == "ready", package_entry["path"] + " ready")
    check(sha(ROOT / package_entry["path"]) == package_entry["sha256"], package_entry["path"] + " hash")
    packages.append(package_entry)
    folder = (ROOT / package_entry["path"]).parent
    for source in package["actors"]:
        actor_id = "dragon_" + source["id"]
        selected_ids.add(actor_id)
        actor = actors[actor_id]
        check(abs(actor["height"] - source["referenceHeight"] * source["recommendedIntegerScale"] * 6.2 / 720) < 1e-6, actor_id + " body scale")
        clips = {c["motion"]: c for c in actor["clips"]}
        for motion, original in source["clips"].items():
            clip = clips[motion]
            pivot = original.get("pivotTopLeft") or original.get("feetPivotTopLeft") or source["pivotTopLeft"]
            check(len(clip["frames"]) == len(original["frames"]), actor_id + "/" + motion + " frame count")
            check(clip["sourceFrames"] == original.get("sourceIndices", list(range(len(clip["frames"])))), actor_id + "/" + motion + " source order")
            check(clip.get("frameDurationsMs", []) == original.get("frameDurationsMs", []) and clip["fps"] == original["fps"] and clip["loop"] == original["loop"], actor_id + "/" + motion + " holds/fps/loop")
            check(clip["contactFrame"] == (-1 if original.get("contactFrame") is None else original["contactFrame"]), actor_id + "/" + motion + " contact")
            face = original.get("faceAnchorTopLeft") or source.get("faceAnchorTopLeft")
            expected_faces = original.get("faceAnchorsTopLeft") or ([face] * len(clip["frames"]) if face else [])
            check(clip.get("faceAnchors", []) == [{"x": x, "y": y} for x, y in expected_faces], actor_id + "/" + motion + " face anchors")
            hashes = set()
            for i, target in enumerate(clip["frames"]):
                image_path = ROOT / original["frames"][i]
                if not image_path.is_file():
                    image_path = folder / original["frames"][i]
                source_paths.add(str(image_path.resolve()))
                target_path = ROOT / "Client" / target
                texture = textures[target]
                check(sha(image_path) == sha(target_path) == texture["sha256"], target + " source/runtime hash")
                check(texture["pivot"] == {"x": pivot[0], "y": pivot[1]}, target + " pivot")
                if source["id"] != "hero_on":
                    check(pivot == source["pivotTopLeft"], target + " common actor feet pivot")
                with Image.open(image_path) as image:
                    rgba = image.convert("RGBA")
                    check(rgba.size == (source["width"], source["height"]), target + " dimensions")
                    check(set(rgba.getchannel("A").get_flattened_data()) <= {0, 255}, target + " binary alpha")
                    hashes.add(hashlib.sha256(rgba.tobytes()).hexdigest())
            holds = clip.get("frameDurationsMs") or [1000 / clip["fps"]] * len(clip["frames"])
            contact = clip["contactFrame"]
            rows.append({"actor": actor_id, "motion": motion, "frames": len(clip["frames"]), "uniquePixelFrames": len(hashes), "pivotTopLeft": pivot, "faceAnchors": len(expected_faces), "contactFrame": contact, "contactMs": sum(holds[:contact]) if contact >= 0 else None, "durationMs": sum(holds), "postContactMs": sum(holds[contact:]) if contact >= 0 else None})

forest_rows = [r for r in rows if r["actor"] != "dragon_hero_on"]
player_rows = [r for r in rows if r["actor"] == "dragon_hero_on"]
check(len(forest_rows) == 30 and sum(r["frames"] for r in forest_rows) == 235, "forest 30 clips / 235 references")
check(sum(r["frames"] for r in player_rows) == 66, "player 66 references")
check(sum(r["frames"] for r in rows) == bindings["textureReferences"] == 301, "301 new references")
retained = [a for a in runtime["actors"] if a["id"] not in selected_ids]
check(len(retained) == bindings["preservedOtherActors"] == 38, "38 other actor entries retained")
for actor_id, expected_count in [("dragon_goblin_slinger", 6), ("dragon_moss_mushroom", 7), ("dragon_gib", 6)]:
    row = next(r for r in rows if r["actor"] == actor_id and r["motion"] == "Attack")
    check(row["frames"] == expected_count, actor_id + " variable Attack count")
for actor_id in ("dragon_goblin_guard", "dragon_gib"):
    row = next(r for r in rows if r["actor"] == actor_id and r["motion"] == "Attack")
    check(row["contactFrame"] == 4 and row["contactMs"] == 200 and row["durationMs"] == 500, actor_id + " 200ms contact / 500ms duration")
gib = next(r for r in rows if r["actor"] == "dragon_gib" and r["motion"] == "Attack")
check(gib["uniquePixelFrames"] == 4 and gib["postContactMs"] == 300 and gib["pivotTopLeft"] == [24, 45], "gib four poses / 300ms post-contact / common pivot")

result = {"status": "PASS_SOURCE_PREFLIGHT_ONLY", "packages": packages, "runtimeManifestSha256": sha(ROOT / manifest_path), "checksPassed": len(checks), "clips": rows, "forestClips": len(forest_rows), "forestReferences": 235, "playerReferences": 66, "newReferences": 301, "uniqueSourcePaths": len(source_paths), "otherActors": {"count": len(retained), "ids": [a["id"] for a in retained], "preservationEvidence": "Current count and importer report agree; importer explicitly asserts equality of old and resulting untouched actor entries. QA did not have a full pre-import JSON snapshot for an independent historical byte comparison."}, "unityRun": False, "nativePlayerRun": False, "visualReviewPerformed": False, "checks": checks}
OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: result[k] for k in ("status", "checksPassed", "forestClips", "forestReferences", "playerReferences", "newReferences", "uniqueSourcePaths")}, ensure_ascii=False))
