# Auto-Move Cards Into Overlapping Card Zones

## Why

While playing a game, a card can end up sitting on top of a card zone in the play area without actually being in that zone. Card zones only capture cards during a drag (via pointer-enter placeholders), so cards added to the play area programmatically — played from a deck, added from card search, or spawned over the network — land at a position that may overlap a zone but are never moved into it. This breaks the expectation that a zone contains whatever cards visibly rest on it.

## What Changes

- When a card is added to the play area card zone and its position overlaps a child card zone (horizontal or vertical zone), the card is automatically moved into that zone instead of remaining loose in the play area.
- The move uses the existing placeholder mechanism: a placeholder is created in the overlapping zone at the appropriate layout position, and the card animates to the placeholder, then reparents into the zone (same flow as drag-and-drop capture).
- Cards that do not overlap any child card zone keep the current behavior (snap to grid in the play area).
- Applies in both solo and multiplayer play, for all code paths that add cards to the play area.

## Capabilities

### New Capabilities

- `card-zone-capture`: Automatic capture of cards into card zones when a card added to the play area overlaps a zone, including placeholder creation, movement animation, and zone add behaviors (face preference, default action).

### Modified Capabilities

<!-- None: existing specs (card-back-persistence, card-flip-animation, card-rotation-animation, netplayable-ownership, schema-validation-test-mode, seat-based-deck-placement) have no requirement changes. -->

## Impact

- `Assets/Scripts/Cgs/CardGameView/Multiplayer/CardZone.cs` — `OnAdd` for the play area zone gains an overlap check against child card zones.
- `Assets/Scripts/Cgs/CardGameView/Multiplayer/CardModel.cs` — placeholder / move-to-placeholder flow reused for programmatic adds.
- `Assets/Scripts/Cgs/Play/PlayController.cs` — `AddCardToPlayArea` and related card-creation paths.
- Multiplayer: zone membership already syncs via existing `MoveCardToServer` on zone add; no protocol changes expected.
