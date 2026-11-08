"""Keep the importable verification fixture reproducible and self-contained."""

import json
import tempfile
import unittest
import zipfile
from pathlib import Path

import verification_fixture as fixture


class VerificationFixtureTests(unittest.TestCase):
    def test_offline_package_matches_maintained_fixture_and_zip(self):
        with tempfile.TemporaryDirectory() as temporary:
            game, archive = fixture.build(Path(temporary) / "offline")
            maintained = (
                fixture.ROOT / "developer-docs/fixtures/localization-verification"
            )
            expected = {
                path.relative_to(maintained)
                for path in maintained.rglob("*")
                if path.is_file()
            }
            actual = {
                path.relative_to(game) for path in game.rglob("*") if path.is_file()
            }
            self.assertEqual(actual, expected)
            with zipfile.ZipFile(archive) as package:
                for path in actual:
                    payload = (game / path).read_bytes()
                    self.assertEqual(payload, (maintained / path).read_bytes())
                    self.assertEqual(
                        payload, package.read((Path(game.name) / path).as_posix())
                    )
            manifest = json.loads((game / "cgs.json").read_text(encoding="utf-8"))
            self.assertEqual(manifest["autoUpdate"], -1)
            for source in manifest["localization"]["locales"].values():
                catalog_path = game / source["url"]
                catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
                for reference in catalog.get("assets", {}).values():
                    self.assertTrue(
                        (catalog_path.parent / reference)
                        .read_bytes()
                        .startswith(b"\x89PNG\r\n\x1a\n")
                    )

    def test_hosted_fixture_uses_requested_origin_and_enables_refresh(self):
        with tempfile.TemporaryDirectory() as temporary:
            game, _ = fixture.build(
                Path(temporary) / "hosted", "https://example.org/final/game/"
            )
            manifest = json.loads((game / "cgs.json").read_text(encoding="utf-8"))
            self.assertEqual(manifest["autoUpdate"], 0)
            self.assertEqual(
                manifest["allCardsUrl"], "https://example.org/final/game/AllCards.json"
            )
            self.assertEqual(
                manifest["localization"]["locales"]["es"]["url"], "locales/es.json"
            )
            state = json.loads(
                (game / "localization/provenance.json").read_text(encoding="utf-8")
            )
            self.assertEqual(state["game"], fixture.NAME + "@example.org")


if __name__ == "__main__":
    unittest.main()
