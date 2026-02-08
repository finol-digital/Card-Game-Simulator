/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace FinolDigital.Cgs.Json.Unity
{
    public delegate void LoadJTokenDelegate(JToken jToken, string defaultValue);

    public delegate IEnumerator CardGameCoroutineDelegate(UnityCardGame cardGame);

    public class UnityCardGame : CardGame
    {
        public static readonly UnityCardGame UnityInvalid = new(null);

        public static string GamesImportPath => Path.Combine(Application.temporaryCachePath, "cgsGamesImport");
        public static string GamesDirectoryPath => Path.Combine(Application.persistentDataPath, "games");
        public static string GamesExportPath => GamesDirectoryPath + "Export";

        public string GameDirectoryPath => Path.Combine(GamesDirectoryPath, UnityFileMethods.GetSafeFileName(Id));

        public string GameBackupFilePath => Path.Combine(GameDirectoryPath,
            UnityFileMethods.GetSafeFileName(Name) + UnityFileMethods.JsonExtension);

        public string GameFilePath => GameDirectoryPath + "/cgs.json";

        public string CardsFilePath => GameDirectoryPath + "/AllCards.json";
        public string DecksFilePath => GameDirectoryPath + "/AllDecks.json";
        public string SetsFilePath => GameDirectoryPath + "/AllSets.json";

        public string BannerImageFilePath => GameDirectoryPath + "/Banner." +
                                             UnityFileMethods.GetSafeFileName(BannerImageFileType);

        public string CardBackImageFilePath => GameDirectoryPath + "/CardBack." +
                                               UnityFileMethods.GetSafeFileName(CardBackImageFileType);

        public string PlayMatImageFilePath => GameDirectoryPath + "/PlayMat." +
                                              UnityFileMethods.GetSafeFileName(PlayMatImageFileType);

        public string BacksDirectoryPath => GameDirectoryPath + "/backs";
        public string DecksDirectoryPath => GameDirectoryPath + "/decks";
        public string GameBoardsDirectoryPath => GameDirectoryPath + "/boards";
        public string SetsDirectoryPath => GameDirectoryPath + "/sets";

        public float CardAspectRatio => CardSize.Y > 0 ? Mathf.Abs(CardSize.X / CardSize.Y) : 0.715f;
        public IReadOnlyDictionary<string, UnityCard> Cards => LoadedCards;
        public IReadOnlyDictionary<string, Set> Sets => LoadedSets;

        public bool IsUploaded => CgsGamesLink != null && CgsGamesLink.IsWellFormedOriginalString()
                                  || AutoUpdateUrl != null && AutoUpdateUrl.IsWellFormedOriginalString();

        public MonoBehaviour CoroutineRunner { get; set; }

        public bool HasReadProperties { get; private set; }

        public bool IsDownloading
        {
            get => _isDownloading;
            private set
            {
                _isDownloading = value;
                Debug.Log("Download " + (_isDownloading ? "Start" : "End"));
            }
        }

        private bool _isDownloading;

        public float DownloadProgress { get; private set; }

        public string DownloadStatus
        {
            get => _downloadStatus;
            private set
            {
                _downloadStatus = value;
                Debug.Log(_downloadStatus);
            }
        }

        private string _downloadStatus = "N / A";

        public bool HasDownloaded { get; private set; }
        public bool IsLoading { get; private set; }
        public bool HasLoaded { get; private set; }
        public string Error { get; private set; }

        public HashSet<string> CardNames { get; } = new();

        protected Dictionary<string, UnityCard> LoadedCards { get; } = new();

        protected Dictionary<string, Set> LoadedSets { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Sprite BannerImageSprite
        {
            get => _bannerImageSprite ??= Resources.Load<Sprite>("Banner");
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
            get => _cardBackImageSprite ??= Resources.Load<Sprite>("CardBack");
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

        public Dictionary<string, Sprite> CardBackFaceImageSprites { get; } = new();

        private Sprite _cardBackImageSprite;

        public Sprite PlayMatImageSprite
        {
            get => _playMatImageSprite ??= Resources.Load<Sprite>("PlayMat");
            private set
            {
                if (_playMatImageSprite != null && _hasPlayMatImage)
                {
                    Object.Destroy(_playMatImageSprite.texture);
                    Object.Destroy(_playMatImageSprite);
                }

                _hasPlayMatImage = true;
                _playMatImageSprite = value;
            }
        }

        private Sprite _playMatImageSprite;
        private bool _hasPlayMatImage;

        public UnityCardGame(MonoBehaviour coroutineRunner, string id = DefaultName, string autoUpdateUrl = "")
            : base(id, autoUpdateUrl)
        {
            CoroutineRunner = coroutineRunner;
        }

        public void ClearDefinitionLists()
        {
            CardProperties.Clear();
            DeckPlayCards.Clear();
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
                // We need to read the cgs.json file, but reading it can cause *Game:ID* to change, so account for that
                var gameDirectoryPath = GameDirectoryPath;
                var gameFilePath = GameFilePath;
                if (!File.Exists(gameFilePath))
                    gameFilePath = GameBackupFilePath;

                ClearDefinitionLists();
                JsonConvert.PopulateObject(File.ReadAllText(gameFilePath), this);
                RefreshId();

                if (!gameDirectoryPath.Equals(GameDirectoryPath) && Directory.Exists(gameDirectoryPath) &&
                    !Directory.Exists(GameDirectoryPath))
                    Directory.Move(gameDirectoryPath, GameDirectoryPath);

                if (!gameFilePath.Equals(GameFilePath) && File.Exists(gameFilePath) && !File.Exists(GameFilePath))
                    File.Copy(gameFilePath, GameFilePath, true);

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

        public IEnumerator Download(bool isRedo = false)
        {
            if (IsDownloading)
            {
                Debug.LogWarning("Duplicate Download Request Ignored");
                yield break;
            }

            IsDownloading = true;

            if (isRedo)
            {
                DeckUrls.Clear();
                GameStartDecks.Clear();
                LoadedSets.Clear();
                LoadedCards.Clear();
                CardNames.Clear();
            }

            // We should always first get the cgs.json file and read it before doing anything else
            DownloadProgress = 0f / (7f + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Card Game Specification...";
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

            DownloadProgress = 1f / (8f + DeckUrls.Count + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Banner";
            if (BannerImageUrl != null && BannerImageUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(BannerImageUrl.AbsoluteUri, BannerImageFilePath);

            DownloadProgress = 2f / (8f + DeckUrls.Count + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: CardBack";
            if (CardBackImageUrl != null && CardBackImageUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(CardBackImageUrl.AbsoluteUri, CardBackImageFilePath);

            var cardBack = 0;
            foreach (var cardBackFaceImageUrl in CardBackFaceImageUrls)
            {
                cardBack++;
                DownloadProgress = (2f + cardBack) /
                                   (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount);
                DownloadStatus = $"Downloading: CardBacks: {cardBack,5} / {CardBackFaceImageUrls.Count}";

                if (string.IsNullOrEmpty(cardBackFaceImageUrl.Id))
                {
                    Debug.Log($"Ignoring cardBack {cardBack}");
                    continue;
                }

                var backFilePath = Path.Combine(BacksDirectoryPath,
                    cardBackFaceImageUrl.Id + "." + CardBackImageFileType.ToLower());
                if (cardBackFaceImageUrl.Url.IsAbsoluteUri)
                    yield return UnityFileMethods.SaveUrlToFile(cardBackFaceImageUrl.Url.AbsoluteUri, backFilePath);
                else
                    Debug.Log($"Empty url for cardBackFaceImageUrl {cardBackFaceImageUrl}");
            }

            DownloadProgress = (3f + CardBackFaceImageUrls.Count) /
                               (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: PlayMat";
            if (PlayMatImageUrl != null && PlayMatImageUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(PlayMatImageUrl.AbsoluteUri, PlayMatImageFilePath);

            DownloadProgress = (4f + CardBackFaceImageUrls.Count) /
                               (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: Boards";
            foreach (var gameBoardUrl in GameBoardUrls.Where(gameBoardUrl =>
                         !string.IsNullOrEmpty(gameBoardUrl.Id) &&
                         gameBoardUrl.Url.IsAbsoluteUri))
                yield return UnityFileMethods.SaveUrlToFile(gameBoardUrl.Url.AbsoluteUri,
                    GameBoardsDirectoryPath + "/" + gameBoardUrl.Id + "." + GameBoardImageFileType);

            DownloadProgress = (5f + CardBackFaceImageUrls.Count) /
                               (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount);
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
                    var root = JToken.Parse(File.ReadAllText(DecksFilePath));
                    JArray dataContainer;
                    if (!string.IsNullOrEmpty(AllDecksUrlDataIdentifier))
                    {
                        var childProcessor = root;
                        foreach (var childName in AllDecksUrlDataIdentifier.Split(new[] { '.' },
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

            var deck = 0;
            foreach (var deckUrl in DeckUrls)
            {
                deck++;
                DownloadProgress = (5f + CardBackFaceImageUrls.Count + deck) /
                                   (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount);
                DownloadStatus = $"Downloading: Decks: {deck,5} / {DeckUrls.Count}";

                if (string.IsNullOrEmpty(deckUrl.Name) || !deckUrl.IsAvailable)
                {
                    Debug.Log($"Ignoring deckUrl {deckUrl}");
                    continue;
                }

                var deckFilePath = Path.Combine(DecksDirectoryPath,
                    deckUrl.Name + "." + DeckFileType.ToString().ToLower());
                if (!string.IsNullOrEmpty(AllDecksUrlTxtRoot) && !string.IsNullOrEmpty(deckUrl.Txt))
                    yield return UnityFileMethods.SaveUrlToFile(AllDecksUrlTxtRoot + deckUrl.Txt, deckFilePath);
                else if (deckUrl.Url != null && deckUrl.Url.IsAbsoluteUri)
                    yield return UnityFileMethods.SaveUrlToFile(deckUrl.Url.AbsoluteUri, deckFilePath);
                else
                    Debug.Log($"Empty url for deckUrl {deckUrl}");
            }

            DownloadProgress = (6f + CardBackFaceImageUrls.Count + DeckUrls.Count) /
                               (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount);
            DownloadStatus = "Downloading: AllSets.json";
            var setsFilePath = SetsFilePath + (AllSetsUrlZipped ? UnityFileMethods.ZipExtension : string.Empty);
            if (AllSetsUrl != null && AllSetsUrl.IsAbsoluteUri)
                yield return UnityFileMethods.SaveUrlToFile(AllSetsUrl.AbsoluteUri, setsFilePath);
            if (AllSetsUrlZipped)
                UnityFileMethods.ExtractZip(setsFilePath, GameDirectoryPath);
            if (AllSetsUrlWrapped)
                UnityFileMethods.UnwrapFile(SetsFilePath);

            if (AllCardsUrl != null && AllCardsUrl.IsWellFormedOriginalString())
            {
                for (var page = AllCardsUrlPageCountStartIndex;
                     page < AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount;
                     page++)
                {
                    Debug.Log($"Downloading page {page} / {AllCardsUrlPageCount} total pages ");
                    DownloadProgress = (7f + CardBackFaceImageUrls.Count + DeckUrls.Count + page -
                                        AllCardsUrlPageCountStartIndex) /
                                       (8f + CardBackFaceImageUrls.Count + DeckUrls.Count + AllCardsUrlPageCount -
                                        AllCardsUrlPageCountStartIndex);
                    DownloadStatus =
                        $"Downloading: Cards: {page,5} / {AllCardsUrlPageCountStartIndex + AllCardsUrlPageCount}";
                    var cardsUrl = AllCardsUrl.OriginalString;
                    if (AllCardsUrlPageCount > 1 && string.IsNullOrEmpty(AllCardsUrlPostBodyContent))
                        cardsUrl += AllCardsUrlPageIdentifier + page;
                    var cardsFilePath = CardsFilePath;
                    if (page != AllCardsUrlPageCountStartIndex)
                        cardsFilePath += page.ToString();
                    if (AllCardsUrlZipped)
                        cardsFilePath += UnityFileMethods.ZipExtension;
                    string jsonBody = null;
                    if (!string.IsNullOrEmpty(AllCardsUrlPostBodyContent))
                    {
                        jsonBody = "{" + AllCardsUrlPostBodyContent;
                        if (AllCardsUrlPageCount > 1 || !string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier))
                            jsonBody += AllCardsUrlPageIdentifier + page;
                        jsonBody += "}";
                    }

                    var headers = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(AllCardsUrlRequestHeader) &&
                        !string.IsNullOrEmpty(AllCardsUrlRequestHeaderValue))
                        headers.Add(AllCardsUrlRequestHeader, AllCardsUrlRequestHeaderValue);
                    yield return UnityFileMethods.SaveUrlToFile(cardsUrl, cardsFilePath, jsonBody, headers);
                    if (AllCardsUrlZipped)
                        UnityFileMethods.ExtractZip(cardsFilePath, GameDirectoryPath);
                    if (AllCardsUrlWrapped)
                        UnityFileMethods.UnwrapFile(cardsFilePath.EndsWith(UnityFileMethods.ZipExtension)
                            ? cardsFilePath.Remove(cardsFilePath.Length - UnityFileMethods.ZipExtension.Length)
                            : cardsFilePath);

                    // Sometimes, we need to get the AllCardsUrlPageCount from the first page of AllCardsUrl
                    if (page != AllCardsUrlPageCountStartIndex ||
                        string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier)) continue;

                    // Get it from the response header if we can
                    if (headers.TryGetValue(AllCardsUrlPageCountIdentifier, out var pageCountString) &&
                        int.TryParse(pageCountString, out var pageCountInt) && pageCountInt > 0)
                        AllCardsUrlPageCount = Mathf.CeilToInt(pageCountInt / (float)AllCardsUrlPageCountDivisor);
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
            // We should have already read the cgs.json, but we need to be sure
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
            var daysSinceUpdate = 0;
            try
            {
                var gameFilePath = GameFilePath;
                if (!File.Exists(gameFilePath))
                    gameFilePath = GameBackupFilePath;
                daysSinceUpdate = (int)DateTime.Today.Subtract(File.GetLastWriteTime(gameFilePath).Date).TotalDays;
            }
            catch
            {
                Debug.Log($"Unable to determine last update date for {Name}. Assuming today.");
            }

            var shouldUpdate = AutoUpdate >= 0 && daysSinceUpdate >= AutoUpdate && updateCoroutine != null;
            if (shouldUpdate)
            {
                if (CoroutineRunner != null)
                    CoroutineRunner.StartCoroutine(updateCoroutine(this));
                else
                    Debug.LogWarning($"Should update {Name}, but CoroutineRunner is null!");
                return;
            }

            // These enum lookups need to be initialized before we load cards and sets
            foreach (var enumDef in Enums)
                enumDef.InitializeLookups();

            // The main load action is to load cards and sets
            CardNames.Clear();
            if (CoroutineRunner != null)
                CoroutineRunner.StartCoroutine(loadCardsCoroutine(this));
            else
                Debug.LogWarning($"Should load cards for {Name}, but CoroutineRunner is null!");
            LoadSets();
            if (CoroutineRunner != null && LoadedSets.Values.Any(set => !string.IsNullOrEmpty(set.CardsUrl)))
                CoroutineRunner.StartCoroutine(loadSetCardsCoroutine(this));

            // We also re-load the banner and cardBack images now in case they've changed since we ReadProperties
            if (File.Exists(BannerImageFilePath))
                BannerImageSprite = UnityFileMethods.CreateSprite(BannerImageFilePath);
            if (File.Exists(CardBackImageFilePath))
                CardBackImageSprite = UnityFileMethods.CreateSprite(CardBackImageFilePath);

            if (Directory.Exists(BacksDirectoryPath))
            {
                foreach (var backFilePath in Directory.EnumerateFiles(BacksDirectoryPath))
                {
                    var id = Path.GetFileNameWithoutExtension(backFilePath);
                    if (CardBackFaceImageSprites.TryGetValue(id, out var sprite))
                    {
                        Object.Destroy(sprite.texture);
                        Object.Destroy(sprite);
                    }

                    CardBackFaceImageSprites[id] = UnityFileMethods.CreateSprite(backFilePath);
                }
            }

            // The play mat can be loaded last
            if (File.Exists(PlayMatImageFilePath))
                PlayMatImageSprite = UnityFileMethods.CreateSprite(PlayMatImageFilePath);

            // Only considered as loaded if none of the steps failed
            if (string.IsNullOrEmpty(Error))
                HasLoaded = true;
        }

        public void LoadAsync(CardGameCoroutineDelegate updateCoroutine, CardGameCoroutineDelegate loadCardsCoroutine,
            CardGameCoroutineDelegate loadSetCardsCoroutine)
        {
            if (CoroutineRunner != null)
                CoroutineRunner.StartCoroutine(
                    LoadAsyncImpl(updateCoroutine, loadCardsCoroutine, loadSetCardsCoroutine));
            else
            {
                Debug.LogWarning($"LoadAsync called for {Name} but CoroutineRunner is null! Falling back to Load.");
                Load(updateCoroutine, loadCardsCoroutine, loadSetCardsCoroutine);
            }
        }

        private IEnumerator LoadAsyncImpl(CardGameCoroutineDelegate updateCoroutine,
            CardGameCoroutineDelegate loadCardsCoroutine,
            CardGameCoroutineDelegate loadSetCardsCoroutine)
        {
            IsLoading = true;
            try
            {
                // We should have already read the cgs.json, but we need to be sure
                if (!HasReadProperties)
                {
                    ReadProperties();
                    if (!HasReadProperties)
                    {
                        // ReadProperties() should have already populated the Error
                        HasLoaded = false;
                        IsLoading = false;
                        yield break;
                    }
                }

                // Don't waste time loading if we need to update first
                var daysSinceUpdate = 0;
                try
                {
                    var gameFilePath = GameFilePath;
                    if (!File.Exists(gameFilePath))
                        gameFilePath = GameBackupFilePath;
                    daysSinceUpdate = (int)DateTime.Today.Subtract(File.GetLastWriteTime(gameFilePath).Date).TotalDays;
                }
                catch
                {
                    Debug.Log($"Unable to determine last update date for {Name}. Assuming today.");
                }

                var shouldUpdate = AutoUpdate >= 0 && daysSinceUpdate >= AutoUpdate && updateCoroutine != null;
                if (shouldUpdate)
                {
                    if (CoroutineRunner != null)
                        CoroutineRunner.StartCoroutine(updateCoroutine(this));
                    else
                        Debug.LogWarning($"Should update {Name}, but CoroutineRunner is null!");
                    IsLoading = false;
                    yield break;
                }

                // These enum lookups need to be initialized before we load cards and sets
                foreach (var enumDef in Enums)
                    enumDef.InitializeLookups();

                // The main load action is to load cards and sets
                CardNames.Clear();
                yield return loadCardsCoroutine(this);
                LoadSets();
                if (LoadedSets.Values.Any(set => !string.IsNullOrEmpty(set.CardsUrl)))
                    yield return loadSetCardsCoroutine(this);

                // We also re-load the banner and cardBack images now in case they've changed since we ReadProperties
                if (File.Exists(BannerImageFilePath))
                    BannerImageSprite = UnityFileMethods.CreateSprite(BannerImageFilePath);
                if (File.Exists(CardBackImageFilePath))
                    CardBackImageSprite = UnityFileMethods.CreateSprite(CardBackImageFilePath);

                // Load card back images
                if (Directory.Exists(BacksDirectoryPath))
                {
                    foreach (var backFilePath in Directory.EnumerateFiles(BacksDirectoryPath))
                    {
                        var id = Path.GetFileNameWithoutExtension(backFilePath);
                        if (CardBackFaceImageSprites.TryGetValue(id, out var sprite))
                        {
                            Object.Destroy(sprite.texture);
                            Object.Destroy(sprite);
                        }

                        CardBackFaceImageSprites[id] = UnityFileMethods.CreateSprite(backFilePath);
                    }
                }

                // The play mat can be loaded last
                if (File.Exists(PlayMatImageFilePath))
                    PlayMatImageSprite = UnityFileMethods.CreateSprite(PlayMatImageFilePath);

                // Only considered as loaded if none of the steps failed
                if (string.IsNullOrEmpty(Error))
                    HasLoaded = true;
            }
            finally
            {
                IsLoading = false;
            }

            IsLoading = false;
        }

        public void LoadCards(int page)
        {
            var cardsFilePath =
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

            if (LoadedSets.TryGetValue(SetCodeDefault, out var defaultSet))
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
                var root = JToken.Parse(File.ReadAllText(file));

                IJEnumerable<JToken> dataContainer;
                if (!string.IsNullOrEmpty(dataId))
                {
                    var childProcessor = root;
                    foreach (var childName in dataId.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                        (childProcessor as JObject)?.TryGetValue(childName, out childProcessor);
                    dataContainer = childProcessor;
                }
                else
                    dataContainer = root as JArray ?? (IJEnumerable<JToken>)((JObject)root).PropertyValues();

                if (dataContainer != null)
                    foreach (var jToken in dataContainer)
                        load(jToken, defaultSetCode ?? SetCodeDefault);
                else
                    Debug.LogWarning("LoadJsonFromFile::EmptyFile");

                if (string.IsNullOrEmpty(AllCardsUrlPageCountIdentifier))
                    return;

                // Determine AllCardsUrlPageCount
                var allCardsUrlPageCountIdentifier = AllCardsUrlPageCountIdentifier;
                var currentJToken = root;

                for (var delimiterIndex =
                         allCardsUrlPageCountIdentifier.IndexOf(PropertyDef.ObjectDelimiter, StringComparison.Ordinal);
                     delimiterIndex != -1;
                     delimiterIndex =
                         allCardsUrlPageCountIdentifier.IndexOf(PropertyDef.ObjectDelimiter, StringComparison.Ordinal))
                {
                    var currentObjectIdentifier = allCardsUrlPageCountIdentifier[..delimiterIndex];
                    currentJToken = currentJToken[currentObjectIdentifier];
                    if (currentJToken == null)
                    {
                        Debug.LogWarning("LoadJsonFromFile::allCardsUrlPageCountIdentifier:EmptyObject");
                        return;
                    }

                    allCardsUrlPageCountIdentifier = allCardsUrlPageCountIdentifier[(delimiterIndex + 1)..];
                }

                var hasAllCardsUrlPageCount =
                    (currentJToken as JObject)?.ContainsKey(allCardsUrlPageCountIdentifier) ?? false;
                if (!hasAllCardsUrlPageCount)
                    return;

                var allCardsUrlPageCount = currentJToken.Value<int>(allCardsUrlPageCountIdentifier);
                if (allCardsUrlPageCount < 1 || AllCardsUrlPageCount > 1)
                    return;

                AllCardsUrlPageCount = allCardsUrlPageCount;
                if (AllCardsUrlPageCountDivisor > 0)
                    AllCardsUrlPageCount =
                        Mathf.CeilToInt(AllCardsUrlPageCount / (float)AllCardsUrlPageCountDivisor);
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

            var metaProperties = new Dictionary<string, PropertyDefValuePair>();
            var idDef = new PropertyDef(CardIdIdentifier, PropertyType.String);
            PopulateCardProperty(metaProperties, cardJToken, idDef, idDef.Name);
            string cardId;
            if (metaProperties.TryGetValue(CardIdIdentifier, out var cardIdEntry))
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
            if (metaProperties.TryGetValue(CardNameIdentifier, out var cardNameEntry))
                cardName = cardNameEntry.Value;
            else
                Debug.LogWarning("LoadCardFromJToken::ParseNameError");

            var cardBackName = string.Empty;
            if (!string.IsNullOrEmpty(CardNameBackIdentifier))
            {
                var nameBackDef = new PropertyDef(CardNameBackIdentifier, PropertyType.String);
                PopulateCardProperty(metaProperties, cardJToken, nameBackDef, nameBackDef.Name);
                if (metaProperties.TryGetValue(CardNameBackIdentifier.Replace("[].", "[]0."),
                        out var cardNameBackEntry))
                    cardBackName = cardNameBackEntry.Value;
            }

            var imageFileTypeDef = new PropertyDef(CardImageFileTypeIdentifier, PropertyType.String);
            PopulateCardProperty(metaProperties, cardJToken, imageFileTypeDef, imageFileTypeDef.Name);
            var cardImageFileType = CardImageFileType;
            if (!string.IsNullOrEmpty(CardImageFileTypeIdentifier)
                && metaProperties.TryGetValue(CardImageFileTypeIdentifier, out var cardImageFileTypeEntry)
                && !string.IsNullOrEmpty(cardImageFileTypeEntry.Value))
                cardImageFileType = cardImageFileTypeEntry.Value;

            var cardProperties = new Dictionary<string, PropertyDefValuePair>();
            PopulateCardProperties(cardProperties, cardJToken, CardProperties);

            var cardBackProperties = new Dictionary<string, PropertyDefValuePair>();
            if (!string.IsNullOrEmpty(cardBackName))
                PopulateCardProperties(cardBackProperties, cardJToken, CardProperties, "", true);

            // Populate primary property if it was set in the CGS UI
            if (cardJToken["properties"] is JObject { HasValues: true } jObject)
            {
                if (jObject[CardPrimaryProperty] is JObject { HasValues: true } jObject2)
                {
                    var propertyDef = CardProperties.Find(def => def.Name.Equals(CardPrimaryProperty));
                    var propertyValue = jObject2.Value<string>("value");
                    if (propertyDef != null && propertyValue != null)
                    {
                        var propertyDefValuePair = new PropertyDefValuePair
                        {
                            Def = propertyDef,
                            Value = propertyValue
                        };
                        cardProperties[CardPrimaryProperty] = propertyDefValuePair;
                    }
                }
            }

            var cardSets = new Dictionary<string, string>();
            PopulateCardSets(cardSets, cardJToken, defaultSetCode);

            var backs = new List<string>();
            if (cardJToken["backs"] is JArray jArray)
                backs = jArray.ToObject<List<string>>();

            var cardImageWebUrl = string.Empty;
            var backCardImageWebUrl = string.Empty;
            if (!string.IsNullOrEmpty(CardImageProperty))
            {
                // Use the front/back from cardProperties if available
                if (cardProperties.TryGetValue(CardImageProperty, out var valuePair))
                {
                    cardImageWebUrl = valuePair.Value;
                    if (cardBackProperties.TryGetValue(CardImageProperty, out var backValuePair))
                    {
                        backCardImageWebUrl = backValuePair.Value;
                    }
                    else
                    {
                        var backImageDef = new PropertyDef(CardImageProperty, PropertyType.String, string.Empty,
                            string.Empty, false, string.Empty,
                            valuePair.Def?.BackName ?? CardImageProperty, string.Empty, new List<PropertyDef>());
                        PopulateCardProperty(metaProperties, cardJToken, backImageDef, CardImageProperty, true);
                        backCardImageWebUrl = metaProperties.TryGetValue(CardImageProperty, out var backValuePair2)
                            ? backValuePair2.Value
                            : cardImageWebUrl;
                    }
                }
                else
                {
                    // CardImageProperty should resolve to a string, but it may be an object and/or a list
                    var isImagePropertyObject = false;
                    var childName = string.Empty;
                    var childProperties = new List<PropertyDef>();
                    var imageDefName = CardImageProperty;
                    if (imageDefName.Contains(PropertyDef.ObjectDelimiter))
                    {
                        isImagePropertyObject = true;
                        var delimiterIndex =
                            imageDefName.LastIndexOf(PropertyDef.ObjectDelimiter, StringComparison.Ordinal);
                        childName = imageDefName[(delimiterIndex + 1)..];
                        childProperties.Add(new PropertyDef(childName, PropertyType.String));
                        imageDefName = imageDefName[..delimiterIndex];
                    }

                    var isImagePropertyList = imageDefName.Contains('[');
                    if (isImagePropertyList)
                        imageDefName = imageDefName[..imageDefName.IndexOf('[')];

                    var imagePropertyType = isImagePropertyObject switch
                    {
                        true when isImagePropertyList => PropertyType.ObjectList,
                        true => PropertyType.Object,
                        _ => isImagePropertyList ? PropertyType.StringList : PropertyType.String
                    };

                    var imageDef = new PropertyDef(imageDefName, imagePropertyType, string.Empty, string.Empty,
                        false, string.Empty, string.Empty, string.Empty, childProperties);
                    PopulateCardProperty(metaProperties, cardJToken, imageDef, imageDefName);
                    if (isImagePropertyObject && metaProperties.TryGetValue(
                            imageDefName + PropertyDef.ObjectDelimiter + childName,
                            out var cardObjectImageEntry))
                        cardImageWebUrl = cardObjectImageEntry.Value;
                    else if (metaProperties.TryGetValue(
                                 CardImageProperty.Split(new[] { '[' }, StringSplitOptions.None)[0],
                                 out var cardImageEntry))
                        cardImageWebUrl =
                            (cardImageEntry.Value).Split(new[] { EnumDef.Delimiter },
                                StringSplitOptions.None)[0];
                }
            }

            var cardImageUrl = CardImageUrl;
            if (string.IsNullOrEmpty(CardImageProperty) || !string.IsNullOrEmpty(cardImageWebUrl) ||
                !string.IsNullOrEmpty(cardImageUrl))
            {
                if (cardSets.Count == 0)
                    cardSets.Add(defaultSetCode, defaultSetCode);
                foreach (var set in cardSets)
                {
                    if (!Sets.ContainsKey(set.Key))
                        LoadedSets[set.Key] = new Set(set.Key, set.Value);
                    var isReprint = CardNameIsUnique && CardNames.Contains(cardName);
                    if (!isReprint)
                        CardNames.Add(cardName);
                    var cardDuplicateId = cardSets.Count > 1 && isReprint
                        ? cardId + PropertyDef.ObjectDelimiter + set.Key
                        : cardId;
                    var unityCard =
                        new UnityCard(this, cardDuplicateId, cardName, set.Key, cardProperties, isReprint)
                        {
                            ImageFileType = cardImageFileType,
                            ImageWebUrl = cardImageWebUrl
                        };
                    if (!string.IsNullOrEmpty(cardBackName))
                    {
                        var backCardId = cardDuplicateId + "_b";
                        unityCard =
                            new UnityCard(this, cardDuplicateId, cardName, set.Key, cardProperties, isReprint, true,
                                backCardId)
                            {
                                ImageFileType = cardImageFileType,
                                ImageWebUrl = cardImageWebUrl
                            };
                        var backUnityCard =
                            new UnityCard(this, backCardId, cardBackName, set.Key, cardBackProperties, isReprint, true,
                                cardDuplicateId)
                            {
                                ImageFileType = cardImageFileType,
                                ImageWebUrl = backCardImageWebUrl
                            };
                        LoadedCards[backUnityCard.Id] = backUnityCard;
                    }

                    if (backs.Count < 1 || backs.Contains(string.Empty))
                    {
                        LoadedCards[unityCard.Id] = unityCard;
                        isReprint = true;
                    }

                    foreach (var backId in backs)
                    {
                        if (string.Empty.Equals(backId))
                            continue;
                        var backCardId = cardDuplicateId + PropertyDef.ObjectDelimiter + backId;
                        var backUnityCard =
                            new UnityCard(this, backCardId, cardName, set.Key, cardProperties, isReprint, false, backId)
                            {
                                ImageFileType = cardImageFileType,
                                ImageWebUrl = cardImageWebUrl
                            };
                        LoadedCards[backUnityCard.Id] = backUnityCard;
                    }
                }
            }
            else
                Debug.Log($"LoadCardFromJToken::MissingCardImage {cardId}");
        }

        private void PopulateCardProperties(Dictionary<string, PropertyDefValuePair> cardProperties, JToken cardJToken,
            List<PropertyDef> propertyDefs, string keyPrefix = "", bool isBack = false)
        {
            if (cardProperties == null || cardJToken == null || propertyDefs == null)
            {
                Debug.LogError($"PopulateCardProperties::NullInput:{cardProperties}:{propertyDefs}:{cardJToken}");
                return;
            }

            foreach (var property in propertyDefs)
                PopulateCardProperty(cardProperties, cardJToken, property, keyPrefix + property.Name, isBack);
        }

        private void PopulateCardProperty(Dictionary<string, PropertyDefValuePair> cardProperties, JToken cardJToken,
            PropertyDef property, string key, bool isBack = false)
        {
            if (cardProperties == null || cardJToken == null || property == null)
            {
                Debug.LogError($"PopulateCardProperty::MissingInput:{cardProperties}:{cardJToken}:{property}");
                return;
            }

            try
            {
                var newProperty = new PropertyDefValuePair { Def = property };
                StringBuilder listValueBuilder;
                JToken listTokens;
                JObject jObject;
                var identifier = property.Name;
                if (isBack && !string.IsNullOrEmpty(property.BackName))
                    identifier = property.BackName;
                else if (!string.IsNullOrEmpty(property.FrontName))
                    identifier = property.FrontName;
                switch (property.Type)
                {
                    case PropertyType.ObjectEnumList:
                        listValueBuilder = new StringBuilder();
                        listTokens = cardJToken[identifier];
                        if (listTokens != null)
                            foreach (var jToken in listTokens)
                            {
                                if (listValueBuilder.Length > 0)
                                    listValueBuilder.Append(EnumDef.Delimiter);
                                jObject = jToken as JObject;
                                listValueBuilder.Append(jObject?.Value<string>(CardPropertyIdentifier) ?? string.Empty);
                            }

                        newProperty.Value = listValueBuilder.ToString();
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.ObjectList:
                        foreach (var childProperty in property.Properties)
                        {
                            newProperty = new PropertyDefValuePair { Def = childProperty };
                            listValueBuilder = new StringBuilder();
                            var values = new Dictionary<string, PropertyDefValuePair>();
                            var i = 0;
                            listTokens = cardJToken[identifier.Replace("[]", string.Empty)];
                            if (listTokens != null)
                                foreach (var jToken in listTokens)
                                {
                                    var childKey = key + i + PropertyDef.ObjectDelimiter + childProperty.Name;
                                    PopulateCardProperty(values, jToken, childProperty, childKey, isBack);
                                    i++;
                                }

                            foreach (var entry in values)
                            {
                                if (listValueBuilder.Length > 0)
                                    listValueBuilder.Append(EnumDef.Delimiter);
                                listValueBuilder.Append(entry.Value.Value.Replace(EnumDef.Delimiter, ", "));
                            }

                            newProperty.Value = listValueBuilder.ToString();
                            cardProperties[key + PropertyDef.ObjectDelimiter + childProperty.Name] = newProperty;
                            foreach (var newValue in values)
                                cardProperties.Add(newValue.Key, newValue.Value);
                        }

                        break;
                    case PropertyType.ObjectEnum:
                        jObject = cardJToken[identifier] as JObject;
                        newProperty.Value = jObject?.Value<string>(CardPropertyIdentifier) ?? string.Empty;
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.Object:
                        jObject = cardJToken[identifier] as JObject;
                        if (jObject is { HasValues: true })
                            PopulateCardProperties(cardProperties, cardJToken[identifier], property.Properties,
                                key + PropertyDef.ObjectDelimiter, isBack);
                        else
                            PopulateEmptyCardProperty(cardProperties, property, key);
                        break;
                    case PropertyType.StringEnumList:
                    case PropertyType.StringList:
                        listValueBuilder = new StringBuilder();
                        if (string.IsNullOrEmpty(property.Delimiter))
                        {
                            listTokens = cardJToken[identifier];
                            if (listTokens != null)
                            {
                                foreach (var jToken in listTokens)
                                {
                                    if (listValueBuilder.Length > 0)
                                        listValueBuilder.Append(EnumDef.Delimiter);
                                    listValueBuilder.Append(jToken.Value<string>() ?? string.Empty);
                                }

                                if (listValueBuilder.Length == 0)
                                {
                                    var listTokensValueString = listTokens.Value<string>();
                                    if (!string.IsNullOrEmpty(listTokensValueString))
                                    {
                                        foreach (var valueChar in listTokensValueString.ToCharArray())
                                        {
                                            if (listValueBuilder.Length > 0)
                                                listValueBuilder.Append(EnumDef.Delimiter);
                                            listValueBuilder.Append(valueChar);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var token in (cardJToken.Value<string>(identifier) ?? string.Empty).Split(
                                         new[] { property.Delimiter }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (listValueBuilder.Length > 0)
                                    listValueBuilder.Append(EnumDef.Delimiter);
                                listValueBuilder.Append(token);
                            }
                        }

                        newProperty.Value = listValueBuilder.ToString();
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.EscapedString:
                        newProperty.Value = (cardJToken.Value<string>(identifier) ?? string.Empty)
                            .Replace(PropertyDef.EscapeCharacter, string.Empty);
                        cardProperties[key] = newProperty;
                        break;
                    case PropertyType.StringEnum:
                    case PropertyType.Boolean:
                    case PropertyType.Integer:
                    case PropertyType.String:
                    default:
                        if (identifier.Contains('.'))
                        {
                            var segments = identifier.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                            var childProcessorJToken = cardJToken;
                            for (var i = 0; i < segments.Length - 1; i++)
                            {
                                var currentSegment = segments[i];
                                if (currentSegment.EndsWith("[]")
                                    && childProcessorJToken?[currentSegment[..^2]] is JArray { Count: > 0 } arrayToken)
                                    childProcessorJToken = arrayToken[0];
                                else
                                    (childProcessorJToken as JObject)?.TryGetValue(currentSegment,
                                        out childProcessorJToken);
                            }

                            cardJToken = childProcessorJToken;
                            identifier = segments[^1];
                        }

                        newProperty.Value = cardJToken?.Value<string>(identifier) ?? string.Empty;
                        cardProperties[key] = newProperty;
                        break;
                }
            }
            catch
            {
                Debug.LogWarning("PopulateCardProperty::ParsePropertyError:" + property.Name);
                PopulateEmptyCardProperty(cardProperties, property, key);
            }
        }

        private void PopulateEmptyCardProperty(Dictionary<string, PropertyDefValuePair> cardProperties,
            PropertyDef property, string key)
        {
            cardProperties[key] = new PropertyDefValuePair { Def = property, Value = string.Empty };
            foreach (var childProperty in property.Properties)
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

            var dataIdentifier = CardSetIdentifier;
            if (dataIdentifier.Contains('.'))
            {
                var childProcessorJToken = cardJToken;
                var parentNames = CardSetIdentifier.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < parentNames.Length - 1; i++)
                    (childProcessorJToken as JObject)?.TryGetValue(parentNames[i], out childProcessorJToken);
                cardJToken = childProcessorJToken;
                dataIdentifier = parentNames[^1];
            }

            if (CardSetsInList)
            {
                var setJTokens = new List<JToken>();
                try
                {
                    setJTokens = (cardJToken?[dataIdentifier] as JArray)?.ToList() ?? new List<JToken>();
                }
                catch
                {
                    Debug.LogWarning($"PopulateCardSets::BadCardSetIdentifier for {cardJToken}");
                }

                foreach (var setJToken in setJTokens)
                {
                    if (CardSetIsObject)
                    {
                        var setProperties = new Dictionary<string, PropertyDefValuePair>();
                        var setJObject = setJToken as JObject;

                        var setCodeDef = new PropertyDef(SetCodeIdentifier, PropertyType.String);
                        PopulateCardProperty(setProperties, setJObject, setCodeDef, setCodeDef.Name);
                        var setNameDef = new PropertyDef(SetNameIdentifier, PropertyType.String);
                        PopulateCardProperty(setProperties, setJObject, setNameDef, setNameDef.Name);

                        string setCode;
                        if (setProperties.TryGetValue(SetCodeIdentifier, out var setCodeEntry))
                            setCode = setCodeEntry.Value;
                        else
                            setCode = setJObject?.Value<string>(SetCodeIdentifier) ?? defaultSetCode;

                        string setName;
                        if (setProperties.TryGetValue(SetNameIdentifier, out var setNameEntry))
                            setName = setNameEntry.Value;
                        else
                            setName = setJObject?.Value<string>(SetNameIdentifier) ?? setCode;

                        cardSets[setCode] = setName;
                    }
                    else if (CardSetsInListIsCsv)
                    {
                        var code = setJToken.Value<string>() ?? defaultSetCode;
                        cardSets[code] = code;
                    }
                    else
                    {
                        string code;
                        string name;
                        if (setJToken.HasValues)
                        {
                            code = setJToken.Value<string>(dataIdentifier) ?? defaultSetCode;
                            name = setJToken.Value<string>(CardSetNameIdentifier) ?? code;
                        }
                        else
                        {
                            code = setJToken.ToString();
                            name = code;
                        }

                        cardSets[code] = name;
                    }
                }
            }
            else if (CardSetsInListIsCsv)
            {
                var codesCsv = cardJToken?.Value<string>(dataIdentifier) ?? defaultSetCode;
                var namesCsv = cardJToken?.Value<string>(CardSetNameIdentifier) ?? codesCsv;
                var codes = codesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var names = namesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < codes.Length; i++)
                {
                    var code = codes[i];
                    var name = i < names.Length ? names[i] : code;
                    cardSets[code] = name;
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

                var setCode = setJObject?.Value<string>(SetCodeIdentifier) ?? defaultSetCode;
                var setName = setJObject?.Value<string>(SetNameIdentifier) ?? setCode;
                cardSets[setCode] = setName;
            }
            else
            {
                var code = cardJToken?.Value<string>(dataIdentifier) ?? defaultSetCode;
                var name = cardJToken?.Value<string>(CardSetNameIdentifier) ?? code;
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

            var setName = setJToken.Value<string>(SetNameIdentifier) ?? setCode;
            var setCardsUrl = setJToken.Value<string>(SetCardsUrlIdentifier) ?? string.Empty;

            LoadedSets[setCode] = new Set(setCode, setName, setCardsUrl);

            var cards = setJToken.Value<JArray>(SetCardsIdentifier);
            if (cards == null)
                return;

            foreach (var jToken in cards)
                LoadCardFromJToken(jToken, setCode);
        }

        public void Add(UnityCard card, bool writeAllCardsJson = true)
        {
            var isReprint = CardNameIsUnique && CardNames.Contains(card.Name);
            if (!isReprint)
                CardNames.Add(card.Name);
            LoadedCards[card.Id] = card;

            if (!Sets.ContainsKey(card.SetCode))
                LoadedSets[card.SetCode] = new Set(card.SetCode, card.SetCode);

            if (writeAllCardsJson)
                WriteAllCardsJson();
        }

        public void WriteAllCardsJson()
        {
            var allCardsJson = JsonConvert.SerializeObject(Cards.Values.ToList(), new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            });
            File.WriteAllText(CardsFilePath, allCardsJson);
        }

        public void Remove(Card card, bool writeAllCardsJson = true)
        {
            if (card?.Id != null)
                Remove(card.Id, writeAllCardsJson);
        }

        private void Remove(string cardId, bool writeAllCardsJson = true)
        {
            LoadedCards.Remove(cardId);
            if (writeAllCardsJson)
                WriteAllCardsJson();
        }

        [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
        public IEnumerable<UnityCard> FilterCards(CardSearchFilters filters)
        {
            if (filters == null)
            {
                Debug.LogError("FilterCards::NullFilters");
                yield break;
            }

            foreach (var card in Cards.Values)
            {
                if (!string.IsNullOrEmpty(filters.Name) && !filters.Name.ToLower().Split(
                        new[] { CardSearchFilters.Delimiter },
                        StringSplitOptions.RemoveEmptyEntries).All(card.Name.ToLower().Contains))
                    continue;
                if (!string.IsNullOrEmpty(filters.Id) && !card.Id.ToLower().Contains(filters.Id.ToLower()))
                    continue;
                if (!string.IsNullOrEmpty(filters.SetCode) &&
                    !card.SetCode.ToLower().Contains(filters.SetCode.ToLower()))
                    continue;
                var propsMatch = true;
                foreach (var filter in filters.StringProperties)
                    if (!card.GetPropertyValueString(filter.Key).ToLower().Contains(filter.Value.ToLower()))
                        propsMatch = false;
                foreach (var filter in filters.IntMinProperties)
                    if (card.GetPropertyValueInt(filter.Key) < filter.Value)
                        propsMatch = false;
                foreach (var filter in filters.IntMaxProperties)
                    if (card.GetPropertyValueInt(filter.Key) > filter.Value)
                        propsMatch = false;
                foreach (var filter in filters.BoolProperties)
                    if (card.GetPropertyValueBool(filter.Key) != filter.Value)
                        propsMatch = false;
                foreach (var filter in filters.EnumProperties)
                {
                    var enumDef = Enums.FirstOrDefault(def => def.Property.Equals(filter.Key));
                    if (enumDef == null)
                    {
                        propsMatch = false;
                        continue;
                    }

                    if ((card.GetPropertyValueEnum(filter.Key) & filter.Value) != 0)
                        continue;
                    var propertyDef = CardProperties.FirstOrDefault(prop => prop.Name.Equals(filter.Key));
                    if (propertyDef != null)
                        propsMatch = propsMatch && (filter.Value == (1 << enumDef.Values.Count)) && propertyDef
                            .DisplayEmpty
                            .Equals(card.GetPropertyValueString(filter.Key));
                }

                if (propsMatch)
                    yield return card;
            }
        }

        public void ClearError()
        {
            Error = string.Empty;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
