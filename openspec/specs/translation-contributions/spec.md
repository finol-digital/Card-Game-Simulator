# translation-contributions Specification

## Purpose
Define editor-free translation contributions, review provenance, catalog validation, deterministic Unity imports, and completeness gates for shipped languages.

## Requirements

### Requirement: Contributors can edit translations without Unity

The repository SHALL provide UTF-8 per-locale catalogs under `translations/`, with stable semantic keys, English source text, translator context, and argument descriptions. A translation-only contribution SHALL be possible through a text editor or GitHub without editing Unity assets or installing Unity. The repository README SHALL link to `developer-docs/translating.md`, documenting correction and new-locale workflows.

#### Scenario: Correct a translation through GitHub
- **WHEN** a contributor follows the guide to correct an existing Spanish phrase
- **THEN** they can locate its key, understand its context and arguments, edit the locale catalog, and submit a PR
- **AND** the guide explains the validation command and maintainer-owned Unity import step

### Requirement: Ship complete machine-generated catalogs with review provenance

The initial release SHALL include complete catalogs for `en`, `es`, `zh-Hans`, `pt-BR`, `ko`, `fr`, `de`, `ja`, `it`, and `ru`. Non-English machine-generated entries SHALL be marked as such, with source hashes and documented generation method/date. Entries SHALL distinguish machine drafts, reviewed wording, and wording needing review. Human review SHALL NOT be falsely claimed by generation or import.

#### Scenario: Inspect a seeded translation
- **WHEN** a contributor inspects an initially generated Korean entry
- **THEN** its text, machine status, source hash, and English context are available
- **AND** documentation makes its generated origin clear

### Requirement: Preserve contributed wording during updates

Automatic generation SHALL fill missing entries only and SHALL NOT overwrite any existing non-empty translation. English source changes SHALL retain translated wording and expose affected entries as needing review until explicitly reconciled. Importing or regenerating assets SHALL preserve translation metadata.

#### Scenario: Regenerate after a community correction
- **WHEN** a contributor corrects a machine translation and translation generation runs again
- **THEN** the correction remains unchanged even if its status has not been changed to reviewed

#### Scenario: English source changes
- **WHEN** a key's English text, context, or argument contract changes
- **THEN** its existing translations are retained and stale source hashes are reported
- **AND** stale wording must be explicitly marked for review or reconciled instead of silently being represented as current

### Requirement: Validate catalogs before import and release

An editor-free command SHALL detect duplicate or unknown keys, missing/empty entries, unknown locale codes, malformed metadata, placeholder/tag mismatches, and stale source hashes. It SHALL report coverage and review status per locale. Structural errors and unexplained stale entries SHALL fail validation; explicitly marked stale entries SHALL produce review warnings. Unity-side validation SHALL also check Smart String syntax/rendering, catalog/table consistency, and shipped locale/font configuration.

#### Scenario: Broken placeholder in a contribution
- **WHEN** a translated message removes or renames a required argument
- **THEN** validation fails with the locale and key identified before Unity table mutation

#### Scenario: Incomplete or empty catalog set
- **WHEN** a shipped locale has a missing/empty value or validation finds no entries
- **THEN** validation fails rather than reporting full coverage

#### Scenario: Report review debt without hiding it
- **WHEN** a structurally valid stale translation is explicitly marked `needs-review`
- **THEN** validation lists it in review warnings and coverage output
- **AND** its old source hash and wording remain intact until reconciled

### Requirement: Generate Unity tables deterministically from catalogs

The catalogs SHALL be the source of truth for committed Unity String Tables. The importer SHALL validate the complete input set before mutation, preserve asset GUIDs and shared entry IDs for existing keys, and produce no asset changes when rerun with identical input. Key retirement SHALL require an explicit migration instead of silently breaking bindings. A consistency check SHALL reject builds with stale generated tables and explain how to reimport them.

#### Scenario: Repeat a successful import
- **WHEN** a maintainer imports unchanged catalogs a second time
- **THEN** no serialized asset diff is produced and all scene/prefab references remain valid

#### Scenario: Stale generated assets
- **WHEN** catalog text changes but its committed Unity table has not been regenerated
- **THEN** consistency validation fails with an actionable import instruction
- **AND** a release build cannot silently ship the old wording

### Requirement: Add languages through a documented completeness gate

Adding a shipped language SHALL require a manifest entry, complete catalog, locale asset, bundled font coverage, import/validation, and layout checks. Contributor documentation SHALL describe these steps and the maintainer responsibilities. An untranslated locale asset alone SHALL NOT enable a language for players.

#### Scenario: Submit a new locale
- **WHEN** a contributor proposes a language with incomplete text or unverified glyph coverage
- **THEN** it remains outside the shipped manifest and Settings dropdown until its completeness and rendering checks pass
