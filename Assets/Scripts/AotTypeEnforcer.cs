using System;
using System.Collections.Generic;
using CardGameDef;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using UnityEngine;

// ReSharper disable UnusedVariable

public class AotTypeEnforcer : MonoBehaviour
{
    public void Awake()
    {
        AotHelper.EnsureType<StringEnumConverter>();
        AotHelper.Ensure(() =>
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var cardGame = new CardGame(null);
            cardGame.AllCardsUrl = new Uri(UnityExtensionMethods.FilePrefix);
            cardGame.AllCardsUrlPageCount = 1;
            cardGame.AllCardsUrlPageCountDivisor = 1;
            cardGame.AllCardsUrlPageCountIdentifier = string.Empty;
            cardGame.AllCardsUrlPageCountStartIndex = 1;
            cardGame.AllCardsUrlPageIdentifier = string.Empty;
            cardGame.AllCardsUrlPostBodyContent = string.Empty;
            cardGame.AllCardsUrlWrapped = false;
            cardGame.AllCardsUrlZipped = false;
            cardGame.AllSetsUrl = new Uri(UnityExtensionMethods.FilePrefix);
            cardGame.AllSetsUrlWrapped = false;
            cardGame.AllSetsUrlZipped = false;
            cardGame.AutoUpdate = 1;
            cardGame.AutoUpdateUrl = new Uri(UnityExtensionMethods.FilePrefix);
            cardGame.BannerImageFileType = string.Empty;
            cardGame.BannerImageUrl = new Uri(UnityExtensionMethods.FilePrefix);
            cardGame.CardBackImageFileType = string.Empty;
            cardGame.CardBackImageUrl = new Uri(UnityExtensionMethods.FilePrefix);
            cardGame.CardDataIdentifier = string.Empty;
            cardGame.CardIdIdentifier = string.Empty;
            cardGame.CardIdStop = string.Empty;
            cardGame.CardImageFileType = string.Empty;
            cardGame.CardImageProperty = string.Empty;
            cardGame.CardImageUrl = string.Empty;
            cardGame.CardNameIdentifier = string.Empty;
            cardGame.CardNameIsUnique = false;
            cardGame.CardPrimaryProperty = string.Empty;
            cardGame.CardProperties = new List<PropertyDef>();
            cardGame.CardPropertyIdentifier = string.Empty;
            cardGame.CardSetIdentifier = string.Empty;
            cardGame.CardSetIsObject = false;
            cardGame.CardSetNameIdentifier = string.Empty;
            cardGame.CardSetsInList = false;
            cardGame.CardSetsInListIsCsv = false;
            cardGame.CardSize = Vector2.one;
            cardGame.DeckFileAltId = string.Empty;
            cardGame.DeckFileTxtId = DeckFileTxtId.Id;
            cardGame.DeckFileType = DeckFileType.Dec;
            cardGame.DeckMaxCount = 1;
            cardGame.DeckUrls = new List<DeckUrl>();
            var deckUrl = new DeckUrl(string.Empty, new Uri(UnityExtensionMethods.FilePrefix));
            cardGame.Enums = new List<EnumDef>();
            var enumDef = new EnumDef(string.Empty,
                new Dictionary<string, string>());
            cardGame.Extras = new List<ExtraDef>();
            var extraDef = new ExtraDef(string.Empty, string.Empty, string.Empty);
            cardGame.GameBoardCards = new List<GameBoardCard>();
            var gameBoard = new GameBoard(string.Empty, Vector2.zero, Vector2.zero);
            var gameBoardCard = new GameBoardCard(string.Empty,
                new List<GameBoard>());
            cardGame.GameBoardFileType = string.Empty;
            cardGame.GameBoardUrls = new List<GameBoardUrl>();
            var gameBoardUrl =
                new GameBoardUrl(string.Empty, new Uri(UnityExtensionMethods.FilePrefix));
            cardGame.GameStartHandCount = 1;
            cardGame.GameStartPointsCount = 1;
            cardGame.Name = string.Empty;
            cardGame.PlayAreaSize = Vector2.one;
            cardGame.RulesUrl = new Uri(UnityExtensionMethods.FilePrefix);
            cardGame.SetCardsIdentifier = string.Empty;
            cardGame.SetCodeDefault = string.Empty;
            cardGame.SetCodeIdentifier = string.Empty;
            cardGame.SetDataIdentifier = string.Empty;
            cardGame.SetNameDefault = string.Empty;
            cardGame.SetNameIdentifier = string.Empty;
        });
    }
}
