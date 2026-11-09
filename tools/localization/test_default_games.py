"""Offline default catalogs must be complete, synchronized, and identity-safe."""

import json
import unittest
from pathlib import Path

import default_games as defaults


class DefaultGameCatalogTests(unittest.TestCase):
    def test_generated_packages_are_current(self):
        for path, text in defaults.outputs():
            with self.subTest(path=str(path)):
                self.assertEqual(path.read_text(encoding="utf-8"), text)

    def test_catalogs_cover_every_authored_label(self):
        locales = {
            entry["code"]
            for entry in json.loads(
                (defaults.ROOT / "translations/locales.json").read_text()
            )["locales"]
        }
        for key in defaults.GAMES:
            site = defaults.ROOT / "docs/games" / key
            manifest = json.loads((site / "cgs.json").read_text())
            cards = json.loads((site / "AllCards.json").read_text())
            sets = json.loads((site / "AllSets.json").read_text())
            self.assertEqual(set(manifest["localization"]["locales"]), locales)
            for locale, source in manifest["localization"]["locales"].items():
                with self.subTest(game=key, locale=locale):
                    catalog = source["inline"]
                    self.assertEqual(
                        set(catalog["cardNames"]), {card["name"] for card in cards}
                    )
                    self.assertEqual(
                        set(catalog["decks"]),
                        {deck["name"] for deck in manifest["deckUrls"]},
                    )
                    self.assertEqual(
                        set(catalog["sets"]), {item["code"] for item in sets}
                    )
                    for prop in manifest["cardProperties"]:
                        self.assertTrue(catalog["properties"][prop["name"]]["display"])
                    for enum in manifest.get("enums", []):
                        self.assertEqual(
                            set(catalog["enums"][enum["property"]]), set(enum["values"])
                        )
                    if locale == "en":
                        self.assertEqual(
                            catalog["cardNames"],
                            {card["name"]: card["name"] for card in cards},
                        )
                    elif key == "Standard":
                        self.assertNotEqual(
                            catalog["cardNames"]["Ace of Spades"], "Ace of Spades"
                        )

    def test_bundled_and_hosted_canonical_content_agree(self):
        for key in defaults.GAMES:
            site = defaults.ROOT / "docs/games" / key
            manifest = json.loads((site / "cgs.json").read_text())
            bundled = (
                defaults.ROOT
                / "Assets/StreamingAssets"
                / (manifest["name"] + "@www.cardgamesimulator.com")
            )
            for filename in ("AllCards.json", "AllSets.json"):
                self.assertEqual(
                    json.loads((site / filename).read_text()),
                    json.loads((bundled / filename).read_text()),
                )
            for deck in manifest["deckUrls"]:
                filename = Path("decks") / (deck["name"] + ".txt")
                self.assertEqual(
                    (site / filename).read_text(), (bundled / filename).read_text()
                )


if __name__ == "__main__":
    unittest.main()
