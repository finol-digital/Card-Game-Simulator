# Design

## Context

See proposal.md for motivation and scope. The source contract is FinolDigital.Cgs.Json's `add-game-content-localization` change. The local library implementation provides `LocalizationTargets`, `LocalizationContext`, safe JSON readers, typed catalogs, explicit resource origins, and generated schemas. Its release is not yet published.

Observed integration points:

- `CgsLocalization.Changed` signals readiness/locale changes; Unity Localization supplies the selected locale. Its assembly currently owns only CGS UI messages.
- `UnityCardGame.ReadProperties` clears lists and populates the live instance. A missing field can survive an update; a failed read can leave partially updated live data.
- `UnityFileMethods.SaveUrlToFile` downloads through redirects/proxies but returns no origin/result. `UnityCardGame.Download` reloads the resulting definition and `CardGameManager` imports/exports whole game directories.
- `CardSearchMenu`, deck parsers, and automated plays share `FilterCards`. Only the interactive call site can acquire translated matching.
- `CardViewer.DisplayNameLookup` reverse-maps labels to property paths; enum-empty controls allocate/look up flags using canonical `DisplayEmpty`.
- Deck files use canonical names; `CgsNetPlayer` creates owned stacks through an owner-only RPC. The stack name is networked and no authored provenance is transmitted.
- `LocalizedMessageBinding` stores precomputed arguments. Game-text arguments need refreshable canonical references, not translated strings captured once.

The implementation inventory is maintained in `developer-docs/game-content-localization.md` and must record verification separately from planned changes.

## Goals / Non-Goals

**Goals:** Integrate the library at rendering boundaries; preserve canonical behavior and existing game content when catalogs are absent; keep all required downstream verification visible.

**Non-goals:** Change the shipped language selector, machine-translate content, change canonical deck/save formats or existing RPC signatures, replace the UI localization system, or change `.github` workflows.

## Decisions

### Consumer ownership and lifetime

Add an assembly-compatible game-content presentation service keyed by `UnityCardGame` instance and installed manifest generation. Pass the selected locale explicitly to the library. Keep the service out of the application-UI localization assembly to avoid a circular dependency with CGS/game models. Refresh snapshot contexts only on canonical data completion, manifest/catalog publication, game selection, or locale change; do not allocate inventories per card render/frame.

Active views subscribe/unsubscribe at their existing lifecycle boundary. Hidden/pooled views read the latest context on enable. A view retains canonical references for labels and dynamic message arguments. User inputs and game-editor source fields stay canonical; label translation must not overwrite edit values. Unselected games use inline, bundled, and cached resources without downloads.

### Staged definitions and independent generation checks

Deserialize into a staged definition with localization reset, validate installability, then publish definition fields/localization and advance the generation on the main thread. Retain the last-good state/generation on failure. Clear old localization immediately on a successful absent/null reload. Preserve existing parser initialization and ID/directory migration semantics. Presentation invalidation cannot reset gameplay objects, filter choices, or deck edits.

### Explicit origins and portable ordered caches

Extend download results to expose the installed document's final origin and success. Persist origin independently of `AutoUpdateUrl` and game ID; redirects retain the final containing-document base. A proxy transport URL is not automatically the origin of the proxied document: retain the real upstream final origin when supplied, otherwise mark remote origin unavailable rather than guess it. This must be covered for native and WebGL downloads. The companion `cgs-games` proxy supplies `X-CGS-Final-URL` from the final server-validated upstream destination after redirects, never from an upstream-supplied header. Expose it using `Access-Control-Expose-Headers` and retain it with cached responses. CGS accepts it only from its trusted HTTPS proxy endpoint and only as an absolute HTTP(S) URL without credentials; a missing/invalid header or untrusted transport leaves remote origin unavailable. Direct downloads use their final transport URL and ignore this header.

Store versioned state, catalog entries, assets, and provenance under the game directory's `localization/` subtree. Keys include canonical game, normalized locale, source URI, and asset target where relevant. Hash keys into controlled filenames; portable metadata records only relative cache paths. Package import starts a fresh cache-owner incarnation.

