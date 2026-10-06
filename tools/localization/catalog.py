"""Shared, editor-free CGS catalog contract. Python standard library only."""

from collections import Counter
from datetime import date
import hashlib
import json
from pathlib import Path
import re


KEY = re.compile(r"[a-z][a-z0-9]*(?:\.[a-z][a-z0-9]*)+\Z")
CODE = re.compile(r"[a-z]{2,3}(?:-[A-Za-z0-9]{2,8})*\Z")
ARGUMENT = re.compile(r"[a-z][A-Za-z0-9]*\Z")
HASH = re.compile(r"[a-f0-9]{64}\Z")
TAG = re.compile(r"<([^<>]*)>")
TAG_NAME = re.compile(r"/?[a-zA-Z][a-zA-Z0-9-]*")
PROVENANCE_DATE_ERROR = "provenance date must be YYYY-MM-DD"
RICH_TAGS = {"b", "i", "u", "s", "color", "size", "material", "quad", "alpha", "align",
             "allcaps", "cspace", "font", "font-weight", "indent", "line-height", "line-indent",
             "link", "lowercase", "margin", "mark", "mspace", "nobr", "noparse", "rotate",
             "smallcaps", "strikethrough", "style", "sub", "sup", "uppercase", "voffset", "width",
             "br", "sprite", "space", "page", "pos"}
STATUSES = ("machine", "reviewed", "needs-review")


class CatalogError(ValueError):
    """Invalid catalog or manifest; safe to show to a contributor."""


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise CatalogError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def load_json(path):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=_unique_object)
    except (OSError, ValueError) as error:
        raise CatalogError(f"{path}: {error}") from error


