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
  - AllCards.json
  - AllSets.json
  - Background.\<BackgroundImageFileType\>
  - CardBack.\<CardBackImageFileType\>
  - decks/
    - *Deck:Name*.\<DeckFileType\>
    - ...
  - sets/
    - *Set:Code*/
      - *Card:Id*.\<CardImageFileType\>
      - ...
    - ...

## JSON File Structure
When downloading a custom game from a url, the data that is being downloaded is the contents of the \<Name\>.json file. CGS generates the rest of the folder structure based off the information in that file. The structure for that file is:

{

| Property Name | Property Type | Default Value | Description |
| --- | --- | --- | --- |
| Name | string | Required | Name is the only required field, as all other fields will use a default value if it is not assigned. This name is the name of the custom card game as it appears in the dropdown in the main menu, and CGS will create the data for the card game in a folder with this name. |
| AllCardsURL | string | "" | From AllCardsURL, CGS downloads the json that contains info about the cards for the game. If CGS is able to successfully download this json, it will save it as AllCards.json. The structure of this file is **[ {\<CardIdIdentifier\>:*Card:Id*, \<CardNameIdentifier\>:*Card:Name*, \<CardSetIdentifier\>:*Card:SetCode*, \<*CardProperties[i]:Name*\>:*PropertyDefValuePair:Value*, ...}, ... ]**. Information about these fields can be found below. You may choose to not have an AllCards.json, since you may instead define all the card information directly in AllSets.json. |
| AllCardsZipped | boolean | false | AllCardsURL may point to a zipped file. If it is zipped, set AllCardsZipped to true, and CGS will unzip the file and then save the unzipped file as AllCards.json. |
| AllSetsURL | string | "" | From AllSetsURL, CGS downloads the json that contains info about the sets for the game. If CGS is able to successfully download this json, it will save it as AllSets.json. The structure of this file is **[ {\<SetCodeIdentifier\>:*Set:Code*, \<SetNameIdentifier\>:*Set:Name*, cards:<AllCards.json>}, ... ]**. You should have at least 1 of either AllCards.json or AllSets.json. You may have both, and if you have both, CGS will combine the data from both to use in-game. |
| AllSetsZipped | boolean | false | AllSetsURL may point to a zipped file. If it is zipped, set AllSetsZipped to true, and CGS will unzip the file and then save the unzipped file as AllSets.json. |
| AutoUpdate | boolean | false | If AutoUpdate is true, CGS will re-download \<Name\>.json, AllCards.json, and AllSets.json every time the user starts to play that custom card game. |
| AutoUpdateURL | string | "" | AutoUpdateURL should correspond to the URL from which users download \<Name\>.json. CGS will automatically redownload the custom game from this url if AutoUpdate is set to true. |
| BackgroundImageFileType | string | "png" | The file type extension for the image file that CGS downloads from BackgroundImageURL. |
| BackgroundImageURL | string | "" | If BackgroundImageURL is a valid url, CGS will download the image at that url and save it as Background.\<BackgroundImageFileType\>. CGS will attempt to display the  Background.\<BackgroundImageFileType\> in the background anytime the custom card game is selected by the user. If it is unable to read Background.\<BackgroundImageFileType\>, CGS will simply display the CGS logo in the background. |
| CardBackImageFileType | string | "png" | The file type extension for the image file that CGS downloads from CardBackImageURL. |
| CardBackImageURL | string | "" | If CardBackImageURL is a valid url, CGS will download the image at that url and save it as CardBack.\<CardBackImageFileType\>. CGS will display the CardBack.\<CardBackImageFileType\> when the user turns a card facedown or if CGS is unable to find the appropriate card image. If CGS is unable to get a custom card back, CGS will use the default CGS card back. |
| CardIdIdentifier | string | "id" | Every card must have a unique card id. When defining a card in AllCards.json or AllSets.json, you can have the card id mapped to the field defined by CardIdIdentifier. Most custom games will likely want to keep the default CardIdIdentifier. |
| CardImageFileType | string | "png" | TODO |
| CardImageURLBase | string | "" | TODO |
| CardImageURLFormat | string | TODO | TODO |
| CardImageURLName | string | TODO | TODO |
| CardNameIdentifier | string | "name" | TODO |
| CardSetIdentifier | string | "set" | TODO |
| CardPrimaryProperty | string | "" | TODO |
| CardProperties | List\<PropertyDef\> | [] | TODO |
| DeckFileType | DeckFileType | "txt" | TODO |
| DeckMaxSize | int | 75 | TODO |
| Enums | List<EnumDef> | [] | TODO |
| Extras | List<ExtraDef> | [] | TODO |
| HandStartSize | int | 5 | TODO |
| HsdPropertyId | string | "dbfId" | TODO |
| SetCodeIdentifier | string | "code" | TODO |
| SetNameIdentifier | string | "name" | TODO |

}

## Example
A functional example of a custom card game definition can be found at: TODO