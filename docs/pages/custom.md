---
permalink: custom.html
---

# CGS Custom Card Game
Card Game Simulator allows users to create, share, and play custom card games.

## CGS Core Concept
A custom card game in CGS follows the `FinolDigital.Cgs.CardGameDef` specification. 
This specification primarily includes information on sets of cards and a definition of what properties those cards have. 
Ancillary information can include decks and boards to use with the game. 
See below for the full specification.

## CGS games directory
Custom games are created with a new folder within the persistent games data directory. 
The location of this persistent data directory varies depending on platform. 
Some examples include:
- Android: /Data/Data/com.finoldigital.cardgamesim/files/games/
- Mac: ~/Library/Containers/com.finoldigital.CardGameSimulator/Data/Library/Application Support/com.finoldigital.CardGameSimulator/games
- Windows (Steam): C:\Users\\<user\>\AppData\LocalLow\Finol Digital LLC\Card Game Simulator\games
- Windows UWP (Microsoft Store): C:\Users\\<user \>\AppData\Local\Packages\FinolDigitalLLC.CardGameSimulator_499qk536pdy94\LocalState\games

## Custom game folder structure
The structure of this custom game folder is:
- *Game:Id*/
  - *Game:Name*.json
  - AllCards.json
  - AllDecks.json
  - AllSets.json
  - Banner.\<bannerImageFileType\>
  - CardBack.\<cardBackImageFileType\>
  - PlayMat.\<playMatImageFileType\>
  - backs/
    * *Card:Id*.\<cardBackImageFileType\>
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
The GGS AutoUpdate Url that is used to download a card game is actually a pointer to the *Game:Name*.json file. 
CGS generates the rest of the folder structure based off that *CardGameDef.json* file. 

You can create your own json and [validate](https://www.jsonschemavalidator.net/) against these schema:
- [CardGameDef](https://github.com/finol-digital/FinolDigital.Cgs.CardGameDef/blob/main/schema/CardGameDef.json)
- [AllCards](https://github.com/finol-digital/FinolDigital.Cgs.CardGameDef/blob/main/schema/AllCards.json)
- [AllDecks](https://github.com/finol-digital/FinolDigital.Cgs.CardGameDef/blob/main/schema/AllDecks.json)
- [AllSets](https://github.com/finol-digital/FinolDigital.Cgs.CardGameDef/blob/main/schema/AllSets.json)

## Examples
The default examples can be found in the [CGS GitHub Repository](https://github.com/finol-digital/Card-Game-Simulator/tree/develop/docs/games).
Further examples can be found in the [CGS Google Drive folder](https://drive.google.com/open?id=1kVms-_CXRw1e4Ob18fRkS84MN_cxQGF5).