def serialize(value):
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def source_hash(entry):
    """SHA-256 of compact, sorted UTF-8 JSON; whitespace inside strings matters."""
    contract = {key: entry[key] for key in ("text", "context", "arguments", "smart")}
    canonical = json.dumps(contract, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def require(condition, message):
    if not condition:
        raise CatalogError(message)


def nonempty(value):
    return isinstance(value, str) and bool(value.strip())


def fields(value, required, optional=()):
    require(isinstance(value, dict), "expected an object")
    missing = set(required) - value.keys()
    unknown = value.keys() - set(required) - set(optional)
    require(not missing, f"missing fields: {', '.join(sorted(missing))}")
    require(not unknown, f"unknown fields: {', '.join(sorted(unknown))}")


def manifest_codes(manifest):
    fields(manifest, ("schemaVersion", "locales"))
    require(type(manifest["schemaVersion"]) is int and manifest["schemaVersion"] == 1,
            "unsupported schemaVersion")
    locales = manifest["locales"]
    require(isinstance(locales, list) and locales, "locales must be a non-empty array")
    identifiers = set()
    codes = []
    for locale in locales:
        fields(locale, ("code", "nativeName", "aliases"))
        require(nonempty(locale["nativeName"]), "nativeName must be non-empty")
        require(isinstance(locale["aliases"], list), "aliases must be an array")
        for code in [locale["code"], *locale["aliases"]]:
            require(isinstance(code, str) and CODE.fullmatch(code), f"invalid locale code: {code}")
            require(code.lower() not in identifiers, f"duplicate locale code/alias: {code}")
            identifiers.add(code.lower())
        codes.append(locale["code"])
    require(codes[0] == "en", "English must be the first locale")
    return codes


def placeholders(text):
    """Check nested named Smart String fields, without implementing Unity's formatter.

    Backslash escapes are supported. Numeric/implicit selectors are deliberately
    outside CGS's named-argument contract. Unity tests validate formatter semantics.
    """
    names = set()
    depth = 0
    index = 0
    while index < len(text):
        char = text[index]
        if char == "\\":
            index += 2
            continue
        if char == "{":
            match = re.match(r"([a-z][A-Za-z0-9]*)(?=[:}])", text[index + 1:])
            require(match is not None, f"expected named argument at character {index}")
            names.add(match[1])
            depth += 1
        elif char == "}":
            depth -= 1
            require(depth >= 0, "unmatched closing brace")
        index += 1
    require(depth == 0, "unclosed argument brace")
    return names


def tags(text):
    """Preserve rich-text tags and attributes, and reject broken nesting."""
    stack = []
    result = Counter()
    for match in TAG.finditer(text):
        content = match[1]
        name_match = TAG_NAME.match(content)
        if name_match is None:
            continue
        closing = "/" if content.startswith("/") else ""
        name = name_match[0].lstrip("/").lower()
        # Instructions can contain literal angle brackets such as <Quantity>.
        if name not in RICH_TAGS:
            continue
        attributes = content[name_match.end():].strip()
        result[(closing, name, attributes)] += 1
        if closing:
            require(stack and stack.pop() == name, f"unbalanced rich-text tag: {match[0]}")
        elif name not in {"br", "sprite", "space", "page", "pos", "quad"} and not attributes.endswith("/"):
            stack.append(name)
    require(not stack, f"unclosed rich-text tags: {', '.join(stack)}")
    return result


def catalog_header(catalog, code):
    fields(catalog, ("schemaVersion", "locale", "entries"))
    require(type(catalog["schemaVersion"]) is int and catalog["schemaVersion"] == 1,
            "unsupported schemaVersion")
    require(catalog["locale"] == code, f"expected locale {code}")
    require(isinstance(catalog["entries"], dict) and catalog["entries"], "catalog entries must not be empty")
    for key in catalog["entries"]:
        require(KEY.fullmatch(key), f"invalid semantic key: {key}")


def validate_source(source):
    catalog_header(source, "en")
    for key, entry in source["entries"].items():
        try:
            fields(entry, ("text", "context", "arguments", "smart"))
            require(nonempty(entry["text"]), "text must be non-empty")
            require(nonempty(entry["context"]), "context must be non-empty")
            require(type(entry["smart"]) is bool, "smart must be a boolean")
            arguments = entry["arguments"]
            require(isinstance(arguments, dict), "arguments must be an object")
            for name, description in arguments.items():
                require(ARGUMENT.fullmatch(name) and nonempty(description), "invalid argument description")
            if entry["smart"]:
                require(placeholders(entry["text"]) == set(arguments), "argument contract does not match text")
            else:
                require(not arguments, "arguments require smart: true")
            tags(entry["text"])
        except CatalogError as error:
            raise CatalogError(f"en / {key}: {error}") from error


def validate_translation_metadata(entry):
    fields(entry, ("text", "status", "sourceHash", "provenance"))
    require(nonempty(entry["text"]), "text must be non-empty")
    require(isinstance(entry["status"], str) and entry["status"] in STATUSES, "invalid review status")
    require(isinstance(entry["sourceHash"], str) and HASH.fullmatch(entry["sourceHash"]), "invalid sourceHash")
    provenance = entry["provenance"]
    fields(provenance, ("method", "date"), ("note",))
    require(nonempty(provenance["method"]), "provenance method must be non-empty")
    require(isinstance(provenance["date"], str), PROVENANCE_DATE_ERROR)
    try:
        parsed = date.fromisoformat(provenance["date"])
        require(parsed.isoformat() == provenance["date"], PROVENANCE_DATE_ERROR)
    except ValueError as error:
        raise CatalogError(PROVENANCE_DATE_ERROR) from error
    require("note" not in provenance or nonempty(provenance["note"]), "provenance note must be non-empty")


def validate_translation(entry, source):
    validate_translation_metadata(entry)
    if source["smart"]:
        require(placeholders(entry["text"]) == set(source["arguments"]), "named arguments differ from English")
    require(tags(entry["text"]) == tags(source["text"]), "rich-text tags differ from English")
    stale = entry["sourceHash"] != source_hash(source)
    require(not stale or entry["status"] == "needs-review", "stale sourceHash; reconcile or mark needs-review")
    return stale


def validate_catalogs(directory):
    """Return errors, warnings and coverage; never write to the source directory."""
    directory = Path(directory)
    errors, warnings, reports = [], [], []
    try:
        codes = manifest_codes(load_json(directory / "locales.json"))
        source = load_json(directory / "en.json")
        validate_source(source)
    except CatalogError as error:
        return [str(error)], warnings, reports
    for path in sorted(directory.glob("*.json")):
        if path.stem not in {*codes, "locales"}:
            errors.append(f"unknown locale catalog: {path.name}")
    entries = source["entries"]
    reports.append(f"en: {len(entries)}/{len(entries)} source entries")
    for code in codes[1:]:
        try:
            catalog = load_json(directory / f"{code}.json")
            catalog_header(catalog, code)
        except CatalogError as error:
            errors.append(f"{code}: {error}")
            reports.append(f"{code}: 0/{len(entries)} valid entries")
            continue
        translations = catalog["entries"]
        statuses = Counter()
        valid = 0
        for key in sorted(entries.keys() | translations.keys()):
            try:
                require(key in entries, "unknown key (retirement requires explicit migration)")
                require(key in translations, "missing translation")
                stale = validate_translation(translations[key], entries[key])
                statuses[translations[key]["status"]] += 1
                valid += 1
                if stale:
                    warnings.append(f"{code} / {key}: stale source acknowledged; needs review")
            except CatalogError as error:
                errors.append(f"{code} / {key}: {error}")
        counts = ", ".join(f"{status}={statuses[status]}" for status in STATUSES)
        reports.append(f"{code}: {valid}/{len(entries)} valid entries; {counts}")
    return errors, warnings, reports
