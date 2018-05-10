---
permalink: roadmap.html
---

# Roadmap

## Current Sprint
- Release v1 to the App Store
- Release v1 to the Mac App Store
- Fix: Crash when quitting on Windows Surface Book
- Fix: Connection to host is occasionally lost
### Done
- Update Deck Txt File Format

## Backlog
- Tech: Enforce https through GitHub Pages instead of CloudFlare
- Tech: Set up build server
- Tech: Add unit tests
- Tech: Use namespaces
- Tech: Review with Resharper
- Allow any player to move cards in the play area, instead of just the player who put it there
- Add option to restart game in *Play Mode*
- Process horizontal/vertical input on *Card Viewer Zoom*, allowing horizontal/vertical swipes
- Allow rotation to keep viewing the currently selected card
- Support deep links for games and decks through https://docs.branch.io/
  - Facebook link and homepage banner image link should use this
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
- Allow cards to snap to each other when moving them
- Consider keyboard shortcuts

## Icebox
- Support Android Search button
- Show loading bar when loading/downloading a card game. Currently, a "cards finished loading" message appears after the cards finish loading
- Synchronize points across teams in *Play Mode*
- Share discard pile between all players who share a deck in *Play Mode*
- Synchronize dice across all connected players in *Play Mode*
- Add link to rules from somewhere within *Play Mode*
- Support multiple card backs
- Support more than 1 card face
- Support custom card backgrounds (Hearthstone)
- Support decimal card property data type
- Support object card property data type
- Support grouping of dice
- Support different colored dice
- Add display name to card properties
- Deck Editor Search Results Text-Only View
- New Mahjong tile set, with default property to type instead of suit
- Define order and enums for Mahjong properties
- Resize zones based off \<cardSize\> in *Play Mode*
- Allow automatic deletion of empty decks in *Play Mode*
- Create *Sort Menu* when you click on the sort button in the *Deck Editor*
- Organize cards by category when saving a deck in the *Deck Editor*
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
- Console versions (Switch, ps4, xbone)
- Support encryption of game information
- Partner with other companies to provide licensed games
- Create tool to automatically convert games and decks from OCTGN/LackeyCCG to CGS
- Add ability to create a new card game from within CGS
- Support game-specific rules enforcement

