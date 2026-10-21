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
        self.assertEqual(sum(e["upgradeCosts"]), 42297564)
        self.assertEqual(sum(g * (5 if s % 10 == 0 else 3 if s % 5 == 0 else 1) for s, g in enumerate(e["normalGoldPerStage"]) if s), 172833824)
        self.assertEqual(5 * (e["minibossFragments"] + e["bossFragments"]), 20)
        self.assertEqual(self.data["stages"][9]["maxHp"], 706)
        self.assertEqual(self.data["stages"][10]["enemyId"], "chain_thrall")

    def test_duplicate_relic_id(self):
        self.reject(lambda d: d["relics"][1].update(id=d["relics"][0]["id"]), "duplicate ID")

    def test_current_presentation_timing_and_story_are_exported(self):
        self.assertEqual(self.data["combat"]["attackWindupMs"], 200)
        t = self.data["transitions"]
        self.assertEqual(t["enemyDefeatMs"] + t["travelMs"] + t["arrivalMs"], 3250)
        self.assertEqual(t["deathMs"], 1800)
        self.assertEqual([t["minibossIntroMs"], t["bossIntroMs"]], [1800, 2200])
        self.assertEqual(self.data["stories"][0]["stage"], 10)
        self.assertEqual(self.data["stories"][0]["lines"][0]["text"], "문은… 안에서 잠갔다.")

    def test_invalid_transition_duration(self):
        self.reject(lambda d: d["transitions"].update(travelMs=0), "duration must be positive")

    def test_story_cannot_reference_miniboss_or_unknown_character(self):
        self.reject(lambda d: d["stories"][0].update(stage=5), "MVP story")
        self.setUp()
        self.reject(lambda d: d["stories"][0]["lines"][2]["responses"][0].update(characterId="unknown"), "invalid character")

    def test_unknown_enemy_reference(self):
        self.reject(lambda d: d["stages"][0].update(enemyId="not_in_catalog"), "unknown enemy")

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

    def test_duplicate_json_key_rejected(self):
        with tempfile.TemporaryDirectory(prefix="lastbell-catalog-", dir=Path(__file__).parent) as folder:
            source = Path(folder) / "source.json"
            source.write_text('{"schemaVersion":1,"schemaVersion":2}', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "duplicate JSON object key"):
                catalog.prepare(source)


if __name__ == "__main__":
    unittest.main()
