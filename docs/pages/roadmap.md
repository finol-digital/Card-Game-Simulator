---
permalink: roadmap.html
---

# Roadmap

## What's New
- Play online by forwarding port 7777
- For networked game sessions, you can set session name and password options
- Deal/Draw Hand prompts for gameStartHandCount cards

## Current Sprint
- Release for WebGL
- Fix: Version generation on publish release tag
- Tech: Android Build Parameters
- Tech: Editor unit test to generate schema and confirm it matches docs/schema folder
- Tech: Runtime unit test to validate StreamingAssets/ against docs/schema
- Tech: Runtime unit test to succesfully load StreamingAssets/ without error
- Tech: Runtime unit test to compare docs/games contents to StreamingAssets/
- Playmats
- Layout cards in the Deck Editor more dynamically
  - Have stacks link to each other
  - Have # of stacks be based off the number of unique cards (remove deckMaxCount)
- Highlight auto scroll area when auto scrolling
- Play Mode Redesign
  - More dynamic restart (check ResetPlayArea, take cards from all zones, reset deck, and prompt redeal for each player)
  - gamePlayDeckName (default="Stock", "Square Wall" for Mahjong)
  - Allow cards to snap to each other when moving them
  - Allow any player to move cards in the play area, instead of just the player who put it there
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
- dec sideboard is extras
- ydk side is also extra

## Backlog
- Update all images/screenshots (on cgs website, ln, and all stores)
- Re-record training video (show on cgs webpage, personal webpage, play store)
- Tech: Branch on desktop
- Tech: Google Play Instant
- Android Searchbar Widget
- Add sorting in the Deck Editor and Cards Explorer
- Edit Deck List in the Deck Save Menu
- Support multiple card backs
- Support more than 1 card face (DFC)

## Icebox
- Tech: Replace SwipeManager
- Card Search Results View Options
  - Text-only
  - Small Image
  - Large Image
- Support default search filters
- Support default sort
- Support https://github.com/ValveSoftware/ArtifactDeckCode
- Support Deck Code from https://shadowverse-portal.com/
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

