## Context

Selecting a card stack in play shows `PlayableViewer` (`Assets/Prefabs/CardGameView/Viewer/Playable Viewer.prefab`). The prefab has two relevant regions:

- **View bar** — a top strip that is visible whenever any playable is selected. It holds the value label and the **Delete** button, which calls `PlayableViewer.DeletePlayable`.
- **Stack Action Panel** — a `CanvasGroup` shown only when the selected playable is a `CardStack` *and* `PlaySettings.ShowActionsMenu` is on. It currently holds five button prefab instances wired to `ViewStack`, `RotateStack`, `ShuffleStack`, `FlipStackTopFace`, and `SaveStack`.

Card models get their actions from `CardActionPanel`, whose `Move` action calls `PlayController.Instance.ShowMoveMenu(cardModel)` → `MoveMenu.Show(CardModel)`. `MoveMenu` builds a list of `ICardContainer` destinations (Play, Hand, each named card zone, each card stack), lets the player pick one, calls `AddCard` on it, and then calls `RequestDelete()` on the source card model.

`CardStack` already exposes everything a top-card move needs: `Cards` (bottom-to-top), `IsTopFaceup`, and `RequestRemoveAt(int)`, which routes through the server when the stack is spawned and the local client is not the authority, and falls back to `OwnerRemoveAt` otherwise. `CardStack` itself implements `ICardContainer`, so it is already one of the destinations `MoveMenu` offers.

Constraints: solo desktop play runs as a LAN host, so the online path is the common path and must be handled, not treated as an edge case; and the pop must reuse the existing authority routing rather than mutating card lists directly.

## Goals / Non-Goals

**Goals:**

- Give a selected card stack a Move action that behaves, from the player's point of view, exactly like Move on a card model — same menu, same destination list, same face handling.
- Reuse `MoveMenu` and `CardStack`'s existing authority-aware mutation methods rather than adding a parallel move path.
- Relocate Save to sit beside Delete in the View bar, shown only for card stacks.
- Keep the Stack Action Panel at five buttons so its layout and sizing are unchanged.

**Non-Goals:**

- Moving more than one card at a time, moving the bottom card, or moving an arbitrary card from inside the stack. (The stack viewer already allows dragging any card out.)
- Moving a whole stack to another container — dropping one stack onto another already merges them.
- Changing `MoveMenu`'s destination list, its layout, or its input handling.
- Changing what Save writes or where it writes it.

## Decisions

### Decision 1: Extend `MoveMenu` with a card-stack source rather than materializing a temporary `CardModel`

`MoveMenu.Move()` is written against `_selectedCardModel`: it reads `Value` and `IsFacedown` from it and calls `RequestDelete()` on it afterward. Two ways to support a stack:

- **(A) Chosen** — give `MoveMenu` a second source field, `_selectedCardStack`, set by a new `Show(CardStack)` overload. `Move()` resolves the card value and facedown flag from whichever source is set, and removes from whichever source is set. `CurrentCardContainer` returns the stack itself when the source is a stack, which makes the existing "remove the current container from the options" line exclude the source stack for free.
- **(B) Rejected** — instantiate a hidden `CardModel` for the top card, pop it from the stack up front, and hand it to the existing `Show(CardModel)`. This reuses more code, but it commits the pop before the player has chosen a destination: cancelling the menu, or the menu being destroyed by a scene change, would leak the card out of the stack, and a network round trip would already have removed it from every other player's view.

(A) keeps the stack authoritative until the player confirms, which is the behavior that matters.

### Decision 2: Read the top card at confirm time, not at menu-open time

The top card is captured when `Move()` runs, not when the menu opens. In an online game another player can drag from or drop onto the same stack while the menu is open; capturing at open time would move a card that is no longer on top. If the stack has been emptied (and therefore deleted) by the time the player confirms, `Move()` aborts with a warning and closes the menu rather than moving a blank card.

### Decision 3: Add to the destination first, then remove from the stack

`Move()` calls `AddCard` on the destination and only then calls `RequestRemoveAt(Cards.Count - 1)` on the source stack, mirroring the existing card-model ordering (`AddCard` then `RequestDelete`). Duplicating a card briefly is recoverable; losing one is not.

`RequestRemoveAt` is used rather than the `OwnerPopCard`/`RequestRemoveAt` branch that `CardStack.DragCard` writes out by hand, because `RequestRemoveAt` already encapsulates that branch and is public. Note that removing the last card causes `CardStack` to delete itself, which is the same as what happens when the last card is dragged off.

### Decision 4: Facedown state comes from `IsTopFaceup`

The moved card is facedown when `!stack.IsTopFaceup`, so a face-down deck yields a face-down card and a flipped-up deck yields a face-up card. This matches `CardStack.DragCard`, which passes `!IsTopFaceup` as the facedown flag when a card is dragged off the top. The destination may still override this: `PlayController.AddCard` clears facedown for back-face cards, and card zones apply their own face preference.

### Decision 5: Move is driven by `PlayableViewer`, using the existing `Card/Move` input action

`PlayableViewer` gets `MoveStackTopCard()` next to `RotateStack`/`ShuffleStack`/`FlipStackTopFace`, and subscribes to `Tags.CardMove` the way it already subscribes to `Tags.CardRotate` and `Tags.CardFlip`. `CardActionPanel` also listens to `Card/Move`, but the two never both fire: `CardActionPanel.IsBlocked` requires its own canvas group to be interactable (a card model must be selected), and `PlayableViewer.IsBlocked` requires a playable to be selected. No new input action or binding is added.

### Decision 6: Save's visibility is driven by `Redisplay()`, not by a new panel

The Save button is reparented in the prefab from the Stack Action Panel to the View bar, immediately after the Delete button, keeping its existing `SaveStack` click binding. `PlayableViewer` gains a `public Button saveButton` reference and, in `Redisplay()`, activates it only when `SelectedPlayable is CardStack`. A die or counter selection shows Delete alone, as it does today.

Consequence, and it is intended: the View bar ignores `PlaySettings.ShowActionsMenu`, so Save becomes reachable for a stack even when the actions menu is turned off. Delete already works that way. The `Decks/Save` keyboard shortcut is unaffected — it is handled by `PlayableViewer.InputSave`, which is gated on the selected playable being a `CardStack`, not on the button's parent.

## Risks / Trade-offs

- **A concurrent pop makes the moved card differ from the one the player saw** → The window is the few seconds the menu is open, and the outcome ("the top card moves") still holds. Capturing at confirm time keeps the stack and the destination consistent, which is the property worth protecting.
- **Add succeeds and remove fails (ownership lost mid-move, host disconnects)** → The card is duplicated rather than lost. Chosen deliberately over the reverse ordering; matches the existing card-model move.
- **Moving the last card deletes the stack while the menu is closing** → `MoveMenu` already guards its container reference with `IsSelectedCardContainerAvailable` (a destroyed Unity object test); the source stack reference gets the same treatment before it is dereferenced.
- **Save in the View bar competes for width with Delete on narrow phone screens** → The View bar's layout is checked at a phone aspect ratio during implementation; the buttons are the same prefab and size as the ones already in the bar.
- **Players used to Save in the actions menu will not find it** → Save keeps its label, icon, tooltip, and keyboard shortcut, and the View bar is on-screen whenever a stack is selected, so it is more visible than before, not less.
