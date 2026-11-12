# Spec Delta

## MODIFIED Requirements

### Requirement: Localize all application-owned interface text

CGS SHALL localize its authored and dynamically produced interface text across MainMenu, Settings, CardsExplorer, DeckEditor, PlayGame, and their shared or instantiated UI. This includes labels, buttons, dropdown options, input placeholders, tooltips, help, dialog prompts, loading/progress text, status messages, and user-facing errors. A coverage inventory SHALL identify each text source as localized or excluded with a reason.

User-entered text, chat, canonical names, game/card definitions, URLs, identifiers, native OS controls, and developer-only logs SHALL retain their original values. Opt-in authored game-content translations and asset variants SHALL affect only presentation under the localized-game-content contract; games with neither authored localization metadata nor an applicable bundled default catalog SHALL retain original presentation. CGS-provided titles/prompts passed to native dialogs SHALL use the current language when opened.

#### Scenario: Traverse the application in a non-English language
- **WHEN** the player visits all five shipped scenes and opens their menus/dialogs in a supported non-English language
- **THEN** CGS-owned interface text uses that locale's catalog
- **AND** remaining original text is accounted for by an explicit scope exclusion

#### Scenario: Preserve player and game content
- **WHEN** the player changes language with a named game, custom deck name, typed input, and chat present
- **THEN** canonical content and arbitrary user text remain unchanged while application labels translate and verified authored content uses available game translations

