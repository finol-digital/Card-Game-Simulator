---
permalink: roadmap.html
---

# Roadmap

## What's New
- Add Basic In-App Card Creation and Deletion
- Print deck as pdf

## Current Sprint
- Play Mode Redesign
  - Fix: Server freezes when sharing deck
  - Fix: Card dissappears when connection to server is dropped
  - Allow any player to move cards in the play area, instead of just the player who put it there
  - Allow cards to snap to each other when moving them
  - Have view for only hand vs view for only field
  - Card Grouping
    - Define card zones in play area (remove gameHasDiscard/gameCatchesDiscard?)
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
- Tech: GitHub Actions CI/CD with Android, Windows, Web, and Linux
- Tech: Branch on desktop
- Tech: Allow space and dash in deck filename on Windows and Android
- Tech: Use Json.Net schema
- Tech: Google Play Instant
- Navigate with hotkeys in SelectionPanel
- Android Searchbar Widget
- Animate "Tap Anywhere to Start" text
- Highlight current selected button in Main Menu
- Complete Main Menu Carousel
- Further highlight selected card and de-highlight unselected
- Create Sort Menu for the Deck Editor and Cards Explorer
- Group cards in the Deck Editor with a number instead of stacking them
- Have # of stacks in the Deck Editor grow dynamically (remove deckMaxCount?)
- Edit Deck List in the Deck Save Menu
- Press and hold on Popups/Card Viewer to copy text

## Icebox
- Tech: Restore CardClearsBackground
- Tech: Replace SwipeManager
- Support default search filters
- Support default sort
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
- Card Search Results Text-Only View
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

