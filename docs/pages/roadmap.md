---
permalink: roadmap.html
---

# Roadmap

## What's New
- Deck Editor: Cards move in their stacks to better make use of visible space
- Play Mode: Re-organized buttons and card stacks

## Current Sprint
- Card Grouping
  - gamePlayDeckName (default="Stock", "Square Wall" for Mahjong)
  - Define card zones in play area (replace gameHasDiscard/gameCatchesDiscard)
    - Resize hand and zones based off card game settings
    - Share discard pile between all players who share a deck in Play Mode
    - Card zones can define what actions are possible in that zone
    - .dec sideboard & .ydk side are extras
- Allow any player to move cards in the play area, instead of just the player who put it there
- Play Area SharePreference
- Card Actions
  - Define
    - Toggle rotation between 0 and 90/180/270
    - Rotate 90/180/270
    - Toggle facedown
    - Move to hand
    - Move to top/bottom of deck
    - Move to discard/delete
  - Double-clicking takes a default action, based on the zone the card is in
  - Single-click show menu with all possible actions at the bottom
- Place card in deck in a certain spot (x from top, x from bottom) with toggle for facedown or faceup
- Rotate play area (2 finger or right-click)
- Play area zoom and rotation sliders in bottom corners (hidden by hand)
- Playmats

## Backlog
- Tech: Editor unit test to generate schema and confirm it matches docs/schema folder
- Tech: Runtime unit test to validate StreamingAssets/ against docs/schema
- Tech: Runtime unit test to succesfully load StreamingAssets/ without error
- Tech: Runtime unit test to compare docs/games contents to StreamingAssets/
- Integration: Re-record training video (show on app first launch, cgs webpage, personal webpage, play store, and maybe other store)
- Deck Editor & Cards Explorer: Add sorting + Sort Menu
- Deck Editor: Edit Deck List in the Deck Save Menu
- Cards: Support multiple card backs
- Cards: Support more than 1 card face (DFC)
- Play Mode: Overlap snap, stack-full, stack-vertical, stack-horizontal, stack-diagonal

## Icebox
- Tech: Replace SwipeManager
- Tech: Branch on desktop
- Tech: Google Play Instant
- Tech: Automate store images through fastlane
- Integration: Android Searchbar Widget
- Play Mode: Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Deck Editor: Focus buttons move cards
- Card Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Support default search filters
- Support default sort
- Keep current page when Deck Editor Layout changes
- Apply autoUpdate to cached card images
- Support Android TV and tvOS
- Support custom card backgrounds (Hearthstone)
- Support grouping of dice
- Support different colored dice
- Automatically roll dice on phone shake
- Synchronize points across teams in Play Mode
- Allow automatic deletion of empty zones in Play Mode
- Organize cards by category when saving a deck in the Deck Editor
- Allow rotation to keep viewing the currently selected card
- Display keyboard shortcuts/hotkeys in-app
- Allow pre-fetching of card images
- Set card image cache limit
- Support different formats/game-types for custom card games
- Support svg images
- Support multiple languages (Spanish,Chinese)
- Support different resolutions and languages for card images
- Integration: Partner with other companies to provide licensed games
- Integration: Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG
- Integration: Magic Set Editor + Cockatrice integration
- Super Stretch Goal: Support game-specific rules enforcement

