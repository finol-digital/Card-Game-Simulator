---
permalink: roadmap.html
---

# Roadmap

## What's New
- Deck Editor: Cards move in their zones to better make use of visible space
- Play Mode: Re-organized buttons and card layouts
- Play Mode: Card stacks in play area
- Play Mode: Allow any player to move cards and dice in the play area, instead of just the player who put it there
- CardGameDef: gamePlayDeckName ("Stock" for Standard and Dominoes, "Square Wall" for Mahjong)
- Website: Added external links

## Current Sprint
- Play Mode: Resize stack viewer based off card game settings
- Play Mode: .dec sideboard & .ydk side are extra stacks
- Play Mode: Pre-defined stack locations
- Play Mode: Rotate stacks with 2 finger or right-click
- Play Mode: Single-clicking card shows the Actions Bar
  - Flip Face (in hand and play area)
  - Rotate 90/180/270 (only in play area)
  - Snap rotation to 0/90/180/270 (only in play area)
  - Move to hand/other card stack(s) in a certain spot (x from top, x from bottom)
- Play Mode: Rotate play area (2 finger or right-click)
- Play Mode: Play area zoom and rotation sliders in bottom corners (hidden by hand and with reset buttons)
- Play Mode: Playmats

## Backlog
- Tech: Editor unit test to generate schema and confirm it matches docs/schema folder
- Tech: Runtime unit test to validate StreamingAssets/ against docs/schema
- Tech: Runtime unit test to successfully load StreamingAssets/ without error
- Tech: Runtime unit test to compare docs/games contents to StreamingAssets/
- Tech: GitHub for Unity
- Integration: Re-record tutorial video (show on app first launch, cgs website, personal website, play store, and maybe other stores)
- Cards Explorer & Deck Editor: Add sorting + Sort Menu
- Deck Editor: Edit Deck List in the Deck Save Menu
- Cards: Support multiple card backs
- Cards: Support more than 1 card face (DFC)
- Play Mode: Card Zones: Overlap snap, stack-full, stack-vertical, stack-horizontal, stack-diagonal
- Play Mode: Face-up cards in cards stacks

## Icebox
- Tech: Replace SwipeManager
- Tech: Branch on desktop
- Tech: Google Play Instant
- Tech: Automate store images through Fastlane
- Platforms: Full support for web through WebGL
- Integration: Android Search Bar Widget
- Play Mode: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Deck Editor: Focus buttons move cards
- Cards Explorer: Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Cards: Support default search filters
- Cards: Support default sort
- Deck Editor: Keep current page when orientation changes
- Cards: Keep viewing the currently selected card when orientation changes
- Cards: Apply autoUpdate to cached images
- Cards: Set card image cache limit
- Cards: Allow pre-fetching of card images
- Play Mode: Support grouping of dice
- Play Mode: Support different colored dice
- Play Mode: Automatically roll dice on phone shake
- Play Mode: Synchronize points across teams
- Play Mode: Allow automatic deletion of empty zones
- Deck Editor: Organize cards by category when saving
- Platforms: Display keyboard shortcuts/hotkeys in-app
- Cards: Support .svg images
- Integration: Support multiple languages (Spanish,Chinese)
- Integration: Support different resolutions and languages for card images
- Integration: NanDeck + Squib + Magic Set Editor
- Integration: Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG/Cockatrice
- Platforms: Support Android TV and tvOS
- Platforms: Support VR + AR
- Stretch Goal: Support game-specific rules enforcement
