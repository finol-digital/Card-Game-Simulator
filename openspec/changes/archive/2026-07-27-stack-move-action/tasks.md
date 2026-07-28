## 1. Move menu accepts a card stack source

- [x] 1.1 In `MoveMenu.cs`, add a `_selectedCardStack` field and a `Show(CardStack selectedCardStack)` overload that clears `_selectedCardModel`, sets the stack, shows the modal, and builds the destination options; make `Show(CardModel)` clear `_selectedCardStack` symmetrically
- [x] 1.2 Add an `IsSelectedCardStackAvailable` guard (destroyed-Unity-object test) alongside the existing `IsSelectedCardContainerAvailable`
- [x] 1.3 Make `CurrentCardContainer` return the selected card stack when the source is a stack, so `BuildCardZoneSelectionOptions` removes the source stack from the destination list
- [x] 1.4 In `Update()`, keep `moveButton.interactable` false unless a source (card model or card stack) is available and a destination toggle is on
- [x] 1.5 Extend `Move()` to handle the stack source: read the top card and `!IsTopFaceup` at confirm time, abort with a warning and `Hide()` if the stack is gone or empty, add the card to the destination through the existing `CardZone`/`PlayController`/`ICardContainer` switch, then call `RequestRemoveAt(Cards.Count - 1)` on the source stack
- [x] 1.6 Add `PlayController.ShowMoveMenu(CardStack cardStack)` calling `Mover.Show(cardStack)`

## 2. Stack Move action in the playable viewer

- [x] 2.1 Add `PlayableViewer.MoveStackTopCard()` next to `RotateStack`/`ShuffleStack`/`FlipStackTopFace`, warning and returning when no stack is selected or the stack has no cards, otherwise calling `PlayController.Instance.ShowMoveMenu(Stack)`
- [x] 2.2 Add `InputMove` and subscribe/unsubscribe `Tags.CardMove` in `OnEnable`/`OnDisable`, gated on `IsBlocked` and on the selected playable being a `CardStack`
- [x] 2.3 Verify no double-fire against `CardActionPanel`'s `Card/Move` handler: with a card model selected only `CardActionPanel` reacts, with a stack selected only `PlayableViewer` reacts

## 3. Save button relocation

- [x] 3.1 Add a `public Button saveButton` field to `PlayableViewer`
- [x] 3.2 In `Redisplay()`, activate `saveButton` only when `SelectedPlayable is CardStack`, independent of `PlaySettings.ShowActionsMenu`
- [x] 3.3 Confirm `InputSave` still routes the `Decks/Save` shortcut to `SaveStack` for a selected stack

## 4. Playable Viewer prefab wiring

- [x] 4.1 Reparent the existing Save Button from the Stack Action Panel to the View bar, positioned immediately next to the Delete Button, keeping its `SaveStack` click binding, label, icon, and tooltip
- [x] 4.2 Assign the reparented Save Button to the new `saveButton` field on the `PlayableViewer` component
- [x] 4.3 Add a Move Button to the Stack Action Panel from the same button prefab the other stack actions use, with text "Move", a "Move Top Card" tooltip, the move icon used by the card action panel, and an `OnClick` bound to `PlayableViewer.MoveStackTopCard`
- [x] 4.4 Order the Stack Action Panel as View, Move, Rotate, Shuffle, Flip and confirm the panel still holds five buttons with unchanged sizing
- [x] 4.5 Check the View bar layout at a phone aspect ratio so Save and Delete both fit without clipping

## 5. Verification

- [x] 5.1 Offline (WebGL/solo): move the top card of a face-down stack to Play, Hand, a card zone, and another stack; confirm face state, stack order, and count label
- [x] 5.2 Offline: flip the stack top face up, move the top card, and confirm it arrives face up
- [x] 5.3 Offline: move the only card of a one-card stack and confirm the empty stack is removed
- [x] 5.4 Offline: open Move on a stack and cancel; confirm the stack is unchanged
- [x] 5.5 Online (LAN host plus a client): move a top card from a stack the client does not own and confirm both players see the same result and count
- [x] 5.6 Online: empty the source stack from a second client while the move menu is open, then confirm; verify the abort path leaves no blank card behind
- [x] 5.7 Turn off "Show Actions Menu", select a stack, and confirm Save and Delete are both usable while the stack action menu stays hidden
- [x] 5.8 Select a die and a counter and confirm the top bar shows Delete with no Save button
- [x] 5.9 Run `unity command run_tests --mode playmode --async_tests` and confirm no regressions (26/27 pass; `CanGetGames` fails on DNS for aminduna.arcmage.org, unrelated to this change)
