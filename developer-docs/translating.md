# Translating CGS

> **Review stack availability:** PR #558 supplies the English catalog, manifest,
> and Python tools. Run `python -m unittest discover -s tools/localization` to
> exercise the tools on their self-contained fixtures. The default catalog
> validator intentionally reports missing translations until language packs
> #561–#563 land; do not treat a partial stack as release-ready or suppress these
> errors. The `es.json` example becomes available in #561. Unity importer and
> font commands require the foundations and font assets in #559–#560, all
> language packs, and the collections/Addressables registration in #564.
> UI authoring (including tooltips and the language dropdown) additionally
> requires the menu integration in #565. The Unity steps below describe that
> assembled feature, not commands available at the first review layer.

CGS stores its interface translations in `translations/`. You can correct wording
without Unity. Card names, game definitions, deck text, URLs, chat and player names
are supplied content and must not be translated by this system.

## Correct one message

1. Open the target file on GitHub, for example `translations/es.json`.
2. Find the semantic key, such as `settings.language`. Read its English text,
   context and argument descriptions in `translations/en.json`.
3. Change only the translated `text`. If you have reviewed it against the current
   English entry, set `status` to `reviewed`. Keep the existing `sourceHash` when
   it already matches the source; use the validator to check it.
4. Preserve provenance. Add a short `note` describing the correction to the
   existing provenance object rather than claiming the original machine draft
   was written by a human. A human-origin replacement may use `method: human`
   with the actual review date and a note about the prior draft.
5. Run `python tools/localization/validate.py` from the repository root and submit
   the JSON change for review. Unity-generated assets are a maintainer step.

For example, a reviewed Spanish `settings.language` can use `"text": "Idioma"`
and `"status": "reviewed"`; keep its hash and other required metadata intact.
Do not copy a hash from a different entry.

## Preserve message structure

- Keep named arguments such as `{gameName}`, `{count}` and `{version}` intact.
  Their order may change to fit the language. Argument values are supplied content.
- Keep rich-text tags balanced and preserve their required attributes. Literal
  deck grammar such as `<Quantity>` is an example for users, not a rich-text tag.
- Use the same meaning and terminology across related messages. Read the glossary
  in `tools/localization/README.md`, including the distinctions between a game,
  deck, card stack, set, hand, drawer and zone.
- Do not replace an untranslated message with English to make validation pass.
  Identical wording is appropriate for proper names and conventional labels only.
- Source changes require review. `needs-review` explicitly acknowledges an old
  source hash; it does not claim the translation has been reviewed against the new
  text. After actual review, use the current hash and set `reviewed`.

## Validate and import

Python 3.10 or newer is required; no Python packages or translation service account
are needed. From the project root:

```powershell
python tools/localization/validate.py
python -m unittest discover -s tools/localization -p 'test_*.py'
unity command eval --caller plugin --skill localization --format json --timeout 60 --code 'Cgs.Editor.Localization.CatalogImporter.Import(); return "Imported";' -- --timeout 60000
unity command eval --caller plugin --skill localization --format json --timeout 60 --code 'Cgs.Editor.Localization.CatalogImporter.ValidateGenerated(); return "Valid";' -- --timeout 60000
```

Keep this project open in Unity for the last two commands. The Editor menu offers
`CGS > Localization > Import Catalogs` and `Validate Generated Tables` too.
Set `CGS_PYTHON` to an absolute Python executable path before launching Unity if
Python is not on its PATH. Import validates all JSON and representative Smart
String arguments before editing any assets. It preserves shared IDs and asset
GUIDs. Removed keys are rejected with an explicit migration report. The pre-build
hook rejects stale generated tables, missing preload/fallback configuration and
fonts or tables that are not bundled locally. It also checks bundled font glyphs.
Import sets Addressables to build with each player, independently of the Editor's
global preference. This prevents a successful player build from silently reusing
old bundles that omit newly added languages, strings or fonts.

A slow Editor can finish an operation after the CLI request times out. Check the
Editor Console or validate generated tables before retrying a mutation. Never
manually edit Unity YAML to work around a timeout.

## Machine drafts

Generated text must use `status: machine`, a generation method/date, and the source
hash. Generate from the English context and glossary, then merge with:

```powershell
python tools/localization/merge_drafts.py es path/to/es-draft.json
python tools/localization/validate.py
```

The merger fills missing/empty entries only. It preserves non-empty contributor
text and provenance, including corrections that still carry machine status.
Changed sources retain their old hashes and become `needs-review`. Repeated
imports never regenerate translation wording.

## Add a locale

1. Add a unique locale code, native name and deliberate device aliases to
   `translations/locales.json`, preserving existing display order. Exact supported
   codes are accepted as explicit selections; device aliases are separate.
2. Create a complete catalog matching every English key, with valid arguments,
   tags, source hashes and provenance. Validate it before importing.
3. Import catalogs to create its locale/table assets and English fallback. Unused
   locale assets are preserved, but only manifest locales remain available.
4. Select bundled, redistributable fonts for its characters and include their
   licenses. Extend `FontCatalogBuilder` and rebuild `CgsFonts`; check both legacy
   Text and TMP, regular/bold styles and every native language name. Fonts must be
   local Addressables outside Resources, with saved TMP sources/materials/atlases.
5. For a new authored control, add its explicit path/property/key to
   `translations/bindings/authored.json`. For dynamic text, pass a `UiMessage`
   with a semantic key, formatted English fallback and named arguments. Keep
   literal presenter paths for supplied game/user content.
6. Run importer, locale-selection, presentation and glyph tests through Unity CLI.
   Check all scenes, narrow and wide layouts, dropdown navigation, hidden/reused
   dialogs and offline switching in desktop and WebGL players. Different clients
   must continue to exchange the same game data regardless of UI language.

Runtime startup order is supported `-language=` override, saved `LanguageCode`,
script-aware device match, then English. The command-line override is session-only;
choosing Language explicitly saves the supported code. Initialization is asynchronous.

The current implementation and remaining validation are tracked in
`developer-docs/localization-coverage.md` and the OpenSpec change; a complete JSON
catalog alone is not evidence of complete UI coverage.
