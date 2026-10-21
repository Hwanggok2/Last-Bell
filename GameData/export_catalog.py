"""Validate the editable JSON and atomically export a JsonUtility-compatible catalog.

This tool never imports balance values from Planning: mvp-catalog.json is the source.
No third-party packages are required. All numbers in errors use JSON paths.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "GameData/mvp-catalog.json"
DEFAULT_OUTPUT = ROOT / "Client/Assets/Game/Data/Generated/mvp-catalog.json"
NOTICE = "GENERATED from GameData/mvp-catalog.json; DO NOT EDIT. Run python GameData/export_catalog.py."
DIRECTIONS = {"low_sweep": "up", "thrust": "back", "high_sweep": "down", "outer_arc": "forward"}
EFFECTS = {"attackPercent", "maxHpPercent", "normalStaggerPercent", "perfectStaggerPercent", "stunDamageBonus", "damageReduction", "critChanceBonus"}


def fields(names: str, kind: type) -> dict:
    return dict.fromkeys(names.split(), kind)


COMBAT = {
    **fields("dodgeActiveMs dodgeRecoveryMs perfectWindowMs comboMinGapMs normalStagger perfectStagger staggerThreshold damageStaggerPenalty attackIntervalMs attackWindupMs normalStunMs minibossStunMs bossStunMs postStunImmunityMs singleTelegraphMs comboOverviewMs followupHighlightMs maxVisibleThreats hpUnitsPerPoint", int),
    **fields("critDamageMultiplier critChanceCap goldLuckBonusCap stunDamageMultiplierCap", float),
}
GROWTH = {
    **fields("minLevel maxLevel effectiveMilestoneLevels", int),
    "breakthroughLevels": [int], "starScale": [float],
    **fields("attackBase attackLinearPerLevel attackBreakBase attackBreakPerStar hpBase hpLinearPerLevel hpBreakBase hpBreakPerStar critBase critPerEffectiveLevel goldBonusPerEffectiveLevel resonanceBase resonancePerEffectiveLevel", float),
}
ECONOMY = {
    **fields("initialGold initialFragments minibossFragments bossFragments minibossGoldMultiplier bossGoldMultiplier exchangeGoldPerFragment", int),
    **dict.fromkeys("normalGoldPerZone maxRelicGoldPerZone upgradeCosts slotCosts slotUnlockStages".split(), [int]),
    "ngGoldMultiplier": float,
}
CHARACTER = {**fields("id displayName artId", str), "stars": [int], "availableInMvp": bool}
ENEMY = fields("id displayName artId", str)
STEP = {**fields("danger requiredDodge", str), **fields("telegraphMs intervalMs comboStep", int)}
STAGE = {**fields("number maxHp damage", int), **fields("enemyId kind teachDodge", str), "patternSteps": [STEP]}
RELIC = {**fields("id displayName effect effectLabel", str), "cap": float, "values": [float]}
SCHEMA = {
    **fields("schemaVersion mvpLastStage boundaryStage", int),
    **fields("dataVersion sourcePlanningVersion generatedNotice sourceSha256 defaultCharacterId", str),
    "combat": COMBAT, "growth": GROWTH, "economy": ECONOMY,
    "relicRules": {**fields("maxLevel maxEquipped choicesPerBoss", int), "includeMaxInPool": bool},
    "characters": [CHARACTER], "enemies": [ENEMY], "stages": [STAGE], "relics": [RELIC],
}


class CatalogValidationError(ValueError):
    def __init__(self, errors: list[str]):
        self.errors = errors
        super().__init__("\n".join(errors))


def _shape(value, spec, path: str, errors: list[str]) -> None:
    if isinstance(spec, dict):
        if not isinstance(value, dict):
            errors.append(f"{path}: expected object")
            return
        for key in sorted(set(spec) - set(value)):
            errors.append(f"{path}.{key}: missing field")
        for key in sorted(set(value) - set(spec)):
            errors.append(f"{path}.{key}: unknown field (JsonUtility would ignore it)")
        for key in spec:
            if key in value:
                _shape(value[key], spec[key], f"{path}.{key}", errors)
    elif isinstance(spec, list):
        if not isinstance(value, list):
            errors.append(f"{path}: expected array")
            return
        for i, item in enumerate(value):
            _shape(item, spec[0], f"{path}[{i}]", errors)
    elif spec is float:
        if type(value) not in (int, float) or not math.isfinite(value) or abs(value) > 3.402823466e38:
            errors.append(f"{path}: expected finite float32 number")
    elif type(value) is not spec:
        errors.append(f"{path}: expected {spec.__name__}")
    elif spec is int and not (-2147483648 <= value <= 2147483647):
        errors.append(f"{path}: outside C# Int32 range")


def validate(data: dict) -> None:
    errors: list[str] = []
    _shape(data, SCHEMA, "$", errors)
    if errors:
        raise CatalogValidationError(errors)

    def need(condition: bool, path: str, message: str) -> None:
        if not condition:
            errors.append(f"{path}: {message}")

    need(data["schemaVersion"] == 1, "$.schemaVersion", "supported version is 1")
    for key in ("dataVersion", "sourcePlanningVersion", "generatedNotice"):
        need(bool(data[key].strip()), f"$.{key}", "must not be blank")
    need(data["mvpLastStage"] == 10 and data["boundaryStage"] == 11, "$", "MVP must contain 1..10 and boundary 11")
    ids = {}
    for table in ("characters", "enemies", "relics"):
        seen = set()
        need(bool(data[table]), f"$.{table}", "must not be empty")
        for i, row in enumerate(data[table]):
            path = f"$.{table}[{i}]"
            need(bool(re.fullmatch(r"[a-z][a-z0-9_]*", row["id"])), path + ".id", "use stable lower_snake_case ID")
            need(row["id"] not in seen, path + ".id", "duplicate ID")
            seen.add(row["id"])
            need(bool(row["displayName"].strip()), path + ".displayName", "must not be blank")
            if "artId" in row:
                need(bool(re.fullmatch(r"[a-z][a-z0-9_]*", row["artId"])), path + ".artId", "use stable asset ID")
        ids[table] = seen
    need(data["defaultCharacterId"] in ids["characters"], "$.defaultCharacterId", "unknown character")
    for i, row in enumerate(data["characters"]):
        need(len(row["stars"]) == 4 and all(1 <= x <= 5 for x in row["stars"]), f"$.characters[{i}].stars", "four 1..5 values: attack/hp/luck/resonance")
    default = next((x for x in data["characters"] if x["id"] == data["defaultCharacterId"]), None)
    need(default is not None and default["availableInMvp"], "$.defaultCharacterId", "must be available in MVP")

    c, g, e, rr = (data[x] for x in ("combat", "growth", "economy", "relicRules"))
    for key, value in c.items():
        need(value >= 0 if key == "dodgeRecoveryMs" else value > 0, "$.combat." + key, "invalid nonpositive value")
    need(c["perfectWindowMs"] <= c["dodgeActiveMs"], "$.combat.perfectWindowMs", "must fit inside active dodge")
    need(c["attackWindupMs"] < c["attackIntervalMs"], "$.combat.attackWindupMs", "must be shorter than attack interval")
    need(c["comboMinGapMs"] >= max(450, c["dodgeActiveMs"] + c["dodgeRecoveryMs"]), "$.combat.comboMinGapMs", "must allow dodge reuse and be at least 450ms")
    need(c["hpUnitsPerPoint"] == 1000, "$.combat.hpUnitsPerPoint", "MVP saves integer milli-HP")
    need(0 < c["critChanceCap"] <= 1 and 0 < c["goldLuckBonusCap"] <= 1, "$.combat", "probability/bonus cap must be in (0,1]")
    need(c["followupHighlightMs"] <= c["comboMinGapMs"], "$.combat.followupHighlightMs", "must fit inside hit interval")
    need(g["minLevel"] == 0 and g["maxLevel"] == 50, "$.growth", "supported level range is 0..50")
    need(g["breakthroughLevels"] == [11, 21, 31, 41], "$.growth.breakthroughLevels", "expected 10->11, 20->21, 30->31, 40->41")
    need(len(g["starScale"]) == 5 and all(x > 0 for x in g["starScale"]) and all(a < b for a, b in zip(g["starScale"], g["starScale"][1:])), "$.growth.starScale", "five positive increasing coefficients required")
    for key, value in g.items():
        if key not in ("minLevel", "maxLevel", "starScale", "breakthroughLevels"):
            need(value > 0, "$.growth." + key, "must be positive")
    for key in ("initialGold", "initialFragments"):
        need(e[key] >= 0, "$.economy." + key, "must be nonnegative")
    need(e["minibossFragments"] == 1 and e["bossFragments"] == 3, "$.economy", "fixed per-kill fragments must be 1/3")
    need(e["exchangeGoldPerFragment"] == 100, "$.economy.exchangeGoldPerFragment", "MVP exchange is 1 fragment -> 100 gold")
    need(e["minibossGoldMultiplier"] > 0 and e["bossGoldMultiplier"] > 0 and e["ngGoldMultiplier"] == 1, "$.economy", "invalid gold multipliers")
    for key in ("normalGoldPerZone", "maxRelicGoldPerZone"):
        need(len(e[key]) == 5 and all(x > 0 for x in e[key]), "$.economy." + key, "five positive zone values required")
    need(len(e["upgradeCosts"]) == 51 and e["upgradeCosts"][0] == 0 and all(x > 0 for x in e["upgradeCosts"][1:]), "$.economy.upgradeCosts", "51 values indexed by target level, zero only at initial Lv0")
    need(e["slotCosts"] == [4, 8] and e["slotUnlockStages"] == [10, 20], "$.economy", "MVP slots cost 4/8 and unlock after 10/20")
    need(rr == {"maxLevel": 5, "maxEquipped": 3, "choicesPerBoss": 3, "includeMaxInPool": True}, "$.relicRules", "expected Lv5, three equipped/choices, MAX included")
    need(len(data["relics"]) == 10, "$.relics", "pool must contain ten distinct relics")
    caps = {}
    for i, r in enumerate(data["relics"]):
        path = f"$.relics[{i}]"
        need(r["effect"] in EFFECTS, path + ".effect", "unsupported effect ID")
        need(r["cap"] > 0, path + ".cap", "must be positive")
        need(len(r["values"]) == 5 and all(0 < x <= r["cap"] for x in r["values"]) and all(a < b for a, b in zip(r["values"], r["values"][1:])), path + ".values", "five increasing total effects within cap required")
        if r["effect"] in caps:
            need(caps[r["effect"]] == r["cap"], path + ".cap", "same effect must share the same final cap")
        caps[r["effect"]] = r["cap"]

    numbers = [s["number"] for s in data["stages"]]
    need(numbers == list(range(1, 12)), "$.stages", "must be ordered unique floors 1..11")
    for i, s in enumerate(data["stages"]):
        path = f"$.stages[{i}]"
        need(s["enemyId"] in ids["enemies"], path + ".enemyId", "unknown enemy")
        expected_kind = "boss" if s["number"] % 10 == 0 else "miniboss" if s["number"] % 5 == 0 else "normal"
        need(s["kind"] == expected_kind, path + ".kind", "inconsistent with floor")
        need(s["maxHp"] > 0 and s["damage"] > 0, path, "HP and damage must be positive")
        need(s["teachDodge"] in ("", *DIRECTIONS.values()), path + ".teachDodge", "unknown direction")
        steps = s["patternSteps"]
        need(bool(steps), path + ".patternSteps", "ordered steps required")
        for j, step in enumerate(steps):
            sp = path + f".patternSteps[{j}]"
            need(step["danger"] in DIRECTIONS and DIRECTIONS.get(step["danger"]) == step["requiredDodge"], sp, "danger and requiredDodge mismatch")
            n = step["comboStep"]
            cue = c["singleTelegraphMs"] if n == 0 else c["comboOverviewMs"] if n == 1 else c["followupHighlightMs"]
            need(n >= 0 and step["telegraphMs"] == cue, sp + ".telegraphMs", "cue must match single/start/followup contract")
            need(step["intervalMs"] >= max(c["comboMinGapMs"], step["telegraphMs"]), sp + ".intervalMs", "hit interval must fit telegraph and minimum gap")
            if n == 1:
                need(j + 1 < len(steps) and steps[j + 1]["comboStep"] == 2, sp + ".comboStep", "combo start must be followed by step 2")
            if n > 1:
                need(j > 0 and steps[j - 1]["comboStep"] == n - 1, sp + ".comboStep", "followups must be consecutive after a start")
        if 1 <= s["number"] <= 4:
            direction = ("up", "back", "down", "forward")[s["number"] - 1]
            need(s["teachDodge"] == direction and bool(steps) and steps[0]["requiredDodge"] == direction, path, "floors 1..4 introduce up/back/down/forward on the first attack")
    if errors:
        raise CatalogValidationError(errors)


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON object key: {key}")
        result[key] = value
    return result


def prepare(source: Path) -> tuple[bytes, dict]:
    raw = source.read_bytes()
    data = json.loads(raw.decode("utf-8-sig"), object_pairs_hook=_unique_object)
    validate(data)
    data["generatedNotice"] = NOTICE
    data["sourceSha256"] = hashlib.sha256(raw).hexdigest()
    output = (json.dumps(data, ensure_ascii=False, indent=2, allow_nan=False) + "\n").encode("utf-8")
    return output, data


def export(source: Path = DEFAULT_SOURCE, output: Path = DEFAULT_OUTPUT, check: bool = False, validate_only: bool = False) -> dict:
    content, data = prepare(source)
    unchanged = output.is_file() and output.read_bytes() == content
    if check and not unchanged:
        raise ValueError(f"{output}: generated catalog missing or stale; run exporter")
    if not check and not validate_only and not unchanged:
        output.parent.mkdir(parents=True, exist_ok=True)
        temporary = None
        try:
            with tempfile.NamedTemporaryFile(mode="wb", prefix=".catalog-", suffix=".tmp", dir=output.parent, delete=False) as handle:
                temporary = Path(handle.name)
                handle.write(content)
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temporary, output)
        finally:
            if temporary is not None and temporary.exists():
                temporary.unlink()
    return {"valid": True, "schemaVersion": data["schemaVersion"], "dataVersion": data["dataVersion"], "sourceSha256": data["sourceSha256"], "stages": len(data["stages"]), "relics": len(data["relics"]), "written": not (check or validate_only or unchanged), "checked": check}


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="fail if generated file differs; never write")
    mode.add_argument("--validate-only", action="store_true", help="only validate editable source")
    args = parser.parse_args(argv)
    try:
        result = export(args.source, args.output, args.check, args.validate_only)
    except (OSError, ValueError, UnicodeError) as error:
        print(f"{args.source}: {error}", file=sys.stderr)
        return 1
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
