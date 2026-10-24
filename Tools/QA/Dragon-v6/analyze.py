"""Read-only V6 observation integrity report. It never grants visual/acceptance PASS."""
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
    errors, cases, visual_warnings = [], [], []
    campaign_path = root / "campaign.json"
    campaign = read(campaign_path) if campaign_path.is_file() else {}
    health_only = bool(campaign.get("runs")) and all(r.get("mode") == "v6-hp-only" for r in campaign["runs"])
    for resolution in ("1280x720", "640x360"):
        for mode in (("fresh",) if health_only else ("fresh", "resume")):
            case = root / resolution / mode
            required = ["result.json", "v6-evidence.json", "v6-frame-evidence.json", "v6-state.jsonl", "v6-input.jsonl", "frames.jsonl"]
            missing = [name for name in required if not (case / name).is_file()]
            if missing:
                errors.append({"case": str(case.relative_to(root)), "missing": missing})
                continue
            result, evidence, frame_evidence = (read(case / name) for name in required[:3])
            frames, states, inputs = lines(case / "frames.jsonl"), lines(case / "v6-state.jsonl"), lines(case / "v6-input.jsonl")
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
            if evidence.get("staticCaptures", 0) + len(frames) != len(list(case.glob("*.png"))):
                errors.append({"case": str(case), "pngCountMismatch": True})
            for state in states:
                if state.get("totalParticles", 0) > 376:
                    errors.append({"case": str(case), "particleBudgetExceeded": state["file"]})
            for tag in sorted({s["tag"] for s in states}):
                overlaps = [s for s in states if s["tag"] == tag and any(p.get("overlapsStaggerRect", False) for p in s.get("popups", []))]
                if overlaps:
                    visual_warnings.append({"case": str(case.relative_to(root)), "tag": tag,
                                            "firstFrame": overlaps[0]["file"], "lastFrame": overlaps[-1]["file"], "samples": len(overlaps),
                                            "reason": "Popup rectangle intersects stagger HUD; inspect actual glyph pixels, rectangle can include empty space."})
            comparisons = []
            for path in sorted((case / "fixtures").glob("*/*-comparison.json")) + sorted(case.glob("native-*-comparison.json")):
                comparison = read(path)
                comparisons.append({"file": str(path.relative_to(case)), **comparison})
                if not comparison.get("passed", False):
                    errors.append({"case": str(case), "dtoComparisonFailed": str(path.relative_to(case))})
            lightning = [s for s in states if s["tag"].startswith("v6-boss-lightning-")]
            for stage in sorted({s["stage"] for s in lightning}):
                samples = [s for s in lightning if s["stage"] == stage]
                minimum = min(s["lightningHeadroom"] for s in samples)
                if minimum < 48:
                    visual_warnings.append({"case": str(case.relative_to(root)), "stage": stage,
                                            "minimumHeadroomUi": minimum,
                                            "reason": "Lightning contact-only fallback risk; inspect original contact PNGs before visual verdict."})
            observed = evidence.get("events", [])
            if mode == "fresh" and result.get("testCase") not in ("v6-tail", "v6-route-tail", "v6-hp-only"):
                for companion in ("tori", "barun", "doto", "rina", "momo"):
                    for proc in (False, True):
                        expected_fixture = "v6-companion-" + companion + ("-proc" if proc else "-basic") + "-seed-fixture"
                        hits = [e for e in observed if e["fixture"] == expected_fixture and e["kind"] == "CompanionDamaged"]
                        if len(hits) != 1 or bool(hits[0].get("skill")) != proc:
                            errors.append({"case": str(case), "missingCompanionContact": expected_fixture})
                for player in ("on", "dam", "rin"):
                    if not any(e["fixture"] == "v6-skill-" + player and e["kind"] == "PlayerSkillContact" for e in observed):
                        errors.append({"case": str(case), "missingIndependentSkill": player})
            if not health_only and mode == "fresh" and resolution == "1280x720" and evidence.get("routeStages") != list(range(1, 21)):
                errors.append({"case": str(case), "continuousRouteIncomplete": evidence.get("routeStages")})
            if health_only:
                health_cases = evidence.get("healthCases", [])
                if {h["target"] for h in health_cases} != {"player", "normal", "boss"} or len(health_cases) != 3:
                    errors.append({"case": str(case), "missingHealthTargets": True})
                for health in health_cases:
                    if any(not health.get(k) or not (case / health[k]).is_file() for k in ("first", "middle", "end")):
                        errors.append({"case": str(case), "missingHealthCapture": health["target"]})
            checks = result.get("checks", [])
            cases.append({"case": str(case.relative_to(root)), "probePassed": result.get("passed", False),
                          "completed": result.get("completed", False), "error": result.get("error"),
                          "checksPassed": sum(x.startswith("PASS ") for x in checks),
                          "checksFailed": [x for x in checks if not x.startswith("PASS ")],
                          "consoleErrors": result.get("errors", []), "frames": len(frames), "stateSamples": len(states),
                          "staticCaptures": evidence.get("staticCaptures"), "inputEdges": len(inputs),
                          "fixtures": evidence.get("fixtures", []), "pending": evidence.get("pending", []),
                          "routeStages": evidence.get("routeStages", []), "fileRoundtrips": evidence.get("fileRoundtrips", []),
                          "continuationOf": evidence.get("continuationOf"), "popupChecks": evidence.get("popupChecks", []),
                          "stunActiveFrame": evidence.get("stunActiveFrame"), "stunEndedFrame": evidence.get("stunEndedFrame"),
                          "healthCases": evidence.get("healthCases", []),
                          "dtoComparisons": comparisons,
                          "maximumActualParticles": max((s["totalParticles"] for s in states), default=0)})
    return {"candidate": str(root), "integrityPassed": not errors and len(cases) == (2 if health_only else 4), "errors": errors, "cases": cases,
            "visualWarnings": visual_warnings,
            "continuation": campaign.get("continuation"),
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
