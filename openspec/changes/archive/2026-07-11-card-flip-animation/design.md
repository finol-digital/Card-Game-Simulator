# Card Flip Animation — Design

## Context

A `CardModel` renders exactly one face at a time through its `Image` component (`View.sprite`). Which face is shown is a side effect of `OnChangeIsFacedown` in `Assets/Scripts/Cgs/CardGameView/Multiplayer/CardModel.cs`:

- Faceup: `Value.RegisterDisplay(this)` → the `UnityCard` pushes its face sprite via `SetImageSprite`.
- Facedown: `Value.UnregisterDisplay(this)` → `RemoveImageSprite` sets `View.sprite = CardBackImageSprite`.

`OnChangeIsFacedown` fires both locally (offline setter) and on every client via `_isFacedownNetworkVariable.OnValueChanged`, so it is the choke point for facedown flips. The sprite swap is currently instantaneous.

Double-faced cards take a different path: `CardActionPanel.Flip` detects `Value.IsBackFaceCard` with a resolvable `BackFaceId` and transforms the card by assigning `cardModel.Value = backFaceCard` — the card's **Id** changes while `IsFacedown` stays false. The visual swap flows through `OnChangeId` → `RegisterDisplay` (locally via the `Id` setter, remotely via `_idNetworkVariable.OnValueChanged`). This transition must animate too.

The project already ships PrimeTween (see `Packages/manifest.json`), used in `MainMenu.cs` for the carousel animation, so tweening the flip requires no new dependency.

## Goals / Non-Goals

**Goals:**
- Animate faceup ↔ facedown transitions with a horizontal "turn" (scale X to 0, swap face, scale X back to 1).
- Animate double-faced card transforms (face A ↔ face B via Id change) with the same flip.
- Animate identically for local and remote players, in the play area and any other zone where a card visibly flips.
- Handle interruption: a flip during a flip retargets cleanly and settles on the final state.
- Skip animation when the change is not a user-visible transition (initial spawn, network state application on join).

**Non-Goals:**
- No 3D rotation or perspective effect; the card is a UI `Image`, so a scale-X flip is the appropriate idiom.
- No animation for other card state changes (rotation, movement, zone changes).
- No changes to how facedown state is synced or authorized over the network.

## Decisions

1. **Use PrimeTween, not a hand-rolled coroutine.** PrimeTween is already a project dependency with an established usage pattern in `MainMenu.cs`. `Tween.ScaleX(transform, 0, duration).OnComplete(...)` then `Tween.ScaleX(transform, 1, duration)` gives the shrink-swap-expand sequence with less code and no coroutine lifecycle management. Alternative considered: coroutine + `Mathf.Lerp` in `OnUpdatePlayable` (matches `RotateZoomableScrollRect`), rejected as more code for the same result.

2. **Animate in `OnChangeIsFacedown`; swap the sprite at the midpoint.** The existing `RegisterDisplay`/`UnregisterDisplay` call moves into the `OnComplete` of the shrink half, so the face changes exactly when the card is edge-on (scale X = 0). This keeps the animation purely presentational — `_isFacedown` and all logic reading it update immediately, as today.

3. **Gate on "was visible state actually shown".** Animate only when the component is active, spawned-and-started (or offline), and `oldValue != newValue`. `OnNetworkSpawnPlayable` applies the initial network value directly to `_isFacedown` without calling `OnChangeIsFacedown`, so joins and spawns already skip animation; keep it that way.

4. **Interruption: stop the running tween sequence, snap scale X to 1, then decide.** If a new facedown change arrives mid-animation, stop the current tween and start a fresh flip toward the new state. PrimeTween's `Tween.Stop`/storing the `Tween` handle on the `CardModel` makes this a one-liner. The card must never be left with scale X ≠ 1 after all tweens finish.

5. **Duration constant ~0.25s total (0.125s per half), defined on `CardModel`.** Matches the snappy feel of `MainMenu.AnimationDuration`; long enough to read, short enough not to slow play.

6. **Double-faced transforms animate via a gated `OnChangeId` hook.** `OnChangeId` fires for many non-flip reasons: initial assignment at spawn/creation, network value application on join, and deferred resolution in `WaitToResolveId`. Animate only when the Id change is a face transform: old and new Ids both resolve to known cards, the values differ, and one card's `BackFaceId` links it to the other (new Id equals old card's `BackFaceId`, or old Id equals new card's `BackFaceId`, including the `"_b"`-suffix convention). All other Id changes keep today's instant behavior. Alternative considered: a flag set by `CardActionPanel.Flip` before assigning `Value`, rejected because remote clients receive the Id change without that call site — the network variable callback is the only signal they get, so the gate must be derivable from the Ids themselves. Note the `"_b"`-faceup branch of `CardActionPanel.Flip` sets `IsFacedown = true` instead, which already animates via decision 2.

## Risks / Trade-offs

- [Tween runs while the card is being dragged or scaled by zoom] → The flip only touches `localScale.x`; drag moves position and zoom operates on the scroll-rect content, not the card's own scale, so no conflict. Verified before implementation in `CardModel` drag code.
- [Card destroyed mid-tween (deleted, stacked, taken by another player)] → PrimeTween safely no-ops tweens on destroyed targets; the midpoint `OnComplete` callback must null-check / rely on PrimeTween's target-binding so `RegisterDisplay` isn't called on a destroyed object. `OnDestroy` already calls `UnregisterDisplay`.
- [Sprite swap depends on `RegisterDisplay` being async (image may still be downloading)] → Unchanged from today: `SetImageSprite` fires whenever the sprite is ready; until then the back/name-label placeholder shows, same as current behavior.
- [Name label visibility (`SetIsNameVisible`) toggles with register/unregister] → Move it together with the sprite swap at the midpoint so text never shows on a "facedown" card mid-flip.
- [DFC transform animates while the new face's image is still downloading] → Same as the facedown case: swap at the midpoint regardless; `RegisterDisplay` delivers the sprite whenever it arrives, and the back/name placeholder shows until then.
- [False-positive transform detection on Id changes that aren't flips] → The gate requires a mutual `BackFaceId` link between old and new card, which only holds for genuine face pairs; a card taken from a stack or resolved after download never satisfies it (old Id is blank or unresolvable in those paths).

## Migration Plan

Purely additive visual change; no data or protocol migration. Rollback = revert the commit.

## Open Questions

None.
