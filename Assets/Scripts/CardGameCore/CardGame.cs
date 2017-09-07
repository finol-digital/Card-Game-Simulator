using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public delegate void LoadJTokenDelegate(JToken jToken);

[JsonObject(MemberSerialization.OptIn)]
public class CardGame
{
    public const string AllSetsFileName = "AllSets.json";
    public const string AllCardsFileName = "AllCards.json";
    public const string BackgroundImageFileName = "Background";
    public const string CardBackImageFileName = "CardBack";
    public const int DefaultCopiesOfCardPerDeck = 4;
    public const int DefaultDeckCardStackCount = 15;
    public const string DefaultDeckFileType = "txt";
    public const string DefaultImageFileType = "png";

    public string Name { get; set; }

    public string FilePathBase { get; set; }

    public string ConfigFilePath { get; set; }

    public string DecksFilePath { get; set; }

    [JsonProperty]
    public string AllCardsURL { get; set; }

    [JsonProperty]
    public string AllSetsURL { get; set; }

    [JsonProperty]
    public bool AutoUpdate { get; set; }

    [JsonProperty]
    public string AutoUpdateURL { get; set; }

    [JsonProperty]
    public string BackgroundImageType { get; set; }

    [JsonProperty]
    public string BackgroundImageURL { get; set; }

    [JsonProperty]
    public string CardBackImageType { get; set; }

    [JsonProperty]
    public string CardBackImageURL { get; set; }

    [JsonProperty]
    public string CardIdIdentifier { get; set; }

    [JsonProperty]
    public string CardImageType { get; set; }

    [JsonProperty]
    public string CardImageURLBase { get; set; }

    [JsonProperty]
    public string CardNameIdentifier { get; set; }

    [JsonProperty]
    public string CardSetIdentifier { get; set; }

    [JsonProperty]
    public string CardPrimaryProperty { get; set; }

    [JsonProperty]
    public List<PropertyDef> CardProperties { get; set; }

    [JsonProperty]
    public int CopiesOfCardPerDeck { get; set; }

    [JsonProperty]
    public int DeckCardStackCount { get; set; }

    [JsonProperty]
    public string DeckFileType { get; set; }

    [JsonProperty]
    public string SetCodeIdentifier { get; set; }

    [JsonProperty]
    public string SetNameIdentifier { get; set; }

    private List<Set> _sets;
    private List<Card> _cards;
    private UnityEngine.Sprite _backgroundImage;
    private UnityEngine.Sprite _cardBackImage;
    private bool _isLoaded;
    private string _error;

    public CardGame(string name, string url = "")
    {
        Name = name;
        AutoUpdateURL = url;

        FilePathBase = CardGameManager.GamesFilePathBase + "/" + Name;
        ConfigFilePath = FilePathBase + "/" + Name + ".json";
        DecksFilePath = FilePathBase + "/decks";

        BackgroundImageType = DefaultImageFileType;
        CardBackImageType = DefaultImageFileType;
        CardImageType = DefaultImageFileType;
        CardIdIdentifier = "id";
        CardNameIdentifier = "name";
        CardSetIdentifier = "set";
        CopiesOfCardPerDeck = DefaultCopiesOfCardPerDeck;
        DeckCardStackCount = DefaultDeckCardStackCount;
        DeckFileType = DefaultDeckFileType;
        SetCodeIdentifier = "code";
        SetNameIdentifier = "name";

        _sets = new List<Set>();
        _cards = new List<Card>();
        _isLoaded = false;
        _error = null;
    }

    public IEnumerator Load()
    {
        UnityEngine.Debug.Log("Loading config for " + Name);
        if (!string.IsNullOrEmpty(AutoUpdateURL) && (AutoUpdate || !File.Exists(ConfigFilePath)))
            yield return UnityExtensionMethods.SaveURLToFile(AutoUpdateURL, ConfigFilePath);
        try { 
            JsonConvert.PopulateObject(File.ReadAllText(ConfigFilePath), this);
        } catch (Exception e) {
            UnityEngine.Debug.LogError("Failed to load card game! Error: " + e.Message);
            _error = e.Message;
            yield break;
        }

        UnityEngine.Debug.Log("Loading Sets and Cards for " + Name);
        string setsFile = FilePathBase + "/" + AllSetsFileName;
        if (!string.IsNullOrEmpty(AllSetsURL) && (AutoUpdate || !File.Exists(setsFile)))
            yield return UnityExtensionMethods.SaveURLToFile(AllSetsURL, setsFile);
        string cardsFile = FilePathBase + "/" + AllCardsFileName;
        if (!string.IsNullOrEmpty(AllCardsURL) && (AutoUpdate || !File.Exists(cardsFile)))
            yield return UnityExtensionMethods.SaveURLToFile(AllCardsURL, cardsFile);
        try { 
            LoadJSONFromFile(setsFile, LoadSetFromJToken);
            LoadJSONFromFile(cardsFile, LoadCardFromJToken);
        } catch (Exception e) {
            UnityEngine.Debug.LogError("Failed to load card game data! Error: " + e.Message);
            CardGameManager.Instance.ShowMessage("Failed to load cards for " + Name + ". Error: " + e.Message);
            _error = e.Message;
            yield break;
        }

        UnityEngine.Debug.Log("Loading Background Image for " + Name);
        UnityEngine.Sprite loadedImage = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<UnityEngine.Sprite>(UnityExtensionMethods.LoadOrGetImage(FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageType, BackgroundImageURL), (output) => loadedImage = output);
        if (loadedImage != null)
            _backgroundImage = loadedImage;

        UnityEngine.Debug.Log("Loading Card Back Image for " + Name);
        loadedImage = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<UnityEngine.Sprite>(UnityExtensionMethods.LoadOrGetImage(FilePathBase + "/" + CardBackImageFileName + "." + CardBackImageType, CardBackImageURL), (output) => loadedImage = output);
        if (loadedImage != null)
            _cardBackImage = loadedImage;

        UnityEngine.Debug.Log(Name + " finished loading");
        _isLoaded = true;
    }

