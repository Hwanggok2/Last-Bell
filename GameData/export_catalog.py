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
from adventure_validation import adventure_schema, COMPANION, validate_adventure

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "GameData/mvp-catalog.json"
DEFAULT_OUTPUT = ROOT / "Client/Assets/Game/Data/Generated/mvp-catalog.json"
NOTICE = "GENERATED from GameData/mvp-catalog.json; DO NOT EDIT. Run python GameData/export_catalog.py."
DIRECTIONS = {"low_sweep": "up", "thrust": "back", "high_sweep": "parry", "frontal_dash": "back"}
EFFECTS = {"attackPercent", "maxHpPercent", "normalStaggerPercent", "perfectStaggerPercent", "stunDamageBonus", "damageReduction", "critChanceBonus"}


def fields(names: str, kind: type) -> dict:
    return dict.fromkeys(names.split(), kind)


COMBAT = {
    **fields("dodgeActiveMs dodgeRecoveryMs perfectWindowMs comboMinGapMs normalStagger perfectStagger staggerThreshold damageStaggerPenalty attackIntervalMs attackWindupMs normalStunMs minibossStunMs bossStunMs postStunImmunityMs stunExtensionMs stunExtensionCapMs perfectStreakMaxSteps singleTelegraphMs fastTelegraphMs attackFlightMs fastAttackFlightMs comboDefaultGapMs perfectCounterMs maxVisibleThreats hpUnitsPerPoint", int),
    **fields("critDamageMultiplier critChanceCap goldLuckBonusCap stunDamageMultiplierCap stunAttackSpeedMultiplier minibossStaggerMultiplier bossStaggerMultiplier perfectStreakStep", float),
}
GROWTH = {
    **fields("minLevel maxLevel effectiveMilestoneLevels", int),
    "breakthroughLevels": [int], "starScale": [float],
    **fields("attackBase attackLinearPerLevel attackBreakBase attackBreakPerStar hpBase hpLinearPerLevel hpBreakBase hpBreakPerStar critBase resonanceBase", float),
    **dict.fromkeys("luckCritPerTenLevels luckGoldPerLevel resonancePerLevel".split(), [int]),
}
class Int64:
    """JSON integer represented by a C# long, distinct from Int32 fields."""


