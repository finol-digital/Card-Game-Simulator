## Context

Players can interact with any card or stack today, regardless of which deck it originated from. We need to enforce deck ownership rules consistently across all interaction entry points, in a multiplayer environment that already has authority validation paths.

## Goals / Non-Goals

**Goals:**
- Centralize deck access checks so card, stack, and deck actions share one authorization path.
- Enforce access based on deck origin: shared config decks are public; individually loaded decks are owner-only.
- Provide a clear denial outcome for unauthorized actions.

**Non-Goals:**
- Redesign deck loading or persistence mechanisms.
- Add per-card exceptions or granular permission roles beyond owner/shared.

## Decisions

- Add a deck access permission layer that evaluates a player against a deck source descriptor.
  - Rationale: keeps rules explicit and testable, and allows reuse across multiple interaction systems.
  - Alternatives: embed checks separately in each interaction handler, which would risk drift and inconsistency.
- Represent deck ownership as metadata on the deck (and cards/stacks derived from it) rather than inferring from runtime scene context.
  - Rationale: deck origin is stable and can travel with cards/stacks through moves and merges.
  - Alternatives: infer ownership by tracking last interacting player, which is ambiguous for shared decks and breaks on reload.
- Deny unauthorized actions early in interaction pipelines and surface a consistent feedback path.
  - Rationale: avoids partial state changes and reduces multiplayer desync risk.
  - Alternatives: allow action then reconcile on server, which complicates rollback and UI.

## Risks / Trade-offs

- Authorization checks in multiple pathways may be missed initially → Mitigation: inventory all interaction entry points and funnel through a shared validator.
- Existing gameplay behaviors may change for groups relying on free-for-all access → Mitigation: document the new rules and ensure shared decks remain accessible to all players.
- Deck metadata propagation bugs could misclassify cards/stacks → Mitigation: add validation at deck load time and when creating stacks from mixed sources.
