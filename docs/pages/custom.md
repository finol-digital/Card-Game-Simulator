---
permalink: custom.html
---

# Defining Custom Games
Card Game Simulator allows users to [download custom card games to use within the application](index.html#create-custom-games).

## CGS games directory
Custom games are defined by creating a new folder within the persistent games data directory. The location of this persistent data directory varies depending on platform. Some examples include:
- Android: /Data/Data/com.finoldigital.cardgamesim/files/games/
- Windows: C:/Users/\<Username\>/AppData/LocalLow/Finol Digital LLC/Card Game Simulator/games/
- Mac: ~/Library/Application Support/Finol Digital LLC/Card Game Simulator/games/

## Custom game folder structure
The structure of this custom game folder is:
- \<name\>/
  - \<name\>.json
  - AllCards.json
  - AllSets.json
  - Background.\<backgroundImageFileType\>
  - CardBack.\<cardBackImageFileType\>
  - boards/
    * *GameBoard:Id*.\<gameBoardFileType\>
    * ...
  - decks/
    * *Deck:Name*.\<deckFileType\>
    * ...
  - sets/
    * *Set:Code*/
      * *Card:Id*.\<cardImageFileType\>
      * ...
    * ...

## JSON File Structure
When downloading a custom game from a url, the data that is being downloaded is the contents of the \<name\>.json file. CGS generates the rest of the folder structure based off the information in that file. You can create your own json and validate against these schema:
- [CardGameDef](schema/CardGameDef.json)
- [AllCards](schema/AllCards.json)
- [AllSets](schema/AllSets.json)

## Examples
Functional examples can be found in the [CGS Google Drive folder](https://drive.google.com/open?id=1kVms-_CXRw1e4Ob18fRkS84MN_cxQGF5).
