# localized-interface Specification

## Purpose
Ensure application-owned interface text refreshes consistently in every shipped language with readable formatting, bundled fonts, usable layouts, and local presentation.

## Requirements

### Requirement: Localize all application-owned interface text

CGS SHALL localize its authored and dynamically produced interface text across TitleScreen, MainMenu, Settings, CardsExplorer, DeckEditor, PlayGame, and their shared or instantiated UI. This includes labels, buttons, dropdown options, input placeholders, tooltips, help, dialog prompts, loading/progress text, status messages, and user-facing errors. A coverage inventory SHALL identify each text source as localized or excluded with a reason.

User-entered text, chat, names, game/card definitions and artwork, URLs, identifiers, native OS controls, and developer-only logs SHALL retain their original values. CGS-provided titles/prompts passed to native dialogs SHALL use the current language when opened.

#### Scenario: Traverse the application in a non-English language
- **WHEN** the player visits all six shipped scenes and opens their menus/dialogs in a supported non-English language
- **THEN** CGS-owned interface text uses that locale's catalog
- **AND** remaining original text is accounted for by an explicit scope exclusion

#### Scenario: Preserve player and game content
- **WHEN** the player changes language with a named game, custom deck name, typed input, and chat present
- **THEN** these content values remain unchanged while the surrounding application labels translate

### Requirement: Refresh visible and future UI when locale changes

Visible localized text SHALL refresh in place following a locale change without requiring the player to reopen a screen. Inactive, pooled, newly instantiated, and newly loaded UI SHALL use the active locale when shown. Localization refresh SHALL preserve dialog actions, input values, and game state.

#### Scenario: Refresh a visible dynamic dialog and tooltip
- **WHEN** the selected locale changes while a dynamic dialog and tooltip exist
- **THEN** their prompts, options, and descriptions refresh using the same keys and arguments
- **AND** no action is invoked and entered text is preserved

#### Scenario: Reuse hidden UI
- **WHEN** the locale changes while a panel is disabled and that panel is later re-enabled or reused
- **THEN** it displays the current language without duplicate listeners or stale labels

#### Scenario: Rapid locale changes
- **WHEN** several language changes occur before prior asynchronous text/font operations complete
- **THEN** the final visible text and fonts correspond to the latest selection
- **AND** late completions do not overwrite newer content or access destroyed UI

### Requirement: Preserve formatting and readable fallback

Localized messages SHALL use stable semantic keys and named arguments for dynamic values, support locale-appropriate plural/number display where applicable, and update when arguments change. Missing or empty translations SHALL fall back to the English entry. Runtime UI SHALL NOT display raw keys or package missing-translation messages. Locale formatting SHALL NOT change stored numeric values, identifiers, or parsing contracts.

#### Scenario: Update a dynamic count
- **WHEN** a count changes while its localized message is visible and the player then changes locale
- **THEN** the message shows the current count with the selected locale's formatting and applicable plural form
- **AND** no braces, unresolved placeholders, or stale numbers remain

#### Scenario: Missing translation at runtime
- **WHEN** a localized entry is absent or empty but its English entry exists
- **THEN** the UI displays the English message with its arguments intact
- **AND** validation still reports the incomplete shipped catalog as an error

### Requirement: Render every shipped language with bundled fonts

Both legacy uGUI Text and TextMeshPro SHALL render all shipped catalog characters and language native names with bundled fonts on supported builds, including WebGL without OS font access. Locale-specific font selection SHALL cover Chinese, Japanese, Korean, Cyrillic, and required Latin diacritics. Existing Japanese game-name rendering SHALL remain supported in every UI locale.

#### Scenario: Inspect CJK and Cyrillic in a player build
- **WHEN** the player selects Chinese, Japanese, Korean, or Russian in a WebGL build
- **THEN** UI text and all dropdown native names render without missing-character boxes or blank glyphs
- **AND** the chosen font data is present in the build without external font requests

#### Scenario: Japanese game title in another UI language
- **WHEN** a Japanese game name is displayed with English or another supported UI locale selected
- **THEN** the original Japanese name still renders correctly

### Requirement: Keep translated layouts and controls usable

Localized UI SHALL remain legible and operable in narrow portrait and wide landscape layouts. Translated text SHALL NOT obscure essential controls or make settings, dialogs, or dropdown choices unreachable. Necessary layout work SHALL occur on content changes rather than loading translations every frame.

#### Scenario: Expanded translated text
- **WHEN** a longer translation or development-only expanded-text sample is displayed in a narrow layout
- **THEN** labels fit, wrap, or use the established readable overflow treatment
- **AND** all actions remain reachable by touch, pointer, keyboard, and gamepad

### Requirement: Localized presentation works offline and independently per client

Shipped locale tables and fonts SHALL be part of local player/Addressables content and require no translation-service connection. Locale selection SHALL affect only local presentation and SHALL NOT change network protocol, game data, or another client's locale.

#### Scenario: Offline language switch
- **WHEN** an installed player, or a WebGL player with its required build content already loaded, switches to any shipped language without network access
- **THEN** all required translations and font assets are available

#### Scenario: Two clients with different languages
- **WHEN** two players use different UI languages in the same game
- **THEN** each sees CGS interface text in their own locale
- **AND** gameplay actions, shared data, and original chat messages remain consistent
