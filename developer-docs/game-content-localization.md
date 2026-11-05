# Game-content localization integration inventory

This is the downstream implementation record for `integrate-game-content-localization`.
The companion contract is FinolDigital.Cgs.Json `add-game-content-localization`
([proposal PR #27](https://github.com/finol-digital/FinolDigital.Cgs.Json/pull/27)).
The dependency build is recorded in [game-content-localization-library.json](game-content-localization-library.json).
It is an **unreleased local development build**, not a published library version.
Rebuild from the recorded source file hashes with the recorded command and verify
the DLL/schema hashes. Replace that provenance with the reviewed source commit and
release before shipping. Preserve the existing plugin `.meta` GUID.

## Presentation surfaces and source boundaries

Paths below are relative to `Assets/Scripts/`. “Pending” means inspected and assigned,
not verified to display translations. Existing static UI labels already use the
separate CGS UI localization system.

| Surface | Concrete owners | Translation responsibility | Canonical state to preserve | Status |
| --- | --- | --- | --- | --- |
| Installed/current game chooser, banner, copyright, rulebook | `Cgs/Menu/MainMenu.cs`, `Cgs/CardGameManager.cs`, `Cgs/Menu/CgsGamesBrowser.cs` | Game metadata; cached/inline-only refresh for unselected installed games; external website titles only when a matching installed game is known | Game IDs, download URLs, website/source metadata, selection | Pending |
| Card display and property viewer | `Cgs/CardGameView/Viewer/CardViewer.cs`, `Cgs/CardGameView/Multiplayer/CardModel.cs` | Card/set names, nested/list text, boolean/enum/empty labels; card artwork | Card ID/face/printing, source image template, controls keyed by path | Viewer/control tests pass; localized artwork lifetime verified |
| Explorer filters and interactive search | `Cgs/Cards/CardSearchMenu.cs`, `Cgs/Cards/CardsExplorer.cs` | Translated display labels and UI-only name/text matching | Enum/empty bits, canonical filter keys; never change `FilterCards` | Interactive/canonical separation and control tests pass |
| Deck picker/editor/preview | `Cgs/Decks/DeckLoadMenu.cs`, `DeckEditor.cs`, `DeckCardSelector.cs`, `SearchResultsLayout.cs` | Verified authored names, card display labels, previews | Filenames, deck source name, unsaved edits and selected cards | Local provenance tests pass; player layout pending |
| Save/import prompts and user naming | `Cgs/Decks/DeckSaveMenu.cs`, `Cgs/Cards/SetImportMenu.cs`, `Cgs/CardGameManager.cs` | Application prompts with refreshable game-text arguments | Entered names, imported content, callbacks; save/rename clears authored binding | Bindings and provenance revocation implemented; player walkthrough pending |
| Shared deck/stack viewer and labels | `Cgs/CardGameView/Multiplayer/CardStack.cs`, `Cgs/CardGameView/Viewer/StackViewer.cs`, `Cgs/Play/PlayController.cs` | Per-client display from validated canonical label reference | Existing stack-name network variable, owner, stack identity/cards, user rename | Loopback creation/snapshot/ownership/ID-reuse tests pass; released-build matrix pending |
| Boards, zones, card backs, playmat | `Cgs/CardGameView/Multiplayer/Board.cs`, `CardZone.cs`, `Cgs/Play/PlayController.cs`, `FinolDigital.Cgs.Json.Unity/UnityCardGame.cs` | Canonical board/back IDs and authored zone labels; localized image variants | Dimensions, placement, snapping, zone gameplay effects, original resource files | Image/zone bindings implemented; no separate board/back-name display exists beyond canonical editor inputs |
| Game/card definition editors | `Cgs/Menu/CardGameEditorMenu.cs`, `Cgs/Cards/CardEditorMenu.cs` | Translate surrounding labels/preview only; preserve optional metadata when serializing | All authored input values and canonical JSON; no display text written back | Labels/metadata preservation implemented; diagnostics verified |
| Dynamic messages and tooltips | `Cgs/Localization/LocalizedMessageBinding.cs`, `LocalizedUiText.cs`, and game-content message call sites | Recompute arguments from canonical references on both UI and game-content refresh | Message keys, actions, arbitrary user strings; CGS UI English fallback unchanged | Live-message component tests pass; canonical argument call sites audited |
| Scoreboard/chat/player names | `Cgs/Play/Scoreboard.cs`, multiplayer message paths | Surrounding CGS-owned UI only | User names, chat and score values remain untouched | Existing exclusion retained |
| Download/error progress and identifiers | `FinolDigital.Cgs.Json.Unity/UnityCardGame.cs`, `Cgs/Menu/ProgressBar.cs` | Translate application templates and game display arguments | Diagnostic logs, paths, URI strings, IDs, numeric progress | Game-name bindings implemented; diagnostic paths/IDs remain canonical |

## Loading, asset, and persistence boundaries

- `UnityCardGame.ReadProperties`: stage/validate before publishing fields and a new
  localization generation. Failed reads preserve last-good localization; absent/null
  metadata on success disables bindings. Keep ID/directory migration behavior.
- `UnityCardGame.Load`/`LoadCards`/`LoadSets`: build presentation inventories after
  canonical data is available; locale changes must not invoke these data loaders.
- `UnityFileMethods.SaveUrlToFile`: expose successful document-origin information;
  redirects/proxy transport must not silently change the catalog's relative base.
- `UnityCard.ImageSprite`, `ImageQueueService`, and `ICardDisplay` registrations:
  select a separate localized image candidate while keeping the original cache
  and oriented-image geometry; stale callbacks cannot replace newer selections.
- `CardGameManager` package import/export: include portable localization files and
  atomically replace the cache owner on import; test relocation offline.
- `PlayController` authored-deck installation and `DeckEditor` user-save paths:
  bind/revoke provenance before overwriting files, including identical replacements.
- `CgsNetPlayer.CreateCardStackServerRpc` is owner-only and populates `CurrentDeck`
  and `_cardStacks`; the optional named-message handler must enforce the same
  authorization and preserve those side effects explicitly.
- `CardStack` rename/despawn: clear/invalidate live references. Session save/load
  does not exist in CGS; optional fingerprint-bound session sidecars are conditional
  on a future save/load feature. Deck and multiplayer provenance remain in scope.

## Canonical query audit

`UnityCardGame.FilterCards` is a shared canonical API. Existing consumers include
`UnityDeck` name/ID import matching and `PlayController` automatic `DeckPlayCard`
queries. Keep all those consumers canonical. Only `CardSearchMenu` may call the new
presentation-search adapter. Duplicate translated property labels must never be
used as dictionary identity; replace `CardViewer.DisplayNameLookup` with stable
paths. `CardSearchMenu` must retain the original `DisplayEmpty` key/bit.

The library-adoption smoke tests in `GameContentLocalizationTests` deliberately
translate `Flame` to another card's canonical name, `Ash`, and exercise CGS's real
TXT/DEC import and `FilterCards` paths. This verifies the initial separation;
repeat with the new interactive adapter before marking search integration complete.

## Verification record

Track commands, outcomes, and limitations here as work lands. A passing library
suite or Editor compile does not establish player/IL2CPP, cached image rendering,
UI layout, or mixed-capability multiplayer compatibility. All corresponding tasks
remain unchecked until observed.

The installed Unity Pipeline currently uses quoted grouped command names, e.g.
`unity command "script recompile"`, `unity command "script recompile_status"`,
`unity command "test run" --mode playmode --filter GameContentLocalizationTests --async_tests`,
and `unity command "test status"`. Use `unity command` to inspect available names.

### Verified local results (2026-11-03)

These results describe local development builds prepared for PR review.
No library release, CGS release, or proxy deployment is established by these checks.

| Check | Result | Scope |
| --- | --- | --- |
| Unity 6000.3.27f2 compilation | Passed | Editor compilation; linker/AOT declarations are present but player stripping is unverified |
| `GameContent` PlayMode filter | 23/23 passed | Canonical imports, control identities, live/hidden/pooled presentation, diagnostics, deck provenance, and real Unity Transport loopback tests |
| `GameLocalization` PlayMode filter | 35/35 passed | Origins, portable caches, ordered catalog/image publication, package replacement, offline relocation, fallback and image lifetime |
| `Image` PlayMode filter | 28/28 passed | Localized bindings plus existing image orientation, queue lifetime, local import, transport fallback and duplicate-image regressions |
| `CardViewerPreviewTests` after merging current `main` | 6/6 passed | Both localization cleanup and the new hover-preview behavior retained; Editor recompilation passed |
| `StackLoadingTests` | 13/13 passed | Existing stacking behavior, explicit authored labels, identical-name user stacks and rename revocation |
| `GameManifestLocalizationTests` | 8/8 passed | Staged replacement/removal, failed reads/validation and ID/directory migration |
| `LocalizedCardSearchTests` | 3/3 passed | Translated interactive search remains separate from canonical imports/queries |
| `DeckLoadingTests` | 10/10 passed | Existing canonical deck behavior |
| Settings-language navigation regression | 1/1 passed | Fixture releases its down press after one frame to avoid accidental key repeat |
| `npm --prefix edge-worker test` | 25/25 passed | Adopted public schema contract |
| Companion proxy Jest suite (Node 24) | 538/538 passed, 36 suites | Redirect origin, spoof resistance, GET/HEAD, shared CORS exposure and cached response headers, plus existing regressions |
| Companion proxy lint and TypeScript | Passed | `eslint`, `next typegen`, and `tsc --noEmit` |

The library's separate verification passed 57 .NET tests and 6 schema tests, plus
its Release build (six pre-existing warnings). DLL plugin GUID remains
`ee4977a9cc95f7a42b04ec04513bfb9e`; the adopted hash is recorded in the adjacent
library provenance file. Both OpenSpec changes passed strict validation, and
`git diff --check` passed in all three repositories. No downstream `.github` files
were changed.

### Accepted scope and implementation boundaries

- Session save/load does not exist in CGS. The user approved making session
  sidecars conditional on a future save/load feature; no new session persistence
  is introduced or required for this release. Deck and live multiplayer provenance
  remain required in both OpenSpec changes.
- The approved companion proxy records the final validated upstream URL on the
  server-built Response and emits `X-CGS-Final-URL` through the shared CORS policy.
  Cached responses and HEAD retain it; upstream headers cannot impersonate it.
  CGS accepts this value only from the trusted HTTPS proxy endpoint and requires
  an absolute HTTP(S) URL without credentials. Missing/invalid headers preserve
  the unknown-origin fallback. The production proxy has not been deployed.
- Image bindings retain usable artwork while the next locale loads, wait for
  shared requests, and use canonical fallback after an unavailable replacement.
  Carousel copies own their image instances. Destroyed views unregister without
  attempting to recreate presentation components.
- Canonical label references identify verified decks, the fixed gameplay-deck
  label, and extra groups. User names never infer provenance. Auto-stacking keeps
  the destination's identity; explicit renames clear its authored label.
- The optional `game-content-labels-v1` channel negotiates version, game, host
  session, and connection nonce. Owner checks and per-connection request outcomes
  protect creation, which reuses the existing canonical server routine. Creation
  retries use the negotiated channel; peers without negotiation retain the legacy
  RPC path. Snapshot payloads carry canonical keys, never locale or translations.
- Spawn/rename/ownership observations receive fresh probe nonces. Host snapshots
  echo the probe with incarnation/revision so delayed state cannot bind reused
  entities. Tests cover duplicate request delivery, distinct same-name creations,
  late join, reconnect, ownership changes, rename/despawn, actual network-ID reuse,
  spoofed senders and unknown-label canonical fallback. The parser accounts for
  NGO's already-consumed name hash by bounding `Length - Position`.

### Library review corrections (2026-11-03)

The bundled DLL now includes the fixes from library PR #28: unchanged localization
metadata retains authored unknown/invalid content and duplicate members, while
intentional model edits serialize the edited model. The converter uses the
`JToken.WriteTo` overload available in Unity's bundled Newtonsoft version.
The library has 57 passing tests, six schema checks with encoding warnings treated
as errors, and unchanged generated schemas. The source commit and DLL hash are
updated in the provenance file. Unity recompilation and all seven
`GameContentLocalizationTests` pass with the refreshed DLL, including unchanged
authored metadata and intentional edits under Unity's Newtonsoft runtime. Test-path assertions also cover symlinked temporary
directories, and compatibility tests assert that translated names are actually used.

The initial Unity PR quality gate reported 13 findings. Follow-up changes add
explicit deck/game null guards, preserve Unity's destroyed-object check in image
callbacks, and group local stack-creation settings in `CardStackCreationOptions`.
The original seven-argument RPC and all named-message fields remain unchanged;
server creation still supplies the authenticated player's ownership. Explicit
subscriber storage retains per-view exception isolation. Remaining findings cover
loop simplification and a nested method name. The CI gate must verify the fixes.

The local analyzer helper skipped because Unity generated a `.slnx` while the
helper expects `.sln`. A direct analyzer build was then stopped under memory/swap
pressure before completing the application project; it is not a passing analysis.
The affected Unity tests are rerun independently of that build. Unity compilation
and all 28 game-content tests passed after the corrections, including capability
negotiation, idempotent creation, ownership/ID-reuse snapshots, early deck-name
callbacks, and subscriber exception isolation. All 13 stack-loading and seven
localized asset/rulebook tests also passed after the refactor. A direct comparison
confirmed the legacy stack RPC signature and named-message packet fields are
unchanged. No `.github` files were modified. The authoritative Sonar/CI result
for this follow-up is still pending.

### Remaining verification and release gates

Tracked progress is **upstream 20/27; downstream 10/19**. Checked tasks have local
implementation/test evidence; unchecked tasks are not waived by this report.

- Complete the UI inventory walkthrough, including pending metadata surfaces,
  unsaved edits/selections, message actions, long labels, multiline rules and fonts.
- Exercise actual redirected native/WebGL downloads and IL2CPP player builds.
  Synthetic transport tests establish parsing/persistence, not browser header access.
- Run supported released legacy/capable host-client builds and record the full
  compatibility matrix and architectural review. A plain loopback legacy peer
  checks ignored extension messages but does not replace released-build testing.
- Verify mixed-language load/play/save-deck flows and no locale-triggered gameplay
  or provenance messages end to end. Session saves are outside this gate.
- Walk through installation, rollback and offline relocation with complete example
  games; disk-level consumer tests do not establish every UI instruction.
- Test desktop/mobile layouts. Device discovery found no available iOS Simulator
  (requires macOS/Xcode) or Android Emulator (SDK not detected by device tooling).
- Replace the local development DLL provenance with reviewed source/release
  provenance and coordinate proxy rollout before claiming hosted relative catalogs
  work in deployed WebGL. Keep both OpenSpec changes active until their gates pass.

## PR build validation handoff

The author will validate the CI/CD builds from the implementation PRs. Keep the
remaining OpenSpec tasks unchecked until those results are recorded.

- Library/schema dependency: [FinolDigital.Cgs.Json #28](https://github.com/finol-digital/FinolDigital.Cgs.Json/pull/28).
- WebGL proxy dependency: [cgs-games #559](https://github.com/finol-digital/cgs-games/pull/559).
- Merge/release the reviewed library and deploy proxy support before shipping the
  Unity integration. The bundled development DLL allows this Unity PR to build
  before the library is published; its exact source commit and hashes are recorded.
- Validate desktop/mobile/WebGL builds, localized and original games, full and
  partial catalogs, offline packages, live language switches, UI state retention,
  rollback, and mixed-language/mixed-capability sessions using the inventory above.
- Hosted relative catalogs require the proxy change to be available at the trusted
  endpoint. Opening the proxy PR alone does not deploy that contract.
- The Unity build workflow currently triggers on PR creation against `main`, not
  subsequent PR pushes. If fixes are pushed later, trigger a fresh build for the
  updated branch before using its artifacts as verification evidence.

## Installation and rollback procedure

This is development-build guidance. The library and CGS changes are not released,
and the final-origin proxy header is not yet deployed.

1. Keep the original manifest/cards/sets/decks and add the optional `localization`
   section plus author-supplied catalogs. The library's `examples/localization`
   files demonstrate the format, but `example.invalid` URLs are placeholders;
   authors must supply real hosted artwork/decks or bundle real local resources.
   Existing URL fields still require absolute URLs.
2. For a bundled game, include `cgs.json`, catalogs, artwork, and canonical data
   inside the game package. Relative catalogs resolve from their containing file,
   confined to the imported game directory. For a hosted game, download its real
   HTTPS manifest through the normal CGS loader. Redirects determine the recorded
   origin; `autoUpdateUrl` does not redefine it. Proxy responses without the new
   final-URL header cannot establish a remote relative base.
3. Select the game and UI language. Only the selected game fetches missing
   catalogs/images, subject to `autoUpdate`; negative values disable automatic
   downloads. Missing translations/images use their parent/default/base fallbacks.
   Other installed games show inline, bundled, or cached content without fetching.
4. An authored deck download establishes its optional fingerprint/generation
   binding. Renaming, saving a user variant, or overwriting the deck revokes it,
   even for identical names/cards. A standalone deck import is unproven. Existing
   user text remains literal. A whole game export includes valid provenance and
   published catalog/image caches; staging files are excluded. Reimporting under
   another directory resolves local mappings there while retaining remote origins.
5. External rulebook websites remain links. A cached catalog containing such a
   URL is not an offline copy of the website. Bundle a supported local resource
   when offline availability is needed.
6. To roll back presentation, remove `localization` or set it to null and reload
   the valid manifest. The successful staged reload returns to canonical content;
   a malformed replacement leaves the previous installed state active. Inert
   caches/sidecars do not enable localization on their own. Keep authoring sources:
   older CGS editors can read base content but discard unknown metadata on save.

Disk-level installation, rollback, and relocation are exercised through the real
consumer APIs in the manifest, origin, cache/export, image, and deck-provenance
tests. A complete player/UI walkthrough and published-build compatibility matrix
remain release requirements. Session-save files are not part of this procedure: CGS
has no full-session save/load feature, and session sidecars are conditional on
that future feature.

## Claude review follow-up

The review of library `7cbffdf` and Unity `b0c7902` identified installation,
negotiation, compatibility, cache, and validation defects. The follow-up:

- Checks filesystem links below the trusted game root, allowing platform-managed
  links above it. A failed origin-state write installs the base definition with a
  digest-bound `cgs.json.origin-unavailable` marker instead of guessing an origin.
- Retains authenticated offers while a client switches/downloads the host game.
  Late acknowledgements and local definition reloads can negotiate in the same
  live connection/session. Earlier legacy creations are never replayed; pending
  negotiated requests retain their idempotency IDs during same-game reloads.
- Handles a valid manifest without `locales` in editor diagnostics and preserves
  the legacy `Name (CODE)` set description, translating only the name.
- Keeps safe nested deck downloads canonical without direct-file provenance;
  rejects escaped or linked destinations before downloading.
- Removes superseded cache payloads after atomic publication and excludes orphan
  payloads from package snapshots. With `autoUpdate: 0`, images refresh once per
  target per selection/definition cycle, not on every content notification.
- Coalesces inventory refreshes during asynchronous paged loads; catalog-only
  publication no longer rebuilds canonical inventories. Coverage uses set lookup
  rather than repeatedly removing names from a list.
- Adopts the updated library: malformed inline catalogs do not disable other
  locales. Resource schemas match runtime restrictions; international paths use
  percent-encoded URI references, as documented for authors.

Library validation: 62 .NET tests, six schema tests with an active URI format
checker, Release build, and strict OpenSpec validation pass. Unity compilation
passes. The 40 localization runtime tests and 25 website/Worker checks pass.
The legacy stack-creation RPC code and named-message packet fields are unchanged.
All 33 game-content presentation/provenance/multiplayer tests also pass, including
late negotiation, game-switch/reload recovery, source-only manifest diagnostics,
set-code preservation, nested deck downloads, and coalesced inventory refreshes.
The 13 legacy stack-loading tests pass as well: 86 focused Unity tests in total.
Fresh PR CI must confirm the quality gate and platform builds. iOS/Android/IL2CPP
and full platform walkthroughs remain unverified; these local tests are not a
substitute for the user's CI build validation.


## Library re-review follow-up

The bundled library now scans manifest tokens before parsing duplicate members.
Duplicates below an inline catalog quarantine only that catalog; duplicates in
the manifest or source declaration remain fatal to localization. Valid siblings
and default-locale fallback survive, and unchanged serialization preserves the
authored duplicate members. Relative resource fragments (including a fragment-only
reference to the containing document) are supported. Schema validation rejects
empty HTTP authorities and trailing newlines; runtime URI validation may reject
further invalid authority/port combinations, as the author guide now states.
Coverage credits exact translations for every printing toward the shared-name
target, and the author guide describes metadata loss after validated model edits.

Library validation: 80 .NET tests and all six URI-aware schema tests pass. Unity
compilation and all 16 localization boundary smoke tests pass against the shipped
DLL, including duplicate-member isolation and remote/bundled fragment references.
All 25 website/Worker checks and strict OpenSpec validation pass. Fresh platform
CI builds and the user's device validation remain pending.

## Scheme-like relative resource regression

The library rejects colons in the first relative path segment, including malformed
references such as `https:es.json`, before they can be interpreted differently
against HTTP and HTTPS origins. Remote URI combination uses `Uri.TryCreate` so an
unresolvable reference returns no candidate instead of throwing. The schema uses
the same first-segment restriction; later path segments, queries, and fragments
may still contain colons. Tests cover manifest rejection, direct resolution, and
localized asset fallback for both origin schemes.

Validation: all 91 .NET tests, seven URI-aware schema tests, and 25 website/Worker
checks pass. The bundled DLL and schemas are rebuilt from the recorded source
commit in `game-content-localization-library.json`.
Unity compilation and all 23 localization boundary PlayMode tests pass, including
the seven malformed-reference cases against both HTTP and HTTPS origins.
Platform CI and the user's device validation remain pending.
