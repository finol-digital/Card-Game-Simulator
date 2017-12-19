using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine.UI;

public delegate void LoadJTokenDelegate(JToken jToken,string defaultValue);

[JsonObject(MemberSerialization.OptIn)]
public class CardGame
{
    public const string AllCardsFileName = "AllCards.json";
    public const string AllSetsFileName = "AllSets.json";
    public const string BackgroundImageFileName = "Background";
    public const string CardBackImageFileName = "CardBack";

    public string FilePathBase => CardGameManager.GamesFilePathBase + "/" + Name;
    public string ConfigFilePath => FilePathBase + "/" + Name + ".json";
    public string DecksFilePath => FilePathBase + "/decks";
    public string GameBoardsFilePath => FilePathBase + "/boards";
    public float AspectRatio => CardHeight > 0 ? Mathf.Abs(CardWidth / CardHeight) : 0.715f;

    [JsonProperty]
    public string Name { get; set; }

    [JsonProperty]
    public string AllCardsUrl { get; set; } = "";

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
    public float CardHeight { get; set; } = 3.5f;

    [JsonProperty]
    public string CardIdIdentifier { get; set; }= "id";

    [JsonProperty]
    public string CardImageFileType { get; set; } = "png";

    [JsonProperty]
    public string CardImageUrlBase { get; set; } = "";

    [JsonProperty]
    public string CardImageUrlFormat { get; set; } = "{0}/{1}.{2}";

    [JsonProperty]
    public string CardImageUrlProperty { get; set; } = "";

    [JsonProperty]
    public string CardNameIdentifier { get; set; } = "name";

    [JsonProperty]
    public string CardSetIdentifier { get; set; } = "set";

    [JsonProperty]
    public string CardPrimaryProperty { get; set; } = "";

    [JsonProperty]
    public List<PropertyDef> CardProperties { get; set; } = new List<PropertyDef>();

    [JsonProperty]
    public float CardWidth { get; set; } = 2.5f;

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
    public float PlayAreaHeight { get; set; } = 13.5f;

    [JsonProperty]
    public float PlayAreaWidth { get; set; } = 23.5f;

    [JsonProperty]
    public string SetCardsIdentifier { get; set; } = "cards";

    [JsonProperty]
    public string SetCodeIdentifier { get; set; } = "code";

    [JsonProperty]
    public string SetNameIdentifier { get; set; } = "name";

    public Dictionary<string, Card> Cards { get; } = new Dictionary<string, Card>();
    public Dictionary<string, Set> Sets { get; } = new Dictionary<string, Set>();

    public bool IsLoading { get; private set; }
    public bool IsLoaded { get; private set; }
    public string Error { get; private set; }

    private Sprite _backgroundImageSprite;
    private Sprite _cardBackImageSprite;

    public CardGame(string name = Set.DefaultCode, string url = "")
    {
        Name = name ?? Set.DefaultCode;
        AutoUpdateUrl = url ?? string.Empty;
    }

    public IEnumerator Load()
    {
        if (IsLoading || IsLoaded)
            yield break;
        IsLoading = true;

        string initialDirectory = FilePathBase;
        if (!string.IsNullOrEmpty(AutoUpdateUrl) && (AutoUpdate || !File.Exists(ConfigFilePath)))
            yield return UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl, ConfigFilePath);
        try {
            JsonConvert.PopulateObject(File.ReadAllText(ConfigFilePath), this);
        } catch (Exception e) {
            Error = e.Message;
            IsLoading = false;
            yield break;
        }
        if (AutoUpdate || !initialDirectory.Equals(FilePathBase)) {
            yield return UnityExtensionMethods.SaveUrlToFile(AutoUpdateUrl, ConfigFilePath);
            if (!initialDirectory.Equals(FilePathBase))
                Directory.Delete(initialDirectory, true);
        }

