# Animate Card Actions (Tap / Rotate) — Design

## Context

A card's rotation lives on the base class `CgsNetPlayable` (`Assets/Scripts/Cgs/CardGameView/Multiplayer/CgsNetPlayable.cs`). There are three paths that write the visual rotation, and all of them are instant today:

1. **Local set** — the `Rotation` property setter (lines 117-129) writes `transform.localRotation = value` immediately, then commits to `_rotationNetworkVariable` (server) or `RequestUpdateRotation` → `UpdateRotationServerRpc` (client). `CardActionPanel.Rotate` and `CardActionPanel.Tap` (lines 136-177 of `CardActionPanel.cs`) both funnel through this setter.
2. **Network update** — `OnChangeRotation(oldValue, newValue)` (lines 706-710) fires on every client via `_rotationNetworkVariable.OnValueChanged` and snaps `transform.localRotation = newValue`. This is the remote-client choke point, the exact analog of `OnChangeIsFacedown` that drives the flip animation.
3. **Continuous drag** — the two-finger `Rotate()` gesture (lines 607-619) calls `transform.Rotate(...)` directly each frame and streams `RequestUpdateRotation`. The local dragger's transform is ahead of the network value.

Initial state on spawn/join is applied by `ApplyPlayableNetworkVariables` writing `transform.localRotation` directly, bypassing both the setter and the callback, so joins and spawns naturally skip any hook placed on paths 1 and 2. `CardModel.OnNetworkSpawn` also sets an initial per-game rotation via the `Rotation` setter before `Start` completes.

The precedent is the card flip animation in `CardModel.cs` (change `2026-07-11-card-flip-animation`): a PrimeTween `Sequence` with a duration constant, a stored handle for interruption, gating on `didStart && gameObject.activeInHierarchy && oldValue != newValue`, cleanup in `OnDestroy`, and a strict presentation-only rule — logical state and network sync update immediately, only the visuals lag. PrimeTween (`com.kyrylokuzyk.primetween`) is already a dependency.

## Goals / Non-Goals

**Goals:**
- Animate a card's rotation smoothly to its new angle when tapped, untapped, or rotated via the card action panel (buttons or input bindings), or when any other code path sets a discrete new rotation.
- Animate a card stack's rotation the same way when rotated via the playable viewer's Rotate action.
- Animate identically for local and remote players, driven by the same rotation change notifications.
- Handle interruption: a new rotation during an animation retargets cleanly and settles exactly on the final rotation.
- Skip animation when the change is not a user-visible transition (spawn, initial per-game rotation, joining in progress) and while the local player is drag-rotating the card.
- Keep the animation purely presentational: `Rotation`, the network variable, and authorization are unchanged.

**Non-Goals:**
- No animation for the play mat (`RotateZoomableScrollRect` is not a playable and keeps its direct rotation).
- No smoothing/interpolation redesign for continuous drag rotation streams; remote observers of a drag get whatever the retargeting tween produces.
- No changes to tap/rotate semantics, `GameCardRotationDegrees`, or `allowsRotation` zone rules.

## Decisions

1. **Route all transform writes through a `protected virtual void ApplyRotation(Quaternion newRotation)` hook on `CgsNetPlayable`.** The `Rotation` setter and `OnChangeRotation` call the hook instead of writing `transform.localRotation` directly; the base implementation keeps today's instant write. The spawn-time network-variable application keeps its direct write so spawn/join never animates. Alternative considered: animate only in `OnChangeRotation` (pure flip pattern) — rejected because the local setter snaps the transform before the network round-trip, so the acting player would never see the animation. Alternative: animate in `CardActionPanel.Tap`/`Rotate` call sites — rejected because remote clients only see the network variable change, so the hook must live where both paths converge.

   *Refinement during implementation:* the offline (`!IsSpawned`) `Rotation` getter previously read `transform.localRotation`, which would return mid-tween values once the transform animates (e.g., two quick Rotate actions would compound from a mid-tween angle and land off-step). A `_rotation` backing field now holds the logical rotation, mirroring the existing `_position` pattern; it is synced in the setter, `OnChangeRotation`, the drag `Rotate()` gesture, and at network spawn.

