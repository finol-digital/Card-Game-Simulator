---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.73
- Game-Play: Snap to Grid in Play Area
- Bug-Fix: Multiplayer/Networking

## Active Sprint
convert to stack and convert to card
- Bug-Fix: Account for playmat rotation and zoom on snap to grid
- Tech: Restore SonarQube and Code Coverage
- Accessibility: New How-To-Play Video
- For portrait vs landscape: Easiest way I can think of to handle it would be an optional property in the gameid.json file that lets you specify a column from the allcards.json file that will be "CardRotation".
It could be an int column that contains values 0 - 360, with null being considered to be 0. Or null could default to a DefaultCardRotation property that is also in the gameid.json file.

## Backlog
- Game-Play: Add Tokens, with settings for label, size, shape, and color (also add color to dice)
- Game-Play: Name-Plates indicating player seats
- Game-Play: Label which player is moving cards
- Game-Play: Button for max zoom out
- Game-Play: Click outside card stack viewer to close it
- Game-Play: Cut and merge stacks
- Game-Play: Modify dice to have more than 6 faces
- Game-Play: Color the default card action green
- Game-Play: Always reveal the top card of a stack
- Game-Play: Support multiple card selection
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
