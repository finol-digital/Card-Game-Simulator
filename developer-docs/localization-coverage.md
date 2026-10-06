# Runtime localization coverage

Change: `add-runtime-localization`. Audit completed against source and saved assets
on October 5, 2026. Player/visual validation remains in progress.

## Environment and catalog

Unity 6000.3.15f1, Pipeline 0.8.0-exp.1, Localization 1.5.13 and Addressables 2.9.1.
The user's Package Manager update resolved the original implementation blocker.
There are **373 English source entries and 373 entries in each of nine translated
catalogs**. All initial translations are explicitly machine drafts, not reviewed
translations. Source hashes, context, named arguments and provenance are validated
and preserved in generated Unity table metadata.

## Authored inventory

The live Editor audit traversed all six enabled scenes and 80 prefab dependencies,
including inactive objects. `translations/bindings/authored.json` is the detailed
inventory of 876 sites: 483 legacy Text components, 9 TMP components, 344 tooltip
descriptions and 40 dropdown options. Each row has a key, a dynamic presenter/key
family, or an explicit exclusion. The final classifications are 551 direct keys,
39 dynamic targets and 286 exclusions. Language is an additional Settings row.

Saved bindings passed `AuthoredUiBinder.ValidateAsset` for every bound scene and
prefab. The validator checks key assignment, option indices, tooltip keys, exact
persistent target, public `set_text` method and `EditorAndRuntime` listener state.
The Settings scene was separately inspected: Language and Framerate share the same
parent, at sibling indices 0 and 1. The language caption and cloned item template
use the `mixed` font key. The helper does not rewrite Unity YAML directly.

Dropdown captions/item labels are intentionally driven by options, never by a
second static string binding. File extensions and model property options remain
literal. Reusable base-component placeholders are populated by concrete scene or
prefab overrides, or by the explicitly listed dynamic presenter. Numeric values,
control symbols and empty templates do not require their own translation keys.

## Runtime producers and ownership

`localization-runtime-inventory.json` records 155 literal-key call sites with source
locations. These locations are an audit snapshot; semantic keys remain stable.
The following state-selected families and adapters supplement those sites:

| Producer | Localized presentation | Preserved content |
| --- | --- | --- |
| `TitleScreen`, `MainMenu` | start/version, welcome, quit | version, company/game names and copyright |
| `Settings` | first-row language choice, native names in manifest order | other preferences and values |
| `Dialog`, `DecisionModal`, `DownloadMenu` | keyed messages, choices, placeholders, queued/reused messages | actions, entered text, literal payloads |
| `GamesManagementMenu`, `CardGameEditorMenu`, `CgsGamesBrowser` | import/download choices, native file titles, errors and credit wrapper | names, author/copyright, file paths, URLs, exceptions |
| `CardGameManager` | load/import/export/download/delete prompts, loading status | game names/IDs, paths and diagnostic details |
| `ProgressBar`, `UnityCardGame` | `download.*` from structured stage/count state | original English download diagnostics and serialized/network fields |
| `SetImportMenu` | import status, missing/oversized images, folder errors | set/card names, file type and paths |
| `CardSearchMenu`, `SearchResults` | hints, pages, clear prompt, boolean options | game property labels, enum values and filter input |
| `CardEditorMenu`, `CardsExplorer`, `CardDeletionManager` | editing/import choices, warnings, deletion | card/set names, IDs, artwork and editable values |
| `DeckEditor`, `DeckLoadMenu`, `DeckSaveMenu` | counts, load/save/delete/overwrite, PDF errors, `decks.instructions.*` | deck text/parser grammar, example card names, user names/default editable names |
| `PlayController`, `HandDealer`, `CardDrawer` | restart/back, starting deck/card choices, draw/deal/remove counts | actual deck/card names and gameplay operations |
| `CardStack`, `CardModel`, `Counter`, `Die`, `DiceZone`, viewers | delete/shuffle/save/roll prompts, numeric values, `play.counter.value`/`play.die.value` | model names and existing shuffle/save wire tokens |
| `PlayHelpMenu`, `PlaySettingsMenu` | mobile/desktop help with binding arguments, missing-rules/update notices | binding names/action IDs, game rules and URLs |
| `LobbyMenu`, `CgsNetManager`, `CgsNetPlayer`, `Scoreboard` | room label/placeholder, connection/password/share prompts, offline/counts | player/chat/room values, passwords, network messages and parsing |
| `CgsNetDiagnostics` | toolbar, sharing feedback, `diagnostics.status.*` | exported diagnostic trace and its protocol/technical values |
| `ToolTip` | authored descriptions plus current control bindings | platform-provided binding names |
| `SharingMessages` | service-result feedback and explicit deck/room copy notices | clipboard/shared payload, native sharing semantics |

