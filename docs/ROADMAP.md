# Roadmap

## Minimum Viable Product (CGS v1)
- Fix: Error message appears when connecting as a client in *Play Mode*
- Fix: Error message appears when dragging a card out and releasing it back in *Play Mode*
- Fix: Only host can control score
- Fix: Clicking on card images that have transparency does not register clicks in the correct location
- Fix: Red highlight that warns when a card will be destroyed does not appear consistently
- Fix: Value, Image, Position, and Rotation are not immediately applied when joining a session and spawning pre-existing cards in *Play Mode*
- Fix: Show indication of when a deck is shuffled in *Play Mode*
- Fix: CGS appears unresponsive when loading a deck in *Play Mode*
- Fix: Hand container does not reorient after removing a card from hand in *Play Mode*
- Fix: *Card Search/Filter Menu* toggles selected enum value when you press enter
- Fix: Dragging cards from the search results in the *Deck Editor* sometimes causes the card image to get "stuck" on the screen
- Fix: Track each card's specific set/id as necessary when saving a deck in the *Deck Editor*

## Maple (CGS v2)
- Resize cards based off \<cardSize\>
- Define actions that can be done on a card:
  - Toggle rotation between 0 and 90/180/270
  - Rotate 90/180/270
  - Toggle facedown
  - Move to hand
  - Move to top/bottom of deck
  - Move to discard/delete
- Allow card zones to be defined within certain segments of the play area (i.e. lands/creatures/etc)
- Allow for contextual setting what card action to do when a card is double-clicked
- Press and hold (or right-click) on a card to bring up a context menu with the full list of possible card actions
- Support special action buttons (i.e. button to reset rotation for all cards, button to turn all cards faceup, etc.)
- Support zooming in/out
- Allow cards to snap to each other when moving them
- Support grouping for cards
- Support moving dice both individually and as a group
- Support rolling dice both individually and as a group
- Support different colored dice, potentially using groups based off dice color
- Support tokens
- Consider keyboard shortcuts

## Icebox (CGS v3+)
- Show loading bar when loading/downloading a card game. Currently, a "cards finished loading" message appears after the cards finish loading
- Support Android Search button
- Support multiple card backs
- Support decimal card property data type
- Support object card property data type
- New Mahjong tile set, with default property to type instead of suit
- Share discard pile between all players who share a deck in *Play Mode*
- Resize zones based off \<cardSize\> in *Play Mode*
- Synchronize dice across all connected players in *Play Mode*
- Synchronize points across all connected players in *Play Mode*
- Allow automatic deletion of empty decks in *Play Mode*
- Clear the background once a card enters the play area in *Play Mode*
- Create *Sort Menu* when you click on the sort button in the *Deck Editor*
- Display keyboard shortcuts in *Options Menu*
- Allow pre-fetching of card images in *Options Menu*
- Set card image cache limit in *Options Menu*
- Support different formats/game-types for custom card games
- Support sideboards
- Support multiple languages (Spanish,Chinese)
- Support game-specific rules enforcement

