## Purpose

Integrate authored translations and resources with CGS language selection while preserving canonical data, existing games, user text, and multiplayer compatibility.

## ADDED Requirements

### Requirement: Game presentation follows the UI language
CGS consumers SHALL use the current UI locale for all supported game-authored display targets. They SHALL refresh active and newly opened views after language changes and after relevant catalogs load, without restarting or reloading gameplay. Game-authored content SHALL use the game's fallback contract rather than the application catalog's English fallback.

#### Scenario: Switch languages with game UI open
- **GIVEN** complete Spanish translations for a loaded game
- **WHEN** the user switches the UI to Spanish with a card viewer, deck list, and filter controls open
- **THEN** game/card/deck/set names, rules text, property labels, enum options, empty labels, group/zone/board/back labels, and game metadata use the translated targets wherever rendered
- **AND** messages embedding those targets refresh while application-owned message templates use the UI translation catalog
- **AND** selections, unsaved deck edits, card positions, and gameplay state remain intact

#### Scenario: New views and original-language games
- **WHEN** the user opens another game-content view after changing language
- **THEN** it resolves the current locale immediately from available data
- **AND** an untranslated Spanish or Japanese game continues showing its original authored content without assuming English

### Requirement: Localized search and controls retain canonical meaning
Interactive text search for localized games SHALL match canonical and translated names/text. Gameplay queries and deck import matching SHALL remain canonical and locale-independent. Controls SHALL retain canonical keys/flags, including empty-value lookup keys, independently of display labels. Presentation sorting SHALL NOT change gameplay order. Unlocalized games SHALL retain existing search behavior.

#### Scenario: Search by translated and original name
- **GIVEN** canonical card `Flame` displayed as `Llama`
- **WHEN** a user searches for either name in Spanish
- **THEN** both searches can find the same canonical card
- **AND** cards sharing a translated name remain distinct by canonical ID

#### Scenario: Active enum filter survives a language switch
- **WHEN** a selected `0x1` enum option changes its visible label from `Fire` to `Fuego`
- **THEN** the selection remains `0x1` and the same cards satisfy the filter

#### Scenario: Translation collision cannot change gameplay queries
- **GIVEN** one card's translated name matches a different card's canonical name
- **WHEN** a deck is imported or an automatic card-play query executes under either language
- **THEN** it resolves the same canonical cards as before localization
- **AND** only interactive search considers translated text

#### Scenario: Empty filters and duplicate property labels
- **WHEN** an empty-enum label is translated and two distinct properties acquire identical translated labels
- **THEN** the empty filter retains its existing canonical key and bit, without allocating another bit
- **AND** both property controls remain independently selectable by canonical path without a display-label lookup collision

### Requirement: Catalog loading is resilient and isolated
External catalog caches SHALL be scoped to canonical game identity, locale, and source URI. Inline catalogs SHALL come from the current manifest without a separate persistent cache. Loading SHALL honor the update policy, retain usable external caches on refresh failure, and fall back when unavailable. Language changes SHALL NOT redownload canonical datasets or allow stale results to override the current game/locale/manifest generation.

#### Scenario: Offline and failed catalog refresh
- **WHEN** a user selects a language while offline
- **THEN** bundled/cached catalogs are used if available and missing entries follow the normal fallback chain
- **AND** a failed or malformed refresh does not replace a usable cached catalog or block gameplay

#### Scenario: Automatic downloads are disabled
- **WHEN** a game has `autoUpdate < 0` and the user switches languages
- **THEN** available inline/local/cached catalogs are used without automatic network requests
- **AND** unavailable translations fall back to base content

#### Scenario: Rapid selection and game changes
- **WHEN** a user switches Spanish to Japanese or changes games before the Spanish request completes
- **THEN** its completion cannot replace the current presentation
- **AND** changing a manifest's source prevents unrelated old-source cache data from being reused

### Requirement: Cache publication preserves newer successful results
For each external catalog/image cache key, successful results SHALL be ordered by request initiation. Once a later request publishes usable data, an earlier completion SHALL NOT replace any part of that persisted entry. Publication SHALL atomically check ordering and install data/metadata. Failed requests SHALL retain last-good data. Ordering SHALL survive locale changes and same-source manifest reloads, and replaced cache stores SHALL reject old in-flight writes.

#### Scenario: Older response arrives after a newer refresh
- **GIVEN** request A starts before request B for the same game, locale, and source key, across a language switch or same-URI manifest reload
- **WHEN** B publishes usable content and A completes afterward
- **THEN** A changes neither the persisted payload nor its origin, mappings, validation metadata, or freshness timestamps
- **AND** subsequent offline selection, restart, and package export retain B's content for both catalogs and localized images

#### Scenario: Failed refresh preserves usable content
- **WHEN** a newer request fails, is cancelled, or returns malformed content
- **THEN** it neither erases usable cached content nor advances the successful-publication order
- **AND** an older valid completion can publish if no later success has been published, without bypassing the separate display-generation guard

