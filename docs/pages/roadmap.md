---
permalink: roadmap.html
---

# Roadmap

## What's New - v1.49
- Fix: Dropping card from deck back on deck causes it to be duplicated
- Integration: [Branch on Windows](https://help.branch.io/developers-hub/docs/windows-chsarp-basic-integration)
- Integration: Re-record tutorial video (show on app first launch, cgs website, personal website, play store, and app stores)

## Current Sprint
- Game-Play: Card Zones: Overlap snap, stack-full, stack-vertical, stack-horizontal
- Game-Play: Pre-defined zones and card stacks in Play Area
- Game-Play: E: Move card to zone or stack
- Game-Play: Multiple hands with drawers

## Backlog
- Cards: Support more than 1 card face (Dual-Faced Cards)
- Cards: Support multiple card backs
- Game-Play: Synchronize points across teams and display all points
- Game-Play: Add submenu to control position, rotation, and zoom of playmat (see Tabletopia)
- Game-Play: Control play area with 2 finger or right-click (see Tabletopia)
- Game-Play: Play area zoom and rotation sliders in bottom corners (hidden by hand and with reset buttons) (compare Tabletopia)
- Game-Play: Support multiple playmats
- Game-Play: Support multiple card selection
- Game-Play: Customize controls/input
- Game-Play: Face-up cards in cards stacks
- Game-Play: Convert card to stack
- Tech: Add code coverage
- Usability: Scale size of UI based off either physical screen size or resolution of display
- Game-Play: Custom colors for Dice
- Cards: Configurable highlight color
- Cards: Support mix of different card sizes in the same game
- Cards: Popup card text on mouse hover over
- Cards Explorer: Add card properties in UI
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
- Deck Editor: Edit Deck List in the Deck Save Menu
- Networking: Vertical layout for Lobby
- Decks: Add extra tags (\*CMDR\* for .txt; sideboards for .dec and .ydk) 

## Icebox
- Fix: Open PDF fails outside of the Unity Editor
- Fix: [GitHub Issues](https://github.com/finol-digital/Card-Game-Simulator/issues)
- Integration: Database for user-created card games, with in-app automation to upload and download from this database
- Tech: Pooling to improve performance of opening stack viewer
- Tech: Replace SwipeManager
- Tech: Google Play Instant
- Tech: Automate store images through Fastlane
- Tech: Upgrade json schema version from v4 to v7
- Tech: Use AllDecks.json for default games
- Tech: SonarQube Scans
- Integration: Automate game upload
- Usability: Alt-text with button name appears when hovering mouse over a button
- Platforms: Networking on WebGL
- Integration: Android Search Bar Widget
- Game-Play: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Deck Editor: Focus buttons move cards
- Cards Explorer: Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Cards: Support default search filters
- Cards: Support default sort
- Deck Editor: Keep current page when orientation changes
- Card Search Results: Keep viewing the currently selected card when orientation changes
- Cards: Apply autoUpdate to cached images
- Cards: Set card image cache limit
- Cards: Allow pre-fetching of card images
- Game-Play: Support grouping of dice
- Game-Play: Automatically roll dice on phone shake
- Game-Play: Allow automatic deletion of empty zones
- Deck Editor: Organize cards by category when saving
- Platforms: Display keyboard shortcuts/hotkeys in-app
- Cards: Support .svg images
- Integration: Support multiple languages (Spanish,Chinese)
- Integration: Support different resolutions and languages for card images
- Integration: NanDeck + Squib + Magic Set Editor
- Integration: Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG/Cockatrice/CardWarden
- Platforms: Support Tilt Five
- Platforms: Support VR + AR
- Platforms: Support Android TV and tvOS
- Only to be pursued if all other goals have been completed: Support game-specific rules enforcement
