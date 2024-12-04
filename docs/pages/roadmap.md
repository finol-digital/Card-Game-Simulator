---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.123
- Bug-Fix: Sometimes cards disappear when moving them
- Bug-Fix: Deck Editor accommodates non-standard card sizes

## Sprint - Priority 1
- Bug-Fix: macOS Mission Control freezes
- Bug-Fix: Importing more than 200 cards fails
- Bug-Fix: Deck Editor slows when loading too many cards
- Cards: Select left and right on zoom in Cards Explorer
- Game-Play: Enhanced Drawer Buttons
- Game-Play: Move card to zone, stack, or drawer (E)
- Game-Play: Animation for card and stack actions
- Cards: Animated highlight
- Game-Play: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Cards: Support mix of different card sizes in the same game
- Game-Play: Apply permissions for each other's decks/stacks/cards, shuffling, moving, deleting, viewing facedown, etc
- Game-Play: Label which player is interacting with cards/playables
- Game-Play: Name-Plates indicating player seats
- Game-Play: Display values on Card in Play Area
- Game-Play: Counter system for players and cards (Dice grouping/parenting)
- Game-Play: Support multiple card selection
- Game-Play: Cut/Copy/Paste Cards
- Game-Play: Undo with Ctrl-Z
- Game-Play: Save/Load Sessions + Log of all (Player) actions
- CardGameDef: Set increments for Game-Play Points-Counter

## Backlog - Priority 2
- Game-Play: Custom Tokens
- Game-Play: Flip Random for tokens
- Game-Play: Rename Stacks
- Game-Play: Add restart button in Play Settings Menu
- Cards: Search backs in Card Search/Filter Menu
- Decks: Support organizing decks into folders
  - Decouple games and decks, so you can use any deck from any game and deleting a game won't delete your decks
- Decks: Add extra tags (\*CMDR\* for .txt; sideboards for .dec and .ydk) 
- Decks: Show error(s) when a card is not found
- Decks: Group cards in Deck Editor
  - Option for card count limits, i.e. 3 copies per deck
  - Option to have a hard deck size limit applied, as well as for extras, I.e. 50 card main deck, 5 card extra deck
- Game-Play: Support multiple playmats
- Game-Play: Setup gamepad and keyboard shortcuts/hotkeys for Game-Play and Settings
- Platforms: Display gamepad and keyboard shortcuts/hotkeys in-app
- Platforms: Full controller support (Steam)
- Integration: Private Lobbies & Deep links to join multiplayer rooms (cgs.link)
- Platforms: Android widgets
- Accessibility: Add audio and sounds throughout the app
- Accessibility: Wiki-page for Playing a Game 
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
- Games: Ability to re-skin CGS per game, changing the look of the buttons, background, scroll bar, etc

## Icebox - Priority 3
- Integration: Json Schema Generation via https://github.com/json-everything/json-everything and https://github.com/coveooss/json-schema-for-humans
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
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
- Deck Editor: Organize cards by category when saving
- Game-Play: Automatically roll dice on phone shake
- Integration: Support multiple languages (Spanish, Chinese, etc)
- Integration: NanDeck + Squib + Magic Set Editor
- Integration: Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG/Cockatrice
- Only to be pursued if all other goals have been completed: Support game-specific rules enforcement
