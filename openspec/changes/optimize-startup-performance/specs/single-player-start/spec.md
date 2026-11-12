# Spec Delta

## Purpose

Define what players experience from choosing to play until a single-player table is playable, so that starting a solo game with a bundled game takes under a second end to end.

## ADDED Requirements

### Requirement: Single-player table becomes playable within budget
On each native reference device, for a bundled game with start decks, the time from choosing Start Game on the Main Menu to a playable table SHALL be at most 500 ms at the 95th percentile, and launch to a playable table SHALL be at most 1 second at the 95th percentile. A playable table means the play area and start decks are shown and the first card action succeeds.

#### Scenario: Start a bundled game from the Main Menu
- **WHEN** the benchmark chooses Start Game for Standard Playing Cards with start decks loading automatically
- **THEN** the 95th-percentile time from that choice until a card can be drawn or moved is 500 ms or less

#### Scenario: Launch straight to a playable table
- **WHEN** the benchmark launches CGS and starts the bundled game with automatic selections
- **THEN** the 95th-percentile time from the launch action to a successful first card action is 1 second or less

### Requirement: Start Game is not delayed by background game loading
If the selected game's data is still loading when the player chooses Start Game, CGS SHALL open the play screen immediately, show loading progress, and make the table playable as soon as the data is ready. Card data SHALL NOT be loaded twice.

#### Scenario: Start Game before loading finishes
- **WHEN** the player chooses Start Game while a large selected game is still loading
- **THEN** the play screen opens with visible progress
- **AND** start decks load once the game data is ready, without reloading cards

### Requirement: Single-player networking does not delay the table
Network setup used by single-player play, such as making the table joinable on the local network, SHALL NOT delay the table becoming playable. Whether solo tables remain automatically joinable SHALL be preserved unless a documented measurement-based decision changes it.

#### Scenario: Solo play on desktop
- **WHEN** the player starts a single-player game on desktop or mobile
- **THEN** the table becomes playable without waiting for network setup
- **AND** if solo tables are joinable, another player on the local network can still join once setup completes

#### Scenario: Network setup fails
- **WHEN** local network setup fails during a single-player game
- **THEN** the table remains playable and the player is informed only if they try to use a network feature

### Requirement: Players can choose to always load start decks
When a game defines start decks, the prompt asking whether to load them SHALL offer an option to remember the choice. When the player has chosen to always load start decks, starting that game SHALL load them without prompting. The player SHALL be able to change this choice in Settings.

#### Scenario: Remember the start-deck choice
- **WHEN** the player chooses to always load start decks and later starts that game again
- **THEN** the start decks load without a prompt

#### Scenario: Undo the remembered choice
- **WHEN** the player turns the remembered choice off in Settings
- **THEN** the next game start asks again whether to load start decks

### Requirement: Opening the play screen does not stall
Opening the play screen and dealing start decks SHALL NOT produce a single frame stall longer than 100 ms on a reference device, including first-use shader or rendering setup.

#### Scenario: First game after launch
- **WHEN** the player starts their first game after launching CGS on a reference device
- **THEN** no frame between choosing Start Game and the first card action takes longer than 100 ms

### Requirement: Web play does not wait for unrelated game downloads
In the web version, starting the selected game SHALL NOT wait for other default games to download. On a repeat visit with the browser cache retained, starting a bundled game SHALL NOT require downloading its data again.

#### Scenario: First web visit
- **WHEN** a player opens the web version for the first time and chooses Start Game
- **THEN** play becomes available once the selected game is ready, while other default games continue downloading in the background

#### Scenario: Repeat web visit
- **WHEN** a player returns to the web version with the browser cache retained
- **THEN** the selected game starts without downloading its data again
