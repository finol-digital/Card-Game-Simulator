---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.82
- Bug Fixes

## Active Sprint
- Bug: Card in hand transforms into card that is drawn https://www.youtube.com/shorts/pYG0N9TqMmA
- Cards: Preview on mouse-over
- Game-Play: Combine Stacks when dropped on each other
- Game-Play: Put Card on bottom of Stack when Stack is dropped on Card

## Backlog - 2023 Q2
- Game-Play: Face-Up Stacks (Always reveal the top card)
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
- Game-Play: Label which player is moving cards
- Game-Play: Support multiple card selection
- Game-Play: Name-Plates indicating player seats
- Game-Play: Custom Tokens
- Game-Play: Rotate Dice
- Game-Play: Cut Stacks
- Game-Play: Prevent player from looking through deck in certain situations
- Game-Play: Save/Load Games and log of actions
- Integration: Private Lobbies & Deep links to join multiplayer rooms
- Game-Play: Clear CgsNetPlayer on restart and move the restart to its own Reset button in the Play Menu
  - Option to keep points and reset or delete decks
- Game-Play: Counter system for players and cards
- Game-Creation: Edit Button in Main Menu
- Accessibility: Tutorial Videos for How-To-Play and How-To-Create-And-Share

## Backlog - 2023 Q3
- Cards: Support more than 1 card face (Dual-Faced Cards)
- Cards: Support multiple card backs
- Decks: Support organizing decks into folders
  - Decouple games and decks, so you can use any deck from any game
- Decks: Add extra tags (\*CMDR\* for .txt; sideboards for .dec and .ydk) 
- Decks: Show error(s) when a card is not found
- Game-Play: Support multiple playmats
- Game-Play: Pre-defined card zones/stacks
- Game-Play: Move card to zone, stack, or drawer (E)
- Game-Play: Setup gamepad and keyboard shortcuts/hotkeys for Game-Play and Settings
- Cards: Configurable highlight color
- Cards: Support mix of different card sizes in the same game
- Deck Editor: Edit Deck List in the Deck Save Menu

## Backlog - 2023 Q4
- Integration: Database for user-created card games, with in-app automation to upload and download from this database (Steam Workshop?)
- Game-Play: Tournament Support (PoQ?)
- Dev option (GUI): Create a default 'Setup' for more complicated card games. 
For example games that use multiple decks, counters, tokens etc that are always placed on the table when a game begins. 
The game dev should be able to put all of this down on the 'table' so that all future players will just be able to play when launching a game without manual setup.

## Icebox
- Game-Play: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Tech: Log all Player actions
- Tech: Upgrade json schema version from v4 to v7
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
- Platforms: Display gamepad and keyboard shortcuts/hotkeys in-app
- Integration: Support multiple languages (Spanish,Chinese)
- Integration: NanDeck + Squib + Magic Set Editor
- Integration: Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG/Cockatrice/CardWarden
- Only to be pursued if all other goals have been completed: Support game-specific rules enforcement
