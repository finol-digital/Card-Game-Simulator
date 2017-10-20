# Defining Custom Games
Card Game Simulator allows users to download custom card games to use within the application. The process for downloading a custom card game through the ui is documented in the [main documentation](README.md).

## CGS games directory
In addition to downloading a custom game from a url, custom card games can also be manually added by creating a new folder within the persistent game data directory. The location of this persistent data directory varies depending on platform. Some examples include:
- Android: /Data/Data/com.finoldigital.cardgamesim/files/games/
- Windows: C:/Users/\<Username\>/AppData/LocalLow/Finol Digital/Card Game Simulator/games/
- Mac: ~/Library/Application Support/Finol Digital/Card Game Simulator/games/

## Custom game folder structure
The structure of this custom game folder is:
- \<Name\>/
  - \<Name\>.json
  - [AllSets.json]
  - [AllCards.json]
  - [Background.\<BackgroundImageFileType\>]
  - [CardBack.\<CardBackImageFileType\>]
  - [decks/]
    - \<Deck.Name\>.\<DeckFileType\>
    - ...
  - sets/
    - \<SetCode\>/
      - \<Card.Id\>.\<CardImageFileType\>
      - ...
    - ...

## JSON File Structure
When downloading a custom game from a url, the data that is being downloaded is the contents of the \<Name\>.json file. CGS generates the rest of the folder structure based off the information in that file. The structure for that file is:

{

| Property Name | Property Type | Default Value | Description |
| --- | --- | --- | --- |
| Name | string | Required | Name is the only required field, as all other fields will use a default value if it is not assigned. This name is the name of the custom card game as it appears in the dropdown in the main menu, and CGS will create the data for the card game in a folder with this name. |
| AllCardsURL | string | "" | From AllCardsURL, CGS downloads the json that contains info about the cards for the game. If CGS is able to successfully download this json, it will save it as AllCards.json. The structure of this file is "[ {\<CardIdIdentifier\>, \<CardNameIdentifier\>, \<CardSetIdentifier\>, \<CardPrimaryProperty\>, \<CardProperties\>}, ... ]". Information about these fields can be found below. You may choose not to have an AllCards.json, and instead define all the card information directly in AllSets.json. |
| AllCardsZipped | boolean | false | AllCardsURL may point to a zipped file. If it is zipped, set AllCardsZipped to true, and CGS will unzip the file and then save the unzipped file as AllCards.json. |
| AllSetsURL | string | "" | From AllSetsURL, CGS downloads the json that contains info about the sets for the game. If CGS is able to successfully download this json, it will save it as AllSets.json. The structure of this file is "[ {\<SetCode\>, \<SetName\>, cards: <AllCards.json>}, ... ]". You must have at least 1 of either AllCards.json or AllSets.json. You may have both, and if you have both, CGS will combine the data from both to use in-game.
| AllSetsZipped | boolean | false | AllSetsURL may point to a zipped file. If it is zipped, set AllSetsZipped to true, and CGS will unzip the file and then save the unzipped file as AllSets.json. |
| AutoUpdate | boolean | false | If AutoUpdate is true, CGS will re-download \<Name\>.json, AllCards.json, and AllSets.json every time the user starts to play a card game. |
| AutoUpdateURL | string | "" | AutoUpdateURL should correspond to the URL from which users download \<Name\>.json. CGS will automatically redownload the custom game from this url if AutoUpdate is set to true. |
| BackgroundImageFileType | string | "png" | TODO |
| BackgroundImageURL | string | "" | TODO |
| CardBackImageFileType | string | "png" | TODO |
| CardBackImageURL | string | "" | TODO |
| CardIdIdentifier | string | "id" | TODO |
| CardImageFileType | string | "png" | TODO |
| CardImageURLBase | string | "" | TODO |
| CardImageURLFormat | string | TODO | TODO |
| CardImageURLName | string | TODO | TODO |
| CardNameIdentifier | string | "name" | TODO |
| CardSetIdentifier | string | "set" | TODO |
| CardPrimaryProperty | string | "" | TODO |
| CardProperties | List<PropertyDef> | [] | TODO |

 DeckFileType DeckFileType

 int DeckMaxSize

 List<EnumDef> Enums

 List<ExtraDef> Extras

 int HandStartSize

 string HsdPropertyId

 string SetCodeIdentifier

 string SetNameIdentifier

}

## Example
A functional example of a custom card game definition can be found at: TODO