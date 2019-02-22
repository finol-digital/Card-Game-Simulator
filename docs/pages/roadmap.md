---
permalink: roadmap.html
---

# Roadmap

## What's New
- Enhanced object card property data type
- Remapped Card Viewer and Deck Editor input
- Fix: Error on app first start
- Fix: Downloading game that already exists causes properties to be appear twice
- Fix: On change page in Cards Explorer, move to top of page
- Fix: Apply Screen Orientation Settings
- Added Windows .exe

## Current Sprint
- Fix: Zoom out in Play Mode makes card disappear
- Add Shuffle deck button
- Unify search filters with searchbar
- Android Searchbar Widget
- Group cards in the Deck Editor with a number instead of stacking them
- Have # of stacks in the Deck Editor grow dynamically (remove deckMaxCount?)
- Edit Deck Name by clicking on container
- Edit Deck List in the Deck Save Menu
- On selected card in Deck Editor and Cards Explorer, focus view on that card
- Remove focus on add card and selectEditorLeft/Right, instead do select on add by drop
- Make Card Viewer be centered instead of top-justified, with darkened background (make popup background also darker)
- Press and hold on Popups/Card Viewer to copy text
- Create Sort Menu for the Deck Editor and Cards Explorer
- Support default sort and filters
- Add Card Editor
- Tech: Travis CI for build automation
- Tech: Review with Resharper/Rider
- Tech: Add unit tests
- Tech: Rename CardInfoViewer to CardViewer (Prefab, Script, Tag), and make it non-static
- Fix: Restore networking capability
- Fix: Card disappears when being sent to host but the connnection is dropped and the card is never received by the host to be replicated

## Backlog
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
- Tech: Restore CardClearsBackground
- Tech: Replace SwipeManager
- Tech: Redo TopCardCanvas and TopMenuCanvas for performance
- Support https://github.com/ValveSoftware/ArtifactDeckCode
- Print proxy deck list as pdf
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
- New Mahjong tile set
- Define order and enums for Mahjong properties
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

