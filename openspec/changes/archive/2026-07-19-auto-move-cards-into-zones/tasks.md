# Tasks: Auto-Move Cards Into Overlapping Card Zones

## 1. CardModel placeholder flow

- [x] 1.1 Add a public method on `CardModel` (implemented as `MoveToOverlappingCardZone()`, which encapsulates guards, zone lookup, placeholder creation, and setting `IsMovingToPlaceHolder = true`) so code outside `CardModel` can start the move-to-placeholder animation
- [x] 1.2 Verify `FinishMovingToPlaceHolder` reparents the card into the zone and fires the zone's `OnAddCardActions` when the placeholder's parent zone is a Horizontal/Vertical zone (confirmed: reparents to `PlaceHolder.parent`, fires previous zone `OnRemove` and new zone `OnAdd` → face preference, default action, `MoveCardToServer`)

## 2. Zone overlap capture in CardZone.OnAdd

- [x] 2.1 Add a helper on `PlayController` (`FindCardZoneAt(worldPosition)`) that returns the first Horizontal/Vertical card zone in `AllCardZones` (excluding `playAreaCardZone`) whose local rect contains the given world position
- [x] 2.2 Guard capture (in `CardModel.MoveToOverlappingCardZone()`) when the card `IsStatic`, `ToDelete`, already moving, already has a `PlaceHolderCardZone`, is not parented to the play area, or is a spawned card not owned locally; invoked from `CardZone.OnAdd` (Area zones, before `SnapToGrid()`), `PlayController.AddCardToPlayArea`, and `CardModel.OnStartPlayable` (covers network-spawned cards)
- [x] 2.3 On overlap: set `PlaceHolderCardZone` to the overlapping zone, call `UpdateLayout(PlaceHolder, transform.position)` to place the placeholder at the correct sibling index, set `IsMovingToPlaceHolder = true`, and return early so `SnapToGrid()` and the play area's add actions are skipped for the captured card
- [x] 2.4 Ensure no-overlap behavior is unchanged: capture method returns false and the pre-existing code paths (snap to grid, play area add actions) run exactly as before

## 3. Verification

- [x] 3.1 Solo: play a card from a deck onto a horizontal zone and onto a vertical zone; confirm the card animates to a placeholder and lands inside the zone with the zone's face preference and default action applied
- [x] 3.2 Solo: add a card from card search while a zone is at the drop position; confirm capture; add a card away from any zone; confirm it snaps to grid as before
- [x] 3.3 Multiplayer (host + client): play a card onto a zone and confirm both players see the card inside the zone
- [x] 3.4 Drag a card into a zone (existing flow) and confirm drag-and-drop capture still behaves exactly as before
- [x] 3.5 Run existing play mode/edit mode tests (if any cover play area or card zones) and confirm they pass (PlayMode: 18/18 passed; EditMode: 2/2 passed; Unity 6000.3.15f1 batch mode)
