#!/usr/bin/env python3
"""Build a complete, disposable game package for localization verification.

The default package is offline-only. Supply an HTTPS base URL to also generate
hostable files; this command neither hosts files nor deploys the CGS proxy.
"""

import argparse
import hashlib
import json
import shutil
import zipfile
from pathlib import Path
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (
    ROOT / "Assets/StreamingAssets/Standard Playing Cards@www.cardgamesimulator.com"
)
NAME = "CGS Localization Verification"
OFFLINE_BASE = "https://example.invalid/localization-verification/"


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def build(output, base_url=None):
    """Create a fresh directory and zip containing real canonical and localized assets."""
    base = base_url or OFFLINE_BASE
    parsed = urlsplit(base)
    if (
        parsed.scheme != "https"
        or not parsed.hostname
        or parsed.username
        or parsed.password
        or parsed.query
        or parsed.fragment
        or not base.endswith("/")
    ):
        raise ValueError(
            "Use an absolute HTTPS directory URL ending in '/', without credentials, query, or fragment."
        )
    game_id = NAME + "@" + parsed.hostname
    output.mkdir(parents=True, exist_ok=False)
    game = output / game_id
    game.mkdir()
    manifest = {
        "name": NAME,
        "autoUpdate": 0 if base_url else -1,
        "autoUpdateUrl": base + "cgs.json",
        "allCardsUrl": base + "AllCards.json",
        "allSetsUrl": base + "AllSets.json",
        "bannerImageUrl": base + "Banner.png",
        "cardBackImageUrl": base + "CardBack.png",
        "cardImageUrl": base + "sets/{cardSet}/{cardId}.png",
        "deckUrls": [
            {"name": "Verification deck", "url": base + "decks/Verification%20deck.txt"}
        ],
        "cardProperties": [
            {"name": "suit", "display": "Suit", "type": "stringEnum"},
            {"name": "rules", "display": "Rules", "type": "string"},
        ],
        "enums": [
            {"property": "suit", "values": {"clubs": "Clubs", "spades": "Spades"}}
        ],
        "localization": {
            "defaultLocale": "en",
            "locales": {
                "es": {"url": "locales/es.json"},
                "ja": {"url": "locales/ja.json"},
            },
        },
    }
    cards = [
        {
            "id": "ace_of_clubs",
            "name": "Ace of Clubs",
            "set": "STD_FR",
            "suit": "clubs",
            "rules": "Draw one card.\nKeep the canonical identity.",
        },
        {
            "id": "ace_of_spades",
            "name": "Ace of Spades",
            "set": "STD_FR",
            "suit": "spades",
            "rules": "",
        },
    ]
    spanish = {
        "game": {"name": "Verificación de idiomas"},
        "cardNames": {"Ace of Clubs": "As de tréboles", "Ace of Spades": "As de picas"},
        "decks": {"Verification deck": {"name": "Baraja de verificación"}},
        "sets": {"STD_FR": {"name": "Conjunto de verificación"}},
        "properties": {"suit": {"display": "Palo"}, "rules": {"display": "Reglas"}},
        "enums": {"suit": {"clubs": "Tréboles", "spades": "Picas"}},
        "cards": {
            "ace_of_clubs": {
                "properties": {
                    "rules": "Roba una carta.\nConserva la identidad original."
                }
            }
        },
        "assets": {"cardBackImageUrl": "images/SpanishBack.png"},
    }
    # Deliberately partial: the spade, property labels, and deck fall back to English.
    japanese = {
        "game": {"name": "言語の検証"},
        "cardNames": {"Ace of Clubs": "クラブのエース"},
    }
    write_json(game / "cgs.json", manifest)
    write_json(game / "AllCards.json", cards)
    write_json(game / "AllSets.json", [{"code": "STD_FR", "name": "Verification set"}])
    write_json(game / "locales/es.json", spanish)
    write_json(game / "locales/ja.json", japanese)
    files = {
        "Banner.png": "Banner.png",
        "CardBack.png": "CardBack.png",
        "locales/images/SpanishBack.png": "backs/DARK_BACK.png",
    }
    for card in cards:
        image = "sets/STD_FR/" + card["id"] + ".png"
        files[image] = image
    for destination, source in files.items():
        target = game / destination
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(SOURCE / source, target)
    deck = b"### CGS Localization Verification Deck: Verification deck\n1 Ace of Clubs\n1 Ace of Spades\n"
    (game / "decks").mkdir()
    (game / "decks/Verification deck.txt").write_bytes(deck)
    fingerprint = hashlib.sha256(deck).hexdigest()
    write_json(
        game / "localization/provenance.json",
        {
            "version": 1,
            "game": game_id,
            "decks": {
                "Verification deck.txt": {
                    "generation": fingerprint[:32],
                    "name": "Verification deck",
                    "source": manifest["deckUrls"][0]["url"],
                    "sha256": fingerprint,
                }
            },
        },
    )
    archive = output / (game_id + ".cgs.zip")
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as package:
        for path in sorted(game.rglob("*")):
            if path.is_file():
                package.write(path, path.relative_to(output))
    return game, archive


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output", type=Path, default=ROOT / "Temp/localization-verification"
    )
    parser.add_argument(
        "--base-url",
        help="Actual final hosting directory; omit for an offline package.",
    )
    args = parser.parse_args()
    game, archive = build(args.output, args.base_url)
    print(f"Game directory: {game}\nImportable package: {archive}")


if __name__ == "__main__":
    main()
