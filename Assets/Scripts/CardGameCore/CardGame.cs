using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[JsonObject(MemberSerialization.OptIn)]
public class CardGame
{
    public const string DefaultImageFileType = "png";
    public const string AllSetsFileName = "AllSets.json";
    public const string AllCardsFileName = "AllCards.json";
    public const string BackgroundImageFileName = "Background";
    public const string CardBackImageFileName = "CardBack";

    public string Name { get; set; }

    public string URL { get; set; }

    public string FilePathBase { get; set; }

    public string DefinitionFilePath { get; set; }

    [JsonProperty]
    public string AllCardsURL { get; set; }

    [JsonProperty]
    public bool AllCardsZipped { get; set; }

    [JsonProperty]
    public string AllSetsURL { get; set; }

    [JsonProperty]
    public bool AllSetsZipped { get; set; }

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
    public string CardImageBaseURL { get; set; }

    [JsonProperty]
    public string CardImageRegex { get; set; }

    [JsonProperty]
    public string CardImageType { get; set; }

    [JsonProperty]
    public string CardNameIdentifier { get; set; }

    [JsonProperty]
    public string CardSetIdentifier { get; set; }

    [JsonProperty]
    public string CardPrimaryProperty { get; set; }

    [JsonProperty]
    public List<PropertyDef> CardProperties { get; set; }

    [JsonProperty]
    public string SetCodeIdentifier { get; set; }

    [JsonProperty]
    public string SetNameIdentifier { get; set; }

    private List<Set> sets;
    private List<Card> cards;
    private Sprite backgroundImage;
    private Sprite cardBackImage;
    private bool isLoaded;

    public CardGame(string name, string url)
    {
        Name = name;
        URL = url;

        FilePathBase = Application.persistentDataPath + "/" + Name;
        DefinitionFilePath = FilePathBase + "/" + Name + ".json";

        AllCardsZipped = false;
        AllSetsZipped = false;
        BackgroundImageType = DefaultImageFileType;
        CardBackImageType = DefaultImageFileType;
        CardImageType = DefaultImageFileType;
        CardIdIdentifier = "id";
        CardNameIdentifier = "name";
        CardSetIdentifier = "set";
        SetCodeIdentifier = "code";
        SetNameIdentifier = "name";

        sets = new List<Set>();
        cards = new List<Card>();
        backgroundImage = Resources.Load<Sprite>(BackgroundImageFileName);
        cardBackImage = Resources.Load<Sprite>(CardBackImageFileName);

        isLoaded = false;
    }

    public IEnumerator LoadFromURL()
    {
        Debug.Log("Loading defintion for " + Name + " from " + URL);
        WWW loadDefinition = new WWW(URL);
        yield return loadDefinition;

        if (string.IsNullOrEmpty(loadDefinition.error)) {
            Debug.Log(" Game definition received from web");

            Debug.Log(" Saving game definition to : " + DefinitionFilePath);
            if (!System.IO.Directory.Exists(FilePathBase)) {
                Debug.Log(" Game file directory does not exist, so creating it");
                Directory.CreateDirectory(FilePathBase);
            }
            File.WriteAllBytes(DefinitionFilePath, loadDefinition.bytes);
            Debug.Log(" Game definition written to file");
        } else { 
            Debug.LogError(" Failed to load game definition from the web!");
        }

        LoadFromFile();

        Debug.Log(" Loading Sets");
        if (!string.IsNullOrEmpty(AllSetsURL))
            yield return GetFromURL(AllSetsURL, AllSetsFileName);
        LoadSetsFromFile();

        Debug.Log(" Loading Cards");
        if (!string.IsNullOrEmpty(AllCardsURL))
            yield return GetFromURL(AllCardsURL, AllCardsFileName);
        LoadCardsFromFile();

        Debug.Log(" Loading Background Image");
        if (!string.IsNullOrEmpty(BackgroundImageURL) && !File.Exists(FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageType))
            yield return GetFromURL(BackgroundImageURL, BackgroundImageFileName + "." + BackgroundImageType);
        string imageFileURL = "file://" + FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageType;
        if (File.Exists(FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageType)) {
            Debug.Log(" Attempting to load background from: " + imageFileURL);
            WWW imageLoader = new WWW(imageFileURL);
            yield return imageLoader;
            if (string.IsNullOrEmpty(imageLoader.error))
                backgroundImage = Sprite.Create(imageLoader.texture, new Rect(0, 0, imageLoader.texture.width, imageLoader.texture.height), new Vector2(0.5f, 0.5f));
            else
                Debug.LogWarning(" Failed to load background image from file: " + imageLoader.error);
        } else
            Debug.Log(" No background image saved, so keeping the default");

        Debug.Log(" Loading Card Back Image");
        if (!string.IsNullOrEmpty(CardBackImageURL) && !File.Exists(FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageType))
            yield return GetFromURL(CardBackImageURL, CardBackImageFileName + "." + CardBackImageType);
        imageFileURL = "file://" + FilePathBase + "/" + CardBackImageFileName + "." + CardBackImageType;
        if (File.Exists(FilePathBase + "/" + CardBackImageFileName + "." + CardBackImageType)) {
            Debug.Log(" Attempting to load card back from: " + imageFileURL);
            WWW imageLoader = new WWW(imageFileURL);
            yield return imageLoader;
            if (string.IsNullOrEmpty(imageLoader.error))
                cardBackImage = Sprite.Create(imageLoader.texture, new Rect(0, 0, imageLoader.texture.width, imageLoader.texture.height), new Vector2(0.5f, 0.5f));
            else
                Debug.LogWarning(" Failed to load card back image from file: " + imageLoader.error);
        } else
            Debug.Log(" No card back image saved, so keeping the default");

        Debug.Log(Name + " finished loading");
        isLoaded = true;

    }

