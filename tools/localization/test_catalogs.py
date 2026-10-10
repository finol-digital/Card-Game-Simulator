"""Regression fixtures for contributor edits, invalid input and repeatable updates."""

from copy import deepcopy
from pathlib import Path
import tempfile
import unittest

from catalog import CatalogError, load_json, manifest_codes, serialize, source_hash, tags, validate_catalogs
from merge_drafts import merge


class CatalogTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.manifest = {"schemaVersion": 1, "locales": [
            {"code": "en", "nativeName": "English", "aliases": []},
            {"code": "es", "nativeName": "Español", "aliases": []}]}
        self.source = {"schemaVersion": 1, "locale": "en", "entries": {
            "cards.count": {"text": "<b>{count}</b> cards", "context": "Number of cards in a deck.",
                            "arguments": {"count": "Integer card count."}, "smart": True}}}
        self.translation = {"schemaVersion": 1, "locale": "es", "entries": {
            "cards.count": {"text": "<b>{count}</b> cartas", "status": "machine",
                            "sourceHash": source_hash(self.source["entries"]["cards.count"]),
                            "provenance": {"method": "test fixture", "date": "2026-10-04"}}}}

    def write(self):
        for name, data in (("locales", self.manifest), ("en", self.source), ("es", self.translation)):
            (self.directory / f"{name}.json").write_text(serialize(data), encoding="utf-8")

    def validate(self):
        self.write()
        return validate_catalogs(self.directory)

    def test_valid_catalog_reports_machine_coverage(self):
        errors, warnings, reports = self.validate()
        self.assertEqual([], errors)
        self.assertEqual([], warnings)
        self.assertIn("es: 1/1 valid entries; machine=1", reports[-1])

    def test_duplicate_json_keys_rejected_at_every_depth(self):
        for text in ('{"entries":{},"entries":{}}', '{"entries":{"cards.count":{},"cards.count":{}}}'):
            path = self.directory / "duplicate.json"
            path.write_text(text, encoding="utf-8")
            with self.assertRaisesRegex(CatalogError, "duplicate JSON key"):
                load_json(path)

    def test_empty_source_or_translation_never_passes(self):
        for target in (self.source, self.translation):
            with self.subTest(locale=target["locale"]):
                original = target["entries"]
                target["entries"] = {}
                self.assertTrue(self.validate()[0])
                target["entries"] = original

    def test_missing_and_unknown_entries_rejected(self):
        self.translation["entries"]["cards.unknown"] = self.translation["entries"].pop("cards.count")
        errors = self.validate()[0]
        self.assertTrue(any("missing translation" in error for error in errors))
        self.assertTrue(any("unknown key" in error for error in errors))

    def test_empty_missing_or_malformed_values_rejected(self):
        original = deepcopy(self.translation)
        for value in ("", "  ", None, [], 42):
            self.translation = deepcopy(original)
            self.translation["entries"]["cards.count"]["text"] = value
            self.assertTrue(self.validate()[0])
        del self.translation["entries"]["cards.count"]["text"]
        self.assertTrue(self.validate()[0])

    def test_broken_arguments_and_tags_rejected(self):
        for text in ("<b>{amount}</b> cartas", "<b>cartas</b>", "<b>{count</b>",
                     "<b>{count}</i>", "{count} cartas", "<b><i>{count}</b></i>"):
            with self.subTest(text=text):
                self.translation["entries"]["cards.count"]["text"] = text
                self.assertTrue(self.validate()[0])

    def test_nested_plural_named_arguments(self):
        self.source["entries"]["cards.count"]["text"] = "{count:plural:{count} card|{count} cards}"
        entry = self.translation["entries"]["cards.count"]
        entry["text"] = "{count:plural:{count} carta|{count} cartas}"
        entry["sourceHash"] = source_hash(self.source["entries"]["cards.count"])
        self.assertEqual([], self.validate()[0])

    def test_literal_angle_brackets_are_not_rich_text_tags(self):
        entry = self.source["entries"]["cards.count"]
        entry.update(text="Enter <Quantity> <Card Name>", smart=False, arguments={})
        translated = self.translation["entries"]["cards.count"]
        translated.update(text="Introduce <Cantidad> <Nombre de carta>", sourceHash=source_hash(entry))
        self.assertEqual([], self.validate()[0])

    def test_stale_source_requires_explicit_acknowledgement(self):
        self.source["entries"]["cards.count"]["context"] += " Changed."
        self.assertTrue(any("stale sourceHash" in error for error in self.validate()[0]))
        old_hash = self.translation["entries"]["cards.count"]["sourceHash"]
        self.translation["entries"]["cards.count"]["status"] = "needs-review"
        errors, warnings, _ = self.validate()
        self.assertEqual([], errors)
        self.assertEqual(1, len(warnings))
        self.assertEqual(old_hash, self.translation["entries"]["cards.count"]["sourceHash"])

    def test_rich_text_attributes_and_self_closing_tags_are_preserved(self):
        parsed = tags('<color=#ff0000><b>Cards</b></color><sprite name="card"/><br>')
        self.assertEqual(1, parsed[("", "color", "=#ff0000")])
        self.assertEqual(1, parsed[("/", "color", "")])
        self.assertEqual(1, parsed[("", "sprite", 'name="card"/')])
        self.assertEqual(1, parsed[("", "br", "")])

    def test_long_unterminated_tag_does_not_hide_following_valid_tags(self):
        parsed = tags("<" + "a" * 100000 + "<b>Cards</b>")
        self.assertEqual({("", "b", ""): 1, ("/", "b", ""): 1}, parsed)

    def test_review_metadata_rejected(self):
        original = deepcopy(self.translation)
        for field, value in (("status", "human-ish"), ("sourceHash", "bad"),
                             ("provenance", {}), ("provenance", {"method": "AI", "date": "yesterday"})):
            self.translation = deepcopy(original)
            self.translation["entries"]["cards.count"][field] = value
            self.assertTrue(self.validate()[0])

    def test_unknown_locale_and_missing_catalog_rejected(self):
        self.write()
        (self.directory / "xx.json").write_text("{}", encoding="utf-8")
        (self.directory / "es.json").unlink()
        self.assertEqual(2, len(validate_catalogs(self.directory)[0]))

    def test_manifest_rejects_ambiguous_aliases(self):
        self.manifest["locales"][1]["aliases"] = ["EN"]
        with self.assertRaises(CatalogError):
            manifest_codes(self.manifest)

    def test_hash_independent_of_dictionary_order_but_not_contract(self):
        entry = self.source["entries"]["cards.count"]
        self.assertEqual(source_hash(entry), source_hash(dict(reversed(list(entry.items())))))
        changed = deepcopy(entry)
        changed["arguments"]["count"] += " Changed."
        self.assertNotEqual(source_hash(entry), source_hash(changed))

    def test_repeated_merge_preserves_human_correction_and_provenance(self):
        draft = deepcopy(self.translation)
        entry = self.translation["entries"]["cards.count"]
        entry["text"] = "Cartas: <b>{count}</b>"
        entry["provenance"]["note"] = "Contributor correction; status deliberately still machine."
        merged = merge(self.source, self.translation, draft)
        self.assertEqual(self.translation, merged)
        self.assertEqual(merged, merge(self.source, merged, draft))

    def test_source_update_preserves_text_hash_and_provenance(self):
        before = deepcopy(self.translation)
        self.source["entries"]["cards.count"]["context"] += " Changed."
        updated = merge(self.source, self.translation, self.translation)
        entry = updated["entries"]["cards.count"]
        self.assertEqual("needs-review", entry["status"])
        for field in ("text", "sourceHash", "provenance"):
            self.assertEqual(before["entries"]["cards.count"][field], entry[field])
        self.assertEqual(before, self.translation)

    def test_fills_only_missing_entries(self):
        empty = {"schemaVersion": 1, "locale": "es", "entries": {}}
        self.assertEqual(self.translation, merge(self.source, empty, self.translation))

    def test_structurally_stale_wording_is_preserved_but_release_validation_fails(self):
        original = deepcopy(self.translation)
        for text, arguments in (("<b>{amount}</b> cards", {"amount": "Card count."}),
                                ("<i>{count}</i> cards", {"count": "Card count."})):
            with self.subTest(text=text):
                self.source["entries"]["cards.count"].update(text=text, arguments=arguments)
                self.translation = merge(self.source, original, original)
                expected = deepcopy(original)
                expected["entries"]["cards.count"]["status"] = "needs-review"
                self.assertEqual(expected, self.translation)
                self.assertTrue(self.validate()[0])
                self.assertEqual(expected, merge(self.source, self.translation, original))

    def test_stale_translation_still_requires_valid_metadata(self):
        self.source["entries"]["cards.count"]["context"] += " Changed."
        for field, value in (("status", "invalid"), ("sourceHash", "bad"), ("provenance", {})):
            with self.subTest(field=field):
                existing = deepcopy(self.translation)
                existing["entries"]["cards.count"][field] = value
                with self.assertRaises(CatalogError):
                    merge(self.source, existing, self.translation)

    def test_invalid_draft_does_not_mutate_inputs(self):
        empty = {"schemaVersion": 1, "locale": "es", "entries": {}}
        self.translation["entries"]["cards.count"]["text"] = "{wrong}"
        with self.assertRaises(CatalogError):
            merge(self.source, empty, self.translation)
        self.assertEqual({}, empty["entries"])

    def test_malformed_existing_entry_is_not_overwritten(self):
        for value in ("Contributor text", {"text": 42}, {"status": "reviewed"}):
            existing = {"schemaVersion": 1, "locale": "es", "entries": {"cards.count": value}}
            before = deepcopy(existing)
            with self.assertRaises(CatalogError):
                merge(self.source, existing, self.translation)
            self.assertEqual(before, existing)

    def test_generation_cannot_claim_human_review(self):
        empty = {"schemaVersion": 1, "locale": "es", "entries": {}}
        self.translation["entries"]["cards.count"]["status"] = "reviewed"
        with self.assertRaisesRegex(CatalogError, "machine status"):
            merge(self.source, empty, self.translation)

    def test_malformed_source_metadata_fails_without_traceback(self):
        for value in (None, [], {}, {"text": "Cards", "context": "", "arguments": {}, "smart": False}):
            self.source["entries"]["cards.count"] = value
            self.assertTrue(self.validate()[0])


if __name__ == "__main__":
    unittest.main()