    public void LoadJSONFromFile(string file, LoadJTokenDelegate load)
    {
        if (!File.Exists(file)) {
            UnityEngine.Debug.Log("JSON file does not exist, so it will not be loaded");
            return;
        }

        JArray jArray = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(file));
        foreach (JToken jToken in jArray)
            load(jToken);
    }

    public void LoadSetFromJToken(JToken setJToken)
    {
        if (setJToken == null) {
            UnityEngine.Debug.LogWarning("Attempted to load a null setJToken! Ignoring it");
            return;
        }

        string setCode = setJToken.Value<string>(SetCodeIdentifier);
        string setName = setJToken.Value<string>(SetNameIdentifier);
        if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(setName))
            _sets.Add(new Set { Code = setCode, Name = setName });
        else
            UnityEngine.Debug.LogWarning("Read empty set in the list of sets! Ignoring it");
    }

    public void LoadCardFromJToken(JToken cardJToken)
    {
        if (cardJToken == null) {
            UnityEngine.Debug.LogWarning("Attempted to load a null cardJToken! Ignoring it");
            return;
        }

        string cardId = cardJToken.Value<string>(CardIdIdentifier);
        string cardName = cardJToken.Value<string>(CardNameIdentifier);
        string cardSet = cardJToken.Value<string>(CardSetIdentifier);
        Dictionary<string, PropertySet> cardProps = new Dictionary<string, PropertySet>();
        foreach (PropertyDef prop in CardProperties) {
            cardProps [prop.Name] = new PropertySet() {
                Key = prop,
                Value = new PropertyDefValue() { Value = cardJToken.Value<string>(prop.Name) }
            };
        }
        if (!string.IsNullOrEmpty(cardId))
            _cards.Add(new Card(cardId, cardName, cardSet, cardProps));
        else
            UnityEngine.Debug.LogWarning("Read card without id in the list of cards! Ignoring it");
    }

    public IEnumerable<Card> FilterCards(string id, string name, string setCode, Dictionary<string, string> properties)
    {
        if (id == null || name == null || setCode == null || properties == null) {
            UnityEngine.Debug.LogWarning("Null parameter(s) passed to FilterCards! Exiting filter early");
            yield break;
        }

        foreach (Card card in Cards) {
            if (card.Id.ToLower().Contains(id.ToLower())
                && card.Name.ToLower().Contains(name.ToLower())
                && card.SetCode.ToLower().Contains(setCode.ToLower())) {
                bool propsMatch = true;
                foreach (KeyValuePair<string, string> entry in properties)
                    if (!(card.Properties [entry.Key].Value.Value).ToLower().Contains(entry.Value.ToLower()))
                        propsMatch = false;
                if (propsMatch)
                    yield return card;
            }
        }
    }

    public List<Set> Sets {
        get {
            if (_sets == null)
                _sets = new List<Set>();
            return _sets;
        }
    }

    public List<Card> Cards {
        get {
            if (_cards == null)
                _cards = new List<Card>();
            return _cards;
        }
    }

    public UnityEngine.Sprite BackgroundImage {
        get {
            if (_backgroundImage == null)
                _backgroundImage = UnityEngine.Resources.Load<UnityEngine.Sprite>(BackgroundImageFileName);
            return _backgroundImage;
        }
    }

    public UnityEngine.Sprite CardBackImage {
        get {
            if (_cardBackImage == null)
                _cardBackImage = UnityEngine.Resources.Load<UnityEngine.Sprite>(CardBackImageFileName);
            return _cardBackImage;
        }
    }

    public bool IsLoaded {
        get {
            return _isLoaded;
        }
    }

    public string Error {
        get {
            return _error;
        }
    }
}
