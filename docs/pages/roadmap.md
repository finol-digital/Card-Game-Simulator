---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.91
- Game-Play: Increase max zoom out and add buffer around edges of playmat

## Active Sprint
- Bug: Sometimes stacks will duplicate the bottom two cards below the stack, creating another stack underneath
- Game-Play: Combine Stacks when dropped on each other
- Game-Play: Put Card on bottom of Stack when Stack is dropped on Card
- Game-Play: Face-Up Stacks (Always reveal the top card); default based on bottom
- Game-Play: Rename Stacks
- Game-Play: Refine tap button and add tap functionality to card zones
- Deck Editor: Card Zone should move containing scrollrect

## Backlog - 2023 Q4
- Game-Creation: Edit Button in Games Management Menu
- Game-Play: Cards you receive at the beginning of the game can be defined by default
- Game-Play: Contextual green button for default action based on card location
- Game-Play: Custom Tokens
- Game-Play: Rotate Tokens/Dice/Stacks
- Game-Play: Label which player is moving cards
- Game-Play: Support multiple card selection
- Game-Play: Name-Plates indicating player seats

## Backlog - 2024 Q1
- Cards: Ability to resize card preview image
- Cards: Support multiple card backs
- Cards: Support more than 1 card face (Dual-Faced Cards)
- Cards: Support mix of different card sizes in the same game
- Game-Play: Counter system for players and cards
- Game-Play: Save/Load Games + Log of all (Player) actions
- Game-Play: Cut/Copy/Paste Cards
- Game-Play: Undo with Ctrl-Z

## Backlog - 2024 Q2
- Game-Play: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Game-Play: Prevent player from looking through Stacks in certain situations
- Game-Play: Cut Stacks
- Game-Play: Move card to zone, stack, or drawer (E)
- Game-Play: Add restart button in Play Settings Menu
- Decks: Support organizing decks into folders
  - Decouple games and decks, so you can use any deck from any game
- Decks: Add extra tags (\*CMDR\* for .txt; sideboards for .dec and .ydk) 
- Decks: Show error(s) when a card is not found
- Decks: Group cards in Deck Editor

## Backlog - 2024 Q3
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
- Game-Play: Support multiple playmats
- Game-Play: Setup gamepad and keyboard shortcuts/hotkeys for Game-Play and Settings
- Games: Ability to re-skin CGS per game, changing the look of the buttons, background, scroll bar, etc
- Integration: Private Lobbies & Deep links to join multiplayer rooms
- Platforms: Productionize WebGL version by adding fullscreen and enabling Multiplayer and Developer Mode
- Platforms: Full controller support (Steam)
- Integration: Database for user-created card games, with in-app automation to upload and download from this database (Steam Workshop?)
- Accessibility: Tutorial Video for How-To-Play
  - 0:00 Intro (Name/Website)
  - 0:00 Playing Solo (Single-player/Goldfishing)
  - 0:00 Playing Online (Multi-player)
  - 0:00 Playing Custom Games
  - 0:00 Editing Decks
  - 0:00 Browsing Cards
  - 0:00 Settings/Contact
  - CGS Website: https://www.cardgamesimulator.com/
- Accessibility: Tutorial Video for How-To-Create-And-Share
- Game-Play: Tournament Support (PoQ?)
- Dev option (GUI): Create a default 'Setup' for more complicated card games. 
For example games that use multiple decks, counters, tokens etc that are always placed on the table when a game begins. 
The game dev should be able to put all of this down on the 'table' so that all future players will just be able to play when launching a game without manual setup.

## Icebox
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
