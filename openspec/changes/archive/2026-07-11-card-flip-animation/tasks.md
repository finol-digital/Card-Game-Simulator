# Tasks: card-flip-animation

## 1. Flip animation in CardModel

- [x] 1.1 Add PrimeTween-based flip helper to `CardModel.cs`: store a `Tween`/`Sequence` handle, define a flip duration constant (~0.25s total), and implement shrink (scale X → 0) → swap face → expand (scale X → 1)
- [x] 1.2 Move the display swap (`RegisterDisplay`/`UnregisterDisplay` and name-label visibility) into the animation midpoint callback so the shown face and label change when the card is edge-on
- [x] 1.3 Rewire `OnChangeIsFacedown` to trigger the flip animation only when `oldValue != newValue` and the card is active and already visible; keep the logical `_isFacedown` update immediate

## 2. Double-faced card transforms

- [x] 2.1 Add a transform-detection gate for `OnChangeId`: old and new Ids both resolve to known cards and one card's `BackFaceId` links it to the other (including the `"_b"` suffix convention)
- [x] 2.2 Route detected transform Id changes through the flip animation with the display swap (`UnregisterDisplay` old / `RegisterDisplay` new, name label) at the midpoint; leave all other Id changes instant
- [x] 2.3 Confirm the `"_b"`-faceup branch of `CardActionPanel.Flip` (which sets `IsFacedown = true` instead of changing Id) animates via the facedown path without double-animating

## 3. Non-animated paths and interruption

- [x] 3.1 Verify spawn/join paths (`OnNetworkSpawnPlayable`, `OnStartPlayable`, `CreateCardModel` with `isFacedown`) apply state without animation; adjust gating if any path routes through the animated swap
- [x] 3.2 Handle interruption: on a new facedown or transform change mid-animation, stop the running tween, ensure scale X returns to 1, and flip toward the newest state
- [x] 3.3 Guard against destruction mid-tween: stop the tween in `OnDestroy` and ensure the midpoint callback cannot act on a destroyed/despawned card

## 4. Verification

- [x] 4.1 Manually verify in solo play: flip a card faceup/facedown via the action panel and double-click, confirm smooth flip, correct face at midpoint, and name label behavior for cards without loaded images
- [x] 4.2 Manually verify double-faced cards in solo play: transform to the back face and back again, confirm the flip animates and ends on the correct face each time
- [x] 4.3 Manually verify in multiplayer: flip a card as host and as client (including a double-faced transform), confirm the other side sees the animation; join a game with facedown cards and confirm no animation on load
- [x] 4.4 Verify edge cases: rapid double-flip settles at correct state and full scale; deleting/stacking a card mid-flip produces no errors; run existing test suite/CI
