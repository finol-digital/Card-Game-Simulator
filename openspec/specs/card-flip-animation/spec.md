# card-flip-animation Specification

## Purpose
TBD - created by archiving change card-flip-animation. Update Purpose after archive.
## Requirements
### Requirement: Card flip is animated
When a visible card's facedown state changes, the card SHALL play a flip animation: it SHALL shrink horizontally to zero width, swap its displayed face, and expand back to full width, rather than instantly swapping the displayed face.

#### Scenario: Flipping a card facedown
- **WHEN** a faceup card in the play area is flipped facedown
- **THEN** the card animates a horizontal flip and shows the card back from the midpoint of the animation onward

#### Scenario: Flipping a card faceup
- **WHEN** a facedown card in the play area is flipped faceup
- **THEN** the card animates a horizontal flip and shows the card face from the midpoint of the animation onward

#### Scenario: Card name label stays consistent with shown face
- **WHEN** a card whose face image has not loaded is flipped
- **THEN** the card name label is only visible during the portions of the animation where the faceup side is shown

### Requirement: Double-faced card transforms are animated
When a double-faced card (a card whose back face is another card) is flipped to its other face, the card SHALL play the same flip animation, with the other face's image shown from the midpoint of the animation onward, even though this transition changes the card's Id rather than its facedown state.

#### Scenario: Transforming a double-faced card
- **WHEN** a player flips a double-faced card that is showing its front face
- **THEN** the card animates a horizontal flip and shows the back face card's image from the midpoint onward

#### Scenario: Transforming back to the front face
- **WHEN** a player flips a double-faced card that is showing its back face card
- **THEN** the card animates a horizontal flip and shows the front face's image from the midpoint onward

#### Scenario: Non-flip Id changes are not animated
- **WHEN** a card's Id changes for a reason other than a face transform (e.g., initial assignment at spawn, or resolving after the card game finishes downloading)
- **THEN** the card's display updates without a flip animation

### Requirement: Remote flips are animated
When a card's facedown state changes because another player flipped it in a multiplayer game, the observing client SHALL play the same flip animation as the client that initiated the flip.

#### Scenario: Another player flips a card
- **WHEN** player A flips a card in a multiplayer game
- **THEN** player B sees the flip animation play on that card

#### Scenario: Another player transforms a double-faced card
- **WHEN** player A flips a double-faced card to its other face in a multiplayer game
- **THEN** player B sees the flip animation play on that card, ending on the other face

### Requirement: Non-transition state changes are not animated
Cards SHALL appear directly in their correct faceup or facedown state, without animation, when the state is being applied for the first time rather than changed by a player.

#### Scenario: Card spawned facedown
- **WHEN** a card is created already facedown (e.g., dealt facedown or spawned from a facedown stack)
- **THEN** the card appears facedown immediately with no flip animation

#### Scenario: Client joins a game in progress
- **WHEN** a client joins a multiplayer game containing facedown cards
- **THEN** those cards appear facedown immediately with no flip animation

#### Scenario: Facedown value reasserted without change
- **WHEN** a card's facedown state is set to the value it already has
- **THEN** no flip animation plays

### Requirement: Interrupted flips settle on the final state
If a card's facedown state changes while a flip animation is still playing, the in-progress animation SHALL be interrupted and the card SHALL animate to and settle at the newest state at full width (scale fully restored).

#### Scenario: Rapid double flip
- **WHEN** a player flips a card faceup and flips it facedown again before the first animation finishes
- **THEN** the first animation is interrupted, a flip toward facedown plays, and the card ends facedown at full size

### Requirement: Animation is presentation-only
The flip animation SHALL NOT delay or alter the card's logical facedown state, network synchronization, or authorization checks; only the displayed face changes on a delay.

#### Scenario: Logical state updates immediately
- **WHEN** a card is flipped facedown
- **THEN** any game logic reading the card's facedown state observes the new value immediately, even while the animation is still playing

#### Scenario: Card destroyed mid-animation
- **WHEN** a card is deleted or stacked while its flip animation is playing
- **THEN** no errors occur and the animation is discarded with the card
