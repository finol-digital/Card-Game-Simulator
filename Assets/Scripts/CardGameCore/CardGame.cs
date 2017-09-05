using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public bool AllCardsZipped { get; set; }

    [JsonProperty]
    public string AllSetsURL { get; set; }

    [JsonProperty]
    public bool AllSetsZipped { get; set; }

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

    private List<Set> sets;
    private List<Card> cards;
    private Sprite backgroundImage;
    private Sprite cardBackImage;
    private bool isLoaded;

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

        sets = new List<Set>();
        cards = new List<Card>();
        backgroundImage = Resources.Load<Sprite>(BackgroundImageFileName);
        cardBackImage = Resources.Load<Sprite>(CardBackImageFileName);

        isLoaded = false;
    }

    public IEnumerator Load()
    {
        Debug.Log("Loading config for " + Name);
        if (!string.IsNullOrEmpty(AutoUpdateURL) && (AutoUpdate || !File.Exists(ConfigFilePath)))
            yield return UnityExtensionMethods.SaveURLToFile(AutoUpdateURL, ConfigFilePath);
        LoadConfigFromFile();

        Debug.Log("Loading Sets for " + Name);
        string setsFile = FilePathBase + "/" + AllSetsFileName;
        if (!string.IsNullOrEmpty(AllSetsURL) && (AutoUpdate || !File.Exists(setsFile)))
            yield return UnityExtensionMethods.SaveURLToFile(AllSetsURL, setsFile);
        LoadJSONFromFile(setsFile, LoadSetFromJToken);

        Debug.Log("Loading Cards for " + Name);
        string cardsFile = FilePathBase + "/" + AllCardsFileName;
        if (!string.IsNullOrEmpty(AllCardsURL) && (AutoUpdate || !File.Exists(cardsFile)))
            yield return UnityExtensionMethods.SaveURLToFile(AllCardsURL, cardsFile);
        LoadJSONFromFile(cardsFile, LoadCardFromJToken);

        Debug.Log("Loading Background Image for " + Name);
        Sprite loadedImage = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.LoadOrGetImage(FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageType, BackgroundImageURL), (output) => loadedImage = output);
        if (loadedImage != null)
            backgroundImage = loadedImage;

        Debug.Log("Loading Card Back Image for " + Name);
        loadedImage = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.LoadOrGetImage(FilePathBase + "/" + CardBackImageFileName + "." + CardBackImageType, CardBackImageURL), (output) => loadedImage = output);
        if (loadedImage != null)
            cardBackImage = loadedImage;

        Debug.Log(Name + " finished loading");
        isLoaded = true;
    }

    public void LoadConfigFromFile()
    {
        try {
            JsonConvert.PopulateObject(File.ReadAllText(ConfigFilePath), this);
        } catch (Exception e) {
            Debug.LogError("Failed to load card game! Error: " + e);
            // TODO: add user feedback to all the logerror and logwarning, so that they are aware
        }
    }

    public void LoadJSONFromFile(string file, LoadJTokenDelegate load)
    {
        if (!File.Exists(file)) {
            Debug.Log("JSON file does not exist, so it will not be loaded");
            return;
        }

        try {
            JArray jArray = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(file));
            foreach (JToken jToken in jArray)
                load(jToken);
        } catch (Exception e) {
            Debug.LogError("Failed to load! Error: " + e);
            // TODO: add user feedback to all the logerror and logwarning, so that they are aware
        }
    }

    public void LoadSetFromJToken(JToken setJToken)
    {
        string setCode = setJToken.Value<string>(SetCodeIdentifier);
        string setName = setJToken.Value<string>(SetNameIdentifier);
        if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(setName))
            sets.Add(new Set { Code = setCode, Name = setName });
        else
            Debug.LogWarning("Read empty sety in the list of sets! Ignoring it");
    }

    public void LoadCardFromJToken(JToken cardJToken)
    {
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
            cards.Add(new Card(cardId, cardName, cardSet, cardProps));
        else
            Debug.LogWarning("Read card without id in the list of cards! Ignoring it");
    }

    public IEnumerable<Card> FilterCards(string id, string name, string setCode, Dictionary<string, string> properties)
    {
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
        get { return sets; }
    }

    public List<Card> Cards { 
        get { return cards; }
    }

    public Sprite BackgroundImage { 
        get { return backgroundImage; }
    }

    public Sprite CardBackImage { 
        get { return cardBackImage; }
    }

    public bool IsLoaded {
        get { return isLoaded; }
    }

}