Literal service errors (`UnityWebRequest.error`, exception details and the explicit
developer log dialog) are diagnostic data, not translation source text. Known CGS
error wrappers are keyed. Native OS/file-picker controls are external UI; CGS-owned
titles are localized before opening. Existing default editable deck/player names
are persisted model data and are not renamed by changing language. `ViewValue`,
download diagnostic strings and the two CardStack action tokens remain unchanged
as model/wire values; only their UI presentation is localized. Hand-count strings
received from other players remain literal compound values rather than being
parsed or rewritten according to the viewer's locale.

Every remaining direct text assignment was checked. User/model display labels use
`SetLiteral`, which also disables inherited authored string bindings. InputField
and TMP_InputField assignments retain their existing parsing and editing behavior.
No global English replacement or inference from arbitrary dialog text is used.
`SharingMessages` is limited to the sharing service's documented feedback results.

## Lifetime, fonts and layout

Startup selects supported command-line override, saved code, script-aware device
match/alias, then English. Unity's SelectedLocale is authoritative. Explicit
selections persist the canonical code; rapid changes are coalesced while the prior
locale's asynchronous preload completes. No runtime WaitForCompletion is used.

Dynamic adapters retain key/arguments and English fallback, unsubscribe while
hidden and restore bindings on reuse. Dialog queues preserve actions and input;
copy feedback has its own binding and never becomes the copied payload. Static
bindings inspect the loaded entry before displaying text, retaining readable
English for missing/empty results. Font adapters unsubscribe while disabled.

Chinese and Korean Noto CJK source fonts and their SIL license are bundled outside
Resources. Japanese uses the existing Noto Sans JP source. Latin/Cyrillic use the
existing Open Sans and Exo 2 faces. CgsFonts provides legacy and saved dynamic TMP
assets, with valid source/material/multi-atlas references; native language names
use a mixed-script font. Existing Japanese fallbacks remain intact. Build checks
require local Addressables groups, preload flags, English fallback and source glyph
coverage. Generated filter buttons resize only when text/font geometry is dirty.

## Validation evidence and outstanding work

Ignored reports and helper outputs are under `Logs/localization-tests/` and
`Logs/localization-*.txt`; generated player output is under `Builds/Localization/`.

- Editor-free validator: all 373 entries in all ten catalogs, no errors/warnings.
- Python tooling: 20 regression tests passed (validator and preservation merger).
- Locale selection: 20 EditMode cases passed.
- Catalog/font checks: six EditMode tests passed, including repeat-import byte
  stability, invalid-input no mutation, source/table drift, source glyphs and
  rejection of a build policy that can reuse stale Addressables content.
- Runtime presentation/startup: sixteen PlayMode tests passed, including visible and reused
  text, queued dialog actions/input, missing-key English fallback, rapid selection,
  empty-entry fallback, native Settings options, explicit persistence, first
  gamepad focus, pointer opening and no redundant callbacks. The test enables
  project input before the manager starts so the test runner's disabled actions
  do not open a developer warning dialog over Settings.
  Shipped-manifest precedence/script/region cases run in PlayMode too, and a fresh
  asynchronous locale initialization restores the saved choice independently of
  the already-running selected locale.
