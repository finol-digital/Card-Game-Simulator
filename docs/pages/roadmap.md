---
permalink: roadmap.html
---

# Roadmap

## What's New
- Tech: GitHub Actions CI
- Added Share button to popups

## Current Sprint
- Fix: New Deck and Save Deck views
- Layout cards in the Deck Editor more dynamically
  - Have stacks link to each other
  - Have # of stacks be based off the number of the unique cards (remove deckMaxCount?)
- Playmats
- Deck & play space share prefence: ask, individual, share
- Re-record training video (show on webpage and play store)

## Sprint+1
- Play Mode Redesign
  - Fix: Server freezes when sharing deck
  - Fix: Card dissappears when connection to server is dropped
  - Allow any player to move cards in the play area, instead of just the player who put it there
  - Allow cards to snap to each other when moving them
  - Have view for only hand vs view for only field
  - Card Grouping
    - Define card zones in play area (replace gameHasDiscard/gameCatchesDiscard)
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

## Backlog
- Tech: Branch on desktop
- Tech: Google Play Instant
- Android Searchbar Widget
- Animate "Tap Anywhere to Start" text
- Highlight current selected button in Main Menu
- Add sorting in the Deck Editor and Cards Explorer
- Edit Deck List in the Deck Save Menu
- Navigate games from Cards Browser

## Icebox
- Tech: Replace SwipeManager
- Card Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Support default search filters
- Support default sort
- Enhance Main Menu Carousel
- Support https://github.com/ValveSoftware/ArtifactDeckCode
- Support Deck Code from https://shadowverse-portal.com/
- Support dual-faced cards (DFC)
- Highlight card that is currently being dragged
- Keep current page when Deck Editor Layout changes
- Apply autoUpdate to cached card images
- Support Android TV and tvOS
- Support custom card backgrounds (Hearthstone)
- Support multiple card backs
- Support more than 1 card face
- Support grouping of dice
- Support different colored dice
- Automatically roll dice on phone shake
- Synchronize dice across all connected players in Play Mode
- Show hotkeys from within Play Mode
- Synchronize points across teams in Play Mode
- Share discard pile between all players who share a deck in Play Mode
- Online vs local matchmaking (Split Play Game to Play Local and Play Online)
- Allow automatic deletion of empty zones in Play Mode
- Organize cards by category when saving a deck in the Deck Editor
- Allow rotation to keep viewing the currently selected card
- Display keyboard shortcuts in-app
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

