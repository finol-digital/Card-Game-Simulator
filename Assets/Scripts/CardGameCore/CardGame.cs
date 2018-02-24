using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public delegate void LoadJTokenDelegate(JToken jToken,string defaultValue);

[JsonObject(MemberSerialization.OptIn)]
public class CardGame
{
    public string FilePathBase => CardGameManager.GamesFilePathBase + "/" + Name;
    public string ConfigFilePath => FilePathBase + "/" + Name + ".json";
    public string CardsFilePath => FilePathBase + "/AllCards.json";
    public string SetsFilePath => FilePathBase + "/AllSets.json";
    public const string BackgroundImageFileName = "Background";
    public string BackgroundImageFilePath => FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageFileType;
    public const string CardBackImageFileName = "CardBack";
    public string CardBackImageFilePath => FilePathBase +"/" + CardBackImageFileName + "." + CardBackImageFileType;
    public string DecksFilePath => FilePathBase + "/decks";
    public string GameBoardsFilePath => FilePathBase + "/boards";
    public float AspectRatio => CardSize.y > 0 ? Mathf.Abs(CardSize.x / CardSize.y) : 0.715f;

    [JsonProperty]
    public string Name { get; set; }

    [JsonProperty]
    public string AllCardsUrl { get; set; } = "";

    [JsonProperty]
    public int AllCardsUrlPageCount { get; set; } = 1;

    [JsonProperty]
    public string AllCardsUrlPageIdentifier { get; set; } = "?page=";

    [JsonProperty]
    public bool AllCardsZipped { get; set; }

    [JsonProperty]
    public string AllSetsUrl { get; set; } = "";

    [JsonProperty]
    public bool AllSetsZipped { get; set; }

    [JsonProperty]
    public bool AutoUpdate { get; set; }

    [JsonProperty]
    public string AutoUpdateUrl { get; set; }

    [JsonProperty]
    public string BackgroundImageFileType { get; set; } = "png";

    [JsonProperty]
    public string BackgroundImageUrl { get; set; } = "";

    [JsonProperty]
    public string CardBackImageFileType { get; set; } = "png";

    [JsonProperty]
    public string CardBackImageUrl { get; set; } = "";

    [JsonProperty]
    public string CardDataIdentifier { get; set; } = "";

    [JsonProperty]
    public string CardIdIdentifier { get; set; }= "id";

    [JsonProperty]
    public string CardImageFileType { get; set; } = "png";

    [JsonProperty]
    public string CardImageUrl { get; set; } = "";

    [JsonProperty]
    public string CardNameIdentifier { get; set; } = "name";

    [JsonProperty]
    public bool CardNameIsAtTop { get; set; } = true;

    [JsonProperty]
    public string CardSetIdentifier { get; set; } = "set";

    [JsonProperty]
    public string CardPrimaryProperty { get; set; } = "";

    [JsonProperty]
    public List<PropertyDef> CardProperties { get; set; } = new List<PropertyDef>();

    [JsonProperty]
    public Vector2 CardSize { get; set; } = new Vector2(2.5f, 3.5f);

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
    public bool GameHasDiscardZone { get; set; }

    [JsonProperty]
    public int GameStartHandCount { get; set; }

    [JsonProperty]
    public int GameStartPointsCount { get; set; }

    [JsonProperty]
    public string HsdPropertyId { get; set; } = "dbfId";

    [JsonProperty]
    public Vector2 PlayAreaSize { get; set; } = new Vector2(23.5f, 20.25f);

    [JsonProperty]
    public string SetCardsIdentifier { get; set; } = "cards";

    [JsonProperty]
    public string SetCodeIdentifier { get; set; } = "code";

    [JsonProperty]
    public string SetDataIdentifier { get; set; } = "";

    [JsonProperty]
    public string SetNameIdentifier { get; set; } = "name";

    public Dictionary<string, Card> Cards { get; } = new Dictionary<string, Card>();
    public Dictionary<string, Set> Sets { get; } = new Dictionary<string, Set>();

    public bool IsDownloading { get; private set; }
    public bool IsLoaded { get; private set; }
    public string Error { get; private set; }

    private Sprite _backgroundImageSprite;
    private Sprite _cardBackImageSprite;

    public CardGame(string name = Set.DefaultCode, string url = "")
    {
        Name = name ?? Set.DefaultCode;
        AutoUpdateUrl = url ?? string.Empty;
        Error = string.Empty;
    }

