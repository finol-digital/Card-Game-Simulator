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
        public string CardImageFileType { get; set; } = "png";

        [JsonProperty]
        public string CardImageProperty { get; set; } = "";

        [JsonProperty]
        public string CardImageUrl { get; set; } = "";

        [JsonProperty]
        public string CardNameIdentifier { get; set; } = "name";

        [JsonProperty]
        public string CardSetIdentifier { get; set; } = "set";

        [JsonProperty]
        public string CardPrimaryProperty { get; set; } = "";

        [JsonProperty]
        public List<PropertyDef> CardProperties { get; set; } = new List<PropertyDef>();

        [JsonProperty]
        public UnityEngine.Vector2 CardSize { get; set; } = new UnityEngine.Vector2(2.5f, 3.5f);

        [JsonProperty]
        public string DeckFileHsdId { get; set; } = "dbfId";

        [JsonProperty]
        public DeckFileTxtId DeckFileTxtId { get; set; } = DeckFileTxtId.Set;

        [JsonProperty]
        public bool DeckFileTxtIdRequired { get; set; }

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
        public bool SetsInCardObject { get; set; }

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
        public bool HasDownloaded { get; private set; }
        public bool HasLoaded { get; private set; }
        public string Error { get; private set; }

        public HashSet<string> CardNames { get; } = new HashSet<string>();

        protected Dictionary<string, Card> LoadedCards { get; } = new Dictionary<string, Card>();
        protected Dictionary<string, Set> LoadedSets { get; } = new Dictionary<string, Set>();

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

        // TODO: C#7 Tuple
        public static Tuple<string, string> Decode(string gameId)
        {
            string name = gameId;
            string url = string.Empty;
            int delimiterIdx = gameId.LastIndexOf('@');
            if (delimiterIdx != -1)
            {
                name = gameId.Substring(0, delimiterIdx);
                url = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(gameId.Substring(delimiterIdx + 1).Replace('_', '/')));
            }
            return new Tuple<string, string>(name, url);
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

        public void ReadProperties()
        {
            try
            {
                // We need to read the *Game:Name*.json file, but reading it can cause *Game:Name* to change, so account for that
                string gameFilePath = GameFilePath;
                string gameDirectoryPath = GameDirectoryPath;
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
            yield return UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl, GameFilePath);
            ReadProperties();
            if (!HasReadProperties)
            {
                // ReadProperties() should have already populated the Error
                IsDownloading = false;
                HasDownloaded = false;
                yield break;
            }


            if (!string.IsNullOrEmpty(AllCardsUrl))
            {
                for (int page = AllCardsUrlPageCountStartIndex; page < AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount; page++)
                {
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
                        jsonBody += AllCardsUrlPageIdentifier + page;
                        jsonBody += "}";
                    }
                    yield return UnityExtensionMethods.SaveUrlToFile(cardsUrl, cardsFile, jsonBody);
                    // Sometimes, we need to get the AllCardsUrlPageCount from the first page of AllCardsUrl
                    if (page == AllCardsUrlPageCountStartIndex && !string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier))
                        LoadCards(page);
                }
            }

            string setsFilePath = SetsFilePath + (AllSetsUrlZipped ? UnityExtensionMethods.ZipExtension : string.Empty);
            if (!string.IsNullOrEmpty(AllSetsUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(AllSetsUrl, setsFilePath);

            if (!string.IsNullOrEmpty(BannerImageUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(BannerImageUrl, BannerImageFilePath);

            if (!string.IsNullOrEmpty(CardBackImageUrl))
                yield return UnityExtensionMethods.SaveUrlToFile(CardBackImageUrl, CardBackImageFilePath);

            foreach (GameBoardUrl boardUrl in GameBoardUrls)
                yield return UnityExtensionMethods.SaveUrlToFile(boardUrl.Url, GameBoardsFilePath + "/" + boardUrl.Id + "." + GameBoardFileType);

            foreach (DeckUrl deckUrl in DeckUrls)
                yield return UnityExtensionMethods.SaveUrlToFile(deckUrl.Url, DecksFilePath + "/" + deckUrl.Name + "." + DeckFileType);

            IsDownloading = false;
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
            int daysSinceUpdate = 0;
            try { daysSinceUpdate = (int)DateTime.Today.Subtract(File.GetLastWriteTime(GameFilePath).Date).TotalDays; } catch { };
            if (AutoUpdate >= 0 && daysSinceUpdate >= AutoUpdate && CoroutineRunner != null)
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
            if (AllCardsUrlZipped)
                UnityExtensionMethods.ExtractZip(cardsFilePath + UnityExtensionMethods.ZipExtension, GameDirectoryPath);
            if (AllCardsUrlWrapped)
                UnityExtensionMethods.UnwrapFile(cardsFilePath);
            if (File.Exists(cardsFilePath))
                LoadJsonFromFile(cardsFilePath, LoadCardFromJToken, CardDataIdentifier);
        }

        public void LoadSets()
        {
            if (AllSetsUrlZipped)
                UnityExtensionMethods.ExtractZip(SetsFilePath + UnityExtensionMethods.ZipExtension, GameDirectoryPath);
            if (AllSetsUrlWrapped)
                UnityExtensionMethods.UnwrapFile(SetsFilePath);
            if (File.Exists(SetsFilePath))
                LoadJsonFromFile(SetsFilePath, LoadSetFromJToken, SetDataIdentifier);

            Set defaultSet;
            if (LoadedSets.TryGetValue(SetCodeDefault, out defaultSet))
                defaultSet.Name = SetNameDefault;
        }

        public void LoadJsonFromFile(string file, LoadJTokenDelegate load, string dataId)
        {
            if (!File.Exists(file))
            {
                UnityEngine.Debug.LogWarning("LoadJsonFromFile::NoFile");
                return;
            }

            try
            {
                JToken root = JToken.Parse(File.ReadAllText(file));
                foreach (JToken jToken in !string.IsNullOrEmpty(dataId) ? root[dataId] : root as JArray ?? (IJEnumerable<JToken>)((JObject)root).PropertyValues())
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

        // Note: Can throw Exception
        public void LoadCardFromJToken(JToken cardJToken, string defaultSetCode)
        {
            if (cardJToken == null)
            {
                UnityEngine.Debug.LogWarning("LoadCardFromJToken::NullCardJToken");
                return;
            }

            string cardId = cardJToken.Value<string>(CardIdIdentifier) ?? string.Empty;
            if (string.IsNullOrEmpty(cardId))
            {
                UnityEngine.Debug.LogWarning("LoadCardFromJToken::EmptyCardId");
                return;
            }

            string cardName = cardJToken.Value<string>(CardNameIdentifier) ?? string.Empty;
            Dictionary<string, PropertyDefValuePair> cardProperties = new Dictionary<string, PropertyDefValuePair>();
            foreach (PropertyDef property in CardProperties)
            {
                PropertyDefValuePair newPropertyEntry = new PropertyDefValuePair() { Def = property };
                try
                {
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
                                listValue += jObject?.Value<string>("id") ?? string.Empty;
                            }
                            newPropertyEntry.Value = listValue;
                            break;
                        case PropertyType.ObjectList:
                            listValue = string.Empty;
                            foreach (JToken jToken in cardJToken[property.Name])
                            {
                                if (!string.IsNullOrEmpty(listValue))
                                    listValue += EnumDef.Delimiter;
                                jObject = jToken as JObject;
                                listValue += jObject?.ToString() ?? string.Empty;
                            }
                            newPropertyEntry.Value = listValue;
                            break;
                        case PropertyType.ObjectEnum:
                            jObject = cardJToken[property.Name] as JObject;
                            newPropertyEntry.Value = jObject.Value<string>("id") ?? string.Empty;
                            break;
                        case PropertyType.Object:
                            jObject = cardJToken[property.Name] as JObject;
                            newPropertyEntry.Value = jObject?.ToString() ?? string.Empty;
                            break;
                        case PropertyType.StringEnumList:
                        case PropertyType.StringList:
                            listValue = string.Empty;
                            foreach (JToken jToken in cardJToken[property.Name])
                            {
                                if (!string.IsNullOrEmpty(listValue))
                                    listValue += EnumDef.Delimiter;
                                listValue += jToken.Value<string>() ?? string.Empty;
                            }
                            newPropertyEntry.Value = listValue;
                            break;
                        case PropertyType.EscapedString:
                            newPropertyEntry.Value = (cardJToken.Value<string>(property.Name) ?? string.Empty).Replace("\\", "");
                            break;
                        case PropertyType.StringEnum:
                        case PropertyType.Boolean:
                        case PropertyType.Integer:
                        case PropertyType.String:
                        default:
                            newPropertyEntry.Value = cardJToken.Value<string>(property.Name) ?? string.Empty;
                            break;
                    }
                }
                catch
                {
                    newPropertyEntry.Value = string.Empty;
                }
                cardProperties[property.Name] = newPropertyEntry;
            }

            HashSet<string> setCodes = new HashSet<string>();
            if (SetsInCardObject)
            {
                JToken setContainer = cardJToken[CardSetIdentifier];
                List<JToken> setJTokens = (setContainer as JArray)?.ToList() ?? new List<JToken>();
                if (setJTokens.Count == 0)
                    setJTokens.Add(setContainer);
                foreach (JToken jToken in setJTokens)
                {
                    JObject setObject = jToken as JObject;
                    string setCode = setObject?.Value<string>(SetCodeIdentifier);
                    if (setCode == null)
                        UnityEngine.Debug.LogWarning("LoadCardFromJToken::InvalidSetObject:" + setContainer.ToString());
                    else
                        setCodes.Add(setCode);
                }
            }
            else
                setCodes.Add(cardJToken.Value<string>(CardSetIdentifier) ?? defaultSetCode);

            foreach (string cardSet in setCodes)
            {
                bool isReprint = CardNames.Contains(cardName);
                if (!isReprint)
                    CardNames.Add(cardName);
                Card newCard = new Card(this, setCodes.Count > 1 ? (cardId + "_" + cardSet) : cardId, cardName, cardSet, cardProperties, isReprint);
                LoadedCards[newCard.Id] = newCard;
                if (!Sets.ContainsKey(cardSet))
                    LoadedSets[cardSet] = new Set(cardSet, cardSet);
            }
        }

        // Note: Can throw Exception
        public void LoadSetFromJToken(JToken setJToken, string defaultSetCode)
        {
            if (setJToken == null)
            {
                UnityEngine.Debug.LogWarning("LoadSetFromJToken::NullSetJToken");
                return;
            }

            string setCode = setJToken.Value<string>(SetCodeIdentifier);
            if (string.IsNullOrEmpty(setCode))
            {
                UnityEngine.Debug.LogWarning("LoadSetFromJToken::EmptySetCode");
                return;
            }
            string setName = setJToken.Value<string>(SetNameIdentifier) ?? setCode;

            LoadedSets[setCode] = new Set(setCode, setName);

            JArray cards = setJToken.Value<JArray>(SetCardsIdentifier);
            if (cards != null)
                foreach (JToken jToken in cards)
                    LoadCardFromJToken(jToken, setCode);
        }

        public IEnumerable<Card> FilterCards(CardSearchFilters filters)
        {
            if (filters == null)
            {
                UnityEngine.Debug.LogWarning("FilterCards::NullFilters");
                yield break;
            }

            foreach (Card card in Cards.Values)
            {
                if (!string.IsNullOrEmpty(filters.Name) && !filters.Name.ToLower().Split(' ').All(card.Name.ToLower().Contains))
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
                                         => prop.Name.Equals(entry.Key)).Empty.Equals(card.GetPropertyValueString(entry.Key));
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
