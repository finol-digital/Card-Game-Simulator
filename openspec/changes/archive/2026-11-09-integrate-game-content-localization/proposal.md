# Proposal

## Why

CGS's language selector translates application UI but still renders game-authored content in its original language. The companion FinolDigital.Cgs.Json `add-game-content-localization` change now supplies a library implementation; CGS must integrate it without changing canonical gameplay or existing games.

## What Changes

- Adopt the localization-aware library and schemas, preserving the installed plugin GUID and recording the exact library build used.
- Bind game/card/deck/set names, property/enum/boolean labels, rules text, zones, boards, extra stacks, artwork, and dynamic message arguments to the selected UI locale.
- Add staged manifest reload, recorded document origins, portable ordered caches, offline fallback, and live refresh without canonical dataset reloads.
- Separate interactive translated search from deck import and automated gameplay queries; identify controls by canonical keys.
- Record authored-deck provenance separately from deck files; add an optional negotiated stack-creation/label channel with ownership, retry, revision, and legacy-peer safeguards.
- Preserve user-entered text, original definitions, save/deck formats, existing RPC signatures, and canonical IDs. Custom games without metadata retain their existing behavior; the three shipped defaults receive bundled catalogs.

## Capabilities

### New Capabilities

- `localized-game-content`: Runtime game-content presentation, loading/cache lifecycle, assets, canonical search boundaries, and authored provenance.

### Modified Capabilities

- `localized-interface`: Refine the game-content exclusion to allow opt-in authored translations while preserving canonical data, arbitrary user text, application fallback rules, and baseline networking behavior.

## Impact

- `Assets/Plugins/FinolDigital.Cgs.Json.dll` and the published schema copies consume the companion library contract. The current library build is local/unreleased; review and release of the companion changes remains a release dependency.
- `UnityCardGame`, `UnityCard`, file/image loading, `CardGameManager`, card/deck viewers and editors, filters, menus, board/zone rendering, and multiplayer paths require integration. Full-session save/load is not currently supported and is not added here; session provenance sidecars are conditional on that future feature.
- The companion `cgs-games` proxy exposes its validated final upstream URL through a CORS-readable response header; CGS uses it for WebGL relative catalogs and assets.
- Existing Unity Localization language selection, fonts, and application message templates remain authoritative for CGS-owned UI.
- Add focused Unity regression tests plus player and mixed-capability multiplayer verification. No `.github` changes are part of this change.

Upstream contract: [FinolDigital.Cgs.Json PR #27](https://github.com/finol-digital/FinolDigital.Cgs.Json/pull/27), with implementation tasks 4–5 tracked in that repository. The schema and resolver alone do not complete the user-facing feature.

Built-in content scope: ship complete offline catalogs for all ten UI languages for Standard Playing Cards, Mahjong, and Dominoes, including safe upgrades of existing installs and separately proven authored decks without modifying user files.
