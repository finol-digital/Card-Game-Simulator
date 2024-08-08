using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FinolDigital.Cgs.CardGameDef;
using FinolDigital.Cgs.CardGameDef.Unity;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using UnityEngine;
using UnityExtensionMethods;

namespace Cgs
{
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public class AotTypeEnforcer : MonoBehaviour
    {
        public void Awake()
        {
            AotHelper.EnsureType<StringEnumConverter>();
            AotHelper.Ensure(() =>
            {
                var cardGame = new UnityCardGame(null);
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
                cardGame.CardBackFaceImageUrls = new List<CardBackFaceImageUrl>();
                var cardBackFaceImageUrl = new CardBackFaceImageUrl(string.Empty, new Uri(UnityFileMethods.FilePrefix));
                cardGame.CardBackFaceImageUrls.Add(cardBackFaceImageUrl);
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
                var propertyDef = new PropertyDef(string.Empty, PropertyType.String);
                propertyDef.Properties = new List<PropertyDef>();
                propertyDef.Delimiter = string.Empty;
                propertyDef.DisplayEmpty = string.Empty;
                propertyDef.Display = string.Empty;
                propertyDef.DisplayEmptyFirst = false;
                propertyDef.Name = string.Empty;
                propertyDef.Type = PropertyType.String;
                var propertyDefValuePair = new PropertyDefValuePair();
                propertyDefValuePair.Def = propertyDef;
                propertyDefValuePair.Value = string.Empty;
                cardGame.CardProperties = new List<PropertyDef>();
                cardGame.CardProperties.Add(propertyDef);
                cardGame.CardPropertyIdentifier = propertyDefValuePair.ToString();
                cardGame.CardRotationDefault = 0;
                cardGame.CardRotationIdentifier = string.Empty;
                cardGame.CardSetIdentifier = string.Empty;
                cardGame.CardSetIsObject = false;
                cardGame.CardSetNameIdentifier = string.Empty;
                cardGame.CardSetsInList = false;
                cardGame.CardSetsInListIsCsv = false;
                cardGame.CardSize = new Float2(1, 1);
                cardGame.CgsGamesLink = new Uri(UnityFileMethods.FilePrefix);
                cardGame.Copyright = string.Empty;
                cardGame.DeckFileAltId = string.Empty;
                cardGame.DeckFileTxtId = DeckFileTxtId.Id;
                cardGame.DeckFileType = DeckFileType.Dec;
                var deckUrl = new DeckUrl(string.Empty, string.Empty, new Uri(UnityFileMethods.FilePrefix));
                cardGame.DeckUrls = new List<DeckUrl> {deckUrl};
                var enumDef = new EnumDef(string.Empty, new Dictionary<string, string>());
                cardGame.Enums = new List<EnumDef> {enumDef};
                var extraDef = new ExtraDef(string.Empty, string.Empty, string.Empty);
                cardGame.Extras = new List<ExtraDef> {extraDef};
                var float2 = new Float2(0f, 0f);
                var gameBoard = new GameBoard(string.Empty, float2, float2);
                var gameBoardCard = new GameBoardCard(string.Empty, new List<GameBoard> {gameBoard});
                cardGame.GameBoardCards = new List<GameBoardCard> {gameBoardCard};
                cardGame.GameBoardImageFileType = string.Empty;
                var gameBoardUrl = new GameBoardUrl(string.Empty, new Uri(UnityFileMethods.FilePrefix));
                cardGame.GameBoardUrls = new List<GameBoardUrl> {gameBoardUrl};
                cardGame.GamePlayDeckName = string.Empty;
                cardGame.GamePlayDeckPositions = new List<Float2>();
                cardGame.GameDefaultCardAction = CardAction.Flip;
                var gamePlayZone = new GamePlayZone(FacePreference.Any, CardAction.Tap, float2, 0, float2,
                    GamePlayZoneType.Area);
                var gamePlayZone2 = new GamePlayZone(FacePreference.Up, CardAction.Move, float2, 0, float2,
                    GamePlayZoneType.Area);
                var gamePlayZone3 = new GamePlayZone(FacePreference.Down, CardAction.Rotate, float2, 0, float2,
                    GamePlayZoneType.Area);
                cardGame.GamePlayZones = new List<GamePlayZone> {gamePlayZone, gamePlayZone2, gamePlayZone3};
                cardGame.GameStartHandCount = 1;
                cardGame.GameStartPointsCount = 1;
                cardGame.Name = string.Empty;
                cardGame.PlayMatImageFileType = string.Empty;
                cardGame.PlayMatImageUrl = new Uri(UnityFileMethods.FilePrefix);
                cardGame.PlayMatSize = new Float2(1, 1);
                cardGame.PlayMatGridCellSize = new Float2(1, 1);
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
