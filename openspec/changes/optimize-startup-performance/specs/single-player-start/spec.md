# Spec Delta

## Purpose

Define what players experience from choosing to play until a single-player table is playable, so that starting a solo game with a bundled game takes under a second end to end.

## ADDED Requirements

### Requirement: Single-player table becomes playable within budget
On each native reference device, for launches with existing saved data and a bundled game with start decks, choosing Start Game to a playable table SHALL take at most 500 ms, and the launch action to a playable table at most 1 second, both at the 95th percentile over at least 30 runs. Start Game SHALL be chosen as soon as the Main Menu is interactive. First install is reported separately.

#### Scenario: Start a bundled game from the Main Menu
- **WHEN** the benchmark chooses Start Game for Standard Playing Cards immediately after the Main Menu becomes interactive, with start decks loading automatically, over at least 30 runs
- **THEN** the 95th-percentile time from that choice until a card can be drawn or moved is 500 ms or less

#### Scenario: Launch straight to a playable table
- **WHEN** an external harness launches CGS with existing saved data at least 30 times and the benchmark starts the bundled game immediately
- **THEN** the 95th-percentile time from the launch action to a successful first card action is 1 second or less

#### Scenario: First install
- **WHEN** the same path is measured on the first launch after installation
- **THEN** the time is reported separately against the documented first-install budget

### Requirement: Start Game is not delayed by background game loading
If the selected game's data is still loading when the player chooses Start Game, CGS SHALL open the play screen immediately, show loading progress, and make the table playable as soon as the data is ready. Card data SHALL NOT be loaded twice. Any work CGS does ahead of time to speed up the play screen SHALL NOT host, create decks, or change game state until the player chooses Start Game.

#### Scenario: Leave the Main Menu without playing
- **WHEN** the player opens Settings, Deck Editor, or Cards Explorer from the Main Menu instead of Start Game, at any point during ahead-of-time preparation
- **THEN** that screen opens without added delay, and no table, host, or decks were created

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

#### Scenario: Join after the table was used
- **WHEN** hosting starts after the solo player has already loaded decks and moved cards, and another player then joins
- **THEN** the joining player sees the same decks, cards, and positions as the host, and both can act on them

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
Opening the play screen and dealing start decks SHALL NOT produce a single frame stall longer than 100 ms on a reference device, including first-use shader or rendering setup, on every graphics backend that the device's build uses.

#### Scenario: First game after launch
- **WHEN** the player starts their first game after launching CGS on a reference device
- **THEN** no frame between choosing Start Game and the first card action takes longer than 100 ms

#### Scenario: Immediate start with cold caches
- **WHEN** the first launch after installation, or after clearing the graphics caches, chooses Start Game on the first interactive Main Menu frame
- **THEN** no frame between choosing Start Game and the first card action takes longer than 100 ms

### Requirement: Web play does not wait for unrelated game downloads
In the web version, starting the selected game SHALL NOT wait for other default games to download. On a repeat visit with the browser cache retained, starting a bundled game SHALL NOT require downloading its data again.

#### Scenario: First web visit
- **WHEN** a player opens the web version for the first time and chooses Start Game
- **THEN** play becomes available once the selected game is ready, while other default games continue downloading in the background

#### Scenario: Repeat web visit
- **WHEN** a player returns to the web version with the browser cache retained
- **THEN** the selected game starts without downloading its data again
