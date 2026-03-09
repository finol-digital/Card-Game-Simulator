## Context

The multiplayer layer uses `CgsNetPlayable` ownership to gate who can move or act on playables. Today, ownership can be requested for shared decks, and `IsDeckShared` appears to be tracked at the player level. The change needs deterministic ownership for new playables while preserving the shared-deck exception.

## Goals / Non-Goals

**Goals:**
- Ensure the creating `CgsNetPlayer` is always the owner of newly spawned `CgsNetPlayable` instances.
- Prevent non-owners from moving, rotating, or interacting with non-shared playables.
- Preserve shared deck behavior for `CardStack` objects created from decks with `IsDeckShared` enabled.
- Scope sharing to the `CardStack` itself so other stacks remain private.

**Non-Goals:**
- Changing the UI/UX flow for requesting ownership beyond necessary gating.
- Redesigning deck creation or shuffling logic unrelated to ownership.
- Introducing new networking frameworks or ownership models.

## Decisions

- **Assign ownership at spawn time for all `CgsNetPlayable`**: On server-spawn, set the owner to the spawning player and enforce interaction checks client-side and server-side.
  - *Alternatives considered*: Leaving ownership unset and relying on later requests (rejected: allows a window where other clients can interact).

- **Move `IsDeckShared` to `CardStack` (if currently on `CgsNetPlayer`)**: Sharing becomes a property of the stack, not the player.
  - *Alternatives considered*: Keep player-level sharing and infer stack sharing at creation time (rejected: unclear for future stacks and complicates ownership rules).

- **Allow ownership requests only for shared stacks**: If `CardStack.IsDeckShared` is true, retain the existing request-ownership flow; otherwise deny ownership requests.
  - *Alternatives considered*: Allow requests for any playable with server validation (rejected: conflicts with deterministic ownership goal).

## Risks / Trade-offs

- **[Legacy flows assume player-level `IsDeckShared`]** → **Mitigation**: Migrate assignment when stacks are created; add default values to preserve behavior.
- **[Client-side interaction checks miss a path]** → **Mitigation**: Enforce ownership on server-side action handlers as well.
- **[Ownership request denied for non-shared stacks breaks expectations]** → **Mitigation**: Audit request paths and provide clear error/logging.
