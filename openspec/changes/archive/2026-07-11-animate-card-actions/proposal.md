# Animate Card Actions (Tap / Rotate)

## Why

When a player taps or rotates a card via the card action panel, the card's rotation changes instantly, snapping to the new angle with no visual transition. This feels abrupt and is easy to miss, especially in multiplayer where another player taps a card. Following the recently added card flip animation, a brief rotation tween makes tap/rotate state changes obvious and gives the game a more polished, tabletop-like feel.

## What Changes

- When a card's rotation changes by a discrete action — Tap or Rotate from the card action panel, their keyboard/gamepad input bindings, or any other code path that sets a card's rotation to a new angle — the card smoothly animates to the new rotation instead of snapping.
- Card stacks rotated via the playable viewer's Rotate action animate the same way; the animation lives on the shared playable base class, so all playables (cards, stacks, dice, counters) rotate smoothly on discrete changes.
- The animation plays for all players in multiplayer: it is driven by the existing rotation change notification, so remote clients see the same rotation tween.
- Rotation changes that are not user-visible transitions (initial spawn, cards created with a default rotation, joining a game in progress) do not animate — the card simply appears at its correct rotation.
- Continuous two-finger drag rotation keeps its current direct, unanimated behavior for the player performing the drag; the card must remain responsive under the player's fingers.
- Rapid repeated taps/rotates interrupt any in-progress animation and settle on the final rotation.
- The logical rotation state and its network synchronization update immediately, as today; only the visual transform is animated.

## Capabilities

### New Capabilities

- `card-rotation-animation`: Visual rotation transition when a card or card stack's rotation changes (tap, untap, rotate), including interruption handling, no-animation cases (spawn/initial state/local drag), and presentation-only guarantees.

### Modified Capabilities

<!-- None: no existing spec's requirements change. Rotation state sync behavior is unchanged; only its visual presentation is enhanced. Tap/Rotate action semantics in CardActionPanel are unchanged. -->

## Impact

- `Assets/Scripts/Cgs/CardGameView/Multiplayer/CgsNetPlayable.cs`: the `Rotation` setter and `OnChangeRotation` currently write `transform.localRotation` instantly; the transform write goes through an `ApplyRotation` hook that tweens the rotation with PrimeTween, mirroring the existing flip animation pattern (duration constant, stored tween handle, interruption handling, `OnDestroy` cleanup). All playables inherit it.
- `Assets/Scripts/Cgs/CardGameView/Multiplayer/CardModel.cs`: the game-default rotation applied at `Start` is pre-written to the transform so it never animates.
- `Assets/Scripts/Cgs/CardGameView/Viewer/CardActionPanel.cs` and `PlayableViewer.cs`: no changes — Tap, Rotate, and Rotate Stack already funnel through the `Rotation` property.
- No new dependencies: PrimeTween is already used for the card flip animation and main menu carousel.
- No network protocol changes: `_rotationNetworkVariable` and its RPC are untouched.
