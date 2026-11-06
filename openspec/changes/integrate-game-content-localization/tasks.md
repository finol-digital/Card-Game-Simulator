# Tasks

This change implements upstream tasks 4–5 from FinolDigital.Cgs.Json's `add-game-content-localization`. Keep the upstream tasks unchecked until all corresponding implementation and verification here is complete. A development DLL is not a published dependency release.

## 1. Adopt the library and establish consumer boundaries

- [x] 1.1 Adopt the localization-aware DLL and both schemas without changing the plugin GUID; record reproducible source/build provenance and verify Unity compilation plus canonical parsing/export and display-resolution smoke tests. Maps to upstream 4.1.
- [x] 1.2 Deliver the display-surface and canonical-data inventory with concrete source files, integration responsibilities, and verification status; confirm canonical import/gameplay query call sites remain separate. Maps to upstream 4.1.

## 2. Load and refresh content safely

- [x] 2.1 Stage definition parsing before publishing live localization/generation; test inline replacement, absent/null removal, failed reads/validation, preserved last-good state, and existing ID/directory migration. Maps to upstream 4.2.
- [x] 2.2 Add presentation-context ownership and selected-locale events; test initial/inactive/active views, rapid game/language changes, and zero canonical reloads or unselected-game downloads. Maps to upstream 4.2 and 4.5.
- [ ] 2.3 Record final document origins independently of update URLs, including the companion cgs-games final-URL header, redirect validation, CORS exposure, cached-response retention, and native/WebGL consumption; test restart, unavailable origin, bundled fallback, and package relocation. Maps to upstream 4.3.
- [x] 2.4 Implement versioned portable catalog caches with per-key initiation tickets and atomic publication; test newer-success-first/older-completes-last, failed refresh retention, same-URI reloads, cache replacement, offline reopen/export, and path confinement. Maps to upstream 4.3.
- [x] 2.5 Integrate localized images and rulebook candidates; test separate ordered caches, regional fallback, unchanged canonical assets/geometry, failed refreshes, offline export/import, and link-only external rulebooks. Maps to upstream 4.4.

## 3. Bind presentation while preserving canonical state

- [ ] 3.1 Bind game-list and active-game metadata, card/set/property/boolean/enum/empty labels, and rules text; test live/hidden/pooled views, canonical dropdown keys, duplicate labels, and preserved empty-filter flags. Maps to upstream 4.5.
- [ ] 3.2 Bind deck/group/zone/board/back labels and dynamic UI message arguments to canonical references; test in-place locale refresh preserving user text, dialog actions, selected cards, unsaved edits, and shared gameplay state. Maps to upstream 4.5.
- [x] 3.3 Add a dedicated interactive translated-search adapter and wire only the search screen; test translated/canonical name collisions against canonical deck imports and automated card queries, and unchanged typed filters. Maps to upstream 4.6.
- [x] 3.4 Add reachability/coverage diagnostics using actual selectable languages plus default fallback; verify unreachable es-ES/zh-CN catalogs warn without rejection/sibling matching, and report orphan and duplicate labels. Maps to upstream 4.11.

- [x] 3.5 Ship synchronized offline catalogs for all ten UI languages for Standard Playing Cards, Mahjong, and Dominoes; verify all authored labels, alternate backs, live switches, old-manifest fallback, explicit-null opt-out, and separately proven built-in decks without overwriting or reclassifying legacy/user files.

## 4. Preserve authored provenance across disk and network boundaries

- [x] 4.1 Establish versioned authored-deck sidecars only on authored install; test identical name/content user replacement, rename/save-as, external mutation, legacy files, package import, and standalone deck import. Maps to upstream 4.7.
- [ ] 4.2 Implement capability offers/acknowledgements and idempotent negotiated creation preserving owner-only checks and canonical creation side effects; test duplicate names, spoofed senders, retries, five-second timeout, unsupported/mismatched versions, no legacy double-send, and unlocalized games. Maps to upstream 4.8.
- [x] 4.3 Add authoritative revision/incarnation snapshots; test late join/reconnect, rename/despawn races, invalid references, and ownership changes. Session-sidecar implementation and matching/mismatched saved-document tests are conditional on a future session save/load feature and are not a current release gate. Maps to upstream 4.9.
- [ ] 4.4 Validate named-message dispatch and canonical replication using supported legacy and capable host/client builds; record the tested compatibility matrix and architectural review before release. Maps to upstream 4.8 and 5.2.

## 5. Document and verify the complete feature

- [ ] 5.1 Document author installation, provenance, cache/rulebook boundaries, old-editor behavior, and successful-reload rollback; verify examples and package relocation instructions against actual behavior. Maps to upstream 4.10.
- [ ] 5.2 Exercise original English/Spanish/Japanese games and complete/partial catalogs across supported desktop/mobile/WebGL layouts; record Unicode, long-label, multiline, startup, repeated-switch, and offline results against the inventory. Maps to upstream 5.1.
- [ ] 5.3 Verify mixed-language and mixed-capability deck loading, gameplay, and deck saving preserve canonical state and locale switches send no gameplay/provenance mutations. Maps to upstream 5.2.
- [ ] 5.4 Verify removal/null rollback, offline relocation, unselected-game refresh, unchanged legacy files/exports/assets, and the reviewed library release dependency before shipping; reconcile both task lists from test evidence. Maps to upstream 5.3.
