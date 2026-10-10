"""Fill missing translations; never replace non-empty contributor wording."""

import argparse
from copy import deepcopy
import os
from pathlib import Path
import tempfile

from catalog import (CatalogError, catalog_header, load_json, manifest_codes, nonempty,
                     require, serialize, source_hash, validate_source, validate_translation,
                     validate_translation_metadata)


def validate_merge_catalog(catalog, code, source):
    # Empty catalogs are allowed here: this is the missing-entry preparation tool.
    probe = deepcopy(catalog)
    if probe.get("entries") == {}:
        probe["entries"] = {"temporary.entry": {}}
    catalog_header(probe, code)
    unknown = catalog["entries"].keys() - source["entries"].keys()
    require(not unknown, f"unknown keys require explicit migration: {', '.join(sorted(unknown))}")


def merge(source, existing, draft):
    validate_source(source)
    require(isinstance(existing, dict) and isinstance(draft, dict), "catalogs must be objects")
    code = existing.get("locale")
    require(code != "en" and nonempty(code), "only translation catalogs can be merged")
    validate_merge_catalog(existing, code, source)
    validate_merge_catalog(draft, code, source)
    result = deepcopy(existing)
    for key, english in sorted(source["entries"].items()):
        current = result["entries"].get(key)
        require(current is None or isinstance(current, dict), f"{key}: entry must be an object")
        if current is not None:
            require(isinstance(current.get("text"), str), f"{key}: text must be a string")
        if isinstance(current, dict) and nonempty(current.get("text")):
            if current.get("sourceHash") != source_hash(english):
                validate_translation_metadata(current)
                current["status"] = "needs-review"
            else:
                validate_translation(current, english)
            continue
        candidate = draft["entries"].get(key)
        if candidate is None:
            continue
        require(isinstance(candidate, dict) and candidate.get("status") == "machine",
                f"{key}: generated drafts must have machine status")
        validate_translation(candidate, english)
        result["entries"][key] = deepcopy(candidate)
    result["entries"] = dict(sorted(result["entries"].items()))
    return result


def write_atomic(path, content):
    """Prepare and validate first; replace only once, without partial JSON writes."""
    path = Path(path)
    descriptor, temporary = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(content)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("locale")
    parser.add_argument("draft", type=Path)
    parser.add_argument("--directory", type=Path, default=Path(__file__).resolve().parents[2] / "translations")
    args = parser.parse_args()
    try:
        codes = manifest_codes(load_json(args.directory / "locales.json"))
        require(args.locale in codes[1:], "locale must be a supported non-English code")
        path = args.directory / f"{args.locale}.json"
        source = load_json(args.directory / "en.json")
        existing = load_json(path) if path.exists() else {"schemaVersion": 1, "locale": args.locale, "entries": {}}
        result = merge(source, existing, load_json(args.draft))
        content = serialize(result)
        if not path.exists() or path.read_text(encoding="utf-8") != content:
            write_atomic(path, content)
        print(f"{args.locale}: preserved existing wording; {len(result['entries'])} entries. Run validate.py next.")
        return 0
    except (CatalogError, OSError) as error:
        print(f"ERROR: {error}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
