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
| AllSetsURL | string | "" | From AllSetsURL, CGS downloads the json that contains info about the sets for the game. If CGS is able to successfully download this json, it will save it as AllSets.json. The structure of this file is **[ {\<SetCodeIdentifier\>:*Set:Code*, \<SetNameIdentifier\>:*Set:Name*, cards:\<AllCards.json\>}, ... ]**. You should have at least 1 of either AllCards.json or AllSets.json. You may have both, and if you have both, CGS will combine the data from both to use in-game. |
| AllSetsZipped | boolean | false | AllSetsURL may point to a zipped file. If it is zipped, set AllSetsZipped to true, and CGS will unzip the file and then save the unzipped file as AllSets.json. |
| AutoUpdate | boolean | false | If AutoUpdate is true, CGS will re-download \<Name\>.json, AllCards.json, and AllSets.json every time the user starts to play that custom card game. |
| AutoUpdateURL | string | "" | AutoUpdateURL should correspond to the URL from which users download \<Name\>.json. CGS will automatically redownload the custom game from this url if AutoUpdate is set to true. |
| BackgroundImageFileType | string | "png" | BackgroundImageFileType is the file type extension for the image file that CGS downloads from BackgroundImageURL. |
| BackgroundImageURL | string | "" | If BackgroundImageURL is a valid url, CGS will download the image at that url and save it as Background.\<BackgroundImageFileType\>. CGS will attempt to display the  Background.\<BackgroundImageFileType\> in the background anytime the custom card game is selected by the user. If it is unable to read Background.\<BackgroundImageFileType\>, CGS will simply display the CGS logo in the background. |
| CardBackImageFileType | string | "png" | CardBackImageFileType is the file type extension for the image file that CGS downloads from CardBackImageURL. |
| CardBackImageURL | string | "" | If CardBackImageURL is a valid url, CGS will download the image at that url and save it as CardBack.\<CardBackImageFileType\>. CGS will display the CardBack.\<CardBackImageFileType\> when the user turns a card facedown or if CGS is unable to find the appropriate card image. If CGS is unable to get a custom card back, CGS will use the default CGS card back. |
| CardIdIdentifier | string | "id" | Every card must have a unique card id. When defining a card in AllCards.json or AllSets.json, you can have the *Card:Id* mapped to the field defined by CardIdIdentifier. Most custom games will likely want to keep the default CardIdIdentifier. |
| CardImageFileType | string | "png" | CardImageFileType is the file type extension for the image files that CGS downloads for each individual card. |
| CardImageURLBase | string | "" | CardImageURLBase can be used by CardImageURLFormat to indicate the domain/directory from which CGS should download individual card images. |
| CardImageURLFormat | string | "{0}/{1}.{2}" | CardImageURLFormat indicates the URL from which CGS should download missing card image files. CardImageURLFormat can be built from provided parameters: **{0}:\<CardImageURLBase\>**, **{1}:*Card:Id***, **{2}:\<CardImageFileType\>**, **{3}:*Card:Name***, **{4}:*Card:SetCode***, **{5}:*Card:\<CardImageURLProperty\>***, **{6}:*Card:NameStrippedToLowerAlphaNum*** |
| CardImageURLProperty | string | "" | CardImageURLProperty can be used by CardImageURLFormat to indicate which *Card:Property* contains the url/name for the image of each individual card. |
| CardNameIdentifier | string | "name" | When defining a card in AllCards.json or AllSets.json, you can have the *Card:Name* mapped to the field defined by CardNameIdentifier. Most custom games will likely want to keep the default CardNameIdentifier. |
| CardSetIdentifier | string | "set" | When defining a card in AllCards.json or AllSets.json, you can have the *Card:SetCode* mapped to the field defined by CardSetIdentifier. Most custom games will likely want to keep the default CardSetIdentifier. If no mapping is created for the set, CGS will use "\_CGSDEFAULT\_" as the default *Set:Code*. |
| CardPrimaryProperty | string | "" | The CardPrimaryProperty is the property that is first selected and displayed in the Card Info Viewer, which appears whenever a user selects a card. |
| CardProperties | List\<PropertyDef\> | [] | *PropertyDef* has the properties: *Name* and *Type*. *PropertyDef:Name* is a string indicating the name of the property, and this name can be referenced by CardImageURLProperty, CardPrimaryProperty, Enums, Extras, and HsdPropertyId. *PropertyDef:Type* can be **string**, **integer**, or **enum**. |
| DeckFileType | DeckFileType | "txt" | When saving a deck, the formatting for how it is saved and loaded is defined by the DeckFileType. **dec** refers to the old MTGO deck file format. **hsd** refers to the Hearstone deck string format. **ydk** refers to the YGOPRO deck file format. **txt** parses each line for the following: **\<Quantity\> [*Card:Id*] *Card:Name* (*Card:SetCode*)** |
| DeckMaxSize | int | 75 | DeckMaxSize is used to decide how many card slots should appear in the deck editor, when the custom game is selected. |
| Enums | List\<EnumDef\> | [] | *EnumDef* has the properties: *Property* and *Values*. *EnumDef:Property* refers to a *PropertyDef:Name* in \<CardProperties\>. *EnumDef:Values* is a Dictionary where the keys are ints and the values are string. The integer keys can be input in hexadecimial, if prefixed with "0x", and multiple values can go through bitwise and/ors to have a single enumValue represent multiple values (flags). |
| Extras | List\<ExtraDef\> | [] | *ExtraDef* has the properties: *Group*, *Property*, and *Value*. *ExtraDef:Property* refers to a *PropertyDef:Name* in \<CardProperties\>. *ExtraDef:Value* is a string. If *Card:Properties\[ExtraDef:Property\]* equals *ExtraDef:Value*, then that card will be moved from the main deck and put into the *ExtraDef:Group* zone during play mode. If no *ExtraDef:Group* is defined, CGS will default to "Extras". The **hsd** DeckFileType treats all extra cards as Heroes, and the **ydk** DeckFileType treats all extra cards as extra deck cards. |
| HandStartSize | int | 5 | HandStartSize indicates how many cards are automatically dealt from the deck to the hand, when a user loads a deck in Play Mode. |
| HsdPropertyId | string | "dbfId" | When saving or loading a deck with the **hsd** DeckFileType, HsdPropertyId refers to the *Card:Property* used to uniquely identify each card and is stored as a varint within the deck sting. |
| SetCodeIdentifier | string | "code" | When defining a set in AllSets.json, you can have the *Set:Code* mapped to the field defined by SetCodeIdentifier. Most custom games will likely want to keep the default SetCodeIdentifier. If no mapping is created for the set, CGS will use "\_CGSDEFAULT\_" as the default *Set:Code*. |
| SetNameIdentifier | string | "name" | When defining a set in AllSets.json, you can have the *Set:Name* mapped to the field defined by SetNameIdentifier. Most custom games will likely want to keep the default SetNameIdentifier. |

}

## Example
A functional example of a custom card game definition can be found [here](https://drive.google.com/drive/u/0/folders/0B563jbWV7hpddFNseDJsdFZKcjA)
