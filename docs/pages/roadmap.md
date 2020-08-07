---
permalink: roadmap.html
---

# Roadmap

## What's New
- Revamp deck editor

## Current Sprint
- Play Mode Redesign
  - Allow any player to move cards in the play area, instead of just the player who put it there
  - gamePlayDeckName (default="Stock", "Square Wall" for Mahjong)
  - Have view for only hand vs view for only field
  - Card Grouping
    - Define card zones in play area (replace gameHasDiscard/gameCatchesDiscard)
      - Share discard pile between all players who share a deck in Play Mode
      - Card zones can define what actions are possible in that zone
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
    - Special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
  - Place card in deck in a certain spot (x from top, x from bottom) with toggle for facedown or faceup
  - Slider to control zoom
  - Play Area SharePreference
  - Rotate play area
- Playmats
- dec sideboard is extras
- ydk side is also extra

## Backlog
- Tech: Editor unit test to generate schema and confirm it matches docs/schema folder
- Tech: Runtime unit test to validate StreamingAssets/ against docs/schema
- Tech: Runtime unit test to succesfully load StreamingAssets/ without error
- Tech: Runtime unit test to compare docs/games contents to StreamingAssets/
- Re-record training video (show on app first launch, cgs webpage, personal webpage, play store, and maybe other store)
- Android Searchbar Widget
- Add sorting in the Deck Editor and Cards Explorer
- Edit Deck List in the Deck Save Menu
- Support multiple card backs
- Support more than 1 card face (DFC)
- Overlap: snap, stack-full, stack-vertical, stack-horizontal, stack-diagonal

## Icebox
- Tech: Replace SwipeManager
- Tech: Branch on desktop
- Tech: Google Play Instant
- Automate store images through fastlane
- Card Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Support default search filters
- Support default sort
- Press sort and direction to move card in deck editor
- Highlight card that is currently being dragged
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
- Support sideboards
- Track personal collection of cards and show which cards you're missing for certain decks
- Support svg images
- Support multiple languages (Spanish,Chinese)
- Support different resolutions and languages for card images
- Support encryption of game information
- Partner with other companies to provide licensed games
- Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG
- Magic Set Editor + Cockatrice integration
- Support game-specific rules enforcement

