using Newtonsoft.Json.Utilities;
using UnityEngine;

public class AotTypeEnforcer : MonoBehaviour
{
    public void Awake()
    {
        AotHelper.EnsureType<Newtonsoft.Json.Converters.StringEnumConverter>();
        AotHelper.Ensure(() =>
        {
            var cardGame = new CardGameDef.CardGame(null);
            cardGame.AllCardsUrl = new System.Uri(UnityExtensionMethods.FilePrefix);
            cardGame.AllCardsUrlPageCount = 1;
            cardGame.AllCardsUrlPageCountDivisor = 1;
            cardGame.AllCardsUrlPageCountIdentifier = string.Empty;
            cardGame.AllCardsUrlPageCountStartIndex = 1;
            cardGame.AllCardsUrlPageIdentifier = string.Empty;
            cardGame.AllCardsUrlPostBodyContent = string.Empty;
            cardGame.AllCardsUrlWrapped = false;
            cardGame.AllCardsUrlZipped = false;
            cardGame.AllSetsUrl = new System.Uri(UnityExtensionMethods.FilePrefix);
            cardGame.AllSetsUrlWrapped = false;
            cardGame.AllSetsUrlZipped = false;
            cardGame.AutoUpdate = 1;
            cardGame.AutoUpdateUrl = new System.Uri(UnityExtensionMethods.FilePrefix);
            cardGame.BannerImageFileType = string.Empty;
            cardGame.BannerImageUrl = new System.Uri(UnityExtensionMethods.FilePrefix);
            cardGame.CardBackImageFileType = string.Empty;
            cardGame.CardBackImageUrl = new System.Uri(UnityExtensionMethods.FilePrefix);
            cardGame.CardDataIdentifier = string.Empty;
            cardGame.CardIdIdentifier = string.Empty;
            cardGame.CardIdStop = string.Empty;
            cardGame.CardImageFileType = string.Empty;
            cardGame.CardImageProperty = string.Empty;
            cardGame.CardImageUrl = string.Empty;
            cardGame.CardNameIdentifier = string.Empty;
            cardGame.CardNameIsUnique = false;
            cardGame.CardPrimaryProperty = string.Empty;
            cardGame.CardProperties = new System.Collections.Generic.List<CardGameDef.PropertyDef>();
            cardGame.CardPropertyIdentifier = string.Empty;
            cardGame.CardSetIdentifier = string.Empty;
            cardGame.CardSetIsObject = false;
            cardGame.CardSetNameIdentifier = string.Empty;
            cardGame.CardSetsInList = false;
            cardGame.CardSetsInListIsCsv = false;
            cardGame.CardSize = Vector2.one;
            cardGame.DeckFileAltId = string.Empty;
            cardGame.DeckFileTxtId = CardGameDef.DeckFileTxtId.Id;
            cardGame.DeckFileType = CardGameDef.DeckFileType.Dec;
            cardGame.DeckMaxCount = 1;
            cardGame.DeckUrls = new System.Collections.Generic.List<CardGameDef.DeckUrl>();
            var deckUrl = new CardGameDef.DeckUrl(string.Empty, new System.Uri(UnityExtensionMethods.FilePrefix));
            cardGame.Enums = new System.Collections.Generic.List<CardGameDef.EnumDef>();
            var enumDef = new CardGameDef.EnumDef(string.Empty, new System.Collections.Generic.Dictionary<string, string>());
            cardGame.Extras = new System.Collections.Generic.List<CardGameDef.ExtraDef>();
            var extraDef = new CardGameDef.ExtraDef(string.Empty, string.Empty, string.Empty);
            cardGame.GameBoardCards = new System.Collections.Generic.List<CardGameDef.GameBoardCard>();
            var gameBoard = new CardGameDef.GameBoard(string.Empty, Vector2.zero, Vector2.zero);
            var gameBoardCard = new CardGameDef.GameBoardCard(string.Empty, new System.Collections.Generic.List<CardGameDef.GameBoard>());
            cardGame.GameBoardFileType = string.Empty;
            cardGame.GameBoardUrls = new System.Collections.Generic.List<CardGameDef.GameBoardUrl>();
            var gameBoardUrl = new CardGameDef.GameBoardUrl(string.Empty, new System.Uri(UnityExtensionMethods.FilePrefix));
            cardGame.GameStartHandCount = 1;
            cardGame.GameStartPointsCount = 1;
            cardGame.Name = string.Empty;
            cardGame.PlayAreaSize = Vector2.one;
            cardGame.RulesUrl = new System.Uri(UnityExtensionMethods.FilePrefix);
            cardGame.SetCardsIdentifier = string.Empty;
            cardGame.SetCodeDefault = string.Empty;
            cardGame.SetCodeIdentifier = string.Empty;
            cardGame.SetDataIdentifier = string.Empty;
            cardGame.SetNameDefault = string.Empty;
            cardGame.SetNameIdentifier = string.Empty;
        });
    }
}
