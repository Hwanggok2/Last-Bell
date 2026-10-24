"""Read-only V5 observation integrity report. It never grants visual/acceptance PASS."""
import argparse
import hashlib
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def lines(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def analyze(root):
    root = root.resolve(strict=True)
    errors, cases = [], []
    for resolution in ("1280x720", "640x360"):
        for mode in ("fresh", "resume"):
            case = root / resolution / mode
            required = ["result.json", "v5-evidence.json", "v5-frame-evidence.json", "v5-state.jsonl", "v5-input.jsonl", "frames.jsonl"]
            missing = [name for name in required if not (case / name).is_file()]
            if missing:
                errors.append({"case": str(case.relative_to(root)), "missing": missing})
                continue
            result, evidence, frame_evidence = (read(case / name) for name in required[:3])
            frames, states, inputs = lines(case / "frames.jsonl"), lines(case / "v5-state.jsonl"), lines(case / "v5-input.jsonl")
            seen = set()
            for frame in frames:
                name = frame["file"]
                path = (case / name).resolve()
                if path.parent != case.resolve() or path.is_symlink():
                    errors.append({"case": str(case), "unsafeFrame": name})
                    continue
                if name in seen or not path.is_file():
                    errors.append({"case": str(case), "missingOrDuplicateFrame": name})
                    continue
                seen.add(name)
                if hashlib.sha256(path.read_bytes()).hexdigest() != frame["sha256"].lower():
                    errors.append({"case": str(case), "hashMismatch": name})
            for state in states:
                if state.get("file") and not (case / state["file"]).is_file():
                    errors.append({"case": str(case), "stateCaptureMissing": state["file"]})
            segments = frame_evidence.get("segments", [])
            if frame_evidence.get("writtenFrames") != len(frames) or sum(s["frames"] for s in segments) != len(frames):
                errors.append({"case": str(case), "frameCountMismatch": True})
            if any(s.get("missingRenderedFrames", 0) for s in segments):
                errors.append({"case": str(case), "missingRenderedFrames": True})
            if evidence["stateSamples"] != len(states) or evidence["inputEdges"] != len(inputs):
                errors.append({"case": str(case), "stateOrInputCountMismatch": True})
            checks = result.get("checks", [])
            cases.append({"case": str(case.relative_to(root)), "probePassed": result.get("passed", False),
                          "completed": result.get("completed", False), "error": result.get("error"),
                          "checksPassed": sum(x.startswith("PASS ") for x in checks),
                          "checksFailed": [x for x in checks if not x.startswith("PASS ")],
                          "consoleErrors": result.get("errors", []), "frames": len(frames), "stateSamples": len(states),
                          "staticCaptures": evidence.get("staticCaptures"), "inputEdges": len(inputs),
                          "fixtures": evidence.get("fixtures", []), "pending": evidence.get("pending", [])})
    campaign_path = root / "campaign.json"
    campaign = read(campaign_path) if campaign_path.is_file() else {}
    return {"candidate": str(root), "integrityPassed": not errors and len(cases) == 4, "errors": errors, "cases": cases,
            "observationPassed": campaign.get("observationPassed", False), "acceptance": "NOT_CERTIFIED",
            "visualReview": "NOT_RUN", "audioListening": "NOT_RUN", "osInput": "DEFERRED",
            "userDefaultProfile": campaign.get("userDefaultProfile", "NOT_ACCESSED"),
            "limits": "Metadata and hash integrity only. Open original PNGs for visual review; inspect exact fixture scopes and manual clock gaps."}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("candidate", type=Path)
    args = parser.parse_args()
    summary = analyze(args.candidate)
    print(json.dumps(summary, ensure_ascii=True, indent=2))
    raise SystemExit(0 if summary["integrityPassed"] and summary["observationPassed"] else 1)
