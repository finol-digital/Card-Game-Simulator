# card-rotation-animation Specification

## Purpose
Add a rotation animation when a card or card stack's rotation changes (tap, untap, rotate), including interruption handling, no-animation cases (spawn/initial state/local drag), and presentation-only guarantees, so rotation changes are visually clear in both solo and multiplayer.
## Requirements
### Requirement: Card rotation changes are animated
When a visible card's rotation is changed to a new angle by a discrete action (such as Tap or Rotate from the card action panel or their input bindings), the card SHALL smoothly animate from its current rotation to the new rotation instead of snapping instantly.

#### Scenario: Rotating a card
- **WHEN** a player uses the card action panel's Rotate action on a card in the play area
- **THEN** the card smoothly animates to its new rotation over a brief duration

#### Scenario: Tapping a card
- **WHEN** a player uses the card action panel's Tap action on an untapped card
- **THEN** the card smoothly animates from its untapped rotation to its tapped rotation

#### Scenario: Untapping a card
- **WHEN** a player uses the card action panel's Tap action on a tapped card
- **THEN** the card smoothly animates back to its untapped rotation

### Requirement: Card stack rotation changes are animated
When a visible card stack's rotation is changed to a new angle by a discrete action (such as Rotate from the playable viewer or its input binding), the stack SHALL smoothly animate from its current rotation to the new rotation instead of snapping instantly, with the same gating, interruption, and presentation-only behavior as cards.

#### Scenario: Rotating a card stack
- **WHEN** a player uses the playable viewer's Rotate action on a card stack in the play area
- **THEN** the stack smoothly animates to its new rotation over a brief duration

### Requirement: Remote rotation changes are animated
When a card's rotation changes because another player tapped or rotated it in a multiplayer game, the observing client SHALL play the same rotation animation as the client that initiated the change.

#### Scenario: Another player taps a card
- **WHEN** player A taps a card in a multiplayer game
- **THEN** player B sees the card smoothly animate to its tapped rotation

### Requirement: Non-transition rotation states are not animated
Cards SHALL appear directly at their correct rotation, without animation, when the rotation is being applied for the first time rather than changed by a player.

#### Scenario: Card spawned with a rotation
- **WHEN** a card is created already rotated (e.g., a game's default card rotation or a card placed in a rotated zone)
- **THEN** the card appears at that rotation immediately with no animation

#### Scenario: Client joins a game in progress
- **WHEN** a client joins a multiplayer game containing tapped or rotated cards
- **THEN** those cards appear at their current rotations immediately with no animation

#### Scenario: Rotation reasserted without change
- **WHEN** a card's rotation is set to the value it already has
- **THEN** no rotation animation plays

### Requirement: Drag rotation stays direct for the dragging player
While the local player is rotating a card with the continuous drag gesture, the card SHALL follow the gesture directly without animation, exactly as it does today.

#### Scenario: Two-finger drag rotation
- **WHEN** a player rotates a card using the two-finger drag gesture
- **THEN** the card's rotation tracks the gesture immediately, with no tween lag, including when echoed network updates arrive mid-drag

### Requirement: Interrupted rotation animations settle on the final rotation
If a card's rotation changes while a rotation animation is still playing, the in-progress animation SHALL be interrupted and the card SHALL animate from its current visual rotation to the newest target rotation, settling exactly at that rotation.

#### Scenario: Rapid repeated rotates
- **WHEN** a player triggers Rotate twice in quick succession
- **THEN** the first animation is interrupted and the card animates to and settles exactly at the final rotation

### Requirement: Animation is presentation-only
The rotation animation SHALL NOT delay or alter the card's logical rotation state, network synchronization, or authorization checks; only the visual transform changes on a delay.

#### Scenario: Logical rotation updates immediately
- **WHEN** a card is tapped
- **THEN** any game logic reading the card's rotation observes the new value immediately, even while the animation is still playing

#### Scenario: Card destroyed mid-animation
- **WHEN** a card is deleted or stacked while its rotation animation is playing
- **THEN** no errors occur and the animation is discarded with the card
