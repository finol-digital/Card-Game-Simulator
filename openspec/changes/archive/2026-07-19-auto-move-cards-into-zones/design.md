# Design: Auto-Move Cards Into Overlapping Card Zones

## Context

The play area is itself a `CardZone` of type `Area` (`PlayController.playAreaCardZone`), and games can define child card zones (`CardZoneType.Horizontal` / `Vertical`) inside it. Today, zone capture only happens during drag: `CardZone.OnPointerEnterPlayable` sets `CardModel.PlaceHolderCardZone`, which creates a placeholder `RectTransform` inside the zone; on release, `IsMovingToPlaceHolder` animates the card to the placeholder and `FinishMovingToPlaceHolder` reparents the card into the zone, firing the zone's `OnAddCardActions` (face preference, default action, `MoveCardToServer`).

Programmatic adds bypass all of this. `PlayController.AddCardToPlayArea`, deck-play (`MoveToPlay`), card search (`AddCard`), and network spawns parent the card directly under `playAreaCardZone`; `CardZone.OnAdd` for an Area zone just calls `SnapToGrid()`. A card can therefore visually sit on a zone without belonging to it.

## Goals / Non-Goals

**Goals:**
- Any card that becomes a direct child of the play area card zone and whose position overlaps a child card zone is moved into that zone via the existing placeholder flow.
- Zone add behaviors (face preference, default card action, multiplayer sync) apply exactly as they do for drag-and-drop capture.
- Works in solo and multiplayer, regardless of which code path added the card.

**Non-Goals:**
- Changing drag-and-drop capture behavior.
- Capturing cards into `CardStack`s or `Area`-type child zones (only Horizontal/Vertical zones, matching the drag-capture check in `CardModel.UpdatePosition`).
- Retroactively capturing cards that already overlap a zone when the zone is created or moved.
- Changing the multiplayer protocol.

## Decisions

1. **Hook points: capture logic lives in `CardModel.MoveToOverlappingCardZone()`, invoked from three thin call sites.** Implementation revealed that `CardZone.OnAdd` alone does not cover all paths: solo deck-play (`MoveToPlay`) and `PlayController.AddCard` call `AddCardToPlayArea` directly without `OnAdd`, and cards spawned over the network reach clients only via `OnNetworkSpawn`/`OnStartPlayable`. The guarded capture method is therefore called from: (a) `CardZone.OnAdd` for Area zones, before `SnapToGrid()`; (b) `PlayController.AddCardToPlayArea`, before the online/solo branch; (c) `CardModel.OnStartPlayable` when the card starts as a direct child of the play area (covers network-spawned cards; only the owner acts). The method is idempotent and guard-protected, so overlapping call sites are harmless.

2. **Overlap test: card world position transformed into the zone's local rect.** Programmatic adds have no `PointerEventData`, so `RectTransformUtility.RectangleContainsScreenPoint` with pointer position (used during drag) is unavailable. Instead, `PlayController.FindCardZoneAt(worldPosition)` transforms the card's world position into each candidate zone's local space (`InverseTransformPoint`) and tests `rect.Contains`, which stays correct under zone rotation and scale. First overlapping Horizontal/Vertical zone wins, iterating `PlayController.AllCardZones` and skipping the play area zone itself.

3. **Reuse the placeholder flow rather than reparenting directly.** Setting `cardModel.PlaceHolderCardZone = zone` creates the placeholder in the zone; `zone.UpdateLayout(placeholder, cardPosition)` picks the correct sibling index for Horizontal/Vertical layout; then the card is flagged to move to the placeholder so `Update` animates it and `FinishMovingToPlaceHolder` reparents it, firing `OnAddCardActions`. This matches the requested UX (visible movement into the zone) and guarantees identical side effects to drag capture. `IsMovingToPlaceHolder` remains private: the entire capture (guards, zone lookup, placeholder creation, animation start) is encapsulated in the public `CardModel.MoveToOverlappingCardZone()`, so callers never touch the flag directly.

4. **Skip capture when it does not apply.** No capture when the card `IsStatic`, is marked `ToDelete`, already has a `PlaceHolderCardZone` (drag flow in progress), or overlaps no child zone. In those cases behavior is unchanged (`SnapToGrid` for the play area).

5. **Multiplayer sync comes for free.** `FinishMovingToPlaceHolder` reparents the card into the zone, and the zone's `OnAddCardActions` include `CgsNetManager.Instance.LocalPlayer.MoveCardToServer`, which already syncs container changes. Only the client that created the card runs the capture; other clients receive the resulting container update.

## Risks / Trade-offs

- [Capture races with AutoStack/SnapToGrid deletion] → Run the zone-overlap check before `SnapToGrid()` in `OnAdd` and return early on capture, so the card cannot be stacked/deleted and captured simultaneously.
- [Card spawned at a position overlapping multiple zones] → First matching zone in `AllCardZones` order wins; deterministic and matches drag behavior (last-writer during drag is effectively arbitrary too).
- [Network timing: card or zone not yet spawned when `OnAdd` runs on a client] → Capture runs only on the originating client (the one that calls `OnAdd`); zones are found via `PlayController.AllCardZones`, which only returns instantiated zones. If the zone is absent locally, the card simply stays in the play area (current behavior).
- [Placeholder lost mid-animation (zone deleted)] → Existing `FinishMovingToPlaceHolder`/`RecoverLostPlaceholder` warning paths already handle a null placeholder; no new handling needed.