ECONOMY = {
    "initialGold": Int64,
    **fields("initialFragments minibossFragments bossFragments minibossGoldMultiplier bossGoldMultiplier", int),
    **dict.fromkeys("normalGoldPerStage maxRelicGoldPerZone upgradeCosts exchangeGoldPerUnlockTier".split(), [Int64]),
    **dict.fromkeys("storageExpansionCosts exchangeTierUnlockBosses".split(), [int]),
    "ngGoldMultiplier": float,
}
CHARACTER = {**fields("id displayName artId", str), "stars": [int], "availableInMvp": bool}
SKILL = {**fields("id characterId displayName description", str), **fields("cooldownMs contactDelayMs damagePercent shieldMaxHpPercent shieldDurationMs staggerGain", int)}
ENEMY = fields("id displayName artId", str)
STEP = {**fields("danger requiredDodge", str), **fields("telegraphMs intervalMs comboStep", int)}
STAGE = {**fields("number maxHp damage", int), **fields("enemyId kind teachDodge", str), "patternSteps": [STEP], "randomPatterns": [str], "sequenceWeights": [int]}
RELIC = {**fields("id displayName effect effectLabel", str), "cap": float, "values": [float]}
TRANSITIONS = fields("enemyDefeatMs travelMs arrivalMs deathMs minibossIntroMs bossIntroMs repeatMinibossIntroMs repeatBossIntroMs", int)
STORY_LINE = {**fields("speaker text portraitId", str), "responses": [fields("characterId text", str)]}
STORY = {**fields("id bossId kind companionId", str), "stage": int, "lines": [STORY_LINE]}
SCHEMA = {
    **fields("schemaVersion mvpLastStage boundaryStage", int),
    **fields("dataVersion sourcePlanningVersion generatedNotice sourceSha256 defaultCharacterId", str),
    "combat": COMBAT, "growth": GROWTH, "economy": ECONOMY,
    "relicRules": {**fields("maxLevel initialCapacity maxOwned choicesPerBoss", int), "includeMaxInPool": bool},
    "characters": [CHARACTER], "skills": [SKILL], "enemies": [ENEMY], "stages": [STAGE], "relics": [RELIC],
    "transitions": TRANSITIONS, "stories": [STORY], "plannedStories": [STORY], "legacyStories": [STORY],
    "adventure": adventure_schema(Int64), "companions": [COMPANION],
    "onboarding": {"prologueDurationMs": int, "prologueTitle": str, "prologueParagraphs": [str], "comboHint": str},
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
    elif spec is Int64:
        if type(value) is not int or not (-9223372036854775808 <= value <= 9223372036854775807):
            errors.append(f"{path}: expected C# Int64 integer")
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

    need(data["schemaVersion"] == 2, "$.schemaVersion", "supported version is 2")
    for key in ("dataVersion", "sourcePlanningVersion", "generatedNotice"):
        need(bool(data[key].strip()), f"$.{key}", "must not be blank")
    need(data["mvpLastStage"] == 20 and data["boundaryStage"] == 21, "$", "MVP must contain 1..10 and boundary 11")
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
        need(len(row["stars"]) == 5 and all(1 <= x <= 5 for x in row["stars"]), f"$.characters[{i}].stars", "five 1..5 values: attack/hp/luck/resonance/recovery")
    default = next((x for x in data["characters"] if x["id"] == data["defaultCharacterId"]), None)
    need(default is not None and default["availableInMvp"], "$.defaultCharacterId", "must be available in MVP")

    skill_ids, skill_characters = set(), set()
    for i, skill in enumerate(data["skills"]):
        path = f"$.skills[{i}]"
        need(bool(re.fullmatch(r"[a-z][a-z0-9_]*", skill["id"])) and skill["id"] not in skill_ids, path+".id", "unique stable ID required")
        need(skill["characterId"] in ids["characters"] and skill["characterId"] not in skill_characters, path+".characterId", "one skill per known character")
        need(bool(skill["displayName"].strip()) and bool(skill["description"].strip()), path, "name and description required")
        need(0 < skill["cooldownMs"] <= 300000 and 0 < skill["damagePercent"] <= 1000, path, "positive bounded cooldown and damage required")
        need(skill["cooldownMs"] == 20000 and skill["contactDelayMs"] == 200, path, "expected independent 200ms contact and 20000ms cooldown")
        need(0 <= skill["shieldMaxHpPercent"] <= 100 and 0 <= skill["shieldDurationMs"] <= 60000 and 0 <= skill["staggerGain"] <= 100, path, "invalid secondary effect")
        need((skill["shieldMaxHpPercent"] > 0) == (skill["shieldDurationMs"] > 0), path, "shield amount and duration must be paired")
        skill_ids.add(skill["id"]); skill_characters.add(skill["characterId"])
    need(all(c["id"] in skill_characters for c in data["characters"] if c["availableInMvp"]), "$.skills", "every available character needs a skill")

    for key, value in data["transitions"].items():
        need(value > 0, "$.transitions." + key, "duration must be positive")
    validate_adventure(data, need)

    c, g, e, rr = (data[x] for x in ("combat", "growth", "economy", "relicRules"))
    for key, value in c.items():
        need(value >= 0 if key == "dodgeRecoveryMs" else value > 0, "$.combat." + key, "invalid nonpositive value")
    need(c["perfectWindowMs"] <= c["dodgeActiveMs"], "$.combat.perfectWindowMs", "must fit inside active dodge")
    need(c["attackWindupMs"] < c["attackIntervalMs"], "$.combat.attackWindupMs", "must be shorter than attack interval")
    need(c["comboMinGapMs"] >= c["dodgeActiveMs"] + c["dodgeRecoveryMs"], "$.combat.comboMinGapMs", "must allow dodge reuse")
    need(c["hpUnitsPerPoint"] == 1000, "$.combat.hpUnitsPerPoint", "MVP saves integer milli-HP")
    need(0 < c["critChanceCap"] <= 1 and 0 < c["goldLuckBonusCap"] <= 2, "$.combat", "probability cap in (0,1], gold bonus cap in (0,2]")
    need(c["singleTelegraphMs"] % 3 == 0 and c["fastTelegraphMs"] % 3 == 0,
         "$.combat", "warnings must have three equal beats")
    need(0 < c["attackFlightMs"] < c["fastTelegraphMs"] <= c["singleTelegraphMs"] and c["comboDefaultGapMs"] >= c["comboMinGapMs"] and c["perfectCounterMs"] > 0, "$.combat", "invalid sequence timing")
    need(g["minLevel"] == 0 and g["maxLevel"] == 50, "$.growth", "supported level range is 0..50")
    need(g["breakthroughLevels"] == [11, 21, 31, 41], "$.growth.breakthroughLevels", "expected 10->11, 20->21, 30->31, 40->41")
    need(len(g["starScale"]) == 5 and all(x > 0 for x in g["starScale"]) and all(a < b for a, b in zip(g["starScale"], g["starScale"][1:])), "$.growth.starScale", "five positive increasing coefficients required")
    for key, value in g.items():
        if key in ("luckCritPerTenLevels", "luckGoldPerLevel", "resonancePerLevel"):
            need(len(value) == 5 and all(x > 0 for x in value) and all(a <= b for a,b in zip(value,value[1:])), "$.growth." + key, "five positive nondecreasing integer star gains required")
        elif key not in ("minLevel", "maxLevel", "starScale", "breakthroughLevels"):
            need(value > 0, "$.growth." + key, "must be positive")
    for key in ("initialGold", "initialFragments"):
        need(e[key] >= 0, "$.economy." + key, "must be nonnegative")
    need(e["minibossFragments"] == 0 and e["bossFragments"] == 4, "$.economy", "boss-only fragments must be 0/4")
    need(e["exchangeTierUnlockBosses"] == [0,10,20,30,40], "$.economy.exchangeTierUnlockBosses", "expected completed boss thresholds 0/10/20/30/40")
    need(len(e["exchangeGoldPerUnlockTier"]) == 5 and e["exchangeGoldPerUnlockTier"][0] == 100 and all(a < b for a,b in zip(e["exchangeGoldPerUnlockTier"],e["exchangeGoldPerUnlockTier"][1:])), "$.economy.exchangeGoldPerUnlockTier", "five increasing exchange rates, initial100")
    need(len(e["normalGoldPerStage"]) == 51 and e["normalGoldPerStage"][0] == 0 and all(a < b for a,b in zip(e["normalGoldPerStage"],e["normalGoldPerStage"][1:])), "$.economy.normalGoldPerStage", "51 increasing values indexed by stage, index0=0")
    need(e["minibossGoldMultiplier"] > 0 and e["bossGoldMultiplier"] > 0 and e["ngGoldMultiplier"] == 1, "$.economy", "invalid gold multipliers")
    for key in ("maxRelicGoldPerZone",):
        need(len(e[key]) == 5 and all(x > 0 for x in e[key]), "$.economy." + key, "five positive zone values required")
    need(len(e["upgradeCosts"]) == 51 and e["upgradeCosts"][0] == 0 and all(x > 0 for x in e["upgradeCosts"][1:]), "$.economy.upgradeCosts", "51 values indexed by target level, zero only at initial Lv0")
    need(all(a < b for a,b in zip(e["upgradeCosts"],e["upgradeCosts"][1:])), "$.economy.upgradeCosts", "cost must increase at every level")
    need(all(x <= 9223372036854775807 // max(e["minibossGoldMultiplier"],e["bossGoldMultiplier"],1) for x in e["normalGoldPerStage"]), "$.economy.normalGoldPerStage", "kind reward must fit Int64")
    need(e["storageExpansionCosts"] == [4, 8, 12, 18, 26, 36, 48], "$.economy.storageExpansionCosts", "expected seven progressive storage expansions")
    need(rr == {"maxLevel": 5, "initialCapacity": 3, "maxOwned": 10, "choicesPerBoss": 3, "includeMaxInPool": True}, "$.relicRules", "expected auto-apply, three initial / ten maximum owned relics")
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
    need(numbers == list(range(1, 22)), "$.stages", "must be ordered unique floors 1..21")
    playable = [s["enemyId"] for s in data["stages"] if s["number"] <= data["mvpLastStage"]]
    need(len(playable) == len(set(playable)), "$.stages", "playable stages must use distinct enemies")
    art_ids = {e["id"]: e["artId"] for e in data["enemies"]}
    playable_art = [art_ids.get(e) for e in playable]
    need(len(playable_art) == len(set(playable_art)), "$.stages", "playable stages must use distinct enemy art")
    for i, s in enumerate(data["stages"]):
        path = f"$.stages[{i}]"
        need(s["enemyId"] in ids["enemies"], path + ".enemyId", "unknown enemy")
        expected_kind = "boss" if s["number"] % 10 == 0 else "miniboss" if s["number"] % 5 == 0 else "normal"
        need(s["kind"] == expected_kind, path + ".kind", "inconsistent with floor")
        need(s["maxHp"] > 0 and s["damage"] > 0, path, "HP and damage must be positive")
        need(s["teachDodge"] in ("", *DIRECTIONS.values()), path + ".teachDodge", "unknown direction")
        steps = s["patternSteps"]
        dash_intro = s["number"] == 11 and s["randomPatterns"] == ["frontal_dash"]
        need((dash_intro or len(set(s["randomPatterns"])) >= 2) and all(x in DIRECTIONS for x in s["randomPatterns"]), path, "invalid random pattern pool")
        need(len(s["sequenceWeights"]) == 3 and all(x >= 0 for x in s["sequenceWeights"]) and sum(s["sequenceWeights"]) == 100, path, "sequence weights must sum to 100")
        need(bool(steps), path + ".patternSteps", "ordered steps required")
        for j, step in enumerate(steps):
            sp = path + f".patternSteps[{j}]"
            need(step["danger"] in DIRECTIONS and DIRECTIONS.get(step["danger"]) == step["requiredDodge"], sp, "danger and requiredDodge mismatch")
            n = step["comboStep"]
            need(n >= 0 and step["telegraphMs"] in (c["singleTelegraphMs"], c["fastTelegraphMs"]), sp + ".telegraphMs", "cue must match default/fast timing")
            need(step["intervalMs"] >= c["comboMinGapMs"] and (n > 1 or step["intervalMs"] >= step["telegraphMs"]), sp + ".intervalMs", "hit interval must allow dodge reuse and full cue")
            need(n <= c["maxVisibleThreats"], sp + ".comboStep", "combo exceeds threat capacity")
            if n > 1:
                need(j > 0 and steps[j-1]["telegraphMs"] == step["telegraphMs"], sp + ".telegraphMs", "combo tempo must match")
            if n == 1:
                need(j + 1 < len(steps) and steps[j + 1]["comboStep"] == 2, sp + ".comboStep", "combo start must be followed by step 2")
            if n > 1:
                need(j > 0 and steps[j - 1]["comboStep"] == n - 1, sp + ".comboStep", "followups must be consecutive after a start")
        if 1 <= s["number"] <= 3:
            direction = ("up", "back", "parry", "forward")[s["number"] - 1]
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
