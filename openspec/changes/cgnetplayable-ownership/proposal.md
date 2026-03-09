## Why

When a player creates a networked playable, ownership should be deterministic so other players cannot manipulate it. This prevents unintended actions and aligns with player expectations for private objects, while preserving shared deck behavior.

## What Changes

- Newly created `CgsNetPlayable` instances are owned by the `CgsNetPlayer` that created them by default.
- Non-owners cannot move, rotate, or perform other actions on playables they did not create.
- CardStacks created from shared decks (`IsDeckShared` true) preserve current behavior: players can request ownership to act on them.
- `IsDeckShared` may move from `CgsNetPlayer` to `CardStack` to scope sharing to the stack itself.

## Capabilities

### New Capabilities
- `netplayable-ownership`: Enforce creator ownership for networked playables, with shared-deck exceptions.

### Modified Capabilities
- (none)

## Impact

- Multiplayer ownership rules for `CgsNetPlayable`, `CardStack`, and related action flows.
- Potential data model change for `IsDeckShared` (player-level to stack-level).
- Network ownership request logic and UI/interaction permissions.
