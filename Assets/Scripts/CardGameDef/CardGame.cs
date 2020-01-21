/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CardGameDef
{
    public delegate void LoadJTokenDelegate(JToken jToken, string defaultValue);
    public delegate IEnumerator CardGameCoroutineDelegate(CardGame cardGame);

    [JsonObject(MemberSerialization.OptIn)]
    public class CardGame
    {
        public const string BannerImageFileName = "Banner";
        public const string CardBackImageFileName = "CardBack";
        public const string DefaultName = "_INVALID_";

        public static JsonSerializerSettings SerializerSettings
        {
            get
            {
                return new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
            }
        }

        public static readonly CardGame Invalid = new CardGame(null);

        public static string GamesDirectoryPath => UnityEngine.Application.persistentDataPath + "/games";

        public string GameDirectoryPath => GamesDirectoryPath + "/" + UnityExtensionMethods.GetSafeFileName(Id);
        public string GameFilePath => GameDirectoryPath + "/" + UnityExtensionMethods.GetSafeFileName(Name) + ".json";
        public string CardsFilePath => GameDirectoryPath + "/AllCards.json";
        public string SetsFilePath => GameDirectoryPath + "/AllSets.json";
        public string BannerImageFilePath => GameDirectoryPath + "/" + BannerImageFileName + "." + UnityExtensionMethods.GetSafeFileName(BannerImageFileType);
        public string CardBackImageFilePath => GameDirectoryPath + "/" + CardBackImageFileName + "." + UnityExtensionMethods.GetSafeFileName(CardBackImageFileType);
        public string DecksFilePath => GameDirectoryPath + "/decks";
        public string GameBoardsFilePath => GameDirectoryPath + "/boards";

        public float CardAspectRatio => CardSize.y > 0 ? UnityEngine.Mathf.Abs(CardSize.x / CardSize.y) : 0.715f;
        public IReadOnlyDictionary<string, Card> Cards => LoadedCards;
        public IReadOnlyDictionary<string, Set> Sets => LoadedSets;

        // *Game:Id* = *Game:Name*@<base64(*Game:AutoUpdateUrl*)>
        // Since urls must be unique, this id will also be unique and human-recognizable
        public string Id => Name + EncodedUrl;
        public string EncodedUrl => (AutoUpdateUrl != null && AutoUpdateUrl.IsWellFormedOriginalString()) ?
            "@" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(AutoUpdateUrl.OriginalString)) : "";
        public bool IsExternal => (AutoUpdateUrl != null && AutoUpdateUrl.IsWellFormedOriginalString())
            || (AllCardsUrl != null && AllCardsUrl.IsWellFormedOriginalString())
            || (AllSetsUrl != null && AllSetsUrl.IsWellFormedOriginalString());

        [JsonProperty]
        [JsonRequired]
        [Description("The name of the custom card game as it appears to the user. This name is required for the *Game:Id*.")]
        public string Name { get; set; }

        [JsonProperty]
        [Description("From allCardsUrl, CGS downloads the json that contains the Card data for the game. If CGS is able to successfully download this file, it will save it as AllCards.json.")]
        public Uri AllCardsUrl { get; set; }

        [JsonProperty]
        [Description("If allCardsUrlPageCount > 1, CGS will download <allCardsUrl> with <allCardsUrlPageIdentifier>+<page> for each page.")]
        [DefaultValue(1)]
        public int AllCardsUrlPageCount { get; set; } = 1;

        [JsonProperty]
        [Description("If allCardsUrlPageCountIdentifier is set, CGS will set the allCardsUrlPageCount to the response value of <allCardsUrlPageCountIdentifier> from <allCardsUrl>.")]
        public string AllCardsUrlPageCountIdentifier { get; set; } = "";

        [JsonProperty]
        [Description("allCardsUrlPageCountDivisor can be set to the # of cards per page, ie: allCardsUrlPageCount = <allCardsUrlPageCountIdentifier pointing to total # of cards>/<allCardsUrlPageCountDivisor>.")]
        [DefaultValue(1)]
        public int AllCardsUrlPageCountDivisor { get; set; } = 1;

        [JsonProperty]
        [Description("allCardsUrlPageCountStartIndex is used to identify the first page of allCardsUrlPageCount.")]
        [DefaultValue(1)]
        public int AllCardsUrlPageCountStartIndex { get; set; } = 1;

        [JsonProperty]
        [Description("If allCardsUrlPageCount > 1, CGS will download <allCardsUrl> with <allCardsUrlPageIdentifier>+<page> for each page.")]
        [DefaultValue("?page=")]
        public string AllCardsUrlPageIdentifier { get; set; } = "?page=";

        [JsonProperty]
        [Description("If allCardsUrlPostBodyContent is set, CGS will make a POST to <allCardsUrl> with a JSON body that contains <allCardsUrlPostBodyContent>. If not set, CGS will just GET from <allCardsUrl>.")]
        public string AllCardsUrlPostBodyContent { get; set; } = "";

        [JsonProperty]
        [Description("If allCardsUrl points to file(s) enclosed by extra characters, set allCardsUrlWrapped to true, and CGS will trim the first and last characters.")]
        public bool AllCardsUrlWrapped { get; set; }

        [JsonProperty]
        [Description("If allCardsUrl points to zipped file(s), set allCardsUrlZipped to true, and CGS will unzip the file(s).")]
        public bool AllCardsUrlZipped { get; set; }

        [JsonProperty]
        [Description("From allSetsUrl, CGS downloads the json that contains the Set data for the game. If CGS is able to successfully download this json, it will save it as AllSets.json.")]
        public Uri AllSetsUrl { get; set; }

        [JsonProperty]
        [Description("If allSetsUrl points to a file enclosed by extra characters, set allSetsUrlWrapped to true, and CGS will trim the first and last characters.")]
        public bool AllSetsUrlWrapped { get; set; }

        [JsonProperty]
        [Description("If allSetsUrl points to a zipped file, set allSetsUrlZipped to true, and CGS will unzip the file.")]
        public bool AllSetsUrlZipped { get; set; }

        [JsonProperty]
        [Description("autoUpdate indicates how many days to use cached files instead of re-downloading. autoUpdate=0 will re-download files at every opportunity. autoUpdate<0 will never attempt to download anything.")]
        [DefaultValue(30)]
        public int AutoUpdate { get; set; } = 30;

        [JsonProperty]
        [Description("autoUpdateUrl indicates the url from which users download *Game:Name*.json, and CGS will automatically re-download the custom game from this url every <autoUpdate> days. This url is used in the *Game:Id*. You should host *Game:Name*.json at this url, but if you do not, you can set autoUpdate to -1, and there should be no issues.")]
        public Uri AutoUpdateUrl { get; set; }

        [JsonProperty]
        [Description("bannerImageFileType is the file type extension for the image file that CGS downloads from bannerImageUrl.")]
        [DefaultValue("png")]
        public string BannerImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description("If bannerImageUrl is a valid url, CGS will download the image at that url and save it as Banner.<bannerImageFileType>. CGS will attempt to display the Banner.<bannerImageFileType> as an identifier to the user. If it is unable to read Banner.<bannerImageFileType>, CGS will simply display the CGS logo.")]
        public Uri BannerImageUrl { get; set; }

        [JsonProperty]
        [Description("cardBackImageFileType is the file type extension for the image file that CGS downloads from cardBackImageUrl.")]
        [DefaultValue("png")]
        public string CardBackImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description("If cardBackImageUrl is a valid url, CGS will download the image at that url and save it as CardBack.<cardBackImageFileType>. CGS will display the CardBack.<cardBackImageFileType> when the user turns a card facedown or if CGS is unable to find the appropriate card image. If CGS is unable to get a custom card back, CGS will use the default CGS card back.")]
        public Uri CardBackImageUrl { get; set; }

        [JsonProperty]
        [Description("If cardDataIdentifier is set to a non-empty string, AllCards.json will be parsed as a JSON object: {\"@cardDataIdentifier\":{\"$ref\":\"AllCards.json\"}}")]
        public string CardDataIdentifier { get; set; } = "";

        [JsonProperty]
        [Description("Every Card must have a unique id. When defining a Card in AllCards.json or AllSets.json, you can have the *Card:Id* mapped to the field defined by cardIdIdentifier. Most custom games will likely want to keep the default cardIdIdentifier.")]
        [DefaultValue("id")]
        public string CardIdIdentifier { get; set; } = "id";

        [JsonProperty]
        [Description("Every Card must have a unique id. When defining a Card in AllCards.json or AllSets.json, you can have the *Card:Id* mapped to the field defined by cardIdIdentifier. Most custom games will likely want to keep the default cardIdIdentifier. If cardIdStop is set, any id that contains cardIdStop will be stopped at <cardIdStop>.")]
        public string CardIdStop { get; set; } = "";

        [JsonProperty]
        [Description("cardImageFileType is the file type extension for the image files that CGS downloads for each individual Card.")]
        [DefaultValue("png")]
        public string CardImageFileType { get; set; } = "png";

        [JsonProperty]
        [Description("cardImageProperty is the *Card:Property* which points to the image for this Card. If <cardImageProperty> is empty, <cardImageUrl> will be used instead.")]
        public string CardImageProperty { get; set; } = "";

        [JsonProperty]
        [Description("cardImageUrl is a parameterized template url from which CGS downloads card image files if <cardImageProperty> is empty. Parameters: {cardId}=*Card:Id*, {cardName}=*Card:Name*, {cardSet}=*Card:SetCode*, {cardImageFileType}=<cardImageFileType>, {<property>}=*Card:<property>*. Example: https://www.cardgamesimulator.com/games/Standard/sets/{cardSet}/{cardId}.{cardImageFileType}")]
        public string CardImageUrl { get; set; } = "";

        [JsonProperty]
        [Description("When defining a Card in AllCards.json or AllSets.json, you can have the *Card:Name* mapped to the field defined by cardNameIdentifier. Most custom games will likely want to keep the default cardNameIdentifier.")]
        [DefaultValue("name")]
        public string CardNameIdentifier { get; set; } = "name";

        [JsonProperty]
        [Description("If cardNameIsUnique is true, different Cards are not allowed to have the same *Card:Name*. Cards with the same name will be treated as reprints, with the option to hide reprints available. If cardNameIsUnique false, DeckFileType.Txt will require <deckFileTxtId> for every Card.")]
        [DefaultValue(true)]
        public bool CardNameIsUnique { get; set; } = true;

        [JsonProperty]
        [Description("The cardPrimaryProperty is the *Card:Property* that is first selected and displayed in the Card Viewer, which appears whenever a user selects a card.")]
        public string CardPrimaryProperty { get; set; } = "";

        [JsonProperty]
        [Description("cardProperties defines the name keys for *Card:Property*s. The values should be mapped in AllCards.json or AllSets.json.")]
        public List<PropertyDef> CardProperties { get; set; } = new List<PropertyDef>();

        [JsonProperty]
        [Description("When defining a Card in AllCards.json or AllSets.json, you can integrate objectEnum and objectEnumList properties with enums by using cardPropertyIdentifier. Most custom games will likely want to keep the default cardPropertyIdentifier.")]
        [DefaultValue("id")]
        public string CardPropertyIdentifier { get; set; } = "id";

        [JsonProperty]
        [Description("When defining a Card in AllCards.json, you can have the *Card:SetCode* mapped to the field defined by cardSetIdentifier. If the mapping is missing, CGS will use <setCodeDefault>. Most custom games will likely want to keep the default cardSetIdentifier.")]
        [DefaultValue("set")]
        public string CardSetIdentifier { get; set; } = "set";

        [JsonProperty]
        [Description("If cardSetIsObject is set to true, <cardSetIdentifier> should point to an object (or list of objects) that follows the rules for AllSets.json.")]
        public bool CardSetIsObject { get; set; }

        [JsonProperty]
        [Description("When defining a Card in AllCards.json, you can have the *Card:SetCode* mapped to the field defined by cardSetIdentifier. That Set's name can be defined by cardSetNameIdentifier.")]
        [DefaultValue("setname")]
        public string CardSetNameIdentifier { get; set; } = "setname";

        [JsonProperty]
        [Description("If cardSetInList is set to true, Cards will be duplicated for each Set in <cardSetIdentifier>.")]
        public bool CardSetsInList { get; set; }

        [JsonProperty]
        [Description("If cardSetsInListIsCsv is set to true, Cards will be duplicated for each Set found in the comma-separated list of <cardSetIdentifier>.")]
        public bool CardSetsInListIsCsv { get; set; }

        [JsonProperty]
        [Description("cardSize indicates a card's width and height in inches.")]
        [DefaultValue("(x: 2.5, y: 3.5)")]
        public UnityEngine.Vector2 CardSize { get; set; } = new UnityEngine.Vector2(2.5f, 3.5f);

        [JsonProperty]
        [Description("When saving or loading a deck with <deckFileType> NOT txt, deckFileAltId refers to the *Card:Property* used to uniquely identify each Card. For hsd, this is stored as a varint within the deck string.")]
        [DefaultValue("dbfId")]
        public string DeckFileAltId { get; set; } = "dbfId";

        [JsonProperty]
        [Description("When saving a deck as txt, different Cards may share the same name, and if they do, the *Card:<deckFileTxtId>* will be used to uniquely identify Cards.")]
        [DefaultValue("set")]
        public DeckFileTxtId DeckFileTxtId { get; set; } = DeckFileTxtId.Set;

        [JsonProperty]
        [Description("When saving a deck, the formatting for how it is saved and loaded is defined by the deckFileType. dec refers to the old MTGO deck file format. hsd refers to the Hearthstone deck string format. ydk refers to the YGOPRO deck file format. txt parses each line for the following: <Quantity> [*Card:Id*] *Card:Name* (*Card:SetCode*).")]
        [DefaultValue("txt")]
        public DeckFileType DeckFileType { get; set; } = DeckFileType.Txt;

        [JsonProperty]
        [Description("deckMaxCount is used to decide how many card slots should appear in the deck editor.")]
        [DefaultValue(75)]
        public int DeckMaxCount { get; set; } = 75;

        [JsonProperty]
        [Description("CGS will go through each DeckUrl and save the data from *DeckUrl:Url* to 'decks/*DeckUrl:Name*.<deckFileType>'")]
        public List<DeckUrl> DeckUrls { get; set; } = new List<DeckUrl>();

        [JsonProperty]
        [Description("The value is displayed to the user through the UI while the keys remain hidden. If the keys are entered as a hexadecimal integers (prefixed with 0x), multiple values can go through bitwise and/ors to have a single enumValue represent multiple values. The multiple values would be displayed together to the user, using | as the delimiter.")]
        public List<EnumDef> Enums { get; set; } = new List<EnumDef>();

        [JsonProperty]
        [Description("Describes extra cards separate from the main deck: The hsd deckFileType treats all extra cards as Heroes, and the ydk deckFileType treats all extra cards as extra deck cards.")]
        public List<ExtraDef> Extras { get; set; } = new List<ExtraDef>();

        [JsonProperty]
        [Description("gameBoardFileType is the file type extension for the image files that CGS downloads for each game board.")]
        [DefaultValue("png")]
        public string GameBoardFileType { get; set; } = "png";

        [JsonProperty]
        public List<GameBoardCard> GameBoardCards { get; set; } = new List<GameBoardCard>();

        [JsonProperty]
        [Description("CGS will go through each GameBoardUrl and save the data from *GameBoardUrl:Url* to 'boards/*GameBoardUrl:Id*.<gameBoardFileType>'")]
        public List<GameBoardUrl> GameBoardUrls { get; set; } = new List<GameBoardUrl>();

        [JsonProperty]
        [Description("gameStartHandCount indicates how many cards are automatically dealt from the deck to the hand, when a user loads a deck in Play Mode.")]
        public int GameStartHandCount { get; set; }

        [JsonProperty]
        [Description("gameStartPointsCount indicates how many points are assigned to each player, when that player loads a deck in Play Mode.")]
        public int GameStartPointsCount { get; set; }

        [JsonProperty]
        [Description("playAreaSize indicates the width and height in inches of the play area in Play Mode.")]
        [DefaultValue("(x: 36, y: 24)")]
        public UnityEngine.Vector2 PlayAreaSize { get; set; } = new UnityEngine.Vector2(36f, 24f);

        [JsonProperty]
        [Description("rulesUrl should link to this game's online rulebook.")]
        public Uri RulesUrl { get; set; }

        [JsonProperty]
        [Description("When defining a Set in AllSets.json, you can also define Cards to include in that Set by indicating them with setCardsIndentifier. Most custom games will likely want to keep the default setCardsIdentifier.")]
        [DefaultValue("cards")]
        public string SetCardsIdentifier { get; set; } = "cards";

        [JsonProperty]
        [Description("If a Card does not specify its Set, it will be included in the Set with *Set:Code* specified by setCodeDefault. This Set's name is specified by setNameDefault.")]
        [DefaultValue("_CGSDEFAULT_")]
        public string SetCodeDefault { get; set; } = Set.DefaultCode;

        [JsonProperty]
        [Description("When defining a Set in AllSets.json, you can have the *Set:Code* mapped to the field defined by setCodeIdentifier. Most custom games will likely want to keep the default setCodeIdentifier.")]
        [DefaultValue("code")]
        public string SetCodeIdentifier { get; set; } = "code";

        [JsonProperty]
        [Description("If setDataIdentifier is set to a non-empty string, AllSets.json will be parsed as a JSON object: {\"@setDataIdentifier\":{\"$ref\":\"AllSets.json\"}}")]
        public string SetDataIdentifier { get; set; } = "";

        [JsonProperty]
        [Description("If a Card does not specify its Set, it will be included in the Set with *Set:Code* specified by setCodeDefault. This Set's name is specified by setNameDefault.")]
        [DefaultValue("_CGSDEFAULT_")]
        public string SetNameDefault { get; set; } = Set.DefaultName;

        [JsonProperty]
        [Description("When defining a Set in AllSets.json, you can have the *Set:Name* mapped to the field defined by setNameIdentifier. If the mapping is missing, CGS will use the *Set:Code*. Most custom games will likely want to keep the default setNameIdentifier.")]
        [DefaultValue("name")]
        public string SetNameIdentifier { get; set; } = "name";

        public UnityEngine.MonoBehaviour CoroutineRunner { get; set; }
        public bool HasReadProperties { get; private set; }
        public bool IsDownloading { get; private set; }
        public float DownloadProgress { get; private set; }
        public string DownloadStatus { get; private set; } = "N / A";
        public bool HasDownloaded { get; private set; }
        public bool HasLoaded { get; private set; }
        public string Error { get; private set; }

        public HashSet<string> CardNames { get; } = new HashSet<string>();

        protected Dictionary<string, Card> LoadedCards { get; } = new Dictionary<string, Card>();
        protected Dictionary<string, Set> LoadedSets { get; } = new Dictionary<string, Set>(StringComparer.OrdinalIgnoreCase);

        public UnityEngine.Sprite BannerImageSprite
        {
            get { return _bannerImageSprite ?? (_bannerImageSprite = UnityEngine.Resources.Load<UnityEngine.Sprite>(BannerImageFileName)); }
            private set
            {
                if (_bannerImageSprite != null)
                {
                    UnityEngine.Object.Destroy(_bannerImageSprite.texture);
                    UnityEngine.Object.Destroy(_bannerImageSprite);
                }
                _bannerImageSprite = value;
            }
        }
        private UnityEngine.Sprite _bannerImageSprite;

        public UnityEngine.Sprite CardBackImageSprite
        {
            get { return _cardBackImageSprite ?? (_cardBackImageSprite = UnityEngine.Resources.Load<UnityEngine.Sprite>(CardBackImageFileName)); }
            private set
            {
                if (_cardBackImageSprite != null)
                {
                    UnityEngine.Object.Destroy(_cardBackImageSprite.texture);
                    UnityEngine.Object.Destroy(_cardBackImageSprite);
                }
                _cardBackImageSprite = value;
            }
        }
        private UnityEngine.Sprite _cardBackImageSprite;

        public static (string name, string url) Decode(string gameId)
        {
            string name = gameId;
            string url = string.Empty;
            int delimiterIdx = gameId.LastIndexOf('@');
            if (delimiterIdx != -1)
            {
                name = gameId.Substring(0, delimiterIdx);
                url = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(gameId.Substring(delimiterIdx + 1).Replace('_', '/')));
            }
            return (name, url);
        }

        public CardGame(UnityEngine.MonoBehaviour coroutineRunner, string name = DefaultName, string url = "")
        {
            CoroutineRunner = coroutineRunner;
            Name = name ?? DefaultName;
            AutoUpdateUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute) ? new Uri(url) : null;
            Error = string.Empty;
        }

        public bool IsEnumProperty(string propertyName)
        {
            return Enums.Where(def => def.Property.Equals(propertyName)).ToList().Count > 0;
        }

        public void ClearDefinitionLists()
        {
            CardProperties.Clear();
            DeckUrls.Clear();
            Enums.Clear();
            Extras.Clear();
            GameBoardCards.Clear();
            GameBoardUrls.Clear();
        }

        public void ReadProperties()
        {
            try
            {
                // We need to read the *Game:Name*.json file, but reading it can cause *Game:Name* to change, so account for that
                string gameFilePath = GameFilePath;
                string gameDirectoryPath = GameDirectoryPath;
                ClearDefinitionLists();
                JsonConvert.PopulateObject(File.ReadAllText(GameFilePath), this);
                if (!gameFilePath.Equals(GameFilePath) && File.Exists(gameFilePath))
                {
                    string tempGameFilePath = gameDirectoryPath + "/" + UnityExtensionMethods.GetSafeFileName(Name) + ".json";
                    File.Move(gameFilePath, tempGameFilePath);
                }
                if (!gameDirectoryPath.Equals(GameDirectoryPath) && Directory.Exists(gameDirectoryPath))
                    Directory.Move(gameDirectoryPath, GameDirectoryPath);

                // We're being greedy about loading these now, since these could be shown before the game is selected
                if (File.Exists(BannerImageFilePath))
                    BannerImageSprite = UnityExtensionMethods.CreateSprite(BannerImageFilePath);
                if (File.Exists(CardBackImageFilePath))
                    CardBackImageSprite = UnityExtensionMethods.CreateSprite(CardBackImageFilePath);

                HasReadProperties = true;
            }
            catch (Exception e)
            {
                Error += e.Message + e.StackTrace + Environment.NewLine;
                HasReadProperties = false;
            }
        }

        public IEnumerator Download()
        {
            if (IsDownloading)
                yield break;
            IsDownloading = true;

            // We should always first get the *Game:Name*.json file and read it before doing anything else
            DownloadProgress = 0f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: CardGameDef...";
            if (AutoUpdateUrl != null && AutoUpdateUrl.IsAbsoluteUri)
                yield return UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl.AbsoluteUri, GameFilePath);
            ReadProperties();
            if (!HasReadProperties)
            {
                // ReadProperties() should have already populated the Error
                IsDownloading = false;
                HasDownloaded = false;
                yield break;
            }

            DownloadProgress = 1f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Banner";
            if (BannerImageUrl != null && BannerImageUrl.IsAbsoluteUri)
                yield return UnityExtensionMethods.SaveUrlToFile(BannerImageUrl.AbsoluteUri, BannerImageFilePath);

            DownloadProgress = 2f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: CardBack";
            if (CardBackImageUrl != null && CardBackImageUrl.IsAbsoluteUri)
                yield return UnityExtensionMethods.SaveUrlToFile(CardBackImageUrl.AbsoluteUri, CardBackImageFilePath);

            DownloadProgress = 3f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Boards";
            foreach (GameBoardUrl boardUrl in GameBoardUrls)
                if (!string.IsNullOrEmpty(boardUrl.Id) && boardUrl.Url != null && boardUrl.Url.IsAbsoluteUri)
                    yield return UnityExtensionMethods.SaveUrlToFile(boardUrl.Url.AbsoluteUri, GameBoardsFilePath + "/" + boardUrl.Id + "." + GameBoardFileType);

            DownloadProgress = 4f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Decks";
            foreach (DeckUrl deckUrl in DeckUrls)
                if (!string.IsNullOrEmpty(deckUrl.Name) && deckUrl.Url != null && deckUrl.Url.IsAbsoluteUri)
                    yield return UnityExtensionMethods.SaveUrlToFile(deckUrl.Url.AbsoluteUri, DecksFilePath + "/" + deckUrl.Name + "." + DeckFileType);

            DownloadProgress = 5f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: AllSets.json";
            string setsFilePath = SetsFilePath + (AllSetsUrlZipped ? UnityExtensionMethods.ZipExtension : string.Empty);
            if (AllSetsUrl != null && AllSetsUrl.IsAbsoluteUri)
                yield return UnityExtensionMethods.SaveUrlToFile(AllSetsUrl.AbsoluteUri, setsFilePath);
            if (AllSetsUrlZipped)
                UnityExtensionMethods.ExtractZip(setsFilePath, GameDirectoryPath);
            if (AllSetsUrlWrapped)
                UnityExtensionMethods.UnwrapFile(SetsFilePath);

            if (AllCardsUrl != null && AllCardsUrl.IsWellFormedOriginalString())
            {
                for (int page = AllCardsUrlPageCountStartIndex; page < AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount; page++)
                {
                    DownloadProgress = (6f + page - AllCardsUrlPageCountStartIndex) / (7f + AllCardsUrlPageCount - AllCardsUrlPageCountStartIndex);
                    DownloadStatus = $"Downloading: Cards: {page,5} / {AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount}";
                    string cardsUrl = AllCardsUrl.OriginalString;
                    if (AllCardsUrlPageCount > 1 && string.IsNullOrEmpty(AllCardsUrlPostBodyContent))
                        cardsUrl += AllCardsUrlPageIdentifier + page;
                    string cardsFile = CardsFilePath;
                    if (page != AllCardsUrlPageCountStartIndex)
                        cardsFile += page.ToString();
                    if (AllCardsUrlZipped)
                        cardsFile += UnityExtensionMethods.ZipExtension;
                    string jsonBody = null;
                    if (!string.IsNullOrEmpty(AllCardsUrlPostBodyContent))
                    {
                        jsonBody = "{" + AllCardsUrlPostBodyContent;
                        if (AllCardsUrlPageCount > 1 || !string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier))
                            jsonBody += AllCardsUrlPageIdentifier + page;
                        jsonBody += "}";
                    }
                    yield return UnityExtensionMethods.SaveUrlToFile(cardsUrl, cardsFile, jsonBody);
                    if (AllCardsUrlZipped)
                        UnityExtensionMethods.ExtractZip(cardsFile, GameDirectoryPath);
                    if (AllCardsUrlWrapped)
                        UnityExtensionMethods.UnwrapFile(cardsFile.EndsWith(UnityExtensionMethods.ZipExtension)
                            ? cardsFile.Remove(cardsFile.Length - UnityExtensionMethods.ZipExtension.Length) : cardsFile);
                    // Sometimes, we need to get the AllCardsUrlPageCount from the first page of AllCardsUrl
                    if (page == AllCardsUrlPageCountStartIndex && !string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier))
                        LoadCards(page);
                }
            }

            IsDownloading = false;
            DownloadStatus = "Complete!";
            HasDownloaded = true;
            HasLoaded = false;
        }

        public void Load(CardGameCoroutineDelegate updateCoroutine, CardGameCoroutineDelegate loadCardsCoroutine)
        {
            // We should have already read the *Game:Name*.json, but we need to be sure
            if (!HasReadProperties)
            {
                ReadProperties();
                if (!HasReadProperties)
                {
                    // ReadProperties() should have already populated the Error
                    HasLoaded = false;
                    return;
                }
            }

            // Don't waste time loading if we need to update first
            bool shouldUpdate;
#if UNITY_WEBGL
            shouldUpdate = !HasDownloaded;
#else
            int daysSinceUpdate = 0;
            try { daysSinceUpdate = (int)DateTime.Today.Subtract(File.GetLastWriteTime(GameFilePath).Date).TotalDays; } catch { };
            shouldUpdate = AutoUpdate >= 0 && daysSinceUpdate >= AutoUpdate && CoroutineRunner != null;
#endif
            if (shouldUpdate)
            {
                CoroutineRunner.StartCoroutine(updateCoroutine(this));
                return;
            }

            // These enum lookups need to be initialized before we load cards and sets
            foreach (EnumDef enumDef in Enums)
                enumDef.InitializeLookups();

            // The main load action is to load cards and sets
            if (CoroutineRunner != null)
                CoroutineRunner.StartCoroutine(loadCardsCoroutine(this));
            LoadSets();

            // We also re-load the banner and cardback images now in case they've changed since we ReadProperties
            if (File.Exists(BannerImageFilePath))
                BannerImageSprite = UnityExtensionMethods.CreateSprite(BannerImageFilePath);
            if (File.Exists(CardBackImageFilePath))
                CardBackImageSprite = UnityExtensionMethods.CreateSprite(CardBackImageFilePath);

            // Only considered as loaded if none of the steps failed
            if (string.IsNullOrEmpty(Error))
                HasLoaded = true;
        }

        public void LoadCards(int page)
        {
            string cardsFilePath = CardsFilePath + (page != AllCardsUrlPageCountStartIndex ? page.ToString() : string.Empty);

            if (File.Exists(cardsFilePath))
                LoadJsonFromFile(cardsFilePath, LoadCardFromJToken, CardDataIdentifier);
            else
                UnityEngine.Debug.Log("LoadCards::NOAllCards.json");
        }

        public void LoadSets()
        {
            if (File.Exists(SetsFilePath))
                LoadJsonFromFile(SetsFilePath, LoadSetFromJToken, SetDataIdentifier);
            else
                UnityEngine.Debug.Log("LoadSets::NOAllSets.json");

            if (LoadedSets.TryGetValue(SetCodeDefault, out Set defaultSet))
                defaultSet.Name = SetNameDefault;
        }

        public void LoadJsonFromFile(string file, LoadJTokenDelegate load, string dataId)
        {
            if (!File.Exists(file))
            {
                UnityEngine.Debug.LogError("LoadJsonFromFile::NoFile");
                return;
            }

            try
            {
                JToken root = JToken.Parse(File.ReadAllText(file));

                IJEnumerable<JToken> dataContainer;
                if (!string.IsNullOrEmpty(dataId))
                {
                    JToken childProcessor = root;
                    foreach (String childName in dataId.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                        (childProcessor as JObject)?.TryGetValue(childName, out childProcessor);
                    dataContainer = childProcessor;
                }
                else
                    dataContainer = root as JArray ?? (IJEnumerable<JToken>)((JObject)root).PropertyValues();

                foreach (JToken jToken in dataContainer)
                    load(jToken, SetCodeDefault);

                if (!string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier) && root.Value<int>(AllCardsUrlPageCountIdentifier) > 0)
                {
                    AllCardsUrlPageCount = root.Value<int>(AllCardsUrlPageCountIdentifier);
                    if (AllCardsUrlPageCountDivisor > 0)
                        AllCardsUrlPageCount = UnityEngine.Mathf.CeilToInt(((float)AllCardsUrlPageCount) / AllCardsUrlPageCountDivisor);
                }
            }
            catch (Exception e)
            {
                Error += e.Message + e.StackTrace + Environment.NewLine;
                HasLoaded = false;
            }
        }

        public void LoadCardFromJToken(JToken cardJToken, string defaultSetCode)
        {
            if (cardJToken == null)
            {
                UnityEngine.Debug.LogError("LoadCardFromJToken::NullCardJToken");
                return;
            }

            Dictionary<string, PropertyDefValuePair> metaProperties = new Dictionary<string, PropertyDefValuePair>();
            PropertyDef idDef = new PropertyDef(CardIdIdentifier, PropertyType.String);
            PopulateCardProperty(metaProperties, cardJToken, idDef, idDef.Name);
            string cardId = string.Empty;
            if (metaProperties.TryGetValue(CardIdIdentifier, out PropertyDefValuePair cardIdEntry))
            {
                cardId = cardIdEntry.Value;
                if (string.IsNullOrEmpty(cardId))
                {
                    UnityEngine.Debug.LogWarning("LoadCardFromJToken::MissingCardId");
                    return;
                }
                if (!string.IsNullOrEmpty(CardIdStop))
                    cardId = cardId.Split(CardIdStop[0])[0];
            }
            else
            {
                UnityEngine.Debug.LogWarning("LoadCardFromJToken::ParseIdError");
                return;
            }

            PropertyDef nameDef = new PropertyDef(CardNameIdentifier, PropertyType.String);
            PopulateCardProperty(metaProperties, cardJToken, nameDef, nameDef.Name);
            string cardName = string.Empty;
            if (metaProperties.TryGetValue(CardNameIdentifier, out PropertyDefValuePair cardNameEntry))
                cardName = cardNameEntry.Value ?? string.Empty;
            else
                UnityEngine.Debug.LogWarning("LoadCardFromJToken::ParseNameError");

            Dictionary<string, PropertyDefValuePair> cardProperties = new Dictionary<string, PropertyDefValuePair>();
            PopulateCardProperties(cardProperties, cardJToken, CardProperties);

            Dictionary<string, string> cardSets = new Dictionary<string, string>();
            PopulateCardSets(cardSets, cardJToken, defaultSetCode);

            string cardImageWebUrl = string.Empty;
            if (!string.IsNullOrEmpty(CardImageProperty))
            {
                PropertyDef imageDef = new PropertyDef(CardImageProperty, PropertyType.String);
                PopulateCardProperty(metaProperties, cardJToken, imageDef, imageDef.Name);
                if (metaProperties.TryGetValue(CardImageProperty, out PropertyDefValuePair cardImageEntry))
                    cardImageWebUrl = cardImageEntry.Value ?? string.Empty;
            }

            string cardImageUrl = CardImageUrl;
            if (string.IsNullOrEmpty(CardImageProperty) || !string.IsNullOrEmpty(cardImageWebUrl) || !string.IsNullOrEmpty(cardImageUrl))
            {
                foreach (var set in cardSets)
                {
                    bool isReprint = CardNameIsUnique && CardNames.Contains(cardName);
                    if (!isReprint)
                        CardNames.Add(cardName);
                    string cardDuplicateId = cardSets.Count > 1 && isReprint
                        ? (cardId + PropertyDef.ObjectDelimiter + set.Key) : cardId;
                    Card newCard = new Card(this, cardDuplicateId, cardName, set.Key, cardProperties, isReprint);
                    newCard.ImageWebUrl = cardImageWebUrl;
                    LoadedCards[newCard.Id] = newCard;
                    if (!Sets.ContainsKey(set.Key))
                        LoadedSets[set.Key] = new Set(set.Key, set.Value);
                }
            }
            else
                UnityEngine.Debug.Log("LoadCardFromJToken::MissingCardImage");
        }

        public void PopulateCardProperties(Dictionary<string, PropertyDefValuePair> cardProperties, JToken cardJToken, List<PropertyDef> propertyDefs, string keyPrefix = "")
        {
            if (cardProperties == null || cardJToken == null || propertyDefs == null)
            {
                UnityEngine.Debug.LogError($"PopulateCardProperties::NullInput:{cardProperties}:{propertyDefs}:{cardJToken}");
                return;
            }

            foreach (PropertyDef property in propertyDefs)
                PopulateCardProperty(cardProperties, cardJToken, property, keyPrefix + property.Name);
        }

        public void PopulateCardProperty(Dictionary<string, PropertyDefValuePair> cardProperties, JToken cardJToken, PropertyDef property, string key)
        {
            if (cardProperties == null || cardJToken == null || property == null)
            {
                UnityEngine.Debug.LogError($"PopulateCardProperty::MissingInput:{cardProperties}:{cardJToken}:{property}");
                return;
            }

            try
            {
                PropertyDefValuePair newProperty = new PropertyDefValuePair() { Def = property };
                string listValue = string.Empty;
                JObject jObject = null;
                switch (property.Type)
                {
                    case PropertyType.ObjectEnumList:
                        listValue = string.Empty;
                        foreach (JToken jToken in cardJToken[property.Name])
                        {
                            if (!string.IsNullOrEmpty(listValue))
                                listValue += EnumDef.Delimiter;
                            jObject = jToken as JObject;
                            listValue += jObject?.Value<string>(CardPropertyIdentifier) ?? string.Empty;
                        }
                        newProperty.Value = listValue;
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.ObjectList:
                        foreach (PropertyDef childProperty in property.Properties)
                        {
                            newProperty = new PropertyDefValuePair() { Def = childProperty };
                            listValue = string.Empty;
                            Dictionary<string, PropertyDefValuePair> values = new Dictionary<string, PropertyDefValuePair>();
                            int i = 0;
                            foreach (JToken jToken in cardJToken[property.Name])
                            {
                                PopulateCardProperty(values, jToken, childProperty, key + childProperty.Name + i);
                                i++;
                            }
                            foreach (var entry in values)
                            {
                                if (!string.IsNullOrEmpty(listValue))
                                    listValue += EnumDef.Delimiter;
                                listValue += entry.Value.Value.Replace(EnumDef.Delimiter, ", ");
                            }
                            newProperty.Value = listValue;
                            cardProperties[key + PropertyDef.ObjectDelimiter + childProperty.Name] = newProperty;
                        }
                        break;
                    case PropertyType.ObjectEnum:
                        jObject = cardJToken[property.Name] as JObject;
                        newProperty.Value = jObject.Value<string>(CardPropertyIdentifier) ?? string.Empty;
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.Object:
                        jObject = cardJToken[property.Name] as JObject;
                        if (jObject != null && jObject.HasValues)
                            PopulateCardProperties(cardProperties, cardJToken[property.Name], property.Properties, key + PropertyDef.ObjectDelimiter);
                        else
                            PopulateEmptyCardProperty(cardProperties, property, key);
                        break;
                    case PropertyType.StringEnumList:
                    case PropertyType.StringList:
                        listValue = string.Empty;
                        if (string.IsNullOrEmpty(property.Delimiter))
                        {
                            foreach (JToken jToken in cardJToken[property.Name])
                            {
                                if (!string.IsNullOrEmpty(listValue))
                                    listValue += EnumDef.Delimiter;
                                listValue += jToken.Value<string>() ?? string.Empty;
                            }
                        }
                        else
                        {
                            foreach (string token in (cardJToken.Value<string>(property.Name) ?? string.Empty).Split(new[] { property.Delimiter }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (!string.IsNullOrEmpty(listValue))
                                    listValue += EnumDef.Delimiter;
                                listValue += token;
                            }
                        }
                        newProperty.Value = listValue;
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.EscapedString:
                        newProperty.Value = (cardJToken.Value<string>(property.Name) ?? string.Empty)
                            .Replace(PropertyDef.EscapeCharacter, string.Empty);
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.StringEnum:
                    case PropertyType.Boolean:
                    case PropertyType.Integer:
                    case PropertyType.String:
                    default:
                        newProperty.Value = cardJToken.Value<string>(property.Name) ?? string.Empty;
                        cardProperties[key] = newProperty;
                        break;
                }
            }
            catch
            {
                PopulateEmptyCardProperty(cardProperties, property, key);
            }
        }

        private void PopulateEmptyCardProperty(Dictionary<string, PropertyDefValuePair> cardProperties, PropertyDef property, string key)
        {
            cardProperties[key] = new PropertyDefValuePair() { Def = property, Value = string.Empty };
            foreach (PropertyDef childProperty in property.Properties)
                PopulateEmptyCardProperty(cardProperties, childProperty, key + PropertyDef.ObjectDelimiter + childProperty.Name);
        }

        public void PopulateCardSets(Dictionary<string, string> cardSets, JToken cardJToken, string defaultSetCode)
        {
            if (cardSets == null || cardJToken == null || string.IsNullOrEmpty(defaultSetCode))
            {
                UnityEngine.Debug.LogError($"PopulateCardSets::MissingInput:{cardSets}:{defaultSetCode}:{cardJToken}");
                return;
            }

            string dataIdentifier = CardSetIdentifier;
            if (dataIdentifier.Contains('.'))
            {
                JToken childProcessor = cardJToken;
                string[] parentNames = CardSetIdentifier.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parentNames.Count() - 1; i++)
                    (childProcessor as JObject)?.TryGetValue(parentNames[i], out childProcessor);
                cardJToken = childProcessor;
                dataIdentifier = parentNames[parentNames.Count() - 1];
            }

            if (CardSetsInListIsCsv)
            {
                string codesCsv = cardJToken.Value<string>(dataIdentifier) ?? defaultSetCode;
                string namesCsv = cardJToken.Value<string>(CardSetNameIdentifier) ?? codesCsv;
                string[] codes = codesCsv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string[] names = namesCsv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < codes.Length; i++)
                {
                    string code = codes[i];
                    string name = i < names.Length ? names[i] : code;
                    cardSets[code] = name;
                }
            }
            else if (CardSetsInList)
            {
                List<JToken> setJTokens = new List<JToken>();
                try { setJTokens = (cardJToken[dataIdentifier] as JArray)?.ToList() ?? new List<JToken>(); }
                catch { UnityEngine.Debug.LogWarning($"PopulateCardSets::BadCardSetIdentifier for {cardJToken.ToString()}"); }
                foreach (JToken setJToken in setJTokens)
                {
                    if (CardSetIsObject)
                    {
                        JObject setObject = setJToken as JObject;
                        string setCode = setObject?.Value<string>(SetCodeIdentifier) ?? defaultSetCode;
                        string setName = setObject?.Value<string>(SetNameIdentifier) ?? setCode;
                        cardSets[setCode] = setName;
                    }
                    else
                    {
                        string code = setJToken.Value<string>(dataIdentifier) ?? defaultSetCode;
                        string name = setJToken.Value<string>(CardSetNameIdentifier) ?? code;
                        cardSets[code] = name;
                    }
                }
            }
            else if (CardSetIsObject)
            {
                JObject setObject = null;
                try { setObject = cardJToken[dataIdentifier] as JObject; }
                catch { UnityEngine.Debug.LogWarning($"PopulateCardSets::BadCardSetIdentifier for {cardJToken.ToString()}"); }
                string setCode = setObject?.Value<string>(SetCodeIdentifier) ?? defaultSetCode;
                string setName = setObject?.Value<string>(SetNameIdentifier) ?? setCode;
                cardSets[setCode] = setName;
            }
            else
            {
                string code = cardJToken.Value<string>(dataIdentifier) ?? defaultSetCode;
                string name = cardJToken.Value<string>(CardSetNameIdentifier) ?? code;
                cardSets[code] = name;
            }
        }

        public void LoadSetFromJToken(JToken setJToken, string defaultSetCode)
        {
            if (setJToken == null)
            {
                UnityEngine.Debug.LogError("LoadSetFromJToken::NullSetJToken");
                return;
            }

            string setCode = setJToken.Value<string>(SetCodeIdentifier);
            if (string.IsNullOrEmpty(setCode))
            {
                UnityEngine.Debug.LogError("LoadSetFromJToken::EmptySetCode");
                return;
            }
            string setName = setJToken.Value<string>(SetNameIdentifier) ?? setCode;

            LoadedSets[setCode] = new Set(setCode, setName);

            JArray cards = setJToken.Value<JArray>(SetCardsIdentifier);
            if (cards != null)
                foreach (JToken jToken in cards)
                    LoadCardFromJToken(jToken, setCode);
        }

        public void Add(Card card)
        {
            bool isReprint = CardNameIsUnique && CardNames.Contains(card.Name);
            if (!isReprint)
                CardNames.Add(card.Name);
            LoadedCards[card.Id] = card;

            if (!Sets.ContainsKey(card.SetCode))
                LoadedSets[card.SetCode] = new Set(card.SetCode, card.SetCode);

            WriteAllCardsJson();
        }

        private void WriteAllCardsJson()
        {
            string allCardsJson = JsonConvert.SerializeObject(Cards.Values.ToList(), SerializerSettings);
            File.WriteAllText(CardsFilePath, allCardsJson);
        }

        public void Remove(Card card)
        {
            if (card != null && card.Id != null)
                Remove(card.Id);
        }

        private void Remove(string cardId)
        {
            LoadedCards.Remove(cardId);
            WriteAllCardsJson();
        }

        public IEnumerable<Card> FilterCards(CardSearchFilters filters)
        {
            if (filters == null)
            {
                UnityEngine.Debug.LogError("FilterCards::NullFilters");
                yield break;
            }

            foreach (Card card in Cards.Values)
            {
                if (!string.IsNullOrEmpty(filters.Name) && !filters.Name.ToLower().Split(new[] { CardSearchFilters.Delimiter },
                        StringSplitOptions.RemoveEmptyEntries).All(card.Name.ToLower().Contains))
                    continue;
                if (!string.IsNullOrEmpty(filters.Id) && !card.Id.ToLower().Contains(filters.Id.ToLower()))
                    continue;
                if (!string.IsNullOrEmpty(filters.SetCode) && !card.SetCode.ToLower().Contains(filters.SetCode.ToLower()))
                    continue;
                bool propsMatch = true;
                foreach (KeyValuePair<string, string> entry in filters.StringProperties)
                    if (!card.GetPropertyValueString(entry.Key).ToLower().Contains(entry.Value.ToLower()))
                        propsMatch = false;
                foreach (KeyValuePair<string, int> entry in filters.IntMinProperties)
                    if (card.GetPropertyValueInt(entry.Key) < entry.Value)
                        propsMatch = false;
                foreach (KeyValuePair<string, int> entry in filters.IntMaxProperties)
                    if (card.GetPropertyValueInt(entry.Key) > entry.Value)
                        propsMatch = false;
                foreach (KeyValuePair<string, bool> entry in filters.BoolProperties)
                    if (card.GetPropertyValueBool(entry.Key) != entry.Value)
                        propsMatch = false;
                foreach (KeyValuePair<string, int> entry in filters.EnumProperties)
                {
                    EnumDef enumDef = Enums.FirstOrDefault(def => def.Property.Equals(entry.Key));
                    if (enumDef == null)
                    {
                        propsMatch = false;
                        continue;
                    }
                    if ((card.GetPropertyValueEnum(entry.Key) & entry.Value) == 0)
                        propsMatch = propsMatch && (entry.Value == (1 << enumDef.Values.Count)) && CardProperties.FirstOrDefault(prop
                                         => prop.Name.Equals(entry.Key)).DisplayEmpty.Equals(card.GetPropertyValueString(entry.Key));
                }
                if (propsMatch)
                    yield return card;
            }
        }

        public void ClearError()
        {
            Error = string.Empty;
        }
    }
}
