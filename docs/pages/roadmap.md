---
permalink: roadmap.html
---

# Roadmap

## What's New
- Fix: Screen Rotation Manager
- Fix: Deck Editor Layout
- Fix: Too many cards in deck editor search results initial startup

## Current Sprint
- Show progress while downloading game
- Enhance Cards Explorer
- Enhance object card property data type
- Add identifiers for object card property data types
- Rename current display property to displayName, and add display bool to indicate whether to display at all, and rename empty to displayValueEmpty
- Add displayValueEmptyFirst property to put the empty enum value at the front of the list in the card search menu
- Have # of stacks in the Deck Editor grow dynamically (remove deckMaxCount?)
- Group cards in the Deck Editor with a number instead of stacking them
- Add copy to clipboard buttons next to labels in the Card Viewer
- Add copy button to Popups
- Add Card Editor
- Add Game Creator
- Tech: Travis CI for build automation
- Tech: Review with Resharper/Rider
- Tech: Add unit tests
- Fix: Restore networking capability
- Fix: Card disappears when being sent to host but the connnection is dropped and the card is never received by the host to be replicated

## Backlog
- Fix: Zoom out in Play Mode has card disappear
- Add option to restart game in Play Mode
- Allow any player to move cards in the play area, instead of just the player who put it there
- Allow cards to snap to each other when moving them
- Have view for only hand vs view for only field
- Have card info viewer only go down a sliver on press in play mode; second press can bring it down like normal
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

## Icebox
- Tech: Remove Unity references from CardGameDef by creating and using ICardGameLoader instead of CoroutineRunner
- Tech: Restore CardClearsBackground
- Tech: Replace SwipeManager
- Tech: Redo TopCardCanvas and TopMenuCanvas for performance
- Tech: Rename CardInfoViewer to CardViewer (Prefab, Script, Tag)
- Highlight card that is currently being moved
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
- Deck Editor Search Results Text-Only View
- New Mahjong tile set
- Define order and enums for Mahjong properties
- Allow automatic deletion of empty decks in Play Mode
- Create Sort Menu when you click on the sort button in the Deck Editor
- Support default sort and filters
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
- Google Play Instant
- Linux version
- Release Standalone versions to Steam
- Console versions (Switch, ps4, xbone)
- Support encryption of game information
- Partner with other companies to provide licensed games
- Create tool to automatically convert games/decks to/from OCTGN/LackeyCCG
- Magic Set Editor + Cockatrice integration
- Add ability to create a new card game from within CGS
- Support game-specific rules enforcement

