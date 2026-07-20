# card-zone-capture Specification

## Purpose
Automatically capture cards into card zones when a card added to the play area overlaps a zone, including placeholder creation, movement animation, and zone add behaviors, so zones contain whatever cards visibly rest on them.
## Requirements
### Requirement: Cards added to the play area over a card zone are captured by that zone
When a card is added as a direct child of the play area card zone (played from a deck, added from card search, drawn, or spawned over the network) and its position overlaps a horizontal or vertical card zone inside the play area, the system SHALL move the card into that card zone instead of leaving it loose in the play area.

#### Scenario: Card played from a deck onto a card zone
- **WHEN** a card is played from a deck to a play area position that overlaps a card zone
- **THEN** the card is moved into that card zone and becomes a child of the zone rather than of the play area

#### Scenario: Card added to the play area away from any card zone
- **WHEN** a card is added to the play area at a position that overlaps no card zone
- **THEN** the card remains in the play area and snaps to the grid as it does today

#### Scenario: Card overlapping multiple card zones
- **WHEN** a card is added at a position that overlaps more than one card zone
- **THEN** the card is moved into exactly one of the overlapping zones, chosen deterministically

### Requirement: Capture uses the placeholder movement flow
The system SHALL perform the capture by creating a placeholder in the target card zone at the layout position corresponding to the card's position, and the card SHALL visibly move to the placeholder before being reparented into the zone.

#### Scenario: Placeholder created at the appropriate location
- **WHEN** a card added to the play area overlaps a horizontal or vertical card zone
- **THEN** a placeholder is created inside that zone at the sibling index matching the card's position in the zone's layout

#### Scenario: Card animates to the placeholder
- **WHEN** the placeholder has been created
- **THEN** the card moves toward the placeholder position and, upon arrival, is reparented into the card zone at the placeholder's position

### Requirement: Zone add behaviors apply to captured cards
A card captured into a card zone SHALL receive the same zone add behaviors as a card dropped into the zone by drag-and-drop: the zone's default face preference, the zone's default card action, and multiplayer container synchronization.

#### Scenario: Zone face preference applied
- **WHEN** a card is captured into a card zone whose default face preference is Down
- **THEN** the card becomes facedown upon entering the zone

#### Scenario: Capture synchronized in multiplayer
- **WHEN** a card is captured into a card zone during an online game
- **THEN** all connected players see the card inside that card zone

### Requirement: Capture does not interfere with in-progress interactions
The system SHALL NOT capture a card that is static, pending deletion, or already moving to a placeholder from a drag interaction.

#### Scenario: Card already being dragged into a zone
- **WHEN** a card already has a placeholder from an in-progress drag interaction
- **THEN** the automatic capture check does not replace or remove that placeholder
