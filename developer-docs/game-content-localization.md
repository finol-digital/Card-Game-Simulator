# Game-content localization integration inventory

This is the downstream implementation record for `integrate-game-content-localization`.
The companion contract is FinolDigital.Cgs.Json `add-game-content-localization`
([proposal PR #27](https://github.com/finol-digital/FinolDigital.Cgs.Json/pull/27)).
The dependency build is recorded in [game-content-localization-library.json](game-content-localization-library.json).
It is the published **v1.15.0** release from the successful
[release workflow](https://github.com/finol-digital/FinolDigital.Cgs.Json/actions/runs/42570234520).
The DLL matches the NuGet package and the artifact adopted by dependency PR #602;
the recorded source-file and schema hashes match the previously tested build.
Assembly version is `1.15.0.0`; the existing plugin `.meta` GUID is preserved.
The dated development-build records below describe earlier validation stages.

## Released dependency adoption (2026-11-09)

Library PR #28 is merged and v1.15.0 was published from
`f44e4176da5511439ead5cdbac40e92cb88c17f6`. Dependency PR #602 merged into CGS main
as `3e3c718d294f84139951fc51a230fa8d2d08f8d1`. Unity feature PR #600 remained open;
its update from main resolves the binary conflict in favor of the exact release
DLL, SHA-256 `238b76da2bb7ce09b86b5359e1831eb1a1d2258d2f840340f8af8c213426fd68`.
Both schemas match the release artifacts byte for byte. The release package's
DLL also matches the standalone workflow DLL. No runtime source changes are
needed for this adoption. Release metadata supersedes earlier development-only
provenance; [full CI run 42680518155](https://github.com/finol-digital/Card-Game-Simulator/actions/runs/42680518155)
passed tests, quality checks, and every platform build at `e0c77bad`. PR #600
merged as `bf4452590317b985d2a446913cc01452c8d72d33`. Pages run 42672193735 also
passed on rerun. The attempted local Editor smoke run did not complete because
its Pipeline connection dropped during reload; release validation is supplied by
the successful full CI run, not an inferred local pass.

## Current player verification checklist (reconciled 2026-11-08)

Use this section for current release decisions. The dated records below remain
historical evidence; their older statements that CI and device validation are
pending do not override this section. Do not mark an entire OpenSpec task complete
when only part of its verification has been observed.

### Confirmed evidence

| Check | Result and scope |
| --- | --- |
| Android and Windows, all three default games | User reports successful testing on both platforms. Exact app build identifiers were not supplied. |
| Language switching | User reports success. The exact set of languages tested was not specified; this is not a claim of human review of every translation. |
| Duplicate default-deck choices | User reports success on Android and Windows. |
| Offline use and whole-game transfer | User reports success, including the subsequent offline export/import, restart, and language-switch check. |
| Latest candidate CI | Unity `c69d428394aea7b1a202107f01848939e834556f`: [run 42322729318](https://github.com/finol-digital/Card-Game-Simulator/actions/runs/42322729318) passed tests, quality checks, and all platform builds. |
| Focused duplicate-deck regression checks | 47 default-game tests and 8 provenance tests passed locally before the candidate push. These include all ten locales, unchanged legacy files, modified/renamed/user-saved copies, and explicit-null opt-out. |
| Companion PRs | Library #28 at `ba566e4` and proxy #559 at `2e3834a` passed their checks. A green proxy PR does not establish deployment or browser header access. |

### Player verification results

On 2026-11-07 the user reported “All passed” in response to the four player
checks summarized in the handoff: user-deck preservation, different-language
multiplayer in both host directions, offline whole-game transfer, and layout.
The passes below record that reported scope on Android and Windows. They do not
establish the separate older-client or instrumented engineering checks.

- [x] **P1 — Preserve user decks.** Save an edited deck under a unique name such
  as `My verification deck`, switch languages, and restart. Its name and contents
  remain unchanged while built-in decks translate. **Result: user-reported pass.**
- [x] **P2 — Layout.** Check long Spanish/German labels and Japanese/Korean/Chinese
  characters in card properties, filters, and deck dialogs on Android and Windows.
  **Result: user-reported pass.** This does not claim linguistic review of every
  translation or a complete inventory of every UI surface.
- [x] **P3 — Two current builds, different languages.** Connect Android and
  Windows using different languages. Load, draw, move, rename, and reconnect,
  then repeat with the other device hosting. Cards and moves agree; user-entered
  names remain literal. **Result: user-reported pass.**
- [x] **P4 — Current and supported older builds.** Use an older CGS version that
  can already connect under the existing multiplayer version rules, in a separate
  installation/profile. Test old host/new client and new host/old client, including
  deck load, moves, rename, and reconnect. Gameplay must work without duplicate
  creation or disconnects. With a legacy host, shared stack names stay canonical.
  Legacy peers always see canonical shared names; modern peers translate only
  where negotiated provenance exists. If existing version rules prevent the
  connection, record the version and mark this combination blocked, not passed.
  **Result: user-reported pass for Windows v1.170 host/Android PR client and
  Android PR host/Windows v1.170 client, including load, moves, rename, reconnect,
  and expected canonical shared names.**
- [x] **P5 — Offline whole-game transfer.** Export a whole localized game package
  (not just a deck) from one installation. Import into a disposable, clean profile
  or second installation while offline, then restart and switch language. Cards,
  bundled/cached translations and artwork, and proven authored-deck labels should
  survive. **Result: user-reported pass for the player transfer check.**
  User deck names remain literal. External rulebook websites are not
  promised offline. Start with a default game, but complete E1 with an external
  catalog too: default games can obtain catalogs from application resources and
  therefore do not prove that a remote cache travelled in the package.

### Integration checks requiring a fixture or tooling

These checks are complete from the combined player, automated-test, and source-audit
evidence mapped below. A checked item does not imply every case was manually run
on every platform; the recorded scopes and limitations still apply.

- [x] **E1 — Real native/WebGL origin and cache test.** Prepare a complete custom
  test game with working cards/artwork, an external relative catalog, and a
  manifest URL that redirects into a different directory. Use a partial catalog
  to exercise fallback and a localized image to exercise resource resolution.
  The proxy change must be available at the endpoint the WebGL build trusts;
  do not substitute an arbitrary untrusted proxy. Verify the browser can read
  `X-CGS-Final-URL`, catalogs resolve from the final manifest URL, and images from
  the final catalog URL. Reload/restart and then export/import offline into a
  clean installation. Cached text/images must survive; a failed refresh must
  retain last-good content. Check missing/invalid-header canonical fallback too.
  Library `examples/localization` contains placeholders and is not a runnable
  player fixture without this preparation. The fixture and deployed proxy are now
  available, and the hosted player checks passed. Fault/header cases are covered
  by automated tests, not browser fault injection.
- [x] **E2 — Rollback and original-game compatibility.** On disposable fixtures,
  successfully reload a custom game after removing `localization`, then after
  setting it to null: presentation must return to its canonical source language.
  For a default game, use explicit null; absence deliberately restores bundled
  catalogs. Save/reopen through the updated game editor and confirm null persists.
  A malformed replacement must retain the previous usable state. Include original
  English-, Spanish-, and Japanese-authored games with no localization metadata;
  their content must keep its authored language and their decks/imports still work.
- [x] **E3 — Verify network invariants.** Supplement P3/P4 with logs or transport
  instrumentation: locale-only changes must send no gameplay/provenance mutations;
  compare canonical cards, identifiers, and equivalent saved-deck contents across
  languages. Existing loopback tests cover ordering, retries, ownership, and
  snapshots, but do not replace the supported released-client matrix. No session
  save/load testing is required: that feature remains outside this change.

- [x] **E4 — Complete the remaining surface and transition inventory.** Verify
  installed-game metadata, import/save dialogs, alternate backs, and language
  switches with selected filters and unsaved edits. Confirm explicit identical
  user saves stay separate from built-in decks. In multiplayer, switch language
  during play and join while the client starts on a different game; after the
  game switch/download, newly created authored stacks should translate. Existing
  automated tests cover parts of these cases, but the four reported player checks
  do not independently establish every extended case. Earlier legacy creations
  need not be retroactively translated.

### OpenSpec completion and release mapping

| Evidence | Downstream tasks it helps complete | Upstream tasks it helps complete |
| --- | --- | --- |
| Confirmed smoke tests + P1/P2 + E4 and the surface inventory | 3.1, 3.2, 5.2 | 4.5, 5.1 |
| P3/P4 + E3 and the recorded protocol review | 4.2, 4.4, 5.3 | 4.8, 5.2 |
| P5 + E1/E2 and existing cache/rollback tests | 2.3, 5.1, 5.4 | 4.3, 4.10, 5.3 |

The referenced tasks include additional clauses: reconcile each complete task
against its evidence before checking it off. Record the tested build identifiers,
finish any findings, and replace development dependency provenance with the
reviewed library release before shipping. Merge/release the library and deploy
the proxy before the dependent Unity release. Archive both OpenSpec changes only
after the remaining requirements are satisfied; a build or this checklist alone
does not close them.

### Engineering follow-up results (2026-11-07)

- `GameContentNetworkTests`: **5/5 passed**. The real Unity Transport test now
  wraps both peers' named-message receivers after creation traffic settles.
  Switching Spanish/Japanese/English produced zero additional provenance messages
  and preserved stack IDs, canonical names/cards, ownership, positions, and label
  references. Creation retries, spoofed ownership, late acknowledgements,
  game-switch/reload recovery, reconnect, and unlocalized peers also passed.
  This is an instrumented Editor test, not a released legacy-player matrix or
  a capture of every gameplay RPC. Downstream task 4.2 is complete; task 4.4 and
  upstream task 4.8 remain open for supported legacy/capable player verification.
- `GameLocalizationCatalogTests`: **5/5 passed**, including the complete fixture
  below through Unity's actual loader and presentation context. Spanish names,
  multiline rules, the alternate card-back resource, Japanese per-leaf fallback,
  and a two-card authored deck resolve correctly. Removing localization and
  explicitly nulling it both return the custom fixture to canonical names without
  changing card bytes. This adds runtime evidence for E2; it does not replace its
  remaining player/editor walkthrough and original-game cases.
- The fixture manifest and both catalogs pass the shipped schemas with URI format
  checking enabled. The **2 fixture packaging tests pass** and compare the generated
  files to the maintained fixture and every archive entry, including real images.
- Production proxy GET and HEAD probes of
  `https://cgs.games/api/proxy/www.cardgamesimulator.com/games/Standard/cgs.json`
  returned HTTP 200, but neither included `X-CGS-Final-URL`; neither CORS exposure
  list contained it. **E1 remains blocked on proxy deployment and a hosted redirect
  fixture.** This probe establishes header absence, not browser behavior.

Current implementation progress: **downstream 20/20; upstream 28/28**.
Downstream 5.4/upstream 5.3 are complete following published v1.15.0 adoption,
successful full CI, and the feature merge recorded above. Functional completion combines the dated local/CI results with the subsequent
player reports. The published library dependency is recorded above; a public
CGS app release remains separate.

### Verification reconciliation (2026-11-08)

| Gate | Evidence used for completion |
| --- | --- |
| E1: origins, caches, relocation | Native and WebGL redirected downloads, readable production CORS header, catalog-relative image, restart and offline cross-installation transfer passed. `GameLocalizationOriginTests` covers missing/invalid/untrusted proxy headers, direct-header disregard, unknown-origin fallback and relocation. Cache/catalog/image tests cover publication races and last-good retention. Proxy tests cover validated redirects and cached-header retention. No deliberate bad-header browser injection is claimed. |
| E2: rollback and compatibility | Player absent/null package replacements and editor save/restart passed; `GameManifestLocalizationTests` and catalog tests cover actual same-instance reload and malformed replacement. Default-game tests cover explicit-null opt-out. English legacy fixtures and all three defaults, plus the reported original Spanish/Japanese game walkthrough, establish canonical fallback and compatibility. |
| E3: canonical multiplayer and saves | Both modern host directions and released v1.170 host/client directions passed, as did joining from another game and switching during play. Transport tests preserve canonical stack fields and observe zero locale-only provenance packets. Library `EveryDeckExportAndCanonicalQueryIsIdenticalAcrossLocales` covers all five deck formats; Unity boundary tests preserve imports/exports. The audited `DeckSaveMenu.SaveToFile` writes canonical `deck.ToString()` after revocation. Player edited/identical user saves and restart passed. This combines transport, serializer, save-path and player evidence; it is not a new byte-for-byte capture of files saved by both live peers. |
| E4: presentation and transitions | Control tests cover duplicate labels and empty-enum flags; binding tests cover active/hidden/reused views and dynamic arguments. Dialog tests preserve callbacks/input. Selection-gated presentation/image loading keeps unselected game refresh local. Player layout, language switching, alternate artwork, filters, unsaved edits, intentional deck copies, and multiplayer transitions passed. The surface inventory below records the applicable evidence. |

The successful full player CI is [run 42448382304](https://github.com/finol-digital/Card-Game-Simulator/actions/runs/42448382304)
on `16383eee84fca306bbd94b8c993570d8947306b4`; subsequent commits changed fixtures,
verification documentation, or task status, not runtime/player code. Runtime
source remains based on the verified `c69d428` duplicate-deck fix. Human device
coverage is Android and Windows plus the reported WebGL walkthrough; this does
not claim iOS device testing, a cold offline WebGL shell, or human linguistic
review of every translation. Those limits do not erase the automated coverage.

Library v1.15.0 and the Unity implementation are merged; both completed changes
are archived with their requirements synced into the main specs. Proxy #559 is
deployed. Publishing a new public CGS app release remains a separate release
operation; this archive does not claim production app-store rollout.

### Complete verification fixture

The maintained [fixture directory](fixtures/localization-verification/cgs.json)
contains two canonical cards, actual Standard Playing Cards artwork, a deck with
portable authored provenance, external Spanish/Japanese catalogs, and a Spanish
card-back image relative to the catalog. It is a separate custom game; it cannot
use the built-in default-game catalog fallback. Artwork is copied unchanged from
the repository's Standard game, whose notices and licensing still apply.

Generate an importable package in a new output directory:

```sh
python3 tools/localization/verification_fixture.py --output Temp/localization-verification
```

Import `CGS Localization Verification@example.invalid.cgs.zip` using CGS's normal
whole-game import. This package is deliberately offline-only (`autoUpdate: -1`);
its `example.invalid` URL fields identify the fixture and are not live endpoints.
All canonical cards, artwork, external catalogs, and the authored deck are bundled.
Do not reuse the identity for a real game. Use a separate installation/profile for
repeat import/replacement tests.

Expected results:

- Spanish: game `Verificación de idiomas`, cards `As de tréboles`/`As de picas`,
  deck `Baraja de verificación`, translated rules with a line break, and the dark
  alternate card back. Canonical deck contents remain two English card names.
- Japanese: game `言語の検証` and club `クラブのエース`; the untranslated spade,
  deck, properties, and artwork fall back to their English/canonical values.
- English: canonical names and artwork. Language changes never add/remove cards.
- Export/import offline preserves the external catalog files and authored deck
  binding. This checks bundled external catalogs; a hosted download/cache round
  trip still requires the next step.

For the remote/proxy test, regenerate with the actual final HTTPS hosting directory:

```sh
python3 tools/localization/verification_fixture.py --output Temp/localization-hosted --base-url https://YOUR-HOST/final/game/
```

Serve the generated game directory's contents at that URL and configure an entry
URL in a different directory to redirect to `final/game/cgs.json`. The generator
sets all legacy URLs absolute and enables automatic refresh; localization URLs
stay relative. Load the entry URL as a new hosted game, without pre-importing its
offline zip, so the test actually downloads catalogs and records origins. Verify
the proxy/browser header behavior, restart, and export/import of downloaded caches
as described in E1. The command does not host files, configure redirects, or deploy
the proxy. Do not claim the hosted check passed from importing the offline zip.

## Hosted player test handoff (2026-11-08)

Proxy PR #559 was merged as `9c3bb4c555537a8df08b4d8d130335e2b6d5ed54`.
Firebase reports a successful production rollout. The live proxy now returns
`X-CGS-Final-URL` and includes it in `Access-Control-Expose-Headers`. The deployment
prerequisite is satisfied; the full E1 player/cache walkthrough remains open.

A cross-origin browser fetch from the WebGL preview on 2026-11-08 also passed:
the GitHub redirect returned HTTP 200 with a JavaScript-readable final raw URL;
the Spanish catalog resolved relative to that URL and returned HTTP 200; its
relative card-back image returned HTTP 200, `image/png`, and 15,741 bytes.
This verifies deployed redirect/CORS/resource transport, not Unity's in-app
rendering, persistence, or offline cache export.

The user subsequently reported **all four online WebGL checks passed** on the
linked PR build: downloading the hosted game, Spanish card/deck/artwork display,
intentional Japanese per-leaf fallback, and repeating the language switches after
an online browser reload. Offline switching and transfer of the downloaded caches
were subsequently reported **passed**: switching Spanish/Japanese/English with
the loaded WebGL app offline, whole-game export/import into a native PR build
without internet access, and repeating the switches after an offline native
restart. The user did not identify which native device was used for this final
transfer. This confirms the hosted fixture's browser/cache-transfer walkthrough,
not a cold offline launch of the WebGL application shell.

The user subsequently reported **passed** for a fresh native download through the
redirecting entry URL, Spanish names and localized card back, intentional Japanese
fallback, and repeating language switches after restarting CGS. The requested
platform was Android or Windows; the user did not specify which was used for this
check. The remaining E1 fault cases are separate evidence.

The user reported **all three rollback checks passed** using the Windows PR build
and the separate `CGS Localization Verification@example.invalid` packages:

- Importing the localized baseline showed Spanish presentation.
- Replacing it with the package omitting `localization` returned game, card, and
  deck names to canonical English while the application UI remained Spanish.
- Restoring the localized baseline and then importing explicit `localization: null`
  returned those names to English; saving through the game editor and restarting
  preserved that opt-out.

These are whole-game replacement and editor-persistence checks on the custom
fixture. The five passing catalog tests separately cover same-instance reload.
Default-game opt-out is a separate automated test; the original Spanish/Japanese
game walkthrough is recorded below.

The user reported **filters, unsaved edits, and an intentionally saved identical
deck copy all passed** in a PR build with Standard Playing Cards. A selected
filter and its matching cards survived a language switch, unsaved card quantities
remained intact, and an unchanged built-in deck saved under its original English
name stayed literal and separate from the translated authored choice. The saved
copy loaded correctly after restart. The device was not specified for this step.

The user reported **Suma o Muere and Hokuto no Ken Card Battle both passed**
the original-language compatibility walkthrough on a PR build: import/open and
deck loading, UI switches among English/Spanish/Japanese, preserved authored
content language and card rendering, loading/drawing, and restart. The device
and exact build were not specified for this check. This records the player
compatibility result; it is not an independent inspection of their current
hosted manifests or a claim that new translations were added to those games.

The user reported **both extended multiplayer transition checks passed** with
PR builds on both devices:

- An English host selected Standard Playing Cards; the Spanish client started on
  Mahjong. Joining switched the client to Standard, and a newly loaded built-in
  deck displayed its label in each player's language.
- Switching one device's language during play refreshed its local labels while
  preserving cards, positions, deck contents, and user-entered names, with no
  duplicate creations or disconnects.

This closes the player-reported multiplayer-transition portion of E4. The final
filter/unsaved-edit/copy report and automated binding/dialog tests complete the
remaining cases as mapped in the reconciliation above.

The user reported P4 **passed** with released **v1.170** versus the PR build. The tag
(`c24e3e7f3eee9122f4bdf4fd484c5c3218f26cac`) contains neither `GameContentNetwork`
nor the game-content presentation directory, making it a genuine pre-feature
client. The recorded player matrix is:

| Host | Client | Result |
| --- | --- | --- |
| Windows released v1.170 | Android PR build | User-reported pass: load, draw/move, rename, reconnect; canonical shared names |
| Android PR build | Windows released v1.170 | User-reported pass: same gameplay flows; legacy client retains canonical labels |
| Windows PR build | Android PR build | Previously reported pass with different languages |
| Android PR build | Windows PR build | Previously reported pass with different languages |

Focused architecture audit: comparison with v1.170 confirms the existing
`[ServerRpc]` attribute and seven-parameter `CreateCardStackServerRpc` signature
are unchanged. The optional named-message path checks the authenticated sender
against the player object's owner, reuses the server-only canonical creation
routine, and records request outcomes before creation so retries cannot duplicate
stacks. The canonical path still sets `CurrentDeck` and the player's stack list;
the client returns after negotiated creation instead of also sending the legacy
RPC. Combined with the five passing transport tests and this released-player
matrix, downstream 4.4 and upstream 4.8 are complete. This does not waive the
separately tracked save/export and locale-invariant requirements.

The [hosted fixture](fixtures/localization-hosted/cgs.json) is generated with real
GitHub raw URLs and `autoUpdate: 0`. It uses the same two cards and catalogs as the
offline fixture. Its identity ends in `@raw.githubusercontent.com`, so it is
separate from the offline example. These are review-branch URLs, not permanent
production game URLs; regenerate if the branch is renamed/deleted.

1. Open the [tested WebGL build](https://card-game-simulator--preview-58didt1c.web.app)
   on Windows in Chrome or Edge. This is the successful `16383eee` CI build;
   later fixture/documentation changes do not require another player binary.
   The Firebase preview channel can expire; obtain a fresh preview if unavailable.
2. In game management, choose Import, then **Download from Web AutoUpdate URL**.
   Paste this redirecting entry URL, rather than importing the offline zip:

   ```text
   https://github.com/finol-digital/Card-Game-Simulator/raw/refs/heads/feature/game-content-localization/developer-docs/fixtures/localization-hosted/cgs.json
   ```

   GitHub redirects to `raw.githubusercontent.com`; the production proxy must
   retain that final origin. Catalogs resolve under `locales/`, and the Spanish
   card-back image resolves under `locales/images/`, relative to its catalog.
3. Wait for both cards to load. Select Spanish: expect `Verificación de idiomas`,
   `As de tréboles`, `As de picas`, `Baraja de verificación`, and the dark card back.
   Select Japanese: expect `言語の検証`, `クラブのエース`, but canonical English
   `Ace of Spades` and `Verification deck`. That partial fallback is intentional.
4. Reload the browser while online and repeat the language switch. Then keep the
   loaded app open, set browser DevTools Network to **Offline**, and switch between
   Spanish/Japanese/English again. Cached translations and card backs should work.
   Restore network access afterward. This is not a claim that the WebGL application
   shell itself can cold-start offline; testing a reload with normal network access
   alone also does not prove that cached game content works offline.
5. Export the whole hosted game from WebGL. Import that package into a separate
   native installation/profile while offline, restart, and switch languages. This
   verifies downloaded caches travel with the package. A standalone deck export
   or the prebundled offline fixture cannot substitute for this check.

Record download, Spanish, Japanese fallback, reload, offline switch, and package
transfer separately. Do not close E1 if only the online language checks pass.

## Presentation surfaces and source boundaries

Paths below are relative to `Assets/Scripts/`. Status combines the recorded
automated checks, source audit, and explicitly scoped player reports. Existing
static UI labels already use the separate CGS UI localization system.

| Surface | Concrete owners | Translation responsibility | Canonical state to preserve | Status |
| --- | --- | --- | --- | --- |
| Installed/current game chooser, banner, copyright, rulebook | `Cgs/Menu/MainMenu.cs`, `Cgs/CardGameManager.cs`, `Cgs/Menu/CgsGamesBrowser.cs` | Game metadata; cached/inline-only refresh for unselected installed games; external website titles only when a matching installed game is known | Game IDs, download URLs, website/source metadata, selection | Binding/lifecycle tests and selection-gated loading audited; player game-name/fallback checks passed |
| Card display and property viewer | `Cgs/CardGameView/Viewer/CardViewer.cs`, `Cgs/CardGameView/Multiplayer/CardModel.cs` | Card/set names, nested/list text, boolean/enum/empty labels; card artwork | Card ID/face/printing, source image template, controls keyed by path | Viewer/control tests pass; localized artwork lifetime verified |
| Explorer filters and interactive search | `Cgs/Cards/CardSearchMenu.cs`, `Cgs/Cards/CardsExplorer.cs` | Translated display labels and UI-only name/text matching | Enum/empty bits, canonical filter keys; never change `FilterCards` | Interactive/canonical separation and control tests pass |
| Deck picker/editor/preview | `Cgs/Decks/DeckLoadMenu.cs`, `DeckEditor.cs`, `DeckCardSelector.cs`, `SearchResultsLayout.cs` | Verified authored names, card display labels, previews | Filenames, deck source name, unsaved edits and selected cards | Local provenance tests and player layout/filter/unsaved-edit/identical-copy checks passed |
| Save/import prompts and user naming | `Cgs/Decks/DeckSaveMenu.cs`, `Cgs/Cards/SetImportMenu.cs`, `Cgs/CardGameManager.cs` | Application prompts with refreshable game-text arguments | Entered names, imported content, callbacks; save/rename clears authored binding | Live-message/dialog tests, canonical save-path audit, and player save/import/restart checks passed |
| Shared deck/stack viewer and labels | `Cgs/CardGameView/Multiplayer/CardStack.cs`, `Cgs/CardGameView/Viewer/StackViewer.cs`, `Cgs/Play/PlayController.cs` | Per-client display from validated canonical label reference | Existing stack-name network variable, owner, stack identity/cards, user rename | Loopback tests and released v1.170/current-build matrix passed; live locale/game-switch checks passed |
| Boards, zones, card backs, playmat | `Cgs/CardGameView/Multiplayer/Board.cs`, `CardZone.cs`, `Cgs/Play/PlayController.cs`, `FinolDigital.Cgs.Json.Unity/UnityCardGame.cs` | Canonical board/back IDs and authored zone labels; localized image variants | Dimensions, placement, snapping, zone gameplay effects, original resource files | Image/zone bindings audited; card-back canonical selection and default-game tests passed; authored editor fields stay canonical |
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

The library is released as v1.15.0; the CGS feature is merged in PR #600.
Proxy #559 is deployed and its final-origin header was verified from WebGL.

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
6. To roll back a custom game's presentation, remove `localization` or set it to
   null and reload the valid manifest. For the three default games, use explicit
   null: absent metadata deliberately enables bundled fallback catalogs.
   The successful staged reload returns to canonical content;
   a malformed replacement leaves the previous installed state active. Inert
   caches/sidecars do not enable localization on their own. Keep authoring sources:
   older CGS editors can read base content but discard unknown metadata on save.

Disk-level installation, rollback, and relocation are exercised through the real
consumer APIs in the manifest, origin, cache/export, image, and deck-provenance
tests. The player walkthrough and released v1.170 compatibility matrix are now
recorded above; adoption of the published library remains a release requirement.
Session-save files are not part of this procedure: CGS has no full-session
save/load feature, and session sidecars are conditional on
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

## Encoded bundled filename CI regression

CI run 41993612145 passed the quality check but failed
`OlderBundledGameUsesActualLocationAndConfinesDecodedPaths`, preventing platform
builds. The failure also reproduced locally: Mono's HTTP-context syntax check
rejects an encoded `#` adjacent to encoded Unicode in a valid relative filename.
The library now masks complete percent triplets only while checking syntax,
preserving the original resource URI for resolution. Incomplete or invalid
escapes still fail validation. Regression coverage includes `es # 日本語.json`,
remote resolution, malformed escapes, and bundled confinement. All 94 .NET tests
pass with the rebuilt library.
Unity compilation, all 20 origin tests, and all 26 localization boundary tests
pass, including the CI failure. A standalone Mono public-API probe also confirms
the filename resolves while malformed escapes and paths outside the package are
rejected. A broader `Localization` filter run stopped producing results and was
cancelled; it is not counted as passing. Fresh full CI remains required.


### Explicit-null opt-out follow-up

Library source `eede5fb` preserves authored `localization: null` through
serialization using a presence flag, while absent legacy fields and direct
property clearing still omit metadata. Unity copies this flag during staged
publication and game-editor model creation. Updated default-game tests verify
an edited opt-out remains disabled, and reload tests distinguish absent custom
metadata from explicit null. The library's 97 tests and seven URI-aware schema
tests pass; generated schemas are unchanged. Unity's 38 default-game and nine
manifest-reload tests pass with the rebuilt DLL. The provenance file records
its source and binary hashes. Player CI remains a separate verification step.

## OpenSpec archive validation (2026-11-09)

Both localization changes are complete and archived under `2026-11-09-*`;
their requirements are synced into the main specs. The library's two specs and
Unity's new `localized-game-content` spec pass strict validation. Unity's complete
spec set passes normal validation with four pre-existing warning categories:
placeholder purposes in `card-back-persistence` and `schema-validation-test-mode`,
and overlong requirement descriptions in `localized-interface` and
`translation-contributions`. The existing UI-localization requirement was merged
without dropping scenarios or splitting its established contract. Those unrelated
purpose/structure cleanups are not part of this feature archive.