- Existing lifecycle tests: three passed; network diagnostics: fifteen passed;
  browser LAN regression tests: two passed.
- Action-panel viewport and tooltip tests: three passed, including a font-size
  change with unchanged content/bounds. Single-line tooltip sizing now invalidates
  its cached measurement when the font, size or style changes.
- Existing Japanese font tests: four passed.
- Existing sharing tests: 19/19 passed on the final run. An earlier host clipboard
  failure was reproduced independently through GUIUtility.systemCopyBuffer; that
  direct check later succeeded and the complete sharing suite was rerun successfully.
- Saved scene/prefab binding audit: all bound assets passed.
- Contributor workflow: a temporary Spanish wording correction survived the draft
  merger, passed editor-free validation, preserved the table GUID and entry ID on
  import, and appeared in the running Settings screen. Shipped wording was then
  restored and validated. Reports and screenshots remain under ignored Logs.
- Scene captures cover all six scenes in all ten locales at 1280x720 and 390x844.
  Narrow Main Menu, Deck Editor and Deal/Draw buttons initially clipped longer
  translations. Authored and dynamic button labels now use bounded font fitting;
  those three scenes were recaptured after the fix. The common confirmation dialog
  was also captured across all ten locales at both sizes. These are Editor rendering
  checks, not complete dialog-family, physical-device or standalone-player coverage.
- Expanded Language dropdown captures cover the top and bottom of the list in all
  ten locales at both sizes, using pointer-enter and wheel events. The native names
  remain readable, including the bottom Japanese/Italian/Russian options.
- Import-choice, download, deck-entry and deck-save dialogs were captured in every
  locale at both sizes. This found clipped deck titles/name labels and a clipped
  wide import prompt. Their prefab labels now use bounded fitting; fresh captures
  show the full text. User input areas and button positions remain unchanged.
- Windows IL2CPP player and local Addressables built successfully with the normal
  linker settings: zero errors, two existing environment warnings (unlinked Unity
  Services and disabled runtime Pipeline). The successful report is
  `Logs/localization-tests/windows-build-final.json`. Earlier attempts failed with
  LNK1140 and then insufficient disk space; temporary linker overrides were restored
  before the successful build. Final localization content totals 35,988,508 bytes;
  this is an absolute measurement, not a controlled baseline delta.
- The initial WebGL player build succeeded, but output inspection caught stale
  Addressables content: only the old locale bundle was included. Import now sets
  `BuildWithPlayer` explicitly, and pre-build validation rejects global-preference
  or disabled content building. The final corrected build succeeded with all 29
  localization files (36,019,812 bytes); its reported total player size is
  141,700,443 bytes. Both final builds include the last prefab label corrections
  and report zero errors. Complete dialog-family and input-device
  inspection, offline/two-client behavior and controlled build-size/memory
  comparisons remain pending. Windows/browser automation currently fails during
  initialization with `failed to write kernel assets` (path not found); Editor
  rendering checks do not establish standalone or browser interaction behavior.
- Final source review found no changes under protected `.github` or public `docs`,
  and no localization changes to gameplay serialization, network tokens or parsing.
  User Package Manager updates remain intact. Unity has reordered some unchanged
  Windows Store capability settings while serializing PlayerSettings.

Do not mark the remaining OpenSpec validation tasks complete solely because the
catalogs or source-level checks pass. The release PR does not replace these checks;
merging and deployment require separate authorization.

Remaining acceptance checks:

- **4.3:** initial gamepad focus, pointer opening/wheel scrolling, reopening and
  preference preservation are verified. Complete keyboard/gamepad list traversal
  and touch selection still require interaction verification.
- **7.6:** all six scenes plus common confirmations, import-choice, download,
  deck-entry and deck-save dialogs have ten-language, two-size rendering evidence.
  The other dialog families and full input/device matrix remain open.
- **7.7:** local bundles are present in the player output, but offline switching,
  two clients using different locales, and controlled baseline size/memory
  comparisons have not been verified. Editor memory snapshots and older builds
  made with different settings are not substitutes for those comparisons.