2. **The base `ApplyRotation` implementation is the PrimeTween rotation tween.** `Tween.LocalRotation(transform, newRotation, RotationAnimationDuration)` from the current visual rotation toward the target, with `RotationAnimationDuration = 0.25f` matching the flip's total duration and `MainMenu.AnimationDuration`. The stored `Tween` handle enables interruption. Alternative considered: coroutine + `Quaternion.Lerp` — rejected for the same reasons as in the flip design (more code, lifecycle management).

   *Refinement during implementation:* the tween initially lived in a `CardModel` override, but the user extended scope to card stacks rotated via the playable viewer. Since `CardStack.Rotation` flows through the same setter/`OnChangeRotation` paths and stacks set no rotation at `Start`, the tween (with all its gates) moved into the base implementation on `CgsNetPlayable`, so every playable — card, stack, die, counter — animates discrete rotation changes uniformly. Cleanup runs in a `CgsNetPlayable.OnDestroy` override, which subclass `OnDestroy` overrides already chain to.

3. **Gate: animate only when `didStart && gameObject.activeInHierarchy` and the target differs from the current visual rotation by a meaningful angle (≈1°).** The `didStart` check skips pre-Start assignments (network spawn, zone placement at creation); the angle check makes same-value reasserts a no-op. Below-threshold or gated changes apply instantly via the base implementation. Because the game's default card rotation is assigned *inside* `Start()` (`OnStartPlayable`), where Unity may already report `didStart == true`, that call site pre-writes `transform.localRotation` before committing through the setter, so the angle gate guarantees the initial rotation never animates. An echoed network update that matches an in-flight tween's target is ignored rather than restarting the tween, so the acting client's animation is not stretched by the server round-trip.

4. **Skip animation while the local player is drag-rotating the card.** During the two-finger `Rotate()` gesture the transform is directly manipulated each frame, and echoed `OnChangeRotation` callbacks carry values that lag the finger; tweening toward them would fight the drag and rotate backwards. When the card is being actively dragged by the local player (existing pointer/drag state on `CgsNetPlayable`/`CardModel`), the override applies rotation instantly. Remote observers of a drag still go through the tween path: each incoming update interrupts and retargets, which converges and reads as smoothing.

5. **Interruption: stop the in-flight tween, then start a fresh tween from the current visual rotation to the newest target.** Mirrors `FinishFlipAnimation`: `if (_rotationAnimation.isAlive) _rotationAnimation.Stop();`. Unlike the flip there is no midpoint action to flush — the logical `Rotation` is already committed — so stopping mid-tween leaves the transform mid-way and the next tween (or `OnDestroy`) takes over. `OnDestroy` stops the tween; no snap needed since the object is going away, but any code path that must guarantee the final transform (e.g., before layout in a zone) can call a `FinishRotationAnimation` that stops the tween and snaps `transform.localRotation` to `Rotation`.

6. **The tween and flip animations coexist without coordination.** Flip animates `localScale.x`; rotation animates `localRotation`. They touch disjoint transform channels, so a tap+flip in quick succession composes naturally.

## Risks / Trade-offs

- [Local dragger receives echoed network rotations mid-drag] → Decision 4 applies them instantly while dragging, preserving today's behavior exactly.
- [Card moved between zones (hand ↔ play area) mid-tween; zone layout may set rotation] → Zone placement code that sets `Rotation` goes through the same hook; if the card is inactive or pre-Start it applies instantly, otherwise it retargets the tween. `FinishRotationAnimation` is available where an exact transform is required synchronously.
- [Card destroyed mid-tween (deleted, stacked, taken)] → PrimeTween no-ops tweens on destroyed targets; `OnDestroy` stops the stored handle, matching the flip cleanup.
- [Tween eases through intermediate angles that briefly misalign with grid/zone snapping visuals] → Presentation-only and ≤0.25s; logical position/rotation used by snapping reads the committed values, not the transform.
- [`Quaternion` tween takes the short way around (e.g., 270° request animates −90°)] → Acceptable and arguably desirable for tap/untap; `GameCardRotationDegrees` steps are ≤180° in practice. Noted as expected behavior, not a defect.
- [Remote smoothing of drag streams looks laggy] → Each update retargets a 0.25s tween from the current visual rotation, so error stays bounded within one tween duration; today's behavior is frame-stepped snapping, which is strictly worse.

## Migration Plan

Purely additive visual change; no data or protocol migration. Rollback = revert the commit.

## Open Questions

None.
