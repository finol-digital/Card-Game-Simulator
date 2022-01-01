---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.66
- Bug-Fix: Set Import and Multiplayer Menu freeze
- Bug-Fix: Deep Links
- Bug-Fix: Cards duplicated when multiple players draw from the same stack at the same time

## Active Sprint
- Accessibility: Re-record tutorial video, with emphasis on a) custom game creation and b) uploading to the internet
- Game-Play: Add Tokens, with settings for label, size, shape, and color. Also colorize dice.
- Game-Play: Unify controls for Playable Objects (Cards, Stacks, Dice, and Tokens), and show a red zone up-top where they can be dropped to delete
- Game-Play: Use 2-fingers to move and rotate Stacks
- Game-Play: Option to have top of stack always revealed
- Game-Play: Limited visibility of cards in stack
- Game-Play: Playable Objects can Snap to Grid
- Game-Play: Automatically create D6 by default; double click it to change number of faces

## Backlog
- Game-Creation: Edit Button in Main Menu
- Game-Play: Support multiple card selection
- Cards: Support more than 1 card face (Dual-Faced Cards)
- Cards: Support multiple card backs
- Decks: Show individual deck download progress
- Decks: Support organizing decks into folders
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
- Tech: Add code coverage
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
- Deck Editor: Edit Deck List in the Deck Save Menu
- Integration: Database for user-created card games, with in-app automation to upload and download from this database (Steam Workshop?)
- Game-Play: Tournament Support (PoQ?)

## Icebox
- Game-Play: Log all Player actions
- Game-Play: Clear CgsNetPlayer on restart and move the restart to its own button in the Play Menu
- Game-Play: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Tech: Pooling to improve performance of opening stack viewer
- Tech: Replace SwipeManager
- Tech: Automate store images through Fastlane
- Tech: Upgrade json schema version from v4 to v7
- Tech: Use AllDecks.json for default games
- Tech: SonarQube Scans
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