#### Scenario: Cache replacement and independent keys
- **WHEN** a cache is replaced or imported while a request against its previous store is pending
- **THEN** that request cannot write into the replacement store
- **AND** concurrent requests for different locale/source/asset keys do not suppress one another's valid cache publications

### Requirement: Manifest replacement invalidates presentation bindings
Successful manifest reloads SHALL replace localization and advance binding generation, even at the same URL. Inline changes SHALL appear immediately. Explicit null SHALL disable bindings; missing metadata SHALL disable them for custom games and select shipped catalogs for recognized defaults. Stale bindings SHALL NOT survive successful reloads. Failed loads SHALL preserve the last installed state.

#### Scenario: Inline update and feature removal without restart
- **WHEN** a game reloads a manifest whose inline Spanish name changed at the same source URL
- **THEN** the new name replaces the old binding and earlier callbacks cannot restore it
- **WHEN** the next successful reload removes localization from a custom game, or explicitly nulls it for any game
- **THEN** every game-content binding returns to canonical presentation without a restart, including bindings in already open views
- **WHEN** a recognized default game reloads without localization metadata
- **THEN** the bundled catalog replaces the previous manifest bindings without a restart

### Requirement: Offline caches survive game export and import
Downloaded catalogs, images, recorded origins, and local cache mappings SHALL reside within the game directory and travel with game-package exports. Consumers SHALL resolve imported cache mappings relative to the new installation while preserving remote origin semantics. Missing caches or unsupported metadata SHALL fall back to canonical content without preventing import or play.

#### Scenario: Move a localized game offline
- **WHEN** a game with cached Spanish content is exported as a game package and imported into another directory offline
- **THEN** cached translations/artwork remain usable and remote relative references keep their recorded origins
- **AND** no cache path depends on the old machine's absolute installation path

### Requirement: Unselected installed games refresh without downloads
Visible names, banners, and other metadata for unselected installed games SHALL resolve the UI locale from inline/bundled/cached content and refresh on language changes. Viewing the game list SHALL NOT initiate localization downloads for those games. Unavailable translations SHALL use the game's ordinary fallback.

#### Scenario: Language switch in the game chooser
- **WHEN** multiple installed games appear before one is selected and the user changes language
- **THEN** their available translated metadata refreshes without selecting them or making catalog/image requests for them

### Requirement: Localized assets preserve availability
Consumers SHALL use localized artwork when supplied and available, with locale-chain fallback followed by the original asset. They SHALL isolate localized caches from canonical assets, preserve usable artwork during loading, and keep IDs, file identities, and geometry unchanged. The selected rulebook reference SHALL follow the same locale selection contract without rewriting canonical metadata.

#### Scenario: Missing regional artwork
- **WHEN** an `es-MX` card image fails to load but an `es` image is available
- **THEN** the Spanish image can be displayed
- **AND** if all localized variants fail, the existing card image is used
- **AND** no failed variant overwrites the canonical asset cache

### Requirement: Presentation is local to each participant
Locale changes SHALL NOT alter canonical identities, gameplay messages/state, existing deck-file contents, or provenance metadata. Each participant SHALL resolve available canonical label references using their own locale. Optional provenance sidecars and negotiated presentation messages SHALL carry canonical references, never translations. User-authored names and chat SHALL remain as entered.

#### Scenario: Two languages in one session
- **GIVEN** a capable host and participants with a verified authored-deck reference
- **WHEN** Spanish and Japanese players inspect the same shared card, deck, and play zone
- **THEN** each sees their own available translations while both reference the same canonical entities
- **AND** switching either player's language does not change the other player's display or emit gameplay mutations

#### Scenario: User renames an authored deck
- **WHEN** a user replaces an authored deck name with their own text and changes language
- **THEN** their chosen text remains visible and saved as entered
- **AND** a user-created deck with a matching authored name is not automatically treated as that authored deck

### Requirement: Authored deck bindings require explicit provenance
Consumers SHALL establish authored-deck bindings only through verified authored installation or valid provenance metadata, never matching names/cards alone. Provenance SHALL be separate from existing deck-file formats and tied to artifact identity/generation. User creation, replacement, rename, and save-as SHALL omit or revoke bindings. Legacy/unproven files SHALL keep saved names; mismatched metadata SHALL be ignored.

#### Scenario: User replaces an authored file with identical content
- **WHEN** CGS creates a user deck over an authored deck's filename, even with identical cards and name
- **THEN** it revokes the prior binding before writing and the replacement keeps the user's name in every locale
- **AND** an external file change invalidating the recorded fingerprint also invalidates the binding

#### Scenario: Existing installation and package import
- **WHEN** a legacy deck file has no provenance but its name matches a catalog target
- **THEN** the consumer does not infer authorship; a fresh authored install can establish a binding
- **WHEN** a game package carries valid provenance sidecars for its authored decks
- **THEN** those bindings survive import, while importing a deck file alone does not infer them

