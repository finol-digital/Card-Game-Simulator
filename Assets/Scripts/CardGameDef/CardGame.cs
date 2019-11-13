/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
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
        public string EncodedUrl => !string.IsNullOrEmpty(AutoUpdateUrl) ?
            "@" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(AutoUpdateUrl)) : "";
        public bool IsExternal => !string.IsNullOrEmpty(AutoUpdateUrl) || !string.IsNullOrEmpty(AllCardsUrl) || !string.IsNullOrEmpty(AllSetsUrl);

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string AllCardsUrl { get; set; } = "";

        [JsonProperty]
        public int AllCardsUrlPageCount { get; set; } = 1;

        [JsonProperty]
        public string AllCardsUrlPageCountIdentifier { get; set; } = "";

        [JsonProperty]
        public int AllCardsUrlPageCountDivisor { get; set; } = 1;

        [JsonProperty]
        public int AllCardsUrlPageCountStartIndex { get; set; } = 1;

        [JsonProperty]
        public string AllCardsUrlPageIdentifier { get; set; } = "?page=";

        [JsonProperty]
        public string AllCardsUrlPostBodyContent { get; set; } = "";

        [JsonProperty]
        public bool AllCardsUrlWrapped { get; set; }

        [JsonProperty]
        public bool AllCardsUrlZipped { get; set; }

        [JsonProperty]
        public string AllSetsUrl { get; set; } = "";

        [JsonProperty]
        public bool AllSetsUrlWrapped { get; set; }

        [JsonProperty]
        public bool AllSetsUrlZipped { get; set; }

        [JsonProperty]
        public int AutoUpdate { get; set; } = 30;

        [JsonProperty]
        public string AutoUpdateUrl { get; set; }

        [JsonProperty]
        public string BannerImageFileType { get; set; } = "png";

        [JsonProperty]
        public string BannerImageUrl { get; set; } = "";

        [JsonProperty]
        public string CardBackImageFileType { get; set; } = "png";

        [JsonProperty]
        public string CardBackImageUrl { get; set; } = "";

        [JsonProperty]
        public bool CardClearsBackground { get; set; }

        [JsonProperty]
        public string CardDataIdentifier { get; set; } = "";

        [JsonProperty]
        public string CardIdIdentifier { get; set; } = "id";

        [JsonProperty]
        public string CardIdStop { get; set; } = "";

        [JsonProperty]
        public string CardImageFileType { get; set; } = "png";

        [JsonProperty]
        public string CardImageProperty { get; set; } = "";

        [JsonProperty]
        public string CardImageUrl { get; set; } = "";

        [JsonProperty]
        public string CardNameIdentifier { get; set; } = "name";

        [JsonProperty]
        public bool CardNameIsUnique { get; set; } = true;

        [JsonProperty]
        public string CardPrimaryProperty { get; set; } = "";

        [JsonProperty]
        public List<PropertyDef> CardProperties { get; set; } = new List<PropertyDef>();

        [JsonProperty]
        public string CardPropertyIdentifier { get; set; } = "id";

        [JsonProperty]
        public string CardSetIdentifier { get; set; } = "set";

        [JsonProperty]
        public bool CardSetIsObject { get; set; }

        [JsonProperty]
        public string CardSetNameIdentifier { get; set; } = "setname";

        [JsonProperty]
        public bool CardSetsInList { get; set; }

        [JsonProperty]
        public bool CardSetsInListIsCsv { get; set; }

        [JsonProperty]
        public UnityEngine.Vector2 CardSize { get; set; } = new UnityEngine.Vector2(2.5f, 3.5f);

        [JsonProperty]
        public string DeckFileAltId { get; set; } = "dbfId";

        [JsonProperty]
        public DeckFileTxtId DeckFileTxtId { get; set; } = DeckFileTxtId.Set;

        [JsonProperty]
        public DeckFileType DeckFileType { get; set; } = DeckFileType.Txt;

        [JsonProperty]
        public int DeckMaxCount { get; set; } = 75;

        [JsonProperty]
        public List<DeckUrl> DeckUrls { get; set; } = new List<DeckUrl>();

        [JsonProperty]
        public List<EnumDef> Enums { get; set; } = new List<EnumDef>();

        [JsonProperty]
        public List<ExtraDef> Extras { get; set; } = new List<ExtraDef>();

        [JsonProperty]
        public string GameBoardFileType { get; set; } = "png";

        [JsonProperty]
        public List<GameBoardCard> GameBoardCards { get; set; } = new List<GameBoardCard>();

        [JsonProperty]
        public List<GameBoardUrl> GameBoardUrls { get; set; } = new List<GameBoardUrl>();

        [JsonProperty]
        public bool GameCatchesDiscard { get; set; } = true;

        [JsonProperty]
        public bool GameHasDiscardZone { get; set; }

        [JsonProperty]
        public int GameStartHandCount { get; set; }

        [JsonProperty]
        public int GameStartPointsCount { get; set; }

        [JsonProperty]
        public UnityEngine.Vector2 PlayAreaSize { get; set; } = new UnityEngine.Vector2(36f, 24f);

        [JsonProperty]
        public string RulesUrl { get; set; } = "";

        [JsonProperty]
        public string SetCardsIdentifier { get; set; } = "cards";

        [JsonProperty]
        public string SetCodeDefault { get; set; } = Set.DefaultCode;

        [JsonProperty]
        public string SetCodeIdentifier { get; set; } = "code";

        [JsonProperty]
        public string SetDataIdentifier { get; set; } = "";

        [JsonProperty]
        public string SetNameDefault { get; set; } = Set.DefaultName;

        [JsonProperty]
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
            AutoUpdateUrl = url ?? string.Empty;
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
            if (!string.IsNullOrEmpty(AutoUpdateUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl, GameFilePath);
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
            if (!string.IsNullOrEmpty(BannerImageUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(BannerImageUrl, BannerImageFilePath);

            DownloadProgress = 2f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: CardBack";
            if (!string.IsNullOrEmpty(CardBackImageUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(CardBackImageUrl, CardBackImageFilePath);

            DownloadProgress = 3f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Boards";
            foreach (GameBoardUrl boardUrl in GameBoardUrls)
                yield return UnityExtensionMethods.SaveUrlToFile(boardUrl.Url, GameBoardsFilePath + "/" + boardUrl.Id + "." + GameBoardFileType);

            DownloadProgress = 4f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Decks";
            foreach (DeckUrl deckUrl in DeckUrls)
                yield return UnityExtensionMethods.SaveUrlToFile(deckUrl.Url, DecksFilePath + "/" + deckUrl.Name + "." + DeckFileType);

            DownloadProgress = 5f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: AllSets.json";
            string setsFilePath = SetsFilePath + (AllSetsUrlZipped ? UnityExtensionMethods.ZipExtension : string.Empty);
            if (!string.IsNullOrEmpty(AllSetsUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(AllSetsUrl, setsFilePath);
            if (AllSetsUrlZipped)
                UnityExtensionMethods.ExtractZip(setsFilePath, GameDirectoryPath);
            if (AllSetsUrlWrapped)
                UnityExtensionMethods.UnwrapFile(SetsFilePath);

            if (!string.IsNullOrEmpty(AllCardsUrl))
            {
                for (int page = AllCardsUrlPageCountStartIndex; page < AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount; page++)
                {
                    DownloadProgress = (6f + page - AllCardsUrlPageCountStartIndex) / (7f + AllCardsUrlPageCount - AllCardsUrlPageCountStartIndex);
                    DownloadStatus = $"Downloading: Cards: {page,5} / {AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount}";
                    string cardsUrl = AllCardsUrl;
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

            if (string.IsNullOrEmpty(CardImageProperty) || !string.IsNullOrEmpty(cardImageWebUrl) || !string.IsNullOrEmpty(CardImageUrl))
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
            var defaultContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() };
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = defaultContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            string allCardsJson = JsonConvert.SerializeObject(Cards.Values.ToList(), jsonSerializerSettings);
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