    public void LoadFromFile()
    {
        Debug.Log("Loading definition for " + Name + " from " + DefinitionFilePath);

        string gameJson = File.ReadAllText(DefinitionFilePath);
        try {
            JsonConvert.PopulateObject(gameJson, this);
        } catch (Exception e) {
            Debug.LogError("Failed to load card game! Error: " + e);
            // TODO: add user feedback to all the logerror and logwarning, so that they are aware
        }

        Debug.Log(" Load from file completed");
    }

    public IEnumerator GetFromURL(string URL, string fileName)
    {
        Debug.Log("Gettings from " + URL);
        WWW loader = new WWW(URL);
        yield return loader;

        if (string.IsNullOrEmpty(loader.error)) {
            Debug.Log(" Received from web. Saving to : " + FilePathBase);
            if (!System.IO.Directory.Exists(FilePathBase)) {
                Debug.Log(" Game file directory does not exist, so creating it");
                Directory.CreateDirectory(FilePathBase);
            }
            File.WriteAllBytes(FilePathBase + "/" + fileName, loader.bytes);
            Debug.Log(" Written to file");
        } else { 
            Debug.LogError(" Failed to load  " + fileName + " from " + URL);
            // TODO: BETTER ERROR HANDLING?
        }
    }

    public void LoadSetsFromFile()
    {
        if (!File.Exists(FilePathBase + "/" + AllSetsFileName)) {
            Debug.Log("Sets file does not exist, so it will not be loaded");
            return;
        }
            
        Debug.Log("Loading sets for " + Name + " from " + FilePathBase + "/" + AllSetsFileName);

        try {
            string setsJson = File.ReadAllText(FilePathBase + "/" + AllSetsFileName);
            JArray setJArray = JsonConvert.DeserializeObject<JArray>(setsJson);
            foreach (JToken setJToken in setJArray) {
                string setCode = setJToken.Value<string>(SetCodeIdentifier);
                string setName = setJToken.Value<string>(SetNameIdentifier);
                if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(setName))
                    sets.Add(new Set { Code = setCode, Name = setName });
                else
                    Debug.LogWarning("Read empty sety in the list of sets! Ignoring it");
            }
        } catch (Exception e) {
            Debug.LogError("Failed to load sets! Error: " + e);
            // TODO: add user feedback to all the logerror and logwarning, so that they are aware
        }

        Debug.Log(" Load sets from file completed");
        
    }

    public void LoadCardsFromFile()
    {
        if (!File.Exists(FilePathBase + "/" + AllCardsFileName)) {
            Debug.Log("Card file does not exist, so it will not be loaded");
            return;
        }

        Debug.Log("Loading cards for " + Name + " from " + FilePathBase + "/" + AllCardsFileName);

        try {
            string cardsJson = File.ReadAllText(FilePathBase + "/" + AllCardsFileName);
            JArray cardsJArray = JsonConvert.DeserializeObject<JArray>(cardsJson);
            foreach (JToken cardJToken in cardsJArray) {
                string cardId = cardJToken.Value<string>(CardIdIdentifier);
                string cardName = cardJToken.Value<string>(CardNameIdentifier);
                string cardSet = cardJToken.Value<string>(CardSetIdentifier);
                Dictionary<string, CardPropertySet> cardProps = new Dictionary<string, CardPropertySet>();
                foreach (PropertyDef prop in CardProperties) {
                    cardProps [prop.Name] = new CardPropertySet() {
                        Key = prop,
                        Value = new PropertyDefValue() { Value = cardJToken.Value<string>(prop.Name) }
                    };
                }
                if (!string.IsNullOrEmpty(cardId))
                    cards.Add(new Card(cardId, cardName, cardSet, cardProps));
                else
                    Debug.LogWarning("Read card without id in the list of cards! Ignoring it");
                
            }
        } catch (Exception e) {
            Debug.LogError("Failed to load cards! Error: " + e);
            // TODO: add user feedback to all the logerror and logwarning, so that they are aware
        }

        Debug.Log(" Load cards from file completed");
    }


    public string SerializedDeclaration {
        get {
            return "{ \"name\": \"" + Name + "\", \"url\": \"" + URL + "\" }";
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
