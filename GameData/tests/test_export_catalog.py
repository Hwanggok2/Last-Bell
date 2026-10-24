import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import export_catalog as catalog


class CatalogExportTests(unittest.TestCase):
    def setUp(self):
        self.data = json.loads(catalog.DEFAULT_SOURCE.read_text(encoding="utf-8-sig"))

    def reject(self, edit, expected):
        edit(self.data)
        with self.assertRaises(catalog.CatalogValidationError) as result:
            catalog.validate(self.data)
        self.assertIn(expected, str(result.exception))

    def test_current_source_and_mvp_totals(self):
        catalog.validate(self.data)
        e = self.data["economy"]
        self.assertEqual(sum(e["upgradeCosts"]), 4178943264)
        self.assertEqual(sum(g * (5 if s % 10 == 0 else 3 if s % 5 == 0 else 1) for s, g in enumerate(e["normalGoldPerStage"]) if s), 3971419624)
        self.assertEqual(5 * (e["minibossFragments"] + e["bossFragments"]), 20)
        self.assertEqual(self.data["stages"][9]["maxHp"], 865)
        self.assertEqual(self.data["stages"][10]["enemyId"], "sewer_rat")
        self.assertEqual(self.data["stages"][9]["enemyId"], "bell_knight")
        self.assertEqual(self.data["stages"][19]["enemyId"], "chain_jailer")

    def test_duplicate_relic_id(self):
        self.reject(lambda d: d["relics"][1].update(id=d["relics"][0]["id"]), "duplicate ID")

    def test_duplicate_playable_enemy(self):
        self.reject(lambda d: d["stages"][3].update(enemyId=d["stages"][0]["enemyId"]), "distinct enemies")

    def test_renaming_duplicate_art_does_not_bypass_roster_rule(self):
        first_art = next(e["artId"] for e in self.data["enemies"] if e["id"] == self.data["stages"][0]["enemyId"])
        self.reject(lambda d: next(e for e in d["enemies"] if e["id"] == "red_fox").update(artId=first_art), "distinct enemy art")

    def test_current_presentation_timing_and_story_are_exported(self):
        self.assertEqual(self.data["combat"]["attackWindupMs"], 200)
        t = self.data["transitions"]
        self.assertEqual(t["enemyDefeatMs"] + t["travelMs"] + t["arrivalMs"], 3250)
        self.assertEqual(t["deathMs"], 1800)
        self.assertEqual([t["minibossIntroMs"], t["bossIntroMs"]], [1800, 2200])
        boss=next(s for s in self.data["stories"] if s["stage"]==10)
        self.assertEqual(boss["lines"][0]["text"], "와, 큰엄니도 물리쳤네! 이제 숲으로 갈 수 있겠다.")

    def test_invalid_transition_duration(self):
        self.reject(lambda d: d["transitions"].update(travelMs=0), "duration must be positive")

    def test_boss_story_cannot_change_to_unbound_stage_or_unknown_character(self):
        self.reject(lambda d: next(s for s in d["stories"] if s["stage"]==10).update(stage=5), "invalid scene stages")
        self.setUp()
        self.reject(lambda d: d["stories"][0]["lines"][2]["responses"].append({"characterId": "unknown", "text": "반응"}), "invalid response")

    def test_dialogue_portraits_and_player_replies_are_complete(self):
        self.reject(lambda d: d["stories"][0]["lines"][0].update(portraitId="missing"), "invalid portrait")
        self.setUp()
        self.reject(lambda d: next(s for s in d["stories"] if s["stage"]==10)["lines"][2]["responses"].pop(), "all player responses")
        self.setUp()
        self.reject(lambda d: next(s for s in d["stories"] if s["stage"]==10)["lines"][2].update(portraitId="on"), "invalid portrait/player")

    def test_encounter_stages_are_distinct_and_tori_reveals_identity(self):
        encounter=next(s for s in self.data["stories"] if s["stage"]==2)
        self.assertEqual(encounter["lines"][0]["speaker"],"???")
        self.assertEqual(encounter["lines"][0]["portraitId"],"unknown")
        self.assertEqual(encounter["lines"][1]["portraitId"],"tori")
        self.assertEqual(encounter["bossId"],"")
        self.reject(lambda d: d["stories"].append(copy.deepcopy(d["stories"][-1])), "invalid scene stages")

    def test_adventure_scope_and_legacy_scenes(self):
        self.assertEqual(sorted(s["stage"] for s in self.data["stories"]), [2,10,12,20])
        self.assertEqual(sorted(s["stage"] for s in self.data["plannedStories"]), [22,32,42])
        self.assertEqual(sorted(s["stage"] for s in self.data["legacyStories"]), [2,7,10,15,20])
        self.assertEqual(self.data["mvpLastStage"],20)
        self.assertEqual([c["stars"][4] for c in self.data["characters"]],[3,4,2])
        self.reject(lambda d:d["plannedStories"][0].update(stage=21),"invalid scene stages")

    def test_companion_mismatch_and_duplicate(self):
        self.reject(lambda d:d["companions"][0].update(periodMs=6180),"invalid timing")
        self.setUp()
        self.reject(lambda d:d["companions"][1].update(id="tori"),"distinct stable IDs")

    def test_unknown_enemy_reference(self):
        self.reject(lambda d: d["stages"][0].update(enemyId="not_in_catalog"), "unknown enemy")

    def test_playable_character_cannot_lose_its_skill(self):
        self.reject(lambda d: d["skills"].pop(), "every available character")

    def test_skill_cannot_duplicate_owner_or_create_permanent_shield(self):
        self.reject(lambda d: d["skills"][1].update(characterId="on"), "one skill per known character")
        self.setUp()
        self.reject(lambda d: d["skills"][1].update(shieldDurationMs=0), "shield amount and duration")

    def test_single_pattern_exception_is_only_for_stage11_dash(self):
        catalog.validate(self.data)
        self.reject(lambda d: d["stages"][0].update(randomPatterns=["frontal_dash"]), "invalid random pattern pool")

    def test_stage_gap(self):
        self.reject(lambda d: d["stages"][8].update(number=10), "ordered unique floors")

    def test_unsafe_combo_hit_gap(self):
        self.reject(lambda d: d["stages"][4]["patternSteps"][1].update(intervalMs=259), "intervalMs")

    def test_orphan_followup(self):
        self.reject(lambda d: d["stages"][0]["patternSteps"][0].update(comboStep=2, telegraphMs=250), "followups must be consecutive")

    def test_incomplete_combo(self):
        self.reject(lambda d: d["stages"][0]["patternSteps"][0].update(comboStep=1, telegraphMs=900), "combo start must be followed")

    def test_wrong_dodge_for_danger(self):
        self.reject(lambda d: d["stages"][2]["patternSteps"][0].update(requiredDodge="up"), "danger and requiredDodge mismatch")

    def test_unknown_field_not_silently_ignored(self):
        self.reject(lambda d: d["economy"].update(exhangeGoldPerFragment=999), "unknown field")

    def test_missing_cost_field(self):
        self.reject(lambda d: d["economy"].pop("upgradeCosts"), "missing field")

    def test_bad_numeric_types(self):
        for value in (True, 2**40, "120"):
            with self.subTest(value=value):
                data = copy.deepcopy(self.data)
                data["stages"][0]["maxHp"] = value
                with self.assertRaises(catalog.CatalogValidationError):
                    catalog.validate(data)

    def test_nan_and_float32_overflow(self):
        for value in (float("nan"), 1e100):
            with self.subTest(value=value):
                data = copy.deepcopy(self.data)
                data["growth"]["attackBase"] = value
                with self.assertRaises(catalog.CatalogValidationError):
                    catalog.validate(data)

    def test_relic_levels_must_increase(self):
        self.reject(lambda d: d["relics"][0].update(values=[.04, .05, .05, .07, .08]), "increasing total effects")

    def test_export_is_deterministic_and_invalid_input_preserves_output(self):
        with tempfile.TemporaryDirectory(prefix="lastbell-catalog-", dir=Path(__file__).parent) as folder:
            source, output = Path(folder) / "source.json", Path(folder) / "generated.json"
            source.write_text(json.dumps(self.data), encoding="utf-8")
            self.assertTrue(catalog.export(source, output)["written"])
            before, modified = output.read_bytes(), output.stat().st_mtime_ns
            self.assertFalse(catalog.export(source, output)["written"])
            self.assertEqual(output.stat().st_mtime_ns, modified)
            self.assertTrue(catalog.export(source, output, check=True)["checked"])
            self.data["stages"][0]["enemyId"] = "missing"
            source.write_text(json.dumps(self.data), encoding="utf-8")
            with self.assertRaises(catalog.CatalogValidationError):
                catalog.export(source, output)
            self.assertEqual(output.read_bytes(), before)
            self.assertEqual(output.stat().st_mtime_ns, modified)

    def test_check_rejects_stale_output_without_writing(self):
        with tempfile.TemporaryDirectory(prefix="lastbell-catalog-", dir=Path(__file__).parent) as folder:
            source, output = Path(folder) / "source.json", Path(folder) / "generated.json"
            source.write_text(json.dumps(self.data), encoding="utf-8")
            output.write_bytes(b"previous output")
            with self.assertRaisesRegex(ValueError, "missing or stale"):
                catalog.export(source, output, check=True)
            self.assertEqual(output.read_bytes(), b"previous output")

    def test_independent_skill_contact_and_companion_growth_are_validated(self):
        self.reject(lambda d: d["skills"][0].update(contactDelayMs=0), "independent 200ms")
        self.reject(lambda d: d["skills"][0].update(cooldownMs=10000), "20000ms cooldown")
        self.reject(lambda d: d["adventure"].update(companionSkillChanceCapPercent=100), "expected 45")
        self.reject(lambda d: d["adventure"].update(companionPeriodFloorPercent=50), "expected 70")

    def test_companion_special_effects_cannot_silently_drift(self):
        self.reject(lambda d: d["companions"][1].update(skillStaggerGain=80), "invalid skill")
        self.reject(lambda d: d["companions"][4].update(skillHealMaxHpPercent=20), "invalid skill")
        self.reject(lambda d: d["companions"][2].update(skillId="companion_tori_double_shot"), "invalid skill")

    def test_duplicate_json_key_rejected(self):
        with tempfile.TemporaryDirectory(prefix="lastbell-catalog-", dir=Path(__file__).parent) as folder:
            source = Path(folder) / "source.json"
            source.write_text('{"schemaVersion":1,"schemaVersion":2}', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "duplicate JSON object key"):
                catalog.prepare(source)


if __name__ == "__main__":
    unittest.main()
