## ADDED Requirements

### Requirement: A selected card stack offers a Move action
The card stack action menu SHALL offer a Move action alongside View, Rotate, Shuffle, and Flip. Activating Move SHALL open the same move menu that the Move action on a card model opens, listing the play area, the hand, every named card zone, and every card stack as destinations.

#### Scenario: Move action opens the move menu
- **WHEN** a card stack is selected and the player activates the stack's Move action
- **THEN** the move menu opens with the same destination list a card model's Move action would show

#### Scenario: Source stack is not a destination
- **WHEN** the move menu is opened from a card stack
- **THEN** that stack does not appear among the destinations

#### Scenario: Move via the Card/Move input action
- **WHEN** a card stack is selected and the player triggers the `Card/Move` input action
- **THEN** the stack's Move action runs, the same as activating the Move button

### Requirement: Confirming a move takes the top card off the stack
When the player confirms a destination in a move menu opened from a card stack, the system SHALL add the stack's top card to the chosen destination and then remove that card from the stack, leaving the rest of the stack in place and in order.

#### Scenario: Top card moved to the play area
- **WHEN** the player opens Move on a stack of three cards and confirms "Play"
- **THEN** the stack's top card appears in the play area, the stack holds the remaining two cards in their original order, and the move menu closes

#### Scenario: Top card moved to another stack
- **WHEN** the player opens Move on a stack and confirms another card stack as the destination
- **THEN** the moved card is added to the top of the destination stack and removed from the source stack

#### Scenario: Move is not confirmed
- **WHEN** the player opens Move on a stack and cancels the move menu instead of confirming a destination
- **THEN** the stack is unchanged and still holds its top card

#### Scenario: Moving the last card
- **WHEN** the player moves the top card of a stack that holds exactly one card
- **THEN** the card is added to the destination and the now-empty stack is removed from play, as it is when its last card is dragged off

#### Scenario: Stack emptied while the move menu is open
- **WHEN** the stack a move menu was opened from no longer exists or holds no cards at the moment the player confirms a destination
- **THEN** no card is added to the destination and the move menu closes

### Requirement: A moved card keeps the stack's face state
The card produced by a stack Move SHALL be facedown when the stack's top face is down and face up when the stack's top face is up, matching the card produced by dragging the top card off that stack. Destination-specific face rules SHALL continue to apply to the moved card.

#### Scenario: Moving off a face-down stack
- **WHEN** the top card of a face-down stack is moved to the play area
- **THEN** the card arrives facedown

#### Scenario: Moving off a flipped-up stack
- **WHEN** the stack's top face has been flipped face up and its top card is moved to the play area
- **THEN** the card arrives face up

#### Scenario: Destination overrides the face state
- **WHEN** the top card of a face-down stack is moved into a card zone whose default face preference is up
- **THEN** the card arrives face up, as it would when dropped into that zone

### Requirement: Save is presented next to Delete for card stacks
The Save action SHALL be presented in the playable viewer's top bar immediately next to the Delete action, and SHALL NOT appear in the card stack action menu. It SHALL be shown only while the selected playable is a card stack.

#### Scenario: Save shown for a selected stack
- **WHEN** a card stack is selected
- **THEN** the top bar shows a Save button next to the Delete button, and the stack action menu shows View, Rotate, Shuffle, Flip, and Move only

#### Scenario: Save hidden for other playables
- **WHEN** a die or a counter is selected
- **THEN** the top bar shows Delete without a Save button

#### Scenario: Save available with the actions menu hidden
- **WHEN** the "Show Actions Menu" play setting is off and a card stack is selected
- **THEN** the Save button is still shown and usable, as the Delete button already is

#### Scenario: Saving is unchanged
- **WHEN** the player activates Save for a selected card stack, from the button or from the `Decks/Save` shortcut
- **THEN** the same save prompt appears and the same deck file is written as before the button moved
