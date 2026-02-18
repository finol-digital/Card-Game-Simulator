## ADDED Requirements

### Requirement: Deck access is enforced for interactions
The system SHALL authorize any card, stack, or deck interaction based on the player's access to the originating deck.

#### Scenario: Player interacts with a card from an accessible deck
- **WHEN** a player attempts to move, stack, or otherwise interact with a card from a deck they can access
- **THEN** the system allows the interaction

#### Scenario: Player interacts with a card from a non-accessible deck
- **WHEN** a player attempts to move, stack, or otherwise interact with a card from a deck they cannot access
- **THEN** the system denies the interaction

### Requirement: Shared decks are accessible to all players
The system SHALL grant access to any deck designated as shared by the card game configuration.

#### Scenario: Player interacts with a shared deck card
- **WHEN** a player attempts an interaction with a card from a shared deck
- **THEN** the system treats the player as authorized for that deck

### Requirement: Individual decks are accessible only to the loading player
The system SHALL grant access to an individually loaded deck only to the player who loaded it.

#### Scenario: Loading player interacts with their individual deck
- **WHEN** the player who loaded an individual deck attempts an interaction with a card from that deck
- **THEN** the system treats the player as authorized for that deck

#### Scenario: Other player interacts with an individual deck
- **WHEN** a different player attempts an interaction with a card from an individual deck they did not load
- **THEN** the system denies the interaction

### Requirement: Unauthorized interactions provide a denial result
The system SHALL surface a consistent denial outcome when an interaction is not authorized by deck access rules.

#### Scenario: Unauthorized interaction is rejected
- **WHEN** a player attempts an interaction they are not authorized to perform
- **THEN** the system returns a denial result that can be surfaced to the player
