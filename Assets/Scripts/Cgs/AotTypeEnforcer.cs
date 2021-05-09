using System;
using System.Collections.Generic;
using CardGameDef;
using CardGameDef.Unity;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using UnityEngine;

// ReSharper disable UnusedVariable

namespace Cgs
{
    public class AotTypeEnforcer : MonoBehaviour
    {
        public void Awake()
        {
            AotHelper.EnsureType<StringEnumConverter>();
            AotHelper.Ensure(() =>
            {
                // ReSharper disable once UseObjectOrCollectionInitializer
                var cardGame = new CardGame();
                cardGame.AllCardsUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.AllCardsUrlPageCount = 1;
                cardGame.AllCardsUrlPageCountDivisor = 1;
                cardGame.AllCardsUrlPageCountIdentifier = string.Empty;
                cardGame.AllCardsUrlPageCountStartIndex = 1;
                cardGame.AllCardsUrlPageIdentifier = string.Empty;
                cardGame.AllCardsUrlPostBodyContent = string.Empty;
                cardGame.AllCardsUrlWrapped = false;
                cardGame.AllCardsUrlZipped = false;
                cardGame.AllDecksUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.AllDecksUrlDataIdentifier = string.Empty;
                cardGame.AllDecksUrlPostBodyContent = string.Empty;
                cardGame.AllDecksUrlTxtRoot = string.Empty;
                cardGame.AllSetsUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.AllSetsUrlWrapped = false;
                cardGame.AllSetsUrlZipped = false;
                cardGame.AutoUpdate = 1;
                cardGame.AutoUpdateUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.BannerImageFileType = string.Empty;
                cardGame.BannerImageUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.CardBackImageFileType = string.Empty;
                cardGame.CardBackImageUrl = new Uri(UnityFileMethods.FilePrefix);
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
                cardGame.CardSize = new Float2(1, 1);
                cardGame.DeckFileAltId = string.Empty;
                cardGame.DeckFileTxtId = DeckFileTxtId.Id;
                cardGame.DeckFileType = DeckFileType.Dec;
                cardGame.DeckUrls = new List<DeckUrl>();
                var deckUrl = new DeckUrl(string.Empty, string.Empty, new Uri(UnityFileMethods.FilePrefix));
                cardGame.Enums = new List<EnumDef>();
                var enumDef = new EnumDef(string.Empty,
                    new Dictionary<string, string>());
                cardGame.Extras = new List<ExtraDef>();
                var extraDef = new ExtraDef(string.Empty, string.Empty, string.Empty);
                cardGame.GameBoardCards = new List<GameBoardCard>();
                var float2 = new Float2(0f, 0f);
                var gameBoard = new GameBoard(string.Empty, float2, float2);
                var gameBoardCard = new GameBoardCard(string.Empty,
                    new List<GameBoard>());
                cardGame.GameBoardImageFileType = string.Empty;
                cardGame.GameBoardUrls = new List<GameBoardUrl>();
                var gameBoardUrl =
                    new GameBoardUrl(string.Empty, new Uri(UnityFileMethods.FilePrefix));
                cardGame.GamePlayDeckName = string.Empty;
                cardGame.GameStartHandCount = 1;
                cardGame.GameStartPointsCount = 1;
                cardGame.Name = string.Empty;
                cardGame.PlayMatImageFileType = string.Empty;
                cardGame.PlayMatImageUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.PlayMatSize = new Float2(1, 1);
                cardGame.RulesUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.SetCardsIdentifier = string.Empty;
                cardGame.SetCardsUrlIdentifier = string.Empty;
                cardGame.SetCodeDefault = string.Empty;
                cardGame.SetCodeIdentifier = string.Empty;
                cardGame.SetDataIdentifier = string.Empty;
                cardGame.SetNameDefault = string.Empty;
                cardGame.SetNameIdentifier = string.Empty;
            });
        }
    }
}
