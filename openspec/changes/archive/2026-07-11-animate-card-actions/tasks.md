# Animate Card Actions (Tap / Rotate) — Tasks

## 1. Rotation hook in CgsNetPlayable

- [x] 1.1 Add `protected virtual void ApplyRotation(Quaternion newRotation)` to `CgsNetPlayable.cs` with the current instant behavior (`transform.localRotation = newRotation`)
- [x] 1.2 Change the `Rotation` property setter to call `ApplyRotation(value)` instead of writing `transform.localRotation` directly, keeping the network-commit logic unchanged
- [x] 1.3 Change `OnChangeRotation` to call `ApplyRotation(newValue)` instead of writing `transform.localRotation` directly
- [x] 1.4 Confirm `ApplyPlayableNetworkVariables` keeps its direct `transform.localRotation` write so spawn/join never animates

## 2. Rotation tween in CardModel

- [x] 2.1 Add `RotationAnimationDuration = 0.25f` constant and a stored `Tween _rotationAnimation` field to `CardModel.cs`, near the existing flip animation members
- [x] 2.2 Override `ApplyRotation` in `CardModel`: when `didStart && gameObject.activeInHierarchy`, the local player is not drag-rotating this card, and the target differs from the current visual rotation by ≥ ~1°, stop any in-flight rotation tween and start `Tween.LocalRotation(transform, newRotation, RotationAnimationDuration)`; otherwise fall back to the base instant write
- [x] 2.3 Add `FinishRotationAnimation()` that stops the stored tween (if alive) and snaps `transform.localRotation` to `Rotation`; call it from `OnDestroy` alongside `FinishFlipAnimation`
- [x] 2.4 Verify the drag-rotation gate uses the existing pointer/drag state (the two-finger `Rotate()` path) so echoed network updates mid-drag apply instantly for the dragging player

## 3. Verification

- [x] 3.1 In the play area, use the card action panel to Tap, untap, and Rotate a card: the card animates smoothly to each rotation, and rapid repeated actions retarget cleanly and settle exactly on the final angle
- [x] 3.2 Verify no animation plays on non-transitions: card spawn with a game default rotation, dealing/placing cards into rotated zones, and rejoining a multiplayer game with tapped cards
- [x] 3.3 In a multiplayer session, verify the observing client sees the tap/rotate animation, drag-rotation stays direct for the dragger, and tap+flip in quick succession compose without visual glitches
- [x] 3.4 Verify deleting or stacking a card mid-animation produces no errors, and run the project's existing edit/play mode tests

## 4. Extend to card stacks (playable viewer Rotate)

- [x] 4.1 Move the rotation tween (constants, tween handle, gates, `FinishRotationAnimation`) from the `CardModel` override into the base `CgsNetPlayable.ApplyRotation`, so all playables animate discrete rotation changes
- [x] 4.2 Add a `CgsNetPlayable.OnDestroy` override that finishes the rotation animation; remove the now-redundant `CardModel` members
- [x] 4.3 In the play area, use the playable viewer's Rotate action on a card stack: the stack animates smoothly, and drag-rotation of stacks stays direct
