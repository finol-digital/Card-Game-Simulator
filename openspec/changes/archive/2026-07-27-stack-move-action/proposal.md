## Why

A card in play can be sent to any other container through the Move menu, but a card sitting on top of a stack cannot — the only way to get it somewhere else is to drag it there, which is awkward on touch screens and impossible when the destination is off-screen or is another player's stack. The stack action menu also spends one of its five slots on Save, an infrequent, whole-stack operation that has nothing to do with manipulating the stack in play.

## What Changes

- Add a **Move** action to the card stack action menu, alongside View, Rotate, Shuffle, and Flip. It takes the top card off the stack and opens the same Move menu used by card models, so the top card can be sent to the play area, a hand, a card zone, or another stack.
- The Move action respects the stack's face state: a stack whose top face is down yields a facedown card, and a face-up stack yields a face-up card.
- The stack itself is excluded from the Move menu's destination list, and the action is unavailable for an empty stack.
- Bind the existing `Card/Move` input action to the stack Move action, matching how `Card/Rotate` and `Card/Flip` already drive the stack Rotate and Flip actions.
- **Move the Save button out of the stack action menu** and into the playable viewer's top bar, immediately next to the Delete button. Save is shown there only while a card stack is selected.
- Because the top bar is not governed by the "Show Actions Menu" play setting, Save becomes reachable even when the actions menu is hidden — Delete already behaves this way.

## Capabilities

### New Capabilities
- `stack-actions`: The set of actions offered for a selected card stack in the playable viewer — View, Rotate, Shuffle, Flip, and the new Move — plus where the Save action lives and when it is available.

### Modified Capabilities

*(none — no existing spec in `openspec/specs/` covers the stack action menu)*

## Impact

- `Assets/Scripts/Cgs/CardGameView/Viewer/PlayableViewer.cs` — new `MoveStackTopCard` action, `Card/Move` input handling, Save button visibility tied to the selected playable being a card stack.
- `Assets/Scripts/Cgs/Play/MoveMenu.cs` — new entry point for moving a card stack's top card, including source-container exclusion and removal of the moved card from the stack.
- `Assets/Scripts/Cgs/Play/PlayController.cs` — overload of `ShowMoveMenu` for a card stack.
- `Assets/Prefabs/CardGameView/Viewer/Playable Viewer.prefab` — add a Move button to the Stack Action Panel; reparent the Save button to the View bar next to Delete.
- No changes to networking contracts: the move reuses `CardStack.RequestRemoveAt` and the existing `ICardContainer.AddCard` paths.
