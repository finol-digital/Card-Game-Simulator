## 1. Inventory and catalog contract

- [x] 1.1 Read `developer-docs/unity-csharp-style-guide.md`; audit both legacy Text and TMP in the six enabled scenes and their instantiated/shared prefabs, including options, placeholders, and serialized tooltip/help strings.
- [x] 1.2 Audit script-produced text and indirect string arguments across CGS menus, cards, decks, play, downloads, and multiplayer presentation; record localized targets and justified exclusions in `developer-docs/localization-coverage.md`.
- [x] 1.3 Define the JSON schema, semantic key conventions, named-argument contracts, canonical source hashing, review metadata, and glossary; create the English source catalog with context for each inventoried message.
- [x] 1.4 Create `translations/locales.json` for `en`, `es`, `zh-Hans`, `pt-BR`, `ko`, `fr`, `de`, `ja`, `it`, and `ru`, including native names, stable display order, and explicit device-locale aliases.

## 2. Contributor tooling

- [x] 2.1 Implement the standard-library Python catalog validator with duplicate/unknown key detection, required metadata, completeness, argument/tag checks, stale-source reporting, and per-locale review/coverage output.
- [x] 2.2 Add meaningful validator fixtures for duplicate keys, empty catalogs, missing values, broken placeholders/tags, unexplained stale hashes, and explicitly acknowledged `needs-review` entries.
- [x] 2.3 Implement a safe draft merge/update utility that fills missing entries only, preserves all non-empty contributor text and provenance, and marks changed English sources for review; verify human corrections survive repeated generation/import preparation.
- [x] 2.4 Document the generation input/output contract and context/glossary rules; seed complete initial machine catalogs for Spanish, Brazilian Portuguese, French, German, and Italian, recording generation method/date and machine status.
- [x] 2.5 Seed complete initial machine catalogs for Simplified Chinese, Korean, Japanese, and Russian; run validation across all ten catalogs and resolve structural errors without disguising gaps as English translations.

## 3. Unity tables and runtime foundation

- [x] 3.1 Verify installed Localization/Addressables APIs and assembly references; implement a validate-before-write catalog importer in `Assets/Scripts/Cgs/Editor/Localization/` that creates `CgsUi` tables with stable keys, shared IDs, GUIDs, and metadata.
- [x] 3.2 Add import repeatability and source/table consistency checks, explicit retired-key reporting, and a pre-build validation hook using existing project entry points without changing `.github`.
- [x] 3.3 Reconcile the supported runtime locale subset and Addressables locale labels from the manifest while preserving unused locale assets; configure English fallback and local/preloaded table content.
- [x] 3.4 Implement asynchronous initialization and startup selectors with supported command-line override, saved locale code, script-aware device matching/aliases, and final English fallback; keep `SelectedLocale` as the runtime authority.
- [x] 3.5 Implement preference persistence, readable English failure behavior, and lifecycle-safe localized presentation adapters for keyed text/arguments; avoid blocking loads and stale asynchronous completion updates.

## 4. First-position language setting

- [x] 4.1 Add the Language row immediately before Framerate in `Assets/Scenes/Settings.unity`, reusing the existing Settings Dropdown component prefab and preserving unrelated serialized settings.
- [x] 4.2 Wire `Settings.cs` to populate native names from the supported manifest, map options to locale codes, apply/persist explicit choices, and redisplay without firing callbacks; disable the dropdown until initialization completes.
- [ ] 4.3 Move the initial keyboard/gamepad focus target from Framerate to Language and verify navigation, dropdown scrolling, pointer/touch selection, reopening, and preservation of other preferences.

## 5. Migrate application text

