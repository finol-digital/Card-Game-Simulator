/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CardGame
    {
        public const string DefaultName = "_INVALID_";

        public static CardGame Invalid => new CardGame();

        public static JsonSerializerSettings SerializerSettings =>
            new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

        // *Game:Id* = *Game:Name*@*Game:AutoUpdateUrl:Host*
        // This only works for a single instance of a game per host
        public string Id => _id ??= Name + Host;
        private string _id;

        public string Host => (AutoUpdateUrl != null && AutoUpdateUrl.IsWellFormedOriginalString())
            ? "@" + AutoUpdateUrl.Host
            : "";


        [JsonProperty]
        [JsonRequired]
        [Description(
            "The name of the custom card game as it appears to the user. This name is required for the *Game:Id*.")]
        public string Name { get; set; }

        [JsonProperty]
        [Description(
            "From allCardsUrl, CGS downloads the json that contains the Card data for the game. If CGS is able to successfully download this file, it will save it as AllCards.json.")]
        public Uri AllCardsUrl { get; set; }

        [JsonProperty]
        [Description(
            "If allCardsUrlPageCount > 1, CGS will download <allCardsUrl> with <allCardsUrlPageIdentifier>+<page> for each page.")]
        [DefaultValue(1)]
        public int AllCardsUrlPageCount { get; set; } = 1;

        [JsonProperty]
        [Description(
            "If allCardsUrlPageCountIdentifier is set, CGS will set the allCardsUrlPageCount to the response value of <allCardsUrlPageCountIdentifier> from <allCardsUrl>.")]
        public string AllCardsUrlPageCountIdentifier { get; set; } = "";

        [JsonProperty]
        [Description(
            "allCardsUrlPageCountDivisor can be set to the # of cards per page, ie: allCardsUrlPageCount = <allCardsUrlPageCountIdentifier pointing to total # of cards>/<allCardsUrlPageCountDivisor>.")]
        [DefaultValue(1)]
        public int AllCardsUrlPageCountDivisor { get; set; } = 1;

        [JsonProperty]
        [Description("allCardsUrlPageCountStartIndex is used to identify the first page of allCardsUrlPageCount.")]
        [DefaultValue(1)]
        public int AllCardsUrlPageCountStartIndex { get; set; } = 1;

        [JsonProperty]
        [Description(
            "If allCardsUrlPageCount > 1, CGS will download <allCardsUrl> with <allCardsUrlPageIdentifier>+<page> for each page.")]
        [DefaultValue("?page=")]
        public string AllCardsUrlPageIdentifier { get; set; } = "?page=";

        [JsonProperty]
        [Description(
            "If allCardsUrlPostBodyContent is set, CGS will make a POST to <allCardsUrl> with a JSON body that contains <allCardsUrlPostBodyContent>. If not set, CGS will just GET from <allCardsUrl>.")]
        public string AllCardsUrlPostBodyContent { get; set; } = "";

        [JsonProperty]
        [Description("allCardsUrlRequestHeader and allCardsUrlRequestHeader can be used together for secured APIs.")]
        [DefaultValue("X-Api-Key")]
        public string AllCardsUrlRequestHeader { get; set; } = "X-Api-Key";

        [JsonProperty]
        [Description("allCardsUrlRequestHeader and allCardsUrlRequestHeader can be used together for secured APIs.")]
        public string AllCardsUrlRequestHeaderValue { get; set; } = "";

        [JsonProperty]
        [Description(
            "If allCardsUrl points to file(s) enclosed by extra characters, set allCardsUrlWrapped to true, and CGS will trim the first and last characters.")]
        public bool AllCardsUrlWrapped { get; set; }

        [JsonProperty]
        [Description(
            "If allCardsUrl points to zipped file(s), set allCardsUrlZipped to true, and CGS will unzip the file(s).")]
        public bool AllCardsUrlZipped { get; set; }

        [JsonProperty]
        [Description(
            "From allDecksUrl, CGS downloads the json that contains the Deck data for the game. If CGS is able to successfully download this file, it will save it as AllDecks.json.")]
        public Uri AllDecksUrl { get; set; }

        [JsonProperty]
        [Description(
            "If allDecksUrlDataIdentifier is set to a non-empty string, AllDecks.json will be parsed as a JSON object: {\"@allDecksUrlDataIdentifier\":{\"$ref\":\"AllDecks.json\"}}")]
        public string AllDecksUrlDataIdentifier { get; set; } = "";

        [JsonProperty]
        [Description(
            "If allDecksUrlPostBodyContent is set, CGS will make a POST to <allDecksUrl> with a JSON body that contains <allDecksUrlPostBodyContent>. If not set, CGS will just GET from <allDecksUrl>.")]
        public string AllDecksUrlPostBodyContent { get; set; } = "";

        [JsonProperty]
        [Description("If allDecksUrlTxtRoot is set, CGS will parse deck urls as <allDecksUrlTxtRoot>+*DeckUrl:Txt*")]
        public string AllDecksUrlTxtRoot { get; set; } = "";

        [JsonProperty]
        [Description(
            "From allSetsUrl, CGS downloads the json that contains the Set data for the game. If CGS is able to successfully download this json, it will save it as AllSets.json.")]
        public Uri AllSetsUrl { get; set; }

        [JsonProperty]
        [Description(
            "If allSetsUrl points to a file enclosed by extra characters, set allSetsUrlWrapped to true, and CGS will trim the first and last characters.")]
        public bool AllSetsUrlWrapped { get; set; }

        [JsonProperty]
        [Description(
            "If allSetsUrl points to a zipped file, set allSetsUrlZipped to true, and CGS will unzip the file.")]
        public bool AllSetsUrlZipped { get; set; }

        [JsonProperty]
        [Description(
            "autoUpdate indicates how many days to use cached files instead of re-downloading. autoUpdate=0 will re-download files at every opportunity. autoUpdate<0 will never attempt to download anything.")]
        [DefaultValue(30)]
        public int AutoUpdate { get; set; } = 30;

        [JsonProperty]
        [Description(
            "autoUpdateUrl indicates the url from which users download *Game:Name*.json, and CGS will automatically re-download the custom game from this url every <autoUpdate> days. This url is used in the *Game:Id*. You should host *Game:Name*.json at this url, but if you do not, you can set autoUpdate to -1, and there should be no issues.")]
        public Uri AutoUpdateUrl { get; set; }

        [JsonProperty]
        [Description(
            "bannerImageFileType is the file type extension for the image file that CGS downloads from bannerImageUrl.")]
        [DefaultValue("png")]
        public string BannerImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description(
            "If bannerImageUrl is a valid url, CGS will download the image at that url and save it as Banner.<bannerImageFileType>. CGS will attempt to display the Banner.<bannerImageFileType> as an identifier to the user. If it is unable to read Banner.<bannerImageFileType>, CGS will simply display the CGS logo.")]
        public Uri BannerImageUrl { get; set; }

        [JsonProperty]
        [Description(
            "cardBackImageFileType is the file type extension for the image file that CGS downloads from cardBackImageUrl.")]
        [DefaultValue("png")]
        public string CardBackImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description(
            "If cardBackImageUrl is a valid url, CGS will download the image at that url and save it as CardBack.<cardBackImageFileType>. CGS will display the CardBack.<cardBackImageFileType> when the user turns a card facedown or if CGS is unable to find the appropriate card image. If CGS is unable to get a custom card back, CGS will use the default CGS card back.")]
        public Uri CardBackImageUrl { get; set; }

        [JsonProperty]
        [Description(
            "If cardDataIdentifier is set to a non-empty string, AllCards.json will be parsed as a JSON object: {\"@cardDataIdentifier\":{\"$ref\":\"AllCards.json\"}}")]
        public string CardDataIdentifier { get; set; } = "";

        [JsonProperty]
        [Description(
            "Every Card must have a unique id. When defining a Card in AllCards.json or AllSets.json, you can have the *Card:Id* mapped to the field defined by cardIdIdentifier. Most custom games will likely want to keep the default cardIdIdentifier.")]
        [DefaultValue("id")]
        public string CardIdIdentifier { get; set; } = "id";

        [JsonProperty]
        [Description(
            "Every Card must have a unique id. When defining a Card in AllCards.json or AllSets.json, you can have the *Card:Id* mapped to the field defined by cardIdIdentifier. Most custom games will likely want to keep the default cardIdIdentifier. If cardIdStop is set, any id that contains cardIdStop will be stopped at <cardIdStop>.")]
        public string CardIdStop { get; set; } = "";

        [JsonProperty]
        [Description(
            "cardImageFileType is the file type extension for the image files that CGS downloads for each individual Card.")]
        [DefaultValue("png")]
        public string CardImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description(
            "cardImageProperty is the *Card:Property* which points to the image for this Card. If <cardImageProperty> is empty, <cardImageUrl> will be used instead.")]
        public string CardImageProperty { get; set; } = "";

        [JsonProperty]
        [Description(
            "cardImageUrl is a parameterized template url from which CGS downloads card image files if <cardImageProperty> is empty. Parameters: {cardId}=*Card:Id*, {cardName}=*Card:Name*, {cardSet}=*Card:SetCode*, {cardImageFileType}=<cardImageFileType>, {<property>}=*Card:<property>*. Example: https://www.cardgamesimulator.com/games/Standard/sets/{cardSet}/{cardId}.{cardImageFileType}")]
        public string CardImageUrl { get; set; } = "";

        [JsonProperty]
        [Description(
            "When defining a Card in AllCards.json or AllSets.json, you can have the *Card:Name* mapped to the field defined by cardNameIdentifier. Most custom games will likely want to keep the default cardNameIdentifier.")]
        [DefaultValue("name")]
        public string CardNameIdentifier { get; set; } = "name";

        [JsonProperty]
        [Description(
            "If cardNameIsUnique is true, different Cards are not allowed to have the same *Card:Name*. Cards with the same name will be treated as reprints, with the option to hide reprints available. If cardNameIsUnique is false, DeckFileType.Txt will require <deckFileTxtId> for every Card.")]
        [DefaultValue(true)]
        public bool CardNameIsUnique { get; set; } = true;

        [JsonProperty]
        [Description(
            "The cardPrimaryProperty is the *Card:Property* that is first selected and displayed in the Card Viewer, which appears whenever a user selects a card.")]
        public string CardPrimaryProperty { get; set; } = "";

        [JsonProperty]
        [Description(
            "cardProperties defines the name keys for *Card:Property*s. The values should be mapped in AllCards.json or AllSets.json.")]
        public List<PropertyDef> CardProperties { get; set; } = new List<PropertyDef>();

        [JsonProperty]
        [Description(
            "When defining a Card in AllCards.json or AllSets.json, you can integrate objectEnum and objectEnumList properties with enums by using cardPropertyIdentifier. Most custom games will likely want to keep the default cardPropertyIdentifier.")]
        [DefaultValue("id")]
        public string CardPropertyIdentifier { get; set; } = "id";

        [JsonProperty]
        [Description(
            "When defining a Card in AllCards.json, you can have the *Card:SetCode* mapped to the field defined by cardSetIdentifier. If the mapping is missing, CGS will use <setCodeDefault>. Most custom games will likely want to keep the default cardSetIdentifier.")]
        [DefaultValue("set")]
        public string CardSetIdentifier { get; set; } = "set";

        [JsonProperty]
        [Description(
            "If cardSetIsObject is set to true, <cardSetIdentifier> should point to an object (or list of objects) that follows the rules for AllSets.json.")]
        public bool CardSetIsObject { get; set; }

        [JsonProperty]
        [Description(
            "When defining a Card in AllCards.json, you can have the *Card:SetCode* mapped to the field defined by cardSetIdentifier. That Set's name can be defined by cardSetNameIdentifier.")]
        [DefaultValue("setname")]
        public string CardSetNameIdentifier { get; set; } = "setname";

        [JsonProperty]
        [Description("If cardSetInList is set to true, Cards will be duplicated for each Set in <cardSetIdentifier>.")]
        public bool CardSetsInList { get; set; }

        [JsonProperty]
        [Description(
            "If cardSetsInListIsCsv is set to true, Cards will be duplicated for each Set found in the comma-separated list of <cardSetIdentifier>.")]
        public bool CardSetsInListIsCsv { get; set; }

        [JsonProperty]
        [Description("cardSize indicates a card's width and height in inches.")]
        [DefaultValue("(x: 2.5, y: 3.5)")]
        public Float2 CardSize { get; set; } = new Float2(2.5f, 3.5f);

        [JsonProperty]
        [Description(
            "cgsDeepLink is a clickable url that will take the user directly to this game in CGS, which can be shared between users. This functionality must be configured through Branch.io.")]
        public Uri CgsDeepLink { get; set; }

        [JsonProperty]
        [Description(
            "When saving or loading a deck with <deckFileType> NOT txt, deckFileAltId refers to the *Card:Property* used to uniquely identify each Card. For hsd, this is stored as a varint within the deck string.")]
        [DefaultValue("dbfId")]
        public string DeckFileAltId { get; set; } = "dbfId";

        [JsonProperty]
        [Description(
            "When saving a deck as txt, different Cards may share the same name, and if they do, the *Card:<deckFileTxtId>* will be used to uniquely identify Cards.")]
        [DefaultValue("set")]
        public DeckFileTxtId DeckFileTxtId { get; set; } = DeckFileTxtId.Set;

        [JsonProperty]
        [Description(
            "When saving a deck, the formatting for how it is saved and loaded is defined by the deckFileType. dec refers to the old MTGO deck file format. hsd refers to the Hearthstone deck string format. ydk refers to the YGOPRO deck file format. txt parses each line for the following: <Quantity> [*Card:Id*] *Card:Name* (*Card:SetCode*).")]
        [DefaultValue("txt")]
        public DeckFileType DeckFileType { get; set; } = DeckFileType.Txt;

        [JsonProperty]
        [Description(
            "For networked games, CGS will use deckSharePreference to: ask players if they want to share the same deck, force all players to share the same deck, or force an individual deck for each player.")]
        [DefaultValue("share")]
        public SharePreference DeckSharePreference { get; set; } = SharePreference.Share;

        [JsonProperty]
        [Description(
            "CGS will go through each DeckUrl and save the data from *DeckUrl:Url* to 'decks/*DeckUrl:Name*.<deckFileType>'")]
        public List<DeckUrl> DeckUrls { get; set; } = new List<DeckUrl>();

        [JsonProperty]
        [Description(
            "The value is displayed to the user through the UI while the keys remain hidden. If the keys are entered as a hexadecimal integers (prefixed with 0x), multiple values can go through bitwise and/ors to have a single enumValue represent multiple values. The multiple values would be displayed together to the user, using | as the delimiter.")]
        public List<EnumDef> Enums { get; set; } = new List<EnumDef>();

        [JsonProperty]
        [Description(
            "Describes extra cards separate from the main deck: The hsd deckFileType treats all extra cards as Heroes, and the ydk deckFileType treats all extra cards as extra deck cards.")]
        public List<ExtraDef> Extras { get; set; } = new List<ExtraDef>();

        [JsonProperty]
        [Description(
            "gameBoardImageFileType is the file type extension for the image files that CGS downloads for each game board.")]
        [DefaultValue("png")]
        public string GameBoardImageFileType { get; set; } = "png";

        [JsonProperty] public List<GameBoardCard> GameBoardCards { get; set; } = new List<GameBoardCard>();

        [JsonProperty]
        [Description(
            "CGS will go through each GameBoardUrl and save the data from *GameBoardUrl:Url* to 'boards/*GameBoardUrl:Id*.<gameBoardImageFileType>'")]
        public List<GameBoardUrl> GameBoardUrls { get; set; } = new List<GameBoardUrl>();

        [JsonProperty]
        [Description("gameCardRotationDegrees indicates how many degrees to rotate Cards in Play Mode.")]
        [DefaultValue(90)]
        public int GameCardRotationDegrees { get; set; } = 90;

        [JsonProperty]
        [Description(
            "If possible, CGS will take the gameDefaultCardAction when a Card is double-clicked in Play Mode.")]
        [DefaultValue("flip")]
        public CardAction GameDefaultCardAction { get; set; } = CardAction.Flip;

        [JsonProperty]
        [Description(
            "gamePlayDeckName is the name of the card stack shown when a player loads a deck. If <gamePlayDeckName> is empty, the *Deck:Name* is used.")]
        public string GamePlayDeckName { get; set; }

        [JsonProperty]
        [Description(
            "gameStartHandCount indicates how many cards are automatically dealt from the deck to the hand, when a user loads a deck in Play Mode.")]
        public int GameStartHandCount { get; set; }

        [JsonProperty]
        [Description(
            "gameStartPointsCount indicates how many points are assigned to each player, when that player loads a deck in Play Mode.")]
        public int GameStartPointsCount { get; set; }

        [JsonProperty]
        [Description(
            "playMatImageFileType is the file type extension for the image file that CGS downloads from playMatImageUrl.")]
        [DefaultValue("png")]
        public string PlayMatImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description(
            "If playMatImageUrl is a valid url, CGS will download the image at that url and save it as PlayMat.<playMatImageFileType>. CGS will use the PlayMat.<playMatImageFileType> as the background image while in Play Mode. If CGS is unable to get this image, CGS will use the default table image.")]
        public Uri PlayMatImageUrl { get; set; }

        [JsonProperty]
        [Description("playMatSize indicates the width and height in inches of the play area in Play Mode.")]
        [DefaultValue("(x: 36, y: 36)")]
        public Float2 PlayMatSize { get; set; } = new Float2(36f, 36f);

        [JsonProperty]
        [Description(
            "playMatGridCellSize indicates the width and height in inches of each cell in the play area in Play Mode.")]
        [DefaultValue("(x: 0.5, y: 0.5)")]
        public Float2 PlayMatGridCellSize { get; set; } = new Float2(0.5f, 0.5f);

        [JsonProperty]
        [Description("rulesUrl should link to this game's online rulebook.")]
        public Uri RulesUrl { get; set; }

        [JsonProperty]
        [Description(
            "When defining a Set in AllSets.json, you can also define Cards to include in that Set by indicating them with setCardsIdentifier. Most custom games will likely want to keep the default setCardsIdentifier.")]
        [DefaultValue("cards")]
        public string SetCardsIdentifier { get; set; } = "cards";

        [JsonProperty]
        [Description(
            "When defining a Set in AllSets.json, you can also define Cards to include in that Set by indicating them with SetCardsUrlIdentifier. Most custom games will likely want to keep the default SetCardsUrlIdentifier.")]
        [DefaultValue("cardsUrl")]
        public string SetCardsUrlIdentifier { get; set; } = "cardsUrl";

        [JsonProperty]
        [Description(
            "If a Card does not specify its Set, it will be included in the Set with *Set:Code* specified by setCodeDefault. This Set's name is specified by setNameDefault.")]
        [DefaultValue("_CGSDEFAULT_")]
        public string SetCodeDefault { get; set; } = Set.DefaultCode;

        [JsonProperty]
        [Description(
            "When defining a Set in AllSets.json, you can have the *Set:Code* mapped to the field defined by setCodeIdentifier. Most custom games will likely want to keep the default setCodeIdentifier.")]
        [DefaultValue("code")]
        public string SetCodeIdentifier { get; set; } = "code";

        [JsonProperty]
        [Description(
            "If setDataIdentifier is set to a non-empty string, AllSets.json will be parsed as a JSON object: {\"@setDataIdentifier\":{\"$ref\":\"AllSets.json\"}}")]
        public string SetDataIdentifier { get; set; } = "";

        [JsonProperty]
        [Description(
            "If a Card does not specify its Set, it will be included in the Set with *Set:Code* specified by setCodeDefault. This Set's name is specified by setNameDefault.")]
        [DefaultValue("_CGSDEFAULT_")]
        public string SetNameDefault { get; set; } = Set.DefaultName;

        [JsonProperty]
        [Description(
            "When defining a Set in AllSets.json, you can have the *Set:Name* mapped to the field defined by setNameIdentifier. If the mapping is missing, CGS will use the *Set:Code*. Most custom games will likely want to keep the default setNameIdentifier.")]
        [DefaultValue("name")]
        public string SetNameIdentifier { get; set; } = "name";

        public static (string name, string host) GetNameAndHost(string id)
        {
            var name = string.IsNullOrEmpty(id) ? DefaultName : id;
            var hostIndex = name.LastIndexOf('@');
            if (hostIndex <= 0)
                return (name, null);

            var host = name.Substring(hostIndex + 1);
            name = name.Substring(0, hostIndex);
            return (name, host);
        }

        public CardGame(string id = DefaultName, string autoUpdateUrl = "")
        {
            var (name, host) = GetNameAndHost(id);
            Name = name;
            if (Uri.IsWellFormedUriString(autoUpdateUrl, UriKind.Absolute))
                AutoUpdateUrl = new Uri(autoUpdateUrl);
            else if (Uri.CheckHostName(host) != UriHostNameType.Unknown)
                AutoUpdateUrl = new Uri("https://" + host);
            else
                _id = id;
        }

        protected void RefreshId()
        {
            _id = Name + Host;
        }

        public bool IsEnumProperty(string propertyName)
        {
            return Enums.Where(def => def.Property.Equals(propertyName)).ToList().Count > 0;
        }
    }
}
