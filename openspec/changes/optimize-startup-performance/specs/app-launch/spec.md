# Spec Delta

## Purpose

Define what players experience from launching CGS until the Main Menu is usable, so that launch feels instant on every supported platform without losing content, deep links, or language selection.

## ADDED Requirements

### Requirement: Launch opens directly on the Main Menu
A normal launch SHALL show the Main Menu as the first screen. CGS SHALL NOT show an intermediate title or "press any key" screen, and SHALL NOT require any input before the Main Menu accepts navigation.

#### Scenario: Normal launch
- **WHEN** a player launches CGS without a deep link
- **THEN** the first CGS screen shown is the Main Menu
- **AND** the player can select a Main Menu action without first pressing a key or tapping the screen

#### Scenario: Launch with a gamepad
- **WHEN** a player launches CGS with only a gamepad connected
- **THEN** a Main Menu control is selected and responds to the first gamepad input

### Requirement: Main Menu becomes interactive within the launch budget
On each reference device for native platforms, the time from the launch action to the Main Menu accepting a working action SHALL be at most 500 ms at the 95th percentile for launches with existing saved data. First-install launches and web launches SHALL be measured and reported against their own documented budgets.

#### Scenario: Repeat launch on a reference device
- **WHEN** the benchmark measures at least 30 terminated-process launches with existing saved data on a reference device
- **THEN** the 95th-percentile time to an interactive Main Menu is 500 ms or less

#### Scenario: First install
- **WHEN** CGS is launched for the first time after installation on a reference device
- **THEN** the time to an interactive Main Menu is reported separately and meets the first-install budget documented for that platform

### Requirement: Card data does not block the first interactive Main Menu
The Main Menu SHALL become interactive using only lightweight information about the selected game (such as its name and banner). Loading a game's cards, sets, card backs, and play mat SHALL happen after the Main Menu is interactive and SHALL NOT freeze Main Menu input while it runs.

#### Scenario: Large selected game
- **WHEN** the previously selected game contains thousands of cards and the player launches CGS
- **THEN** the Main Menu becomes interactive within the same budget as for a small game
- **AND** the selected game's name and banner are shown, and card data finishes loading in the background

#### Scenario: Selected game fails to load
- **WHEN** background loading of the selected game fails
- **THEN** the Main Menu stays usable and the player is told about the problem using the existing load-error behavior

### Requirement: Optional services do not delay the Main Menu
Services not needed to show and operate the Main Menu, including online multiplayer services, LAN discovery, platform-store integrations, and automatic game updates, SHALL NOT delay the Main Menu becoming interactive. Each SHALL still be available when the player first uses a feature that needs it.

#### Scenario: Online play after a fast launch
- **WHEN** the player launches CGS and then chooses to join an online game
- **THEN** online services start if they have not already, and joining works as before

#### Scenario: Steam Deck features
- **WHEN** CGS is launched on a Steam Deck through Steam
- **THEN** Steam Deck input and on-screen keyboard support become available without delaying the Main Menu

#### Scenario: Game due for automatic update
- **WHEN** the selected game is due for its automatic update at launch
- **THEN** the Main Menu becomes interactive first and the update proceeds afterwards with visible progress

### Requirement: Bundled games are available on first launch
On first launch, CGS SHALL make the bundled default games available without the installation of all bundled games blocking the Main Menu. The default selected game SHALL be selectable and playable from the first Main Menu shown.

#### Scenario: First launch on Android
- **WHEN** CGS is launched for the first time on Android
- **THEN** the Main Menu shows the default selected game
- **AND** the other bundled games appear in game selection once they are installed, without restarting

#### Scenario: Interrupted first launch
- **WHEN** CGS is closed while bundled games are still being installed and is then relaunched
- **THEN** installation resumes or repairs incomplete games, and no bundled game is left missing or corrupt

### Requirement: Main Menu accepts input as soon as it is shown
Once the Main Menu is visible, CGS SHALL act on the player's first deliberate input. Input that started before the Main Menu was shown SHALL NOT trigger an action unintentionally.

#### Scenario: Quick tap after launch
- **WHEN** the player taps "Start Game" on the first frame the Main Menu is visible
- **THEN** CGS starts loading the game

#### Scenario: Held input from before the menu appeared
- **WHEN** a key or button pressed before the Main Menu appeared is still held as the menu appears
- **THEN** the menu does not act on that press until it is released and pressed again

### Requirement: Startup looks continuous
Any platform startup image or background shown before CGS draws its first frame SHALL match the Main Menu's background color and style, so that launch does not show an unrelated screen or a color flash.

#### Scenario: Launch on mobile
- **WHEN** a player launches CGS on Android or iOS
- **THEN** the platform startup screen uses the Main Menu's background, and the transition to the Main Menu shows no color flash

### Requirement: Startup language is shown from the first Main Menu frame
The first Main Menu shown SHALL use the startup language resolved under the language-selection rules. CGS SHALL NOT visibly show Main Menu text in a different language first and then switch.

#### Scenario: Saved non-English language
- **WHEN** a player who selected Korean launches CGS
- **THEN** the first visible Main Menu text is Korean

### Requirement: Deep links still open their destinations
Deep links that open a CGS screen or download a game SHALL keep working when CGS is launched by the link and when CGS is already running.

#### Scenario: Cold launch from a play link
- **WHEN** CGS is launched by a deep link targeting the play screen
- **THEN** CGS opens the play screen without first requiring Main Menu input

#### Scenario: Game download link
- **WHEN** CGS is launched by a deep link containing a game URL
- **THEN** the Main Menu becomes interactive and the game downloads with visible progress