### Requirement: Shared stack labels use optional versioned provenance
Consumers SHALL negotiate an optional versioned presentation capability separately from existing gameplay contracts. A capable authoritative host SHALL relay canonical game/stack identities, label kind/key, and revision only to supporting peers. Labels SHALL distinguish authored decks, game-defined deck/group labels, and user text. Unknown, invalid, stale, or unavailable provenance SHALL preserve the canonical stack name.

#### Scenario: Capable and legacy peers share one session
- **GIVEN** peers that can already connect using CGS's existing compatibility rules
- **WHEN** the host/sender and some recipients support the provenance capability and others do not
- **THEN** capable recipients translate verified references locally and legacy recipients ignore the optional capability offer, receive no provenance payload, and retain unchanged gameplay replication and canonical stack names
- **AND** no required field or baseline protocol-version change is introduced solely for localization
- **WHEN** the host or sender lacks support, or negotiation times out
- **THEN** all recipients preserve canonical shared-stack names without guessing provenance

#### Scenario: Creation provenance binds to the exact stack instance
- **WHEN** a negotiated creation request supplies canonical creation arguments and an authored label reference
- **THEN** the host applies existing creation/ownership rules once and binds the reference to the actual resulting stack, even if another stack has identical names/cards
- **AND** retries reuse a session-scoped request ID and cannot create duplicates
- **AND** the client does not also submit or retry that request through the legacy creation path

#### Scenario: Rename, despawn, and late join
- **WHEN** a user renames a bound stack or the stack is destroyed and its network identity is reused
- **THEN** its binding is cleared and stale metadata cannot restore the old label
- **WHEN** a supporting participant joins late or reconnects
- **THEN** it receives current authoritative references/revisions for the live stack incarnations
- **AND** changing only a participant's locale sends no gameplay or provenance update

#### Scenario: Future session persistence has no matching provenance
- **GIVEN** a consumer implements a session save/load feature (CGS currently does not, and this change does not add one)
- **WHEN** a session is restored from a legacy save or a save with a mismatched provenance sidecar
- **THEN** saved canonical names are preserved without inferred bindings
- **AND** a matching optional sidecar can restore verified label references without altering the session format
- **AND** session-sidecar implementation and verification apply only when that persistence feature exists; deck and live multiplayer provenance remain required independently

#### Scenario: Unlocalized games retain their network behavior
- **WHEN** a game without localization is played between capable peers
- **THEN** no authored-label provenance extension is emitted and existing gameplay/presentation behavior remains unchanged

### Requirement: Translated content remains readable
Consumers SHALL render game text through supported locale font/layout handling, preserving authored Unicode and line breaks under the normal UI text rules. Long translated labels and non-Latin text SHALL remain accessible in supported views and controls.

#### Scenario: Non-Latin and multiline content
- **WHEN** supported Japanese content and longer Spanish labels are shown in card viewers, deck lists, filters, and game dialogs
- **THEN** glyphs and authored line breaks render correctly and controls keep their normal wrapping, scrolling, or other access to the complete text

### Requirement: Default games ship complete offline language catalogs
CGS SHALL ship authored presentation catalogs for Standard Playing Cards, Mahjong, and Dominoes in all ten selectable languages: `en`, `es`, `fr`, `de`, `it`, `ja`, `ko`, `pt-BR`, `ru`, and `zh-Hans`. Catalogs SHALL cover game, card, deck, set, property, enum, and applicable card-back labels without changing canonical identifiers, card/deck bytes, or artwork. Numeric domino names MAY remain identical across locales.

#### Scenario: Default content follows the language picker offline
- **WHEN** a player selects any supported UI language with a default game installed
- **THEN** built-in card and authored-deck labels SHALL use that language, including generated alternate-back cards
- **AND** catalogs SHALL be available from the build without requiring a website deployment or translation download
- **AND** controls SHALL retain canonical keys while visible labels refresh

#### Scenario: An existing default installation predates localization metadata
- **WHEN** a definition with the exact shipped name and canonical update URL lacks a localization member
- **THEN** CGS SHALL supply the corresponding bundled catalog
- **AND** explicitly authored localization, including null, SHALL take precedence
- **AND** other games without metadata SHALL retain canonical behavior

#### Scenario: Legacy deck files are not evidence of authorship
- **WHEN** a default game has existing deck files without valid provenance, even with identical names and bytes
- **THEN** CGS SHALL preserve those files and their unproven status
- **AND** separately install bundled authored decks with provenance so translated built-in choices remain available
- **AND** fresh packaged authored files with valid sidecars SHALL not produce duplicate built-in choices

#### Scenario: An explicit default-game opt-out survives an editor save
- **WHEN** a definition containing `localization: null` is loaded, edited, saved, and reloaded
- **THEN** the explicit null SHALL remain present and bundled catalog/deck fallback SHALL remain disabled
- **AND** definitions that never contained localization SHALL continue to omit it when saved
- **AND** assigning the model's localization property to null SHALL retain its existing member-clearing behavior; callers requesting an explicit opt-out SHALL also retain or set the presence flag
