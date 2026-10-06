## Context

Issue [#232](https://github.com/finol-digital/Card-Game-Simulator/issues/232) requests multiple languages. The requested entry point is the first setting, before Framerate, and a selection must update the interface without restarting.

Repository inspection found:

- `Packages/packages-lock.json` resolves Unity Localization 1.5.12 with Addressables. `ProjectSettings/EditorBuildSettings.asset` already registers `Assets/Localization/Localization Settings.asset`.
- `Assets/Localization/Locales/` contains 339 locale assets, but no String Table collections exist under `Assets/Localization/`, and CGS scripts do not yet reference Localization APIs. Existing locale assets are not evidence of translated support.
- The settings asset currently selects command-line locale, system locale, then English; string/asset fallback is disabled.
- `Settings.cs` uses legacy uGUI `Dropdown`, persists other settings through PlayerPrefs, and explicitly focuses `framerateDropdown` when navigation starts. Both visual order and this focus target must change.
- Text is authored in scenes/prefabs and constructed in code. Examples include `TitleScreen.VersionMessage`, `DecisionModal.Show`, `ToolTip.TooltipTextContent`, download progress, and menu prompt constants. Changing scene labels alone cannot satisfy the request.
- The six enabled scenes are TitleScreen, MainMenu, PlayGame, DeckEditor, CardsExplorer, and Settings. Shared dialogs and dynamically instantiated prefabs extend this surface.
- `JapaneseFontTests.cs` protects bundled Japanese glyph support for both legacy Text and TMP, including WebGL. Preserve that behavior while adding Chinese/Korean UI fonts.

This change only plans implementation. No package installation, scene migration, translation generation, or runtime changes happen during proposal creation.

## Goals / Non-Goals

**Goals:**

- Provide a discoverable, persistent language setting with immediate updates across every CGS-owned text surface.
- Reuse Unity Localization as the authoritative runtime locale and string system.
- Ship complete initial machine-generated catalogs and local font/table content.
- Make translation-only contributions possible in GitHub or a text editor without installing Unity.
- Define repeatable checks for coverage, formatting, lifecycle behavior, and build inclusion.

**Non-Goals:**

- Translating user data, game definitions, card artwork/rules supplied by game authors, chat, names, URLs, identifiers, or developer-only logs.
- Localizing the public website, Unity Editor tools, or native operating-system controls. CGS-provided native-dialog titles/prompts are localized when the dialog opens; the OS controls its own text and an already-open native dialog.
- Runtime machine translation, a translation service account, remote catalog updates, or a hosted translation platform.
- A wholesale UI/TMP migration, right-to-left language support, or enabling all 339 existing locales in the first release.
- Changing save formats, network messages, or the language of other players' interfaces.

## Decisions

### 1. Use the existing Unity Localization installation

Create a `CgsUi` String Table collection, an asset table for locale-specific UI fonts, and narrow CGS adapters under `Assets/Scripts/Cgs/Localization/`. `LocalizationSettings.SelectedLocale` is the only runtime locale authority. A preference selector reads the saved locale code; it does not maintain a second current-language state.

Use `LocalizeStringEvent` for authored labels and `LocalizedString.StringChanged` or an equivalent binding around the package for dynamic text. Await asynchronous initialization/table loading and avoid `WaitForCompletion`, including indirect synchronous lookups before preload finishes, so WebGL remains supported. Bundle/preload the small initial UI tables locally. Keep readable authored English available if initialization fails, disable language selection until ready, and report the failure through a developer diagnostic without replacing UI with raw keys.

Alternative: a custom JSON runtime lookup system would avoid table import but duplicate locale selection, fallback, formatting, and binding behavior already supplied by the installed package. JSON here is an authoring format, not a second runtime localization engine.

### 2. Explicitly define supported locales and startup precedence

Use a small manifest at `translations/locales.json` to declare shipped locale codes, native names, deterministic display order, and any explicit system-language aliases. The user-selected launch entries are `en` (English), `es` (Español), `zh-Hans` (简体中文), `pt-BR` (Português (Brasil)), `ko` (한국어), `fr` (Français), `de` (Deutsch), `ja` (日本語), `it` (Italiano), and `ru` (Русский). Keep English first and retain this order regardless of the selected locale. Exclude unshipped locales from runtime availability by reconciling the provider/Addressables locale labels; existing unused assets need not be deleted.

Startup precedence is: supported explicit `-language=` override, supported saved `LanguageCode` PlayerPrefs value, supported device locale, then `en`. Command-line selection is session-only unless the player explicitly chooses a dropdown option. System matching tries an exact locale, then a script-preserving parent and declared aliases, then English. Declare `zh-CN` and `zh-SG` as Simplified Chinese aliases; do not silently map Traditional Chinese to Simplified Chinese or European Portuguese to Brazilian Portuguese. Regional Spanish and Korean variants can match their supported language parents. Unsupported/corrupt stored codes are ignored and replaced by a valid code on the next explicit selection.

The dropdown maps options to codes, never saves an index, and uses `SetValueWithoutNotify` during redisplay. A selection updates `SelectedLocale`, saves the code locally, and calls `PlayerPrefs.Save`. The settings row becomes the first sibling immediately before Framerate; default focus and navigation links move with it. Initialization and repeated enable/disable must not add duplicate listeners. Other settings, input focus, and scroll position are preserved where practical.

Alternative: populating all existing locale assets would advertise hundreds of untranslated choices. English-only startup is simpler but discards the existing system-language behavior.

### 3. Audit and migrate static and dynamic text together

Create a coverage inventory in `developer-docs/localization-coverage.md`, listing authored Text/TMP labels, dropdown option labels, input placeholders, serialized tooltip/help strings, script literals/constants, and call sites that pass strings indirectly. Assign stable semantic keys such as `settings.language`, `common.cancel`, and `downloads.progress`. Different meanings receive distinct keys even when the English text matches. Every candidate is either bound or explicitly excluded with a reason; no global string replacement based on English display values is used.

Localize all six shipped scenes and the shared prefabs they instantiate. Use persistent public event bindings for authored text, checking the target and `set_text` listener, and enable authoring previews with `EditorAndRuntime` state. Work in reviewable scene/prefab batches and preserve serialized references and overrides.

Dynamic presenters retain a key and current arguments, not just a translated string. Add localized overloads/bindings for decision/input/selection dialogs and tooltips while preserving literal paths for user-provided content. A visible dialog, loading status, count, or help panel must refresh on a locale change without rerunning its action or losing input. Recompute strings when their arguments change as well. Tooltips must use their localized description when their existing update loop rebuilds the binding hint; that loop must not reintroduce English or perform table loads every frame.

Subscribe on enable/show and release subscriptions/handles on disable/hide/destroy. Rebind on reuse. Guard asynchronous completions with the current binding/request identity so rapid changes or recycled UI cannot apply stale text. Inactive UI reads the current locale when activated; newly loaded scenes and instantiated menus inherit it. Translate only the presentation of enum/action labels; retain stable enum values, game identifiers, numeric serialization, and network data.

Use named Smart String arguments for interpolated sentences and plural-sensitive counts. Preserve variables and rich-text tags during translation, and format visible numbers through the locale without changing invariant parsing or saved data. Enable explicit English fallback for missing/empty translated entries; a shipped catalog must still pass completeness validation. Missing English keys are build/validation failures, with a readable authored fallback as runtime defense.

Alternative: one-time string lookups at `Show`/`Start` leave visible dialogs stale, and only scanning scenes misses runtime text. Localization must attach to the data and lifecycle of each presenter.

### 4. Make UTF-8 JSON catalogs the contributor source of truth

Use repository-root `translations/` so translator files are easy to find and are not public website content:

- `en.json`: semantic keys with English text, translator context, named-argument descriptions, and an explicit Smart String flag.
- `<locale>.json`: matching keys with translated `text`, `status` (`machine`, `reviewed`, or `needs-review`), and a `sourceHash` of the relevant English text/context/argument contract.
- `locales.json`: supported locale manifest and native names.

Files are UTF-8, deterministically ordered by key, with no credentials. Generate initial translations in batches with English context, glossary, formatting rules, and placeholder constraints; record the generation method/date in contributor documentation. Machine translation runs only during development and never on the player's device. Human edits can improve a machine entry without claiming fluent review; only explicitly reviewed entries use `reviewed`.

Provide `tools/localization/validate.py` using Python's standard library for editor-free structural validation. It rejects duplicate/unknown keys, missing/empty values, unknown locale codes, invalid metadata, and placeholder/tag mismatches; detects stale source hashes; and reports per-locale coverage and review status. A stale entry must be explicitly marked `needs-review` and retain its old source hash until reconciled. Such entries produce a visible warning, while structural errors and unexplained stale entries fail. All initial shipped catalogs must be complete; reviewed status is not required for launch.

An Editor importer under `Assets/Scripts/Cgs/Editor/Localization/` validates the full catalog set before mutation, then upserts entries by stable key into `CgsUi`, preserving table/shared-entry IDs and asset GUIDs. Existing keys are never silently renumbered or deleted. Report retired keys for explicit migration. Reimporting unchanged catalogs must produce no asset diff. Validate Smart String syntax with the actual Unity formatter and representative argument values in Editor tests; the Python checker does not pretend to implement Unity's full grammar.

Commit both source catalogs and generated Unity tables so normal Editor checkouts work. Translation-only PR authors change only catalogs; maintainers run the import command and commit the table changes before merging. An Editor consistency test and local pre-build validation reject stale generated tables and locale/font configuration. Reuse existing test/build entry points without editing `.github`.

Generation may fill missing entries only. It must not replace non-empty translations, including machine drafts corrected without a status change. English edits flag affected entries for review and never erase a contributor's text. A short `developer-docs/translating.md`, linked from README, documents a GitHub edit/PR example, validation command, placeholders, glossary, review flags, adding a locale, and the maintainer import step. New locales require complete catalogs and rendering checks before appearing in settings.

Alternative: editing serialized Unity YAML is inaccessible to most translators. Unity CSV import/export is supported, but a single wide multi-language spreadsheet increases merge collisions; per-locale JSON provides a clear place for provenance and context. A hosted platform can be added later without changing semantic keys.

### 5. Treat fonts and layout as localization requirements

Keep existing legacy Text and TMP components. Bundle appropriately licensed Chinese and Korean font files (for example, suitable Noto families after checking their redistribution licenses), with license files; reuse the existing Japanese source font through locale-specific UI assets. Verify Cyrillic and all required Latin diacritics in the chosen regular/bold fonts, bundling additional licensed coverage if needed. Use locale-specific asset-table fonts for UI text, including bold styles, and verify legacy `Font` as well as TMP assets. TMP CJK assets must keep their source fonts, serialized materials/atlas sub-assets, dynamic population, and multiple atlas textures enabled.

Keep the existing Japanese fallbacks for game/user text. The language dropdown must render all native names even in English, so its caption/template need explicitly bundled mixed-script font coverage independently of the current locale. Font asset tables and referenced assets must be Addressable and outside Resources; use valid stable GUIDs and verify player inclusion. Font and text bindings must settle together after a switch.

Adapt affected row sizes, wrapping, layout rebuilds, and dropdown viewport/navigation for expanded text. Rebuild only dirty affected layouts after updates. Preserve the settings screen's existing UI framework and unrelated styling. Check narrow portrait and wide landscape screens with actual translations and a development-only expanded-text sample. Run glyph coverage checks against every shipped catalog and native language name on both text stacks; do not rely on host OS font fallback.

## Risks / Trade-offs

- [The text audit misses strings passed through variables or third-party UI] → Review producer and presenter call sites, scan both text component families, and require a reasoned coverage inventory for CGS-owned text. Do not rewrite vendor packages.
- [Async loading overwrites a later selection or disabled object] → Preload local tables, bind through Unity's locale events, and test rapid switches, pooled dialogs, and scene changes with request-identity guards where needed.
- [Catalogs and generated assets diverge] → Deterministic import, repeat-import tests, Editor consistency checks, and a pre-build failure with an actionable import command.
- [Machine translation is awkward or incorrect] → Record provenance, provide context/glossary and easy corrections, keep human edits, and label catalogs as machine-generated in contribution documentation.
- [CJK fonts increase download/memory use] → Ship only supported UI locales, retain dynamic atlases, measure build-size and runtime-memory deltas, and verify WebGL offline rendering.
- [UI expansion breaks touch or controller use] → Check every scene/dialog family, long labels, dropdown native names, focus order, and existing layout regression tests.
- [Localized display text accidentally changes game/network behavior] → Keep model values and wire data invariant; verify two clients with different locales and preserve user input literally.

## Migration Plan

1. Establish the inventory, semantic English catalog, supported-locale manifest, and validator before moving labels.
2. Add deterministic import, String Tables, bootstrap/preference selection, and assembly references; keep English usable throughout.
3. Add the first-position settings dropdown and migrate authored and dynamic UI in focused batches, including hidden/shared prefabs.
4. Seed and validate non-English catalogs, preserve human text, and add bundled font assets and layout fixes.
5. Run catalog validation, deterministic import/consistency checks, relevant EditMode and PlayMode tests through Unity CLI, then build Addressables and desktop/WebGL players. Inspect mobile layouts and two-client presentation. Store reports in ignored `Logs/` or `Temp/`.
6. Publish the contributor guide alongside the implementation. Merge/release only through the repository's normal authorized process; this proposal does not create or merge a PR.

Rollback is a normal source revert of localization code/bindings/tables plus an Addressables/player rebuild. The additive `LanguageCode` preference can remain harmlessly ignored. No saved-game or multiplayer-data migration is needed.

## Open Questions

- Launch language scope is resolved: `en`, `es`, `zh-Hans`, `pt-BR`, `ko`, `fr`, `de`, `ja`, `it`, and `ru`. Additional languages and variants can be added through the same catalog workflow.
- Exact redistributable Chinese/Korean font files and their size costs must be measured during implementation. Required glyph coverage and WebGL behavior are acceptance criteria regardless of font choice.
- The initial translation generator/provider is an implementation choice; no paid account or runtime service is required by this design. Generation must follow the catalog contract and preserve existing text.

Unity reference: [Localization quick start](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/QuickStartGuideWithVariants.html) describes the package's locale and table model; [CSV documentation](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/CSV.html) describes the import/export alternative. Verify implementation APIs against the installed 1.5.12 package because the online 1.5 documentation currently displays 1.5.13.
