# Defining Custom Games
Card Game Simulator allows users to download custom card games to use within the application. The process for downloading a custom card game through the ui is documented in the [main documentation](README.md).

## Card Game Simulator games directory
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
When downloading a custom game from a url, the data that is being downloaded is the contents of the \<Name\>.json file. Card game simulator generates the rest of the folder structure based off the information in that file. The structure for that file is:
{

 string Name
 
 string AllCardsURL

 bool AllCardsZipped

 string AllSetsURL

 bool AllSetsZipped

 bool AutoUpdate

 string AutoUpdateURL

 string BackgroundImageFileType

 string BackgroundImageURL

 string CardBackImageFileType

 string CardBackImageURL

 string CardIdIdentifier

 string CardImageFileType

 string CardImageURLBase

 string CardImageURLFormat

 string CardImageURLName

 string CardNameIdentifier

 string CardSetIdentifier

 string CardPrimaryProperty

 List<PropertyDef> CardProperties

 DeckFileType DeckFileType

 int DeckMaxSize

 List<EnumDef> Enums

 List<ExtraDef> Extras

 int HandStartSize

 string HsdPropertyId

 string SetCodeIdentifier

 string SetNameIdentifier

}