        Sprite backgroundSprite = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(FilePathBase + "/" + BackgroundImageFileName + "." + BackgroundImageFileType, BackgroundImageUrl), output => backgroundSprite = output);
        if (backgroundSprite != null)
            BackgroundImageSprite = backgroundSprite;
        Sprite cardBackSprite = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(FilePathBase + "/" + CardBackImageFileName + "." + CardBackImageFileType, CardBackImageUrl), output => cardBackSprite = output);
        if (cardBackSprite != null)
            CardBackImageSprite = cardBackSprite;

        foreach (EnumDef enumDef in Enums)
            foreach (string key in enumDef.Values.Keys)
                enumDef.CreateLookup(key);

        string cardsFile = FilePathBase + "/" + AllCardsFileName;
        if (!string.IsNullOrEmpty(AllCardsUrl) && (AutoUpdate || !File.Exists(cardsFile))) {
            yield return UnityExtensionMethods.SaveUrlToFile(AllCardsUrl, AllCardsZipped ? cardsFile + UnityExtensionMethods.ZipExtension : cardsFile);
            if (AllCardsZipped)
                UnityExtensionMethods.ExtractZip(cardsFile + UnityExtensionMethods.ZipExtension, FilePathBase);
        }
        string setsFile = FilePathBase + "/" + AllSetsFileName;
        if (!string.IsNullOrEmpty(AllSetsUrl) && (AutoUpdate || !File.Exists(setsFile))) {
            yield return UnityExtensionMethods.SaveUrlToFile(AllSetsUrl, AllSetsZipped ? setsFile + UnityExtensionMethods.ZipExtension : setsFile);
            if (AllSetsZipped)
                UnityExtensionMethods.ExtractZip(setsFile + UnityExtensionMethods.ZipExtension, FilePathBase);
        }
        try {
            LoadJsonFromFile(cardsFile, LoadCardFromJToken);
            LoadJsonFromFile(setsFile, LoadSetFromJToken);
        } catch (Exception e) {
            Error = e.Message;
            IsLoading = false;
            yield break;
        }

        if (DeckUrls.Count > 0 && !Directory.Exists(DecksFilePath))
            foreach (DeckUrl deckUrl in DeckUrls)
                yield return UnityExtensionMethods.SaveUrlToFile(deckUrl.Url, DecksFilePath + "/" + deckUrl.Name + "." + DeckFileType);

        if (GameBoardUrls.Count > 0 && !Directory.Exists(GameBoardsFilePath))
            foreach (GameBoardUrl boardUrl in GameBoardUrls)
                yield return UnityExtensionMethods.SaveUrlToFile(boardUrl.Url, GameBoardsFilePath + "/" + boardUrl.Id + "." + GameBoardFileType);

        IsLoading = false;
        IsLoaded = true;
    }

    public void LoadJsonFromFile(string file, LoadJTokenDelegate load)
    {
        if (!File.Exists(file))
            return;

        JToken root = JToken.Parse(File.ReadAllText(file));
        IJEnumerable<JToken> jTokenEnumeration = root as JArray ?? (IJEnumerable<JToken>) ((JObject) root).PropertyValues();
        foreach (JToken jToken in jTokenEnumeration)
            load(jToken, Set.DefaultCode);
    }

    public void LoadCardFromJToken(JToken cardJToken, string defaultSetCode)
    {
        if (cardJToken == null)
            return;

        string cardId = cardJToken.Value<string>(CardIdIdentifier) ?? string.Empty;
        string cardName = cardJToken.Value<string>(CardNameIdentifier) ?? string.Empty;
        string cardSet = cardJToken.Value<string>(CardSetIdentifier) ?? defaultSetCode;
        Dictionary<string, PropertyDefValuePair> cardProperties = new Dictionary<string, PropertyDefValuePair>();
        foreach (PropertyDef property in CardProperties) {
            PropertyDefValuePair newPropertyEntry = new PropertyDefValuePair() { Def = property };
            if (property.Type == PropertyType.EnumList) {
                int enumValue = 0;
                EnumDef enumDef = Enums.FirstOrDefault(def => def.Property.Equals(property.Name));
                IJEnumerable<JToken> enumValues = cardJToken as JArray;
                if (enumDef != null && enumValues != null) {
                    foreach (JToken jToken in enumValues) {
                        int lookupValue;
                        if (enumDef.ReverseLookup.TryGetValue(jToken.Value<string>() ?? string.Empty, out lookupValue))
                            enumValue |= lookupValue;
                    }
                }
                newPropertyEntry.Value = enumValue.ToString();
            } else
                newPropertyEntry.Value = cardJToken.Value<string>(property.Name) ?? string.Empty;
            cardProperties [property.Name] = newPropertyEntry;
        }

        if (string.IsNullOrEmpty(cardId))
            return;

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
            if (!card.Id.ToLower().Contains(id.ToLower()) || !card.Name.ToLower().Contains(name.ToLower()) ||
                !card.SetCode.ToLower().Contains(setCode.ToLower())) continue;
            bool propsMatch = true;
            foreach (KeyValuePair<string, string> entry in stringProperties)
                if (!card.GetPropertyValueString(entry.Key).ToLower().Contains(entry.Value.ToLower()))
                    propsMatch = false;
            int intValue;
            foreach (KeyValuePair<string, int> entry in intMinProperties)
                if (int.TryParse(card.GetPropertyValueString(entry.Key), out intValue) && intValue < entry.Value)
                    propsMatch = false;
            foreach (KeyValuePair<string, int> entry in intMaxProperties)
                if (int.TryParse(card.GetPropertyValueString(entry.Key), out intValue) && intValue > entry.Value)
                    propsMatch = false;
            foreach (KeyValuePair<string, int> entry in enumProperties) {
                if ((card.GetPropertyValueEnum(entry.Key) & entry.Value) == 0)
                    propsMatch = false;
            }
            if (propsMatch)
                yield return card;
        }
    }

    public void PutCardImage(CardModel cardModel)
    {
        Card card = cardModel.Value;
        card.ModelsUsingImage.Add(cardModel);

        if (card.ImageSprite != null)
            cardModel.GetComponent<Image>().sprite = card.ImageSprite;
        else if (!card.IsLoadingImage)
            CardGameManager.Instance.StartCoroutine(GetAndSetImageSprite(card));
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
            newSprite = CardGameManager.Current.CardBackImageSprite;

        foreach (CardModel cardModel in card.ModelsUsingImage)
            if (!cardModel.IsFacedown)
                cardModel.GetComponent<Image>().sprite = newSprite;
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
