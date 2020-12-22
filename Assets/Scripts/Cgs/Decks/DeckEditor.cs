/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.Cards;
using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Decks
{
    [RequireComponent(typeof(Canvas))]
    public class DeckEditor : MonoBehaviour, ICardDropHandler
    {
        public const string NewDeckPrompt = "Clear the editor and start a new Untitled deck?";
        public const string SaveChangesPrompt = "You have unsaved changes. Would you like to save?";
        public const string ChangeIndicator = "*";

        public const float CardPrefabHeight = 350f;
        public const float CardZonePrefabSpacing = -225f;

        public GameObject cardViewerPrefab;
        public GameObject cardModelPrefab;
        public GameObject cardZonePrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject deckSaveMenuPrefab;
        public RectTransform layoutContent;
        public CardDropArea dropZone;
        public ScrollRect scrollRect;
        public Text nameText;
        public Text countText;
        public SearchResults searchResults;

        private static int CardsPerZone =>
            Mathf.FloorToInt(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y / CardPrefabHeight * 4);

        public List<CardModel> CardModels
        {
            get
            {
                List<CardModel> cardModels = new List<CardModel>();
                foreach (CardZone zone in CardZones)
                    cardModels.AddRange(zone.GetComponentsInChildren<CardModel>());
                return cardModels;
            }
        }

        private List<CardZone> CardZones { get; } = new List<CardZone>();

        private float CardZoneWidth => _cardZoneWidth > 0
            ? _cardZoneWidth
            : _cardZoneWidth = cardZonePrefab.GetComponent<RectTransform>().rect.width;

        private float _cardZoneWidth = -1f;

        private DeckLoadMenu DeckLoader =>
            _deckLoader ? _deckLoader : _deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>();

        private DeckLoadMenu _deckLoader;

        private DeckSaveMenu DeckSaver =>
            _deckSaver ? _deckSaver : _deckSaver = Instantiate(deckSaveMenuPrefab).GetOrAddComponent<DeckSaveMenu>();

        private DeckSaveMenu _deckSaver;

        private UnityDeck CurrentDeck
        {
            get
            {
                var deck = new UnityDeck(CardGameManager.Current, SavedDeck?.Name ?? Deck.DefaultName,
                    CardGameManager.Current.DeckFileType);
                foreach (CardModel card in CardZones.SelectMany(zone => zone.GetComponentsInChildren<CardModel>()))
                    deck.Add(card.Value);
                return deck;
            }
        }

        private Deck SavedDeck { get; set; }

        private bool HasChanged
        {
            get
            {
                Deck currentDeck = CurrentDeck;
                if (currentDeck.Cards.Count < 1)
                    return false;
                return !currentDeck.Equals(SavedDeck);
            }
        }

        private void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            searchResults.HorizontalDoubleClickAction = AddCardModel;
            CardGameManager.Instance.OnSceneActions.Add(Reset);
        }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());
            dropZone.DropHandler = this;
            ShowDeckLoadMenu();
        }

        private void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                CardGameManager.Instance.ModalCanvas != null || searchResults.inputField.isFocused)
                return;

            if (Inputs.IsSort)
                Sort();
            else if (Inputs.IsNew)
                PromptForClear();
            else if (Inputs.IsLoad)
                ShowDeckLoadMenu();
            else if (Inputs.IsSave)
                ShowDeckSaveMenu();
            else if (Inputs.IsFocus)
                searchResults.inputField.ActivateInputField();
            else if (Inputs.IsFilter)
                searchResults.ShowSearchMenu();
            else if (Inputs.IsCancel)
                CheckBackToMainMenu();
        }

        public void Reset()
        {
            ClearCards();
            Consolidate();
        }

        private void ClearCards()
        {
            foreach (CardZone zone in CardZones)
                zone.transform.DestroyAllChildren();
            scrollRect.horizontalNormalizedPosition = 0;

            CardViewer.Instance.IsVisible = false;
            SavedDeck = null;
            UpdateDeckStats();
        }

        private void Consolidate()
        {
            HashSet<Transform> seen = new HashSet<Transform>();
            List<Transform> cards = new List<Transform>();
            foreach (Transform cardZoneTransform in CardZones.Select(cardZone => cardZone.transform))
            {
                for (var i = 0; i < cardZoneTransform.childCount; i++)
                {
                    Transform current = cardZoneTransform.GetChild(i);
                    if (seen.Contains(current))
                        continue;
                    cards.Add(current);
                    var cardModel = current.GetComponent<CardModel>();
                    if (cardModel != null && cardModel.PlaceHolder != null)
                        seen.Add(cardModel.PlaceHolder);
                    seen.Add(current);
                }
            }

            for (var i = 0; i < cards.Count; i++)
            {
                Transform card = cards[i];
                if (i / CardsPerZone >= CardZones.Count)
                    AddCardZone();
                Transform cardZone = CardZones[i / CardsPerZone].transform;
                if (card.parent != cardZone)
                    card.SetParent(cardZone);
                int cardZoneIndex = i % CardsPerZone;
                if (card.GetSiblingIndex() != cardZoneIndex)
                    card.SetSiblingIndex(cardZoneIndex);
            }

            for (int i = CardZones.Count - 1; i > 0; i--)
            {
                if (CardZones[i].transform.childCount != 0)
                    break;
                GameObject cardZone = CardZones[i].gameObject;
                CardZones.RemoveAt(i);
                Destroy(cardZone);
            }

            if (CardZones.Count * CardsPerZone == cards.Count)
                AddCardZone();

            Resize();
        }

        private void AddCardZone()
        {
            var cardZone = Instantiate(cardZonePrefab, layoutContent).GetOrAddComponent<CardZone>();
            cardZone.type = CardZoneType.Vertical;
            cardZone.scrollRectContainer = scrollRect;
            cardZone.DoesImmediatelyRelease = true;
            cardZone.OnLayout = Consolidate;
            cardZone.OnAddCardActions.Add(OnAddCardModel);
            cardZone.OnRemoveCardActions.Add(OnRemoveCardModel);
            CardZones.Add(cardZone);
            cardZone.GetComponent<VerticalLayoutGroup>().spacing =
                CardZonePrefabSpacing *
                (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y / CardPrefabHeight);
        }

        private void Resize()
        {
            Vector2 size = layoutContent.sizeDelta;
            float width = CardZoneWidth * CardZones.Count;
            if (Math.Abs(width - size.x) > 0.1f)
                layoutContent.sizeDelta = new Vector2(width, layoutContent.sizeDelta.y);
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCardModel(cardModel);
        }

        private void AddCardModel(CardModel cardModel)
        {
            if (cardModel == null)
                return;

            EventSystem.current.SetSelectedGameObject(null, cardModel.CurrentPointerEventData);

            AddCard(cardModel.Value);
        }

        private void AddCard(UnityCard card)
        {
            if (card == null)
                return;

            CardZone cardZone = CardZones.Last();
            var cardModel = Instantiate(cardModelPrefab, cardZone.transform).GetOrAddComponent<CardModel>();
            cardModel.Value = card;

            OnAddCardModel(cardZone, cardModel);
        }

        private void OnAddCardModel(CardZone cardZone, CardModel cardModel)
        {
            if (cardZone == null || cardModel == null)
                return;

            cardModel.SecondaryDragAction = cardModel.UpdateParentCardZoneScrollRect;
            cardModel.DefaultAction = DestroyCardModel;

            Consolidate();
            UpdateDeckStats();
        }

        private void OnRemoveCardModel(CardZone cardZone, CardModel cardModel)
        {
            Consolidate();
            UpdateDeckStats();
        }

        private void DestroyCardModel(CardModel cardModel)
        {
            if (cardModel == null)
                return;

            cardModel.transform.SetParent(null);
            Destroy(cardModel.gameObject);
            CardViewer.Instance.IsVisible = false;

            Consolidate();
            UpdateDeckStats();
        }

        public void FocusScrollRectOn(CardModel cardModel)
        {
            if (cardModel == null || cardModel.ParentCardZone == null)
                return;

            int cardZoneIndex = CardZones.IndexOf(cardModel.ParentCardZone);
            if (cardZoneIndex > 0 && cardZoneIndex < CardZones.Count)
                scrollRect.horizontalNormalizedPosition = CardZones.Count > 1
                    ? cardZoneIndex / (CardZones.Count - 1f)
                    : 0f;
        }

        [UsedImplicitly]
        public void Sort()
        {
            UnityDeck sortedDeck = CurrentDeck;
            sortedDeck.Sort();
            foreach (CardZone zone in CardZones)
                zone.transform.DestroyAllChildren();
            foreach (Card card in sortedDeck.Cards)
                AddCard((UnityCard) card);
        }

        [UsedImplicitly]
        public void PromptForClear()
        {
            CardGameManager.Instance.Messenger.Prompt(NewDeckPrompt, Clear);
        }

        [UsedImplicitly]
        public void Clear()
        {
            Reset();
        }

        [UsedImplicitly]
        public string UpdateDeckName(string newName)
        {
            if (newName == null)
                newName = string.Empty;
            newName = UnityFileMethods.GetSafeFileName(newName);
            nameText.text = newName + (HasChanged ? ChangeIndicator : string.Empty);
            return newName;
        }

        [UsedImplicitly]
        public void UpdateDeckStats()
        {
            string deckName = Deck.DefaultName;
            if (SavedDeck != null)
                deckName = SavedDeck.Name;
            nameText.text = deckName + (HasChanged ? ChangeIndicator : string.Empty);
            countText.text = CurrentDeck.Cards.Count.ToString();
        }

        [UsedImplicitly]
        public void ShowDeckLoadMenu()
        {
            Deck currentDeck = CurrentDeck;
            DeckLoader.Show(LoadDeck, currentDeck.Name, currentDeck.Cards.Count > 0 ? currentDeck.ToString() : null);
        }

        private void LoadDeck(UnityDeck deck)
        {
            if (deck == null)
                return;

            Clear();
            foreach (Card card in deck.Cards)
                AddCard((UnityCard) card);
            SavedDeck = deck;
            UpdateDeckStats();
            scrollRect.horizontalNormalizedPosition = 0;
        }

        [UsedImplicitly]
        public void ShowDeckSaveMenu()
        {
            UnityDeck deckToSave = CurrentDeck;
            bool overwrite = SavedDeck != null && deckToSave.Name.Equals(SavedDeck.Name);
            DeckSaver.Show(deckToSave, UpdateDeckName, OnSaveDeck, overwrite);
        }

        private void OnSaveDeck(Deck savedDeck)
        {
            SavedDeck = savedDeck;
            UpdateDeckStats();
        }

        [UsedImplicitly]
        public void CheckBackToMainMenu()
        {
            if (HasChanged)
            {
                CardGameManager.Instance.Messenger.Ask(SaveChangesPrompt, BackToMainMenu, ShowDeckSaveMenu);
                return;
            }

            BackToMainMenu();
        }

        private static void BackToMainMenu()
        {
            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
