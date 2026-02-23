## Why

Players can currently interact with any card or stack, which undermines deck ownership and breaks expected multiplayer control boundaries. We need explicit deck access rules now to support fair play and enable future per-player workflows.

## What Changes

- Introduce a deck access permission model that limits card/stack interactions to decks a player can access.
- Define access rules: shared decks from the card game config are accessible to all players; individual decks are accessible only to the player who loaded them.
- Block unauthorized interactions consistently across gameplay actions (card moves, stack edits, deck actions).

## Capabilities

### New Capabilities
- `deck-access-permissions`: Defines deck access rules and how player interactions are authorized for cards and stacks tied to decks.

### Modified Capabilities
-

## Impact

- Gameplay interaction logic for cards, stacks, and deck actions.
- Multiplayer authority checks and any interaction validation paths.
- UI messaging or feedback when an action is denied.