Assign a monotonically increasing request ticket per key at initiation. Under a per-key publication lock, validate the payload and compare against the highest successful ticket. Publish payload plus origin/validators/mappings/timestamps and ticket atomically. Use immutable entry generations and an atomic manifest/pointer replacement so readers/export see one complete version. A failed/cancelled/malformed request never advances successful-publication order or erases last-good data. Reject old-incarnation writes. Separately gate view callbacks by locale/game/manifest generation; a cache write alone cannot authorize a view refresh.

Use the library's resource-origin APIs with a platform adapter that resolves every ancestor symlink, or rejects links on platforms where physical resolution is unavailable. Normalize/decode and confine bundled paths before opening them. Verify native and WebGL virtual filesystem behavior. Respect `autoUpdate < 0`, retain existing artwork while loading, and leave canonical image caches untouched. External rulebook sites remain links, not mirrored packages.

### Canonical controls and interactive search

Replace reverse label lookup in `CardViewer` with index-to-canonical-path options. Relabel controls in place and preserve indices, keys, flags, scroll, and selection. Translate empty labels without changing the `DisplayEmpty` lookup key. Keep `FilterCards` canonical; introduce a separate interactive search adapter that combines canonical typed filters with canonical/currently translated text. Deck imports and `DeckPlayCard` queries never invoke that adapter.

### Provenance and optional networking

Implement the upstream design's versioned `localization/provenance.json` artifact-generation/fingerprint bindings. Authored installation establishes a binding; user creation, identical replacement, rename, or save-as clears it. Imports without valid sidecars remain unproven; never infer from names/cards. CGS has no full-session save/load feature; adding one is out of scope. If session persistence is introduced later, optional session sidecars must bind to the exact saved-document fingerprint and deterministic locators, retaining canonical names when absent or mismatched. That future integration and its tests are conditional; deck sidecars and live multiplayer provenance remain required here.

Negotiate NGO named-message capability `game-content-labels-v1` by host offer/client acknowledgement scoped to session/game; timeout after five seconds selects legacy behavior. For negotiated authored creations, send canonical creation arguments, label reference, and a connection/session-scoped idempotency ID through one versioned request. Explicitly validate the sender's player ownership and preserve existing creation side effects (`CurrentDeck`, owner, stack registry). The client must not also send or retry via the old RPC. Repeated requests return/reuse the recorded creation result; reconnect creates a new scope and must not revive old requests.

The host binds references directly to the created stack, validates kind/key/ownership, and sends revision/incarnation snapshots only to capable recipients. Rename/despawn invalidates references; late join gets current snapshots. Legacy hosts/senders preserve canonical shared-stack labels for everyone. No localization extension is emitted for unlocalized games. Verify supported legacy builds' unknown named-message dispatch before release; an Editor compile alone is insufficient.

### Dependency adoption and verification

Preserve the plugin `.meta` GUID while installing the library build. Record DLL hash, assembly version, library source base, and whether sources are uncommitted; do not present a local 1.0.0 development build as a published package. Copy both schemas from the same generator output. Add tests that exercise `UnityCardGame` parsing, `UnityDeck` imports, canonical queries, and library display resolution together.

Use Unity CLI compilation and focused EditMode/PlayMode suites, then supported player targets and multi-process multiplayer checks. Keep external release/build dependencies explicit. Do not mark upstream task 4.1 complete merely because this proposal exists.

## Risks / Trade-offs

- Large UI surface → inventory every render/source-write boundary and verify both active and newly shown views.
- Stale downloads → independent display-generation, cache-ticket, and cache-incarnation checks.
- Older editors → preserve metadata in the updated editor and document that old releases can drop unknown fields.
- Named-message authorization → explicitly preserve owner-only RPC behavior and test forged/duplicate requests before rollout.
- WebGL transport and filesystem differences → exercise origin retention, persistence, package relocation, and confined resource access in a player build.
- Unreleased companion library → pin development provenance now; replace with the reviewed release before shipping.

## Migration Plan

1. Adopt/test the companion library, schemas, and source/display boundary inventory.
2. Implement staged loading/cache/refresh, display bindings, assets, and interactive search.
3. Implement and verify authored provenance and negotiated networking before enabling shared authored-stack translations.
4. Complete offline/rollback, device-layout, and mixed-capability/player verification; update both repositories' task status only against evidence.
5. Release the companion library and then CGS support. Existing games require no edits. Removing/nulling localization on a successful reload returns all presentation to canonical values while inert sidecars remain harmless.
