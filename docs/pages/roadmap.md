---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.73
- Game-Play: Snap to Grid in Play Area
- Bug-Fix: Multiplayer/Networking

## Active Sprint
- Cards: CardRotation field in json for portrait vs landscape.
- Game-Play: Convert to stack and convert to card
- Game-Play: Modify dice to have more than 6 faces and rotate
- Game-Play: Add Tokens, with settings for label, size, shape, and color (also add color to dice)
- Bug-Fix: Account for playmat rotation and zoom on snap to grid
- Dev option (GUI): Create a default 'Setup' for more complicated card games. 
For example games that use multiple decks, counters, tokens etc that are always placed on the table when a game begins. 
The game dev should be able to put all of this down on the 'table' so that all future players will just be able to play when launching a game without manual setup.

## Backlog
- Tech: Restore SonarQube and Code Coverage
- Accessibility: New How-To-Play Video
- Game-Play: Name-Plates indicating player seats
- Game-Play: Label which player is moving cards
- Game-Play: Button for max zoom out
- Game-Play: Click outside card stack viewer to close it
- Game-Play: Cut and merge stacks
- Game-Play: Color the default card action green
- Game-Play: Support multiple card selection
- Game-Play: Always reveal the top card of a stack
- Integration: Deep links to join multiplayer rooms
- Game-Play: Clear CgsNetPlayer on restart and move the restart to its own Reset button in the Play Menu
  - Option to keep points and reset or delete decks
- Game-Creation: Edit Button in Main Menu
- Cards: Support more than 1 card face (Dual-Faced Cards)
- Cards: Support multiple card backs
- Decks: Show individual deck download progress
- Decks: Support organizing decks into folders
  - Decouple games and decks, so you can use any deck from any game
- Decks: Add extra tags (\*CMDR\* for .txt; sideboards for .dec and .ydk) 
- Decks: Show error(s) when a card is not found
- Game-Play: Support multiple playmats
- Game-Play: Convert card to stack
- Game-Play: Pre-defined card zones/stacks
- Game-Play: Move card to zone, stack, or drawer (E)
- Game-Play: Setup gamepad and keyboard shortcuts/hotkeys for Game-Play and Settings
- Cards: Configurable highlight color
- Cards: Support mix of different card sizes in the same game
- Cards: Popup card text on mouse hover over
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
- Deck Editor: Edit Deck List in the Deck Save Menu
- Integration: Database for user-created card games, with in-app automation to upload and download from this database (Steam Workshop?)
- Game-Play: Tournament Support (PoQ?)

## Icebox
- Game-Play: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Tech: Log all Player actions
- Tech: Upgrade json schema version from v4 to v7
- Tech: Use AllDecks.json for default games
- Tech: Automate store images through Fastlane
- Platforms: Productionize WebGL version by adding fullscreen and enabling Multiplayer and Developer Mode
- Cards: Apply autoUpdate to cached images
- Cards: Set card image cache limit
- Cards: Allow pre-fetching of card images
- Cards: Support .svg images
- Cards: Support default search filters
- Cards: Support default sort
- Cards Explorer: Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Card Search Results: Keep viewing the currently selected card when orientation changes
- Deck Editor: Keep current page when orientation changes
- Deck Editor: Focus buttons move cards
- Deck Editor: Organize cards by category when saving
- Game-Play: Support grouping of dice
- Game-Play: Automatically roll dice on phone shake
- Game-Play: Allow automatic deletion of empty zones
- Platforms: Display gamepad and keyboard shortcuts/hotkeys in-app
- Integration: Support multiple languages (Spanish,Chinese)
- Integration: NanDeck + Squib + Magic Set Editor
- Integration: Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG/Cockatrice/CardWarden
- Platforms: Support Tilt Five
- Platforms: Support VR + AR
- Platforms: Support Android TV and tvOS
- Only to be pursued if all other goals have been completed: Support game-specific rules enforcement
