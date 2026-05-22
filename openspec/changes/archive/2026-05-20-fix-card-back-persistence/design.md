## Context

Card Editor currently allows selecting an individual card back for a card. That value is retained in-memory and appears in serialized `AllCards.json`, but reload behavior diverges because load logic consumes `backs` while serialization for editor-created cards can rely on `backFaceId`. Runtime viewers (`CardModel`, `CardStack`) resolve display from `UnityCard.BackFaceId`, so any load-time drop of that value causes fallback to default card back.

This change touches card persistence in `UnityCardGame` and card creation flows in editor/import paths. The implementation must preserve compatibility with existing user content while converging output on documented schema semantics (`backs`).

## Goals / Non-Goals

**Goals:**
- Preserve per-card back assignments across save and reload.
- Support legacy persisted representations (`backFaceId`) during load.
- Normalize newly written card data to schema-aligned `backs` semantics.
- Avoid regressions in runtime back sprite resolution.

**Non-Goals:**
- Redesigning overall card JSON schema beyond back representation.
- Migrating all historical files eagerly on disk.
- Changing multiplayer synchronization or viewer rendering architecture.

## Decisions

1. Dual-read strategy during deserialization
- Decision: Load logic will treat `backs` as primary, but if absent/empty it will read `backFaceId` as compatibility fallback.
- Rationale: Restores behavior for existing editor-generated files without forcing manual migration.
- Alternative considered: Hard-require `backs` only and provide migration script. Rejected due to poor UX and likely user data breakage.

2. Canonical-write strategy during serialization
- Decision: `WriteAllCardsJson` output for cards with per-card back selection will include schema-aligned `backs` representation.
- Rationale: Aligns generated files with published schema and load parser expectations.
- Alternative considered: Keep writing `backFaceId` only and rely on fallback indefinitely. Rejected because it perpetuates non-canonical data shape.

3. Backward compatibility over strict normalization
- Decision: Loader will continue accepting both representations to avoid brittle behavior in mixed datasets.
- Rationale: Users may have existing files from prior versions and imported content with different shapes.
- Alternative considered: One-time migration and strict parser. Rejected because migration timing/rollback can be fragile for local files.

## Risks / Trade-offs

- [Duplicate or conflicting back fields] -> Define precedence (`backs` first, `backFaceId` fallback) and document it.
- [Writer emits unexpected extra variants] -> Constrain write behavior to represent a single selected back plus default semantics only as needed.
- [Regression in cards without any custom back] -> Ensure empty/absent back data still maps to default global card back.
- [Test coverage gaps around persistence round-trip] -> Add/extend play mode tests for save->reload->display flow.

## Migration Plan

- No mandatory file migration required.
- New saves write canonical `backs` representation.
- Existing files continue to load via fallback.
- Rollback strategy: if needed, revert writer normalization while keeping loader fallback to preserve compatibility.

## Open Questions

- Should canonical write include empty string in `backs` (for explicit default option) or only explicit back id when selected?
- Should both `backs` and `backFaceId` be emitted for one release for transition transparency, or only canonical `backs`?