    public IEnumerator Download()
    {
        if (IsDownloading)
            yield break;
        IsDownloading = true;

        string initialDirectory = FilePathBase;
        yield return UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl, ConfigFilePath);
        try {
            if (!IsLoaded)
                JsonConvert.PopulateObject(File.ReadAllText(ConfigFilePath), this);
        } catch (Exception e) {
            Error+= e.Message;
            IsDownloading = false;
            yield break;
        }
        if (!initialDirectory.Equals(FilePathBase)) {
            CardGameManager.Instance.StartCoroutine(UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl, ConfigFilePath));
            Directory.Delete(initialDirectory, true);
        }

        yield return UnityExtensionMethods.SaveUrlToFile(AllCardsUrl, CardsFilePath
                                                         + (AllCardsZipped ? UnityExtensionMethods.ZipExtension : string.Empty));
        if (AllCardsZipped)
            UnityExtensionMethods.ExtractZip(CardsFilePath + UnityExtensionMethods.ZipExtension, FilePathBase);
        for (int page = 2; page <= AllCardsUrlPageCount; page++)
            yield return UnityExtensionMethods.SaveUrlToFile(AllCardsUrl + AllCardsUrlPageIdentifier + page, CardsFilePath + page);

        yield return UnityExtensionMethods.SaveUrlToFile(AllSetsUrl, SetsFilePath
                                                         + (AllSetsZipped ? UnityExtensionMethods.ZipExtension : string.Empty));
        if (AllSetsZipped)
            UnityExtensionMethods.ExtractZip(SetsFilePath + UnityExtensionMethods.ZipExtension, FilePathBase);

        yield return UnityExtensionMethods.SaveUrlToFile(BackgroundImageUrl, BackgroundImageFilePath);
        yield return UnityExtensionMethods.SaveUrlToFile(CardBackImageUrl, CardBackImageFilePath);

        foreach (GameBoardUrl boardUrl in GameBoardUrls)
            yield return UnityExtensionMethods.SaveUrlToFile(boardUrl.Url, GameBoardsFilePath + "/" + boardUrl.Id + "." + GameBoardFileType);

        foreach (DeckUrl deckUrl in DeckUrls)
            yield return UnityExtensionMethods.SaveUrlToFile(deckUrl.Url, DecksFilePath + "/" + deckUrl.Name + "." + DeckFileType);

        if (!IsLoaded) {
            Load(true);
            if (AllCardsUrlPageCount > 1)
                CardGameManager.Instance.StartCoroutine(CardGameManager.Instance.LoadCards());
        }
        IsDownloading = false;
    }

    public void Load(bool didDownload = false)
    {
        try {
            if (!didDownload)
                JsonConvert.PopulateObject(File.ReadAllText(ConfigFilePath), this);
            BackgroundImageSprite = UnityExtensionMethods.CreateSprite(BackgroundImageFilePath);
            CardBackImageSprite = UnityExtensionMethods.CreateSprite(CardBackImageFilePath);
            CreateEnumLookups();
            LoadJsonFromFile(CardsFilePath, LoadCardFromJToken, CardDataIdentifier);
            LoadJsonFromFile(SetsFilePath, LoadSetFromJToken, SetDataIdentifier);

            if (!didDownload) {
                if (AutoUpdate)
                    CardGameManager.Instance.StartCoroutine(Download());
                if (AllCardsUrlPageCount > 1)
                    CardGameManager.Instance.StartCoroutine(CardGameManager.Instance.LoadCards());
            }

            IsLoaded = true;
        } catch (Exception e) {
            Error+= e.Message;
            IsLoaded = false;
        }
    }

    public IEnumerator LoadCardPages()
    {
        for (int page = 2; page <= AllCardsUrlPageCount; page++) {
            try {
                LoadJsonFromFile(CardsFilePath + page, LoadCardFromJToken, CardDataIdentifier);
            } catch (Exception e) {
                Error+= e.Message;
            }
            yield return null;
        }
    }

    public void CreateEnumLookups()
    {
        foreach (EnumDef enumDef in Enums)
            foreach (string key in enumDef.Values.Keys)
                enumDef.CreateLookup(key);
    }

    public void LoadJsonFromFile(string file, LoadJTokenDelegate load, string dataId)
    {
        if (!File.Exists(file))
            return;

        JToken root = JToken.Parse(File.ReadAllText(file));
        foreach (JToken jToken in !string.IsNullOrEmpty(dataId) ? root[dataId] : root as JArray ?? (IJEnumerable<JToken>)((JObject)root).PropertyValues())
            load(jToken, Set.DefaultCode);
    }

    public void LoadCardFromJToken(JToken cardJToken, string defaultSetCode)
    {
        if (cardJToken == null)
            return;

        string cardId = cardJToken.Value<string>(CardIdIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(cardId))
            return;

        string cardName = cardJToken.Value<string>(CardNameIdentifier) ?? string.Empty;
        string cardSet = cardJToken.Value<string>(CardSetIdentifier) ?? defaultSetCode;
        Dictionary<string, PropertyDefValuePair> cardProperties = new Dictionary<string, PropertyDefValuePair>();
        foreach (PropertyDef property in CardProperties) {
            PropertyDefValuePair newPropertyEntry = new PropertyDefValuePair() { Def = property };
            try {
            if (property.Type == PropertyType.EnumList) {
                string listValue = string.Empty;
                foreach (JToken jToken in cardJToken[property.Name]) {
                    if (!string.IsNullOrEmpty(listValue))
                        listValue += EnumDef.Delimiter;
                    listValue += jToken.Value<string>() ?? string.Empty;
                }
                newPropertyEntry.Value = listValue;
            } else {
                newPropertyEntry.Value = cardJToken.Value<string>(property.Name) ?? string.Empty;
            } } catch {
                newPropertyEntry.Value = string.Empty;
            }
            cardProperties [property.Name] = newPropertyEntry;
        }

        Card newCard = new Card(cardId, cardName, cardSet, cardProperties);
        Cards [newCard.Id] = newCard;
        if (!Sets.ContainsKey(cardSet))
            Sets [cardSet] = new Set(cardSet);
    }

    public void LoadSetFromJToken(JToken setJToken, string defaultSetCode)
    {
        if (setJToken == null)
            return;

        string setCode = setJToken.Value<string>(SetCodeIdentifier) ?? defaultSetCode;
        string setName = setJToken.Value<string>(SetNameIdentifier) ?? defaultSetCode;
        if (!string.IsNullOrEmpty(setCode) && !string.IsNullOrEmpty(setName))
            Sets [setCode] = new Set(setCode, setName);
        JArray cards = setJToken.Value<JArray>(SetCardsIdentifier);

        if (cards == null)
            return;

        foreach (JToken jToken in cards)
            LoadCardFromJToken(jToken, setCode);
    }

    public IEnumerable<Card> FilterCards(string id, string name, string setCode, Dictionary<string, string> stringProperties, Dictionary<string, int> intMinProperties, Dictionary<string, int> intMaxProperties, Dictionary<string, int> enumProperties)
    {
        if (id == null)
            id = string.Empty;
        if (name == null)
            name = string.Empty;
        if (setCode == null)
            setCode = string.Empty;
        if (stringProperties == null)
            stringProperties = new Dictionary<string, string>();
        if (intMinProperties == null)
            intMinProperties = new Dictionary<string, int>();
        if (intMaxProperties == null)
            intMaxProperties = new Dictionary<string, int>();
        if (enumProperties == null)
            enumProperties = new Dictionary<string, int>();

        foreach (Card card in Cards.Values) {
            if (!name.ToLower().Split(' ').All(card.Name.ToLower().Contains))
                continue;
            if (!card.Id.ToLower().Contains(id.ToLower()) || !card.SetCode.ToLower().Contains(setCode.ToLower()))
                continue;
            bool propsMatch = true;
            foreach (KeyValuePair<string, string> entry in stringProperties)
                if (!card.GetPropertyValueString(entry.Key).ToLower().Contains(entry.Value.ToLower()))
                    propsMatch = false;
            foreach (KeyValuePair<string, int> entry in intMinProperties)
                if (card.GetPropertyValueInt(entry.Key) < entry.Value)
                    propsMatch = false;
            foreach (KeyValuePair<string, int> entry in intMaxProperties)
                if (card.GetPropertyValueInt(entry.Key) > entry.Value)
                    propsMatch = false;
            foreach (KeyValuePair<string, int> entry in enumProperties) {
                EnumDef enumDef = CardGameManager.Current.Enums.FirstOrDefault(def => def.Property.Equals(entry.Key));
                if (enumDef == null) {
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

    public void PutCardImage(CardModel cardModel)
    {
        Card card = cardModel.Value;
        card.ModelsUsingImage.Add(cardModel);

        if (card.ImageSprite != null) {
            cardModel.HideNameLabel();
            cardModel.image.sprite = card.ImageSprite;
        }
        else {
            cardModel.ShowNameLabel();
            cardModel.image.sprite = CardBackImageSprite;
            if (!card.IsLoadingImage)
                CardGameManager.Instance.StartCoroutine(GetAndSetImageSprite(card));
        }
    }

    public IEnumerator GetAndSetImageSprite(Card card)
    {
        if (card.IsLoadingImage)
            yield break;

        card.IsLoadingImage = true;
        Sprite newSprite = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(card.ImageFilePath, card.ImageWebUrl), output => newSprite = output);
        if (newSprite != null)
            card.ImageSprite = newSprite;
        else
            newSprite = CardBackImageSprite;

        foreach (CardModel cardModel in card.ModelsUsingImage) {
            if (!cardModel.IsFacedown) {
                if (newSprite != CardBackImageSprite)
                    cardModel.HideNameLabel();
                else
                    cardModel.ShowNameLabel();
                cardModel.image.sprite = newSprite;
                if (cardModel == CardInfoViewer.Instance.SelectedCardModel)
                    CardInfoViewer.Instance.cardImage.sprite = newSprite;
            }
        }
        card.IsLoadingImage = false;
    }

    public void RemoveCardImage(CardModel cardModel)
    {
        Card card = cardModel.Value;
        card.ModelsUsingImage.Remove(cardModel);
        if (card.ModelsUsingImage.Count < 1)
            card.ImageSprite = null;
    }

    public Sprite BackgroundImageSprite {
        get { return _backgroundImageSprite ?? (_backgroundImageSprite = Resources.Load<Sprite>(BackgroundImageFileName)); }
        private set { _backgroundImageSprite = value; }
    }

    public Sprite CardBackImageSprite {
        get { return _cardBackImageSprite ?? (_cardBackImageSprite = Resources.Load<Sprite>(CardBackImageFileName)); }
        private set { _cardBackImageSprite = value; }
    }
}
