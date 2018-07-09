---
permalink: roadmap.html
---

# Roadmap

## What's New
- Improve deck loading process
- Increase maximum zoom out

## Current Sprint
- Highlight selected buttons in orange instead of purple
- Double-click on selection to load it
- In Deck Editor, make search button clear filters and only do search by name in name input field
  - Show name filter in filters bar (in quotes)
  - Show condensed value of enum filters in filters bar
- Group cards in the deck editor with a number instead of stacking them

## Backlog
- Support deep links for games and decks through https://docs.branch.io/
  - Facebook link and homepage banner image link should use this
  - Share buttons in the *Game Selection Menu* and the *Deck Load Menu*
- Tech: Finalize namespaces
- Tech: Add unit tests
- Tech: Review with Resharper
- Fix: UWP join and host
- Fix: NetworkDiscovery error on iOS sleep
- Fix: Card dissapears when being sent to host but the connnection is dropped and the card is never received by the host to be replicated
- Add rate limit for how often to update allCards and allSets
- Support Number card property data type (decimals)
- Enhance boolean card property data type
- Enhance object card property data type
- Add identifiers for object card property data types
- Show loading bar when loading/downloading a card game
- Add option to restart game in *Play Mode*
- Allow any player to move cards in the play area, instead of just the player who put it there
- Allow cards to snap to each other when moving them
- Have view for only hand vs view for only field
- Have card info viewer only go down a sliver on press in play mode; second press can bring it down like normal
- Card Grouping
  - Define card zones in play area
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
- Support Controller:
  - LEFT-STICK: Horizontal/Vertical
  - D-PAD: Horizontal/Vertical
  - SELECT: Cancel
  - START: New
  - A: Submit
  - B: Cancel
  - X: Load
  - Y: Save
  - RIGHT-STICK: Column/Page
  - L1: Sort
  - R1: Filter
  - L2: FocusName
  - R2: FocusText
  - L3: Submit
  - R3: Delete
- Support Android TV and tvOS

## Icebox
- Add displayEmptyFirst property to put the empty enum value at the front of the list in the card search menu
- Support custom card backgrounds (Hearthstone)
- Support multiple card backs
- Support more than 1 card face
- Support grouping of dice
- Support different colored dice
- Automatically roll dice on phone shake
- Synchronize dice across all connected players in *Play Mode*
- Add link to rules from somewhere within *Play Mode*
- Show hotkeys from within *Play Mode*
- Synchronize points across teams in *Play Mode*
- Share discard pile between all players who share a deck in *Play Mode*
- Online vs local matchmaking (Split Play Game to Play Local and Play Online)
- Deck Editor Search Results Text-Only View
- New Mahjong tile set
- Define order and enums for Mahjong properties
- Allow automatic deletion of empty decks in *Play Mode*
- Create *Sort Menu* when you click on the sort button in the *Deck Editor*
- Organize cards by category when saving a deck in the *Deck Editor*
- Allow rotation to keep viewing the currently selected card
- Display keyboard shortcuts in *Options Menu*
- Allow pre-fetching of card images in *Options Menu*
- Set card image cache limit in *Options Menu*
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
- Create tool to automatically convert games and decks from OCTGN/LackeyCCG to CGS
- Add ability to create a new card game from within CGS
- Support game-specific rules enforcement

