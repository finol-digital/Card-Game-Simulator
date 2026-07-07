# seat-based-deck-placement Specification

## Purpose
Defines deterministic, seat-based deck (and extra-group stack) placement and rotation so multiplayer placement is independent of deck-load order, and solo multi-deck loading preserves reserved slots for missing extra groups.
## Requirements
### Requirement: Server assigns each player a stable seat index
In a networked play session, the server SHALL assign each player a seat index when that player's network object spawns, in join order (first player = seat 0, second player = seat 1, and so on). The seat index SHALL be replicated to all clients and SHALL NOT change for the duration of the player's connection.

#### Scenario: Seats assigned in join order
- **WHEN** a host starts a session and a second player joins
- **THEN** the host has seat index 0 and the second player has seat index 1

#### Scenario: Seat index is stable across the session
- **WHEN** a player performs any number of actions (loading decks, moving cards) after joining
- **THEN** that player's seat index remains the value assigned at spawn

### Requirement: Slots per player are inferred from the game's extras definitions
The system SHALL compute the number of deck-position slots per player as 1 (for the deck) plus the number of distinct extra group names in the game's `extras` definitions, without requiring any new field in the cgs.json game configuration.

#### Scenario: Yugioh slot inference
- **WHEN** a game defines seven extras entries that all use the group name "Extra Deck"
- **THEN** the inferred slots per player is 2

#### Scenario: Game with no extras
- **WHEN** a game defines no extras
- **THEN** the inferred slots per player is 1

### Requirement: Deck placement uses per-seat slot blocks in multiplayer
When `gamePlayDeckPositions` is non-empty and its length is a multiple of the inferred slots per player (S), a player with seat index `i` SHALL have their deck and extra-group stacks placed within slots `[i*S, (i+1)*S)` of `gamePlayDeckPositions`, regardless of the order in which players load decks or how many stacks other players have spawned.

#### Scenario: Second player loads deck first (Yugioh)
- **WHEN** in a Yugioh session (4 positions, S=2) the second player (seat 1) loads their deck before the first player has loaded theirs
- **THEN** the second player's Main Deck is placed at `gamePlayDeckPositions[2]` and their Extra Deck at `gamePlayDeckPositions[3]`

#### Scenario: First player loads deck after the second player
- **WHEN** the first player (seat 0) loads their deck after the second player's stacks are already in the play area
- **THEN** the first player's Main Deck is placed at `gamePlayDeckPositions[0]` and their Extra Deck at `gamePlayDeckPositions[1]`

#### Scenario: Decks load in join order (regression)
- **WHEN** the first player loads their deck and then the second player loads theirs
- **THEN** the stacks occupy the same positions as before this change (seat 0 block then seat 1 block)

### Requirement: Stacks have fixed offsets within a seat's block and missing groups reserve their slot
Within a seat's slot block, the deck stack SHALL be placed at offset 0 and each extra group SHALL be placed at a fixed offset determined by the game's extras definitions (1 + the group's index among distinct extra group names, in order of first appearance). Offsets SHALL be determined by the game definition, not by the contents of the loaded deck, so a deck lacking cards for an extra group leaves that slot empty and does not shift any other stack's position.

#### Scenario: First player's deck has no Extra Deck cards (Yugioh)
- **WHEN** the first player (seat 0) loads a deck containing no Extra Deck cards, and afterward the second player (seat 1) loads a deck with Extra Deck cards
- **THEN** the first player's Main Deck is at `gamePlayDeckPositions[0]` with `gamePlayDeckPositions[1]` left empty, and the second player's Main Deck and Extra Deck are at `gamePlayDeckPositions[2]` and `gamePlayDeckPositions[3]`

### Requirement: Deck rotation and position derive from the same seat index
A player's default rotation SHALL be derived from the same seat index used for deck placement (seat 0 → 0°, seat 1 → 180°, seat 2 → 90°, seat 3 → 270°), so that a player's deck position and orientation always correspond to the same seat.

#### Scenario: Rotation matches seat regardless of load order
- **WHEN** the second player (seat 1) loads their deck before the first player
- **THEN** the second player's stacks spawn in seat 1's slot block with a 180° rotation

### Requirement: Fallback placement outside the per-seat mapping
When `gamePlayDeckPositions` is empty, when its length is not a multiple of the inferred slots per player, or when a player's seat block lies beyond the configured positions, the system SHALL place that player's stacks using the existing placement behavior (load-order slot filling where configured positions remain, otherwise the non-overlapping free-slot search).

#### Scenario: More players than configured seat blocks
- **WHEN** a third player loads a deck in a game whose positions define only two seat blocks
- **THEN** the third player's stacks are placed at free, non-overlapping positions in the play area

#### Scenario: Positions not authored in block multiples
- **WHEN** a game defines 3 positions but the inferred slots per player is 2
- **THEN** deck placement behaves as it did before this change (load-order filling)

#### Scenario: Game with no configured deck positions
- **WHEN** a player loads a deck in a game that defines no `gamePlayDeckPositions` (e.g., Standard Playing Cards)
- **THEN** placement behavior is unchanged from before this change

### Requirement: Solo play uses block-per-deck-load placement
In a single-player (non-networked) session with a block-mapped configuration, each deck load SHALL occupy the next slot block in load order (first load = block 0, second load = block 1), with the same fixed offsets and reserved slots within the block.

#### Scenario: Solo single deck load (regression)
- **WHEN** a player loads a deck in solo play with an empty play area
- **THEN** the deck is placed at `gamePlayDeckPositions[0]` and its extra groups at the following configured positions, exactly as before this change

#### Scenario: Solo second deck load after a deck without extra cards
- **WHEN** in solo Yugioh a player loads a deck with no Extra Deck cards and then loads a second deck that has Extra Deck cards
- **THEN** the second deck's Main Deck and Extra Deck are placed at `gamePlayDeckPositions[2]` and `gamePlayDeckPositions[3]`
