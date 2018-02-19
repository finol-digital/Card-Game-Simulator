# Roadmap

## Features
- Show loading bar when loading/downloading a card game in the *Game Selection Menu*. Currently, a "cards loaded" message appears after the game finishes loading
- Add links to restore default card games to the *Game Selection Menu*
- Select (and download if needed) the host's selected card game when joining a game session in *Play Mode*
- Show connected players in the bottom-left corner of *Play Mode*
- Consolidate 4 zones viewer buttons to 2 buttons that change contextually and make it only have 1 orientation in *Play Mode*
- Allow automatic deletion of empty decks in *Play Mode*
- Allow players to share a deck in *Play Mode*
- Support special action buttons in *Play Mode* (i.e. Reset rotation for all cards)
- Track each card's specific set/id as necessary when saving a deck in the *Deck Editor*
- Create *Sort Menu* when you click on the sort button in the *Deck Editor*
- Allow PgUp and PgDn to control scrolling, as per [Keyboard Shortcuts](KEYBOARD.md)
- New Mahjong tile set, with default property to type instead of suit
- Add *Settings/Help*:
  - Include a link to a card game's rules from within CGS
  - Display keyboard shortcuts
  - Allow pre-fetching of card images
  - Set card image cache limit

## Maple Features
- Resize cards based off \<cardSize\>
- Allow cards to snap to each other when moving them in *Play Mode*
- Allow card zones to be defined within certain spaces of the play area in *Play Mode*
- Allow for setting different double-click actions in *Play Mode*
- Press and hold (or right-click) to bring up card actions in *Play Mode*
- Synchronize dice across all connected players in *Play Mode*
- Synchronize points across all connected players in *Play Mode*
- Show indication of when a deck is shuffled in *Play Mode*
- Support zooming in/out in *Play Mode*
- Support rolling multiple dice at once from the *Dice Menu* in *Play Mode*
- Support different colored dice in *Play Mode*
- Support tokens
- Add keyboard shortcuts for *Play Mode*

## Bugs
- Fix: Vertical Input is sometimes ignored on the *Main Menu*
- Fix: Typing in *Game Selection Menu* does not ignore keyboard shortcuts
- Fix: Clicking on card images that have transparency does not register clicks in the correct location
- Fix: Red highlight that warns when a card will be destroyed does not appear consistently
- Fix: Rotating the screen orientation in *Play Mode* can cause the card zone extensions to go off-center
- Fix: Update deck count when sending card through network in *Play Mode*
- Fix: Creating dice in *Play Mode* places dice in the center of the play area instead of the center of the player's view
- Fix: Rotation is not immediately applied when joining a session and spawning pre-existing cards in *Play Mode*
- Fix: *Card Search/Filter Menu* toggles selected enum value when you press enter
- Fix: Dragging cards from the search results in the *Deck Editor* sometimes causes the card image to get "stuck" on the screen
- Fix: *Popup*s that Prompt/Ask can have the yes/no buttons displayed with the wrong message

## Icebox
- Support multiple card backs
- Support decimal card property data type
- Support object card property data type
- Clear the background once a card enters the play area in *Play Mode*
- Support different formats/game-types for custom card games
- Support sideboards
- Support multiple languages (Spanish,Chinese)