- [x] 5.1 Bind authored text in TitleScreen, MainMenu, and Settings using public persistent localization events; verify target methods, authoring refresh, and prefab overrides.
- [x] 5.2 Bind authored text in CardsExplorer and DeckEditor, including shared search, load/save, and game-management/editor dialogs.
- [x] 5.3 Bind authored text in PlayGame and its settings/help/action/scoreboard/multiplayer UI, preserving model values and user/game content.
- [x] 5.4 Extend dynamic dialog/prompt presenters to retain localized keys and arguments, refresh visible text, preserve actions/input, and retain literal paths for user-provided content; migrate their CGS-owned callers.
- [x] 5.5 Migrate tooltips, input placeholders, generated dropdown/action labels, version/start text, and help messages; ensure existing update/redisplay code cannot overwrite localized values with English.
- [x] 5.6 Migrate counts, download/loading status, connection/error messages, and other inventoried dynamic text to named Smart Strings; update on both argument and locale changes without altering wire data or invariant parsing.
- [x] 5.7 Audit disabled/pooled/instantiated UI and scene transitions for correct initial locale, subscription cleanup, and late-result guards; close every inventory item with a binding/key or justified exclusion.
- [x] 5.8 Reconcile any keys added during migration across all catalogs, preserving existing wording and review provenance; reimport and require complete shipped tables with valid Smart Strings.

## 6. Font and layout support

- [x] 6.1 Select and bundle redistributable Simplified Chinese and Korean source fonts with licenses; configure Japanese UI assets from the existing bundled font and verify Cyrillic/Latin diacritic coverage for regular/bold styles.
- [x] 6.2 Create locale-specific font asset tables/bindings for legacy Text and TMP, with Addressable references outside Resources and valid saved TMP source/material/multi-atlas assets; retain Japanese user/game-text fallbacks.
- [x] 6.3 Give the language dropdown caption and template mixed-script coverage so all ten native names remain readable in every locale; check glyph coverage against all catalog characters on both text stacks.
- [x] 6.4 Adjust affected layouts for translated text and verify narrow portrait/wide landscape views, dropdown viewport, tooltip bounds, action panels, and dialog controls; rebuild dirty layouts only after content changes.

## 7. Automated and player validation

- [x] 7.1 Add EditMode coverage for deterministic import, stable IDs/GUIDs, no mutation on invalid input, catalog/table drift, Smart String rendering with representative arguments, and supported locale/font configuration.
- [x] 7.2 Add PlayMode coverage for startup precedence, invalid/removed saved codes, script/region matching, local persistence across initialization, first-row/focus behavior, and no redundant settings callbacks.
- [x] 7.3 Add PlayMode coverage for visible static/dynamic refresh, changing arguments, English fallback for missing/empty entries, hidden/reused UI, rapid locale changes, scene transitions, and preservation of user input/actions.
- [x] 7.4 Add glyph checks for all ten locales and all native names, preserving `JapaneseFontTests`; run relevant existing UI/lifecycle tests along with new tests through Unity CLI and retain results in ignored `Logs/` or `Temp/`.
- [x] 7.5 Run the editor-free validator and importer from a clean source state, confirm repeat import produces no diff, and build Addressables plus desktop and WebGL players using the repository's Unity CLI workflow.
- [ ] 7.6 Inspect all six scenes and dialog families across the ten languages, including expanded-text layouts, CJK/Cyrillic glyphs, narrow mobile layouts, and pointer/touch/keyboard/gamepad operation; record and fix observed clipping or untranslated CGS text.
- [ ] 7.7 Verify offline switching in desktop and in WebGL after required build content loads; verify two clients with different locales preserve shared gameplay/chat, and measure localization font/content build-size and memory deltas.

## 8. Contributor handoff

- [x] 8.1 Write `developer-docs/translating.md` with a minimal GitHub correction example, exact validation/import commands, glossary/context guidance, placeholder rules, review/provenance handling, and complete new-locale steps; link it from README.
- [x] 8.2 Exercise the documented workflow with a sample wording correction: editor-free edit/validation, maintainer import, preserved IDs, visible runtime change, and no overwrite on regeneration; confirm documentation matches the implemented commands.
- [x] 8.3 Finish the coverage inventory and validation notes, confirm all shipped catalogs are complete and generated assets current, and review the final diff for unintended changes to game data, public website files, and protected `.github` files.
