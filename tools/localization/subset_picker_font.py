"""Rebuild the shared language-picker font; requires requirements-fonts.txt."""
from pathlib import Path
import argparse
import hashlib
import io
import json


def build(root):
    from fontTools import subset, version
    from fontTools.ttLib import TTFont
    from fontTools.pens.recordingPen import DecomposingRecordingPen

    if version != "4.66.1":
        raise ValueError("Install tools/localization/requirements-fonts.txt for reproducible font output")
    source = root / "Assets/Fonts/Noto Sans CJK/NotoSansCJKsc-Regular.otf"
    if hashlib.sha256(source.read_bytes()).hexdigest() != "2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b":
        raise ValueError("Source font changed; review its license, glyphs, and reproducibility before updating the checksum")
    manifest = json.loads((root / "translations/locales.json").read_text(encoding="utf-8"))
    characters = set(map(ord, "".join(locale["nativeName"] for locale in manifest["locales"])))
    # The existing language row also applies this font to its translated label.
    for locale in manifest["locales"]:
        catalog_path = root / "translations" / (locale["code"] + ".json")
        if not catalog_path.exists():
            raise ValueError("Font generation requires the full language-catalog stack: " + str(catalog_path))
        catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
        characters.update(map(ord, catalog["entries"]["settings.language"]["text"]))
    characters |= set(range(32, 127)) | {0xA0}
    original = TTFont(source)
    source_map = original.getBestCmap()
    if not characters <= source_map.keys():
        raise ValueError("Source font cannot render every native language name")
    font = TTFont(source, recalcTimestamp=False)
    options = subset.Options()
    options.layout_features = ["*"]
    options.name_IDs = ["*"]
    options.name_legacy = True
    options.name_languages = ["*"]
    options.recalc_timestamp = False
    worker = subset.Subsetter(options=options)
    worker.populate(unicodes=characters)
    worker.subset(font)
    family = "CGS Language Picker"
    postscript = "CGSLanguagePicker-Regular"
    replacements = {1: family, 2: "Regular", 3: postscript, 4: family + " Regular",
                    6: postscript, 16: family, 17: "Regular"}
    for record in font["name"].names:
        if record.nameID in replacements:
            record.string = replacements[record.nameID].encode(record.getEncoding())
    cff = font["CFF "].cff
    cff.fontNames = [postscript]
    cff.topDictIndex[0].FamilyName = family
    cff.topDictIndex[0].FullName = family + " Regular"
    output = io.BytesIO()
    font.save(output)
    rebuilt = TTFont(io.BytesIO(output.getvalue()))
    target_map = rebuilt.getBestCmap()
    if not characters <= target_map.keys():
        raise ValueError("Subset is missing required glyphs")
    before, after = original.getGlyphSet(), rebuilt.getGlyphSet()
    for codepoint in characters:
        p1, p2 = DecomposingRecordingPen(before), DecomposingRecordingPen(after)
        before[source_map[codepoint]].draw(p1)
        after[target_map[codepoint]].draw(p2)
        if p1.value != p2.value or original["hmtx"][source_map[codepoint]] != rebuilt["hmtx"][target_map[codepoint]]:
            raise ValueError(f"Subset changed outline or metrics for U+{codepoint:04X}")
    return output.getvalue()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Fail if the checked-in font needs regeneration")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    target = args.root / "Assets/Fonts/Noto Sans CJK/CGSLanguagePicker-Regular.otf"
    data = build(args.root)
    if args.check:
        if not target.exists() or target.read_bytes() != data:
            raise SystemExit("Language-picker font is stale; rerun subset_picker_font.py")
    else:
        target.write_bytes(data)
    print(f"Language-picker font verified: {len(data):,} bytes; outlines and metrics unchanged")


if __name__ == "__main__":
    main()

