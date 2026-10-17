## ADDED Requirements

### Requirement: Device-local size preference
CGS SHALL provide a persistent, device-local Dice and counter size setting with Auto, Standard, Large, and Extra large choices. Standard SHALL use a 1.0x visual multiplier, Large 1.5x, and Extra large 2.0x. Auto SHALL be the default for missing or invalid stored preferences and SHALL resolve to Large on native mobile platforms and positively identified mobile browsers, and Standard on desktop or unclassified environments. Touch hardware alone SHALL NOT classify a desktop as mobile. Explicit choices SHALL override Auto on every platform.

#### Scenario: Default mobile presentation
- **WHEN** a player with no saved choice starts play on Android, iOS, or a positively identified mobile browser
- **THEN** Auto displays dice and counters at 1.5x their baseline visual dimensions
- **AND** cards and other tabletop objects retain their existing dimensions

#### Scenario: Default desktop presentation
- **WHEN** a player with no saved choice starts play on desktop, including a desktop with a touchscreen
- **THEN** Auto displays dice and counters at their existing 1.0x dimensions
- **AND** desktop mouse acquisition has no added touch margin

#### Scenario: Override and persistence
- **WHEN** a player chooses Standard on mobile or Extra large on desktop and restarts the application
- **THEN** the chosen preference is restored with a multiplier of 1.0x or 2.0x respectively
- **AND** choosing Auto later restores the platform default

#### Scenario: Invalid preference or unknown platform
- **WHEN** a stored choice is unrecognized and the browser cannot be classified as mobile
- **THEN** the effective setting is Auto with Standard sizing
- **AND** all explicit size choices remain available

### Requirement: Scale local presentation without altering logical geometry
CGS SHALL apply the multiplier to the complete local dice and counter presentation, including artwork, values, counter logos, and selection feedback, centered on each piece's logical position. CGS MUST preserve logical root sizes, transforms, collider geometry, and shared state. Standard SHALL preserve the existing appearance and base interaction footprint; touch acquisition assistance is specified separately. Resizing SHALL NOT change tabletop zoom or the size of other object types.

#### Scenario: Resize a selected piece
- **WHEN** a player switches a selected die or counter from Standard to Large
- **THEN** its artwork, value or logo, and selection feedback enlarge together by 1.5x
- **AND** its logical position, rotation, root size, collider bounds, value, and selection identity remain unchanged
- **AND** no roll, counter edit, or gameplay action is invoked

#### Scenario: Restore the desktop baseline
- **WHEN** a player switches an enlarged presentation to Standard
- **THEN** the original appearance and footprint are restored without accumulated scaling

### Requirement: Refresh existing and future pieces consistently
CGS SHALL update visible dice and counters when the preference changes without restarting play. Created, loaded, reparented, re-enabled, and newly network-spawned pieces SHALL use the current local preference. Viewport, orientation, and zoom changes SHALL keep presentation aligned and SHALL NOT reset the choice. Local input-device changes SHALL NOT change the visual multiplier.

#### Scenario: Existing and new pieces
- **WHEN** a player changes the size setting and then creates or loads additional dice and counters
- **THEN** existing and added pieces use the same current local multiplier

#### Scenario: Reparent or join a session
- **WHEN** an enlarged piece is reparented or a player joins an existing session
- **THEN** each local piece retains or initializes the correct multiplier exactly once
- **AND** shared positions and sizes remain unchanged

#### Scenario: Rotate screen and switch input
- **WHEN** a player rotates the display, changes zoom, and alternates touch and mouse input
- **THEN** presentation and acquisition bounds remain aligned with each piece
- **AND** the saved setting and effective visual multiplier remain unchanged

### Requirement: Keep sizing independent of multiplayer and persistence
The size preference and presentation multiplier SHALL NOT be sent as gameplay state or written into saved tabletop geometry. A host and client SHALL independently display the same pieces at their own chosen sizes while preserving ownership, positions, values, logical sizes, and gameplay outcomes.

#### Scenario: Mobile host with desktop client
- **WHEN** a mobile host uses Large and a desktop client uses Standard on the same table
- **THEN** each sees its own presentation size
- **AND** a local preference change sends no gameplay size update or ownership request
- **AND** both retain identical shared piece values, positions, and sizes

#### Scenario: Desktop host with mobile client and reload
- **WHEN** a desktop host uses Standard, a mobile client uses Extra large, and the table is saved and reloaded
- **THEN** the saved logical geometry is identical to that without local enlargement
- **AND** each device reapplies its own display preference after reload

### Requirement: Localize the size setting
The setting label and all four choices SHALL follow existing localization and fallback behavior. Changing language SHALL refresh their text without changing the selected preference or invoking gameplay actions.

#### Scenario: Change locale in settings
- **WHEN** a player changes language while the size dropdown is visible
- **THEN** its label and choices use the active locale according to existing localization rules
- **AND** the chosen size and current game state remain unchanged
