# Card Flip Animation

## Why

When a card is flipped between faceup and facedown, its image instantly swaps between the card face and the card back with no visual transition. This feels abrupt and can be easy to miss, especially in multiplayer where another player flips a card. A brief flip animation makes the state change obvious and gives the game a more polished, tabletop-like feel.

## What Changes

- When a card in the play area changes from facedown to faceup, or from faceup to facedown, it plays a short flip animation (horizontal shrink-swap-expand) instead of instantly swapping the sprite.
- Double-faced cards (where the "back" is another card face, e.g. transform cards) also play the flip animation when flipping between their two faces, even though this changes the card's Id rather than its facedown state.
- The face/back sprite swap happens at the midpoint of the animation, so the correct side is always shown while the card "turns".
- The animation plays for all players in multiplayer: it is driven by the existing `IsFacedown` change notification, so remote clients see the same flip.
- Flips that are not user-visible state changes (initial spawn, cards created already facedown, joining a game in progress) do not animate — the card simply appears in its correct state.
- Rapid repeated flips interrupt any in-progress animation and settle on the final state.

## Capabilities

### New Capabilities

- `card-flip-animation`: Visual flip transition when a card's facedown state changes or a double-faced card transforms to its other face, including midpoint sprite swap, interruption handling, and no-animation cases (spawn/initial state).

### Modified Capabilities

<!-- None: no existing spec's requirements change. Facedown state sync behavior is unchanged; only its visual presentation is enhanced. -->

## Impact

- `Assets/Scripts/Cgs/CardGameView/Multiplayer/CardModel.cs`: `OnChangeIsFacedown` currently swaps the displayed sprite immediately via `RegisterDisplay`/`UnregisterDisplay`; it will trigger the flip animation instead. `OnChangeId` similarly swaps the display instantly when a double-faced card transforms (via `CardActionPanel.Flip` setting `Value` to the other face) and will animate that transition too.
- No new dependencies: the project already includes the PrimeTween library (used for the main menu carousel animation in `MainMenu.cs`), which will drive the flip tween.
- No network protocol changes: `_isFacedownNetworkVariable` and its RPC are untouched.
