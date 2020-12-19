/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CardGameDef.Unity
{
    public delegate void LoadJTokenDelegate(JToken jToken, string defaultValue);

    public delegate IEnumerator CardGameCoroutineDelegate(UnityCardGame cardGame);

    public class UnityCardGame : CardGame
    {
        public static UnityCardGame UnityInvalid => new UnityCardGame(null);

        public static string GamesDirectoryPath => Application.persistentDataPath + "/games";

        public string GameDirectoryPath => Path.Combine(GamesDirectoryPath, UnityFileMethods.GetSafeFileName(Id));

        public string GameFilePath => Path.Combine(GameDirectoryPath,
            UnityFileMethods.GetSafeFileName(Name) + UnityFileMethods.JsonExtension);

        public string CardsFilePath => GameDirectoryPath + "/AllCards.json";
        public string DecksFilePath => GameDirectoryPath + "/AllDecks.json";
        public string SetsFilePath => GameDirectoryPath + "/AllSets.json";

        public string BannerImageFilePath => GameDirectoryPath + "/Banner." +
                                             UnityFileMethods.GetSafeFileName(BannerImageFileType);

        public string CardBackImageFilePath => GameDirectoryPath + "/CardBack." +
                                               UnityFileMethods.GetSafeFileName(CardBackImageFileType);

        public string PlayMatImageFilePath => GameDirectoryPath + "/PlayMat." +
                                              UnityFileMethods.GetSafeFileName(PlayMatImageFileType);

        public string DecksDirectoryPath => GameDirectoryPath + "/decks";
        public string GameBoardsDirectoryPath => GameDirectoryPath + "/boards";
        public string SetsDirectoryPath => GameDirectoryPath + "/sets";

        public float CardAspectRatio => CardSize.Y > 0 ? Mathf.Abs(CardSize.X / CardSize.Y) : 0.715f;
        public IReadOnlyDictionary<string, UnityCard> Cards => LoadedCards;
        public IReadOnlyDictionary<string, Set> Sets => LoadedSets;


        public MonoBehaviour CoroutineRunner { get; set; }
        public bool HasReadProperties { get; private set; }
        public bool IsDownloading { get; private set; }
        public float DownloadProgress { get; private set; }
        public string DownloadStatus { get; private set; } = "N / A";
        public bool HasDownloaded { get; private set; }
        public bool HasLoaded { get; private set; }
        public string Error { get; private set; }

        public HashSet<string> CardNames { get; } = new HashSet<string>();

        protected Dictionary<string, UnityCard> LoadedCards { get; } = new Dictionary<string, UnityCard>();

        protected Dictionary<string, Set> LoadedSets { get; } =
            new Dictionary<string, Set>(StringComparer.OrdinalIgnoreCase);

        public Sprite BannerImageSprite
        {
            get => _bannerImageSprite
                ? _bannerImageSprite
                : _bannerImageSprite = Resources.Load<Sprite>("Banner");
            private set
            {
                if (_bannerImageSprite != null)
                {
                    Object.Destroy(_bannerImageSprite.texture);
                    Object.Destroy(_bannerImageSprite);
                }

                _bannerImageSprite = value;
            }
        }

        private Sprite _bannerImageSprite;

        public Sprite CardBackImageSprite
        {
            get => _cardBackImageSprite
                ? _cardBackImageSprite
                : _cardBackImageSprite = Resources.Load<Sprite>("CardBack");
            private set
            {
                if (_cardBackImageSprite != null)
                {
                    Object.Destroy(_cardBackImageSprite.texture);
                    Object.Destroy(_cardBackImageSprite);
                }

                _cardBackImageSprite = value;
            }
        }

        private Sprite _cardBackImageSprite;

        public Sprite PlayMatImageSprite
        {
            get => _playMatImageSprite
                ? _playMatImageSprite
                : _playMatImageSprite = Resources.Load<Sprite>("Table");
            private set
            {
                if (_playMatImageSprite != null)
                {
                    Object.Destroy(_playMatImageSprite.texture);
                    Object.Destroy(_playMatImageSprite);
                }

                _playMatImageSprite = value;
            }
        }

        private Sprite _playMatImageSprite;

        public UnityCardGame(MonoBehaviour coroutineRunner, string id = DefaultName, string autoUpdateUrl = "")
            : base(id, autoUpdateUrl)
        {
            CoroutineRunner = coroutineRunner;
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
                // We need to read the *Game:Name*.json file, but reading it can cause *Game:Name/Id* to change, so account for that
                string gameFilePath = GameFilePath;
                string gameDirectoryPath = GameDirectoryPath;
                ClearDefinitionLists();
                JsonConvert.PopulateObject(File.ReadAllText(GameFilePath), this);
                RefreshId();
                if (!gameFilePath.Equals(GameFilePath) && File.Exists(gameFilePath))
                {
                    string tempGameFilePath =
                        Path.Combine(gameDirectoryPath,
                            UnityFileMethods.GetSafeFileName(Name) + UnityFileMethods.JsonExtension);
                    File.Move(gameFilePath, tempGameFilePath);
                }

                if (!gameDirectoryPath.Equals(GameDirectoryPath) && Directory.Exists(gameDirectoryPath))
                    Directory.Move(gameDirectoryPath, GameDirectoryPath);

                // We're being greedy about loading these now, since these could be shown before the game is selected
                if (File.Exists(BannerImageFilePath))
                    BannerImageSprite = UnityFileMethods.CreateSprite(BannerImageFilePath);
                if (File.Exists(CardBackImageFilePath))
                    CardBackImageSprite = UnityFileMethods.CreateSprite(CardBackImageFilePath);

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
                yield return UnityFileMethods.SaveUrlToFile(AutoUpdateUrl.AbsoluteUri, GameFilePath);
            ReadProperties();
            if (!HasReadProperties)
            {
                // ReadProperties() should have already populated the Error
                IsDownloading = false;
                HasDownloaded = false;
                yield break;
            }

            DownloadProgress = 1f / (8f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Banner";
            if (BannerImageUrl != null && BannerImageUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(BannerImageUrl.AbsoluteUri, BannerImageFilePath);

            DownloadProgress = 2f / (8f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: CardBack";
            if (CardBackImageUrl != null && CardBackImageUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(CardBackImageUrl.AbsoluteUri, CardBackImageFilePath);

            DownloadProgress = 3f / (8f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: PlayMat";
            if (PlayMatImageUrl != null && PlayMatImageUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(PlayMatImageUrl.AbsoluteUri, PlayMatImageFilePath);

            DownloadProgress = 4f / (8f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Boards";
            foreach (GameBoardUrl boardUrl in GameBoardUrls)
                if (!string.IsNullOrEmpty(boardUrl.Id) && boardUrl.Url != null && boardUrl.Url.IsAbsoluteUri)
                    yield return UnityFileMethods.SaveUrlToFile(boardUrl.Url.AbsoluteUri,
                        GameBoardsDirectoryPath + "/" + boardUrl.Id + "." + GameBoardImageFileType);

            DownloadProgress = 5f / (8f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Decks";
            string deckRequestBody = null;
            if (!string.IsNullOrEmpty(AllDecksUrlPostBodyContent))
                deckRequestBody = "{" + AllDecksUrlPostBodyContent + "}";
            if (AllDecksUrl != null && AllDecksUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(AllDecksUrl.AbsoluteUri, DecksFilePath,
                    deckRequestBody);
            if (File.Exists(DecksFilePath))
            {
                try
                {
                    JToken root = JToken.Parse(File.ReadAllText(DecksFilePath));
                    JArray dataContainer;
                    if (!string.IsNullOrEmpty(AllDecksUrlDataIdentifier))
                    {
                        JToken childProcessor = root;
                        foreach (string childName in AllDecksUrlDataIdentifier.Split(new[] {'.'},
                            StringSplitOptions.RemoveEmptyEntries))
                            (childProcessor as JObject)?.TryGetValue(childName, out childProcessor);
                        dataContainer = childProcessor as JArray;
                    }
                    else
                        dataContainer = root as JArray;

                    if (dataContainer != null)
                        DeckUrls.AddRange(dataContainer.ToObject<List<DeckUrl>>() ?? new List<DeckUrl>());
                    else
                        Debug.Log("Empty AllDecks.json");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Unable to read AllDecks.json! {e}");
                }
            }

            foreach (DeckUrl deckUrl in DeckUrls)
            {
                if (string.IsNullOrEmpty(deckUrl.Name))
                {
                    Debug.LogWarning($"Ignoring deckUrl with empty name {deckUrl}!");
                    continue;
                }

                string deckFilePath = DecksDirectoryPath + "/" + deckUrl.Name + "." + DeckFileType.ToString().ToLower();
                if (!string.IsNullOrEmpty(AllDecksUrlTxtRoot) && !string.IsNullOrEmpty(deckUrl.Txt))
                    yield return UnityFileMethods.SaveUrlToFile(AllDecksUrlTxtRoot + deckUrl.Txt, deckFilePath);
                else if (deckUrl.Url != null && deckUrl.Url.IsAbsoluteUri)
                    yield return UnityFileMethods.SaveUrlToFile(deckUrl.Url.AbsoluteUri, deckFilePath);
                else
                    Debug.Log($"Empty url for deckUrl {deckUrl}");
            }

            DownloadProgress = 6f / (8f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: AllSets.json";
            string setsFilePath = SetsFilePath + (AllSetsUrlZipped ? UnityFileMethods.ZipExtension : string.Empty);
            if (AllSetsUrl != null && AllSetsUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(AllSetsUrl.AbsoluteUri, setsFilePath);
            if (AllSetsUrlZipped)
                UnityFileMethods.ExtractZip(setsFilePath, GameDirectoryPath);
            if (AllSetsUrlWrapped)
                UnityFileMethods.UnwrapFile(SetsFilePath);

            if (AllCardsUrl != null && AllCardsUrl.IsWellFormedOriginalString())
            {
                for (int page = AllCardsUrlPageCountStartIndex;
                    page < AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount;
                    page++)
                {
                    DownloadProgress = (7f + page - AllCardsUrlPageCountStartIndex) /
                                       (8f + AllCardsUrlPageCount - AllCardsUrlPageCountStartIndex);
                    DownloadStatus =
                        $"Downloading: Cards: {page,5} / {AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount}";
                    string cardsUrl = AllCardsUrl.OriginalString;
                    if (AllCardsUrlPageCount > 1 && string.IsNullOrEmpty(AllCardsUrlPostBodyContent))
                        cardsUrl += AllCardsUrlPageIdentifier + page;
                    string cardsFile = CardsFilePath;
                    if (page != AllCardsUrlPageCountStartIndex)
                        cardsFile += page.ToString();
                    if (AllCardsUrlZipped)
                        cardsFile += UnityFileMethods.ZipExtension;
                    string jsonBody = null;
                    if (!string.IsNullOrEmpty(AllCardsUrlPostBodyContent))
                    {
                        jsonBody = "{" + AllCardsUrlPostBodyContent;
                        if (AllCardsUrlPageCount > 1 || !string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier))
                            jsonBody += AllCardsUrlPageIdentifier + page;
                        jsonBody += "}";
                    }

                    Dictionary<string, string> responseHeaders = new Dictionary<string, string>();
                    yield return UnityFileMethods.SaveUrlToFile(cardsUrl, cardsFile, jsonBody, responseHeaders);
                    if (AllCardsUrlZipped)
                        UnityFileMethods.ExtractZip(cardsFile, GameDirectoryPath);
                    if (AllCardsUrlWrapped)
                        UnityFileMethods.UnwrapFile(cardsFile.EndsWith(UnityFileMethods.ZipExtension)
                            ? cardsFile.Remove(cardsFile.Length - UnityFileMethods.ZipExtension.Length)
                            : cardsFile);

                    // Sometimes, we need to get the AllCardsUrlPageCount from the first page of AllCardsUrl
                    if (page != AllCardsUrlPageCountStartIndex ||
                        string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier)) continue;

                    // Get it from the response header if we can
                    if (responseHeaders.TryGetValue(AllCardsUrlPageCountIdentifier, out string pageCount) &&
                        int.TryParse(pageCount, out int pageCountInt))
                        AllCardsUrlPageCount = Mathf.CeilToInt(pageCountInt / (float) AllCardsUrlPageCountDivisor);
                    else // Or load it from the json if we have to
                        LoadCards(page);
                }
            }

            IsDownloading = false;
            DownloadStatus = "Complete!";
            HasDownloaded = true;
            HasLoaded = false;
        }

        public void Load(CardGameCoroutineDelegate updateCoroutine, CardGameCoroutineDelegate loadCardsCoroutine,
            CardGameCoroutineDelegate loadSetCardsCoroutine)
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
            var daysSinceUpdate = 0;
            try
            {
                daysSinceUpdate = (int) DateTime.Today.Subtract(File.GetLastWriteTime(GameFilePath).Date).TotalDays;
            }
            catch
            {
                Debug.Log($"Unable to determine last update date for {Name}. Assuming today.");
            }

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
            if (CoroutineRunner != null && LoadedSets.Values.Any(set => !string.IsNullOrEmpty(set.CardsUrl)))
                CoroutineRunner.StartCoroutine(loadSetCardsCoroutine(this));

            // We also re-load the banner and cardBack images now in case they've changed since we ReadProperties
            if (File.Exists(BannerImageFilePath))
                BannerImageSprite = UnityFileMethods.CreateSprite(BannerImageFilePath);
            if (File.Exists(CardBackImageFilePath))
                CardBackImageSprite = UnityFileMethods.CreateSprite(CardBackImageFilePath);

            // The play mat can be loaded last
            if (File.Exists(PlayMatImageFilePath))
                PlayMatImageSprite = UnityFileMethods.CreateSprite(PlayMatImageFilePath);

            // Only considered as loaded if none of the steps failed
            if (string.IsNullOrEmpty(Error))
                HasLoaded = true;
        }

        public void LoadCards(int page)
        {
            string cardsFilePath =
                CardsFilePath + (page != AllCardsUrlPageCountStartIndex ? page.ToString() : string.Empty);

            if (File.Exists(cardsFilePath))
                LoadCards(cardsFilePath, SetCodeDefault);
            else
                Debug.Log("LoadCards::NOAllCards.json");
        }

        public void LoadCards(string cardsFilePath, string defaultSetCode)
        {
            LoadJsonFromFile(cardsFilePath, LoadCardFromJToken, CardDataIdentifier, defaultSetCode);
        }

        public void LoadSets()
        {
            if (File.Exists(SetsFilePath))
                LoadJsonFromFile(SetsFilePath, LoadSetFromJToken, SetDataIdentifier, null);
            else
                Debug.Log("LoadSets::NOAllSets.json");

            if (LoadedSets.TryGetValue(SetCodeDefault, out Set defaultSet))
                defaultSet.Name = SetNameDefault;
        }

        private void LoadJsonFromFile(string file, LoadJTokenDelegate load, string dataId, string defaultSetCode)
        {
            if (!File.Exists(file))
            {
                Debug.LogError("LoadJsonFromFile::NoFile");
                return;
            }

            try
            {
                JToken root = JToken.Parse(File.ReadAllText(file));

                IJEnumerable<JToken> dataContainer;
                if (!string.IsNullOrEmpty(dataId))
                {
                    JToken childProcessor = root;
                    foreach (string childName in dataId.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries))
                        (childProcessor as JObject)?.TryGetValue(childName, out childProcessor);
                    dataContainer = childProcessor;
                }
                else
                    dataContainer = root as JArray ?? (IJEnumerable<JToken>) ((JObject) root).PropertyValues();

                if (dataContainer != null)
                    foreach (JToken jToken in dataContainer)
                        load(jToken, defaultSetCode ?? SetCodeDefault);
                else
                    Debug.LogWarning("LoadJsonFromFile::EmptyFile");

                if (string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier) ||
                    root.Value<int>(AllCardsUrlPageCountIdentifier) <= 0)
                    return;

                AllCardsUrlPageCount = root.Value<int>(AllCardsUrlPageCountIdentifier);
                if (AllCardsUrlPageCountDivisor > 0)
                    AllCardsUrlPageCount =
                        Mathf.CeilToInt(((float) AllCardsUrlPageCount) / AllCardsUrlPageCountDivisor);
            }
            catch (Exception e)
            {
                Error += e.Message + e.StackTrace + Environment.NewLine;
                HasLoaded = false;
            }
        }

        private void LoadCardFromJToken(JToken cardJToken, string defaultSetCode)
        {
            if (cardJToken == null)
            {
                Debug.LogError("LoadCardFromJToken::NullCardJToken");
                return;
            }

            Dictionary<string, PropertyDefValuePair> metaProperties = new Dictionary<string, PropertyDefValuePair>();
            var idDef = new PropertyDef(CardIdIdentifier, PropertyType.String);
            PopulateCardProperty(metaProperties, cardJToken, idDef, idDef.Name);
            string cardId;
            if (metaProperties.TryGetValue(CardIdIdentifier, out PropertyDefValuePair cardIdEntry))
            {
                cardId = cardIdEntry.Value;
                if (string.IsNullOrEmpty(cardId))
                {
                    Debug.LogWarning("LoadCardFromJToken::MissingCardId");
                    return;
                }

                if (!string.IsNullOrEmpty(CardIdStop))
                    cardId = cardId.Split(CardIdStop[0])[0];
            }
            else
            {
                Debug.LogWarning("LoadCardFromJToken::ParseIdError");
                return;
            }

            var nameDef = new PropertyDef(CardNameIdentifier, PropertyType.String);
            PopulateCardProperty(metaProperties, cardJToken, nameDef, nameDef.Name);
            var cardName = string.Empty;
            if (metaProperties.TryGetValue(CardNameIdentifier, out PropertyDefValuePair cardNameEntry))
                cardName = cardNameEntry.Value ?? string.Empty;
            else
                Debug.LogWarning("LoadCardFromJToken::ParseNameError");

            Dictionary<string, PropertyDefValuePair> cardProperties = new Dictionary<string, PropertyDefValuePair>();
            PopulateCardProperties(cardProperties, cardJToken, CardProperties);

            Dictionary<string, string> cardSets = new Dictionary<string, string>();
            PopulateCardSets(cardSets, cardJToken, defaultSetCode);

            var cardImageWebUrl = string.Empty;
            if (!string.IsNullOrEmpty(CardImageProperty))
            {
                PropertyDef imageDef = new PropertyDef(CardImageProperty, PropertyType.String);
                PopulateCardProperty(metaProperties, cardJToken, imageDef, imageDef.Name);
                if (metaProperties.TryGetValue(CardImageProperty, out PropertyDefValuePair cardImageEntry))
                    cardImageWebUrl = cardImageEntry.Value ?? string.Empty;
            }

            string cardImageUrl = CardImageUrl;
            if (string.IsNullOrEmpty(CardImageProperty) || !string.IsNullOrEmpty(cardImageWebUrl) ||
                !string.IsNullOrEmpty(cardImageUrl))
            {
                foreach (KeyValuePair<string, string> set in cardSets)
                {
                    bool isReprint = CardNameIsUnique && CardNames.Contains(cardName);
                    if (!isReprint)
                        CardNames.Add(cardName);
                    string cardDuplicateId = cardSets.Count > 1 && isReprint
                        ? (cardId + PropertyDef.ObjectDelimiter + set.Key)
                        : cardId;
                    var unityCard =
                        new UnityCard(this, cardDuplicateId, cardName, set.Key, cardProperties, isReprint)
                        {
                            ImageWebUrl = cardImageWebUrl
                        };
                    LoadedCards[unityCard.Id] = unityCard;
                    if (!Sets.ContainsKey(set.Key))
                        LoadedSets[set.Key] = new Set(set.Key, set.Value);
                }
            }
            else
                Debug.Log("LoadCardFromJToken::MissingCardImage");
        }

        private void PopulateCardProperties(Dictionary<string, PropertyDefValuePair> cardProperties, JToken cardJToken,
            List<PropertyDef> propertyDefs, string keyPrefix = "")
        {
            if (cardProperties == null || cardJToken == null || propertyDefs == null)
            {
                Debug.LogError($"PopulateCardProperties::NullInput:{cardProperties}:{propertyDefs}:{cardJToken}");
                return;
            }

            foreach (PropertyDef property in propertyDefs)
                PopulateCardProperty(cardProperties, cardJToken, property, keyPrefix + property.Name);
        }

        private void PopulateCardProperty(Dictionary<string, PropertyDefValuePair> cardProperties, JToken cardJToken,
            PropertyDef property, string key)
        {
            if (cardProperties == null || cardJToken == null || property == null)
            {
                Debug.LogError($"PopulateCardProperty::MissingInput:{cardProperties}:{cardJToken}:{property}");
                return;
            }

            try
            {
                var newProperty = new PropertyDefValuePair() {Def = property};
                string listValue;
                JToken listTokens;
                JObject jObject;
                switch (property.Type)
                {
                    case PropertyType.ObjectEnumList:
                        listValue = string.Empty;
                        listTokens = cardJToken[property.Name];
                        if (listTokens != null)
                            foreach (JToken jToken in listTokens)
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
                            newProperty = new PropertyDefValuePair() {Def = childProperty};
                            listValue = string.Empty;
                            Dictionary<string, PropertyDefValuePair> values =
                                new Dictionary<string, PropertyDefValuePair>();
                            var i = 0;
                            listTokens = cardJToken[property.Name];
                            if (listTokens != null)
                                foreach (JToken jToken in listTokens)
                                {
                                    PopulateCardProperty(values, jToken, childProperty, key + childProperty.Name + i);
                                    i++;
                                }

                            foreach (KeyValuePair<string, PropertyDefValuePair> entry in values)
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
                        newProperty.Value = jObject?.Value<string>(CardPropertyIdentifier) ?? string.Empty;
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.Object:
                        jObject = cardJToken[property.Name] as JObject;
                        if (jObject != null && jObject.HasValues)
                            PopulateCardProperties(cardProperties, cardJToken[property.Name], property.Properties,
                                key + PropertyDef.ObjectDelimiter);
                        else
                            PopulateEmptyCardProperty(cardProperties, property, key);
                        break;
                    case PropertyType.StringEnumList:
                    case PropertyType.StringList:
                        listValue = string.Empty;
                        if (string.IsNullOrEmpty(property.Delimiter))
                        {
                            listTokens = cardJToken[property.Name];
                            if (listTokens != null)
                                foreach (JToken jToken in listTokens)
                                {
                                    if (!string.IsNullOrEmpty(listValue))
                                        listValue += EnumDef.Delimiter;
                                    listValue += jToken.Value<string>() ?? string.Empty;
                                }
                        }
                        else
                        {
                            foreach (string token in (cardJToken.Value<string>(property.Name) ?? string.Empty).Split(
                                new[] {property.Delimiter}, StringSplitOptions.RemoveEmptyEntries))
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

        private void PopulateEmptyCardProperty(Dictionary<string, PropertyDefValuePair> cardProperties,
            PropertyDef property, string key)
        {
            cardProperties[key] = new PropertyDefValuePair() {Def = property, Value = string.Empty};
            foreach (PropertyDef childProperty in property.Properties)
                PopulateEmptyCardProperty(cardProperties, childProperty,
                    key + PropertyDef.ObjectDelimiter + childProperty.Name);
        }

        private void PopulateCardSets(Dictionary<string, string> cardSets, JToken cardJToken, string defaultSetCode)
        {
            if (cardSets == null || cardJToken == null || string.IsNullOrEmpty(defaultSetCode))
            {
                Debug.LogError($"PopulateCardSets::MissingInput:{cardSets}:{defaultSetCode}:{cardJToken}");
                return;
            }

            string dataIdentifier = CardSetIdentifier;
            if (dataIdentifier.Contains('.'))
            {
                JToken childProcessor = cardJToken;
                string[] parentNames = CardSetIdentifier.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < parentNames.Length - 1; i++)
                    (childProcessor as JObject)?.TryGetValue(parentNames[i], out childProcessor);
                cardJToken = childProcessor;
                dataIdentifier = parentNames[parentNames.Length - 1];
            }

            if (CardSetsInListIsCsv)
            {
                string codesCsv = cardJToken?.Value<string>(dataIdentifier) ?? defaultSetCode;
                string namesCsv = cardJToken?.Value<string>(CardSetNameIdentifier) ?? codesCsv;
                string[] codes = codesCsv.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                string[] names = namesCsv.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < codes.Length; i++)
                {
                    string code = codes[i];
                    string name = i < names.Length ? names[i] : code;
                    cardSets[code] = name;
                }
            }
            else if (CardSetsInList)
            {
                List<JToken> setJTokens = new List<JToken>();
                try
                {
                    setJTokens = (cardJToken?[dataIdentifier] as JArray)?.ToList() ?? new List<JToken>();
                }
                catch
                {
                    Debug.LogWarning($"PopulateCardSets::BadCardSetIdentifier for {cardJToken}");
                }

                foreach (JToken setJToken in setJTokens)
                {
                    if (CardSetIsObject)
                    {
                        var setJObject = setJToken as JObject;
                        string setCode = setJObject?.Value<string>(SetCodeIdentifier) ?? defaultSetCode;
                        string setName = setJObject?.Value<string>(SetNameIdentifier) ?? setCode;
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
                JObject setJObject = null;
                try
                {
                    setJObject = cardJToken?[dataIdentifier] as JObject;
                }
                catch
                {
                    Debug.LogWarning($"PopulateCardSets::BadCardSetIdentifier for {cardJToken}");
                }

                string setCode = setJObject?.Value<string>(SetCodeIdentifier) ?? defaultSetCode;
                string setName = setJObject?.Value<string>(SetNameIdentifier) ?? setCode;
                cardSets[setCode] = setName;
            }
            else
            {
                string code = cardJToken?.Value<string>(dataIdentifier) ?? defaultSetCode;
                string name = cardJToken?.Value<string>(CardSetNameIdentifier) ?? code;
                cardSets[code] = name;
            }
        }

        private void LoadSetFromJToken(JToken setJToken, string defaultSetCode)
        {
            if (setJToken == null)
            {
                Debug.LogError("LoadSetFromJToken::NullSetJToken");
                return;
            }

            var setCode = setJToken.Value<string>(SetCodeIdentifier);
            if (string.IsNullOrEmpty(setCode))
            {
                Debug.LogError("LoadSetFromJToken::EmptySetCode");
                return;
            }

            string setName = setJToken.Value<string>(SetNameIdentifier) ?? setCode;
            string setCardsUrl = setJToken.Value<string>(SetCardsUrlIdentifier) ?? string.Empty;

            LoadedSets[setCode] = new Set(setCode, setName, setCardsUrl);

            var cards = setJToken.Value<JArray>(SetCardsIdentifier);
            if (cards == null)
                return;

            foreach (JToken jToken in cards)
                LoadCardFromJToken(jToken, setCode);
        }

        public void Add(UnityCard card)
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
            if (card?.Id != null)
                Remove(card.Id);
        }

        private void Remove(string cardId)
        {
            LoadedCards.Remove(cardId);
            WriteAllCardsJson();
        }

        public IEnumerable<UnityCard> FilterCards(CardSearchFilters filters)
        {
            if (filters == null)
            {
                Debug.LogError("FilterCards::NullFilters");
                yield break;
            }

            foreach (UnityCard card in Cards.Values)
            {
                if (!string.IsNullOrEmpty(filters.Name) && !filters.Name.ToLower().Split(
                    new[] {CardSearchFilters.Delimiter},
                    StringSplitOptions.RemoveEmptyEntries).All(card.Name.ToLower().Contains))
                    continue;
                if (!string.IsNullOrEmpty(filters.Id) && !card.Id.ToLower().Contains(filters.Id.ToLower()))
                    continue;
                if (!string.IsNullOrEmpty(filters.SetCode) &&
                    !card.SetCode.ToLower().Contains(filters.SetCode.ToLower()))
                    continue;
                var propsMatch = true;
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

                    if ((card.GetPropertyValueEnum(entry.Key) & entry.Value) != 0)
                        continue;
                    PropertyDef propDef = CardProperties?.FirstOrDefault(prop => prop.Name.Equals(entry.Key));
                    if (propDef != null)
                        propsMatch = propsMatch && (entry.Value == (1 << enumDef.Values.Count)) && propDef
                            .DisplayEmpty
                            .Equals(card.GetPropertyValueString(entry.Key));
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
