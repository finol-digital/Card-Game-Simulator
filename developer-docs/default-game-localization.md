# Default game localization

The three built-in games ship authored catalogs for English, Spanish, French,
German, Italian, Japanese, Korean, Brazilian Portuguese, Russian, and Simplified
Chinese. Names, decks, sets, property labels, enum values, Mahjong text ranks,
play-stack labels, and Standard's alternate card back resolve through the same
presentation API as third-party catalogs. Domino names are numeric and remain
language-neutral. Artwork and external rulebook websites are unchanged.

## Packaging and upgrades

`tools/localization/default_games.py` synchronizes inline catalogs in the public
website and StreamingAssets manifests with `Resources/DefaultGameContent`.
Resources also holds canonical deck payloads. No translation download or website
deployment is necessary to test a PR build, including WebGL installs that obtain
an older default manifest from the live website.

Only the exact shipped game names and canonical update URLs receive fallback
catalogs when localization metadata is absent. Explicit metadata (including null)
wins. Custom games keep the original opt-in and absent/null rollback semantics.

Fresh bundled decks include portable provenance sidecars. Existing user or
legacy deck files are never overwritten or classified by name/content matches.
If no proven authored deck exists, CGS installs a separate read-only original in
`localization/default-decks`, recorded as `defaults/<filename>` in the sidecar.
It appears translated in the deck list and is used by default startup loading.
Legacy/user entries may therefore coexist with translated built-in choices;
the former retain their saved names. Editing and saving an original makes a user
deck through the normal save path. Locale changes never reinstall the originals.

Explicit `localization: null` disables fallback and hides managed originals.
Ordinary deck files remain available. Re-enabling catalogs reuses valid bindings.
Sidecars and managed files travel with whole-game exports, not standalone deck
exports. Canonical card/deck data, IDs, enum flags, and saved user text are unchanged.

## Verification

- Unity `DefaultGameLocalizationTests` (38/38 passing): all three games × all ten locales,
  alternate-back cards, sets/properties/enums and Mahjong text ranks, canonical
  imports, live language-picker refresh, old-manifest staging, explicit-null
  opt-out, packaged sidecars, and preservation of identical legacy/user files.
- Existing `GameContentProvenanceTests` and `GameManifestLocalizationTests` (8/8 each) cover
  replacement/revocation, package relocation, and generic absent/null reloads.
- Python catalog tests check complete targets and synchronized generated files;
  the six generated manifests pass the library schema with URI checking enabled.
- Website/Worker tests and strict validation of both OpenSpec changes also pass.

Translations were authored by Codex in November 2026. Fluent human language review
and physical-device layout verification have not been claimed. CI player builds
and the user's PR-build validation remain the release checks. Copilot quota is
not being used as a release gate.
