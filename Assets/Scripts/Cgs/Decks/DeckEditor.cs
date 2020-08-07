/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using CardGameView;
using Cgs.Cards;
using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Decks
{
    [RequireComponent(typeof(Canvas))]
    public class DeckEditor : MonoBehaviour, ICardDropHandler
    {
        public const string NewDeckPrompt = "Clear the editor and start a new Untitled deck?";
        public const string SaveChangesPrompt = "You have unsaved changes. Would you like to save?";
        public const string ChangeIndicator = "*";

        public const float CardPrefabHeight = 350f;
        public const float CardStackPrefabSpacing = -225f;

        public GameObject cardViewerPrefab;
        public GameObject cardModelPrefab;
        public GameObject cardStackPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject deckSaveMenuPrefab;
        public RectTransform layoutContent;
        public CardDropArea dropZone;
        public ScrollRect scrollRect;
        public Text nameText;
        public Text countText;
        public SearchResults searchResults;

        private static int CardsPerStack =>
            Mathf.FloorToInt(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y / CardPrefabHeight * 4);

        public List<CardModel> CardModels
        {
            get
            {
                List<CardModel> cardModels = new List<CardModel>();
                foreach (CardStack stack in CardStacks)
                    cardModels.AddRange(stack.GetComponentsInChildren<CardModel>());
                return cardModels;
            }
        }

        private List<CardStack> CardStacks { get; } = new List<CardStack>();

        private float CardStackWidth => _cardStackWidth > 0
            ? _cardStackWidth
            : _cardStackWidth = cardStackPrefab.GetComponent<RectTransform>().rect.width;

        private float _cardStackWidth = -1f;

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
                foreach (CardModel card in CardStacks.SelectMany(stack => stack.GetComponentsInChildren<CardModel>()))
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
            foreach (CardStack stack in CardStacks)
                stack.transform.DestroyAllChildren();
            scrollRect.horizontalNormalizedPosition = 0;

            CardViewer.Instance.IsVisible = false;
            SavedDeck = null;
            UpdateDeckStats();
        }

        private void Consolidate()
        {
            HashSet<Transform> seen = new HashSet<Transform>();
            List<Transform> cards = new List<Transform>();
            foreach (Transform cardStackTransform in CardStacks.Select(cardStack => cardStack.transform))
            {
                for (var i = 0; i < cardStackTransform.childCount; i++)
                {
                    Transform current = cardStackTransform.GetChild(i);
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
                if (i / CardsPerStack >= CardStacks.Count)
                    AddCardStack();
                Transform cardStack = CardStacks[i / CardsPerStack].transform;
                if (card.parent != cardStack)
                    card.SetParent(cardStack);
                int cardStackIndex = i % CardsPerStack;
                if (card.GetSiblingIndex() != cardStackIndex)
                    card.SetSiblingIndex(cardStackIndex);
            }

            for (int i = CardStacks.Count - 1; i > 0; i--)
            {
                if (CardStacks[i].transform.childCount != 0)
                    break;
                GameObject cardStack = CardStacks[i].gameObject;
                CardStacks.RemoveAt(i);
                Destroy(cardStack);
            }

            if (CardStacks.Count * CardsPerStack == cards.Count)
                AddCardStack();

            Resize();
        }

        private void AddCardStack()
        {
            var cardStack = Instantiate(cardStackPrefab, layoutContent).GetOrAddComponent<CardStack>();
            cardStack.type = CardStackType.Vertical;
            cardStack.scrollRectContainer = scrollRect;
            cardStack.DoesImmediatelyRelease = true;
            cardStack.OnLayout = Consolidate;
            cardStack.OnAddCardActions.Add(OnAddCardModel);
            cardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
            CardStacks.Add(cardStack);
            cardStack.GetComponent<VerticalLayoutGroup>().spacing =
                CardStackPrefabSpacing *
                (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y / CardPrefabHeight);
        }

        private void Resize()
        {
            Vector2 size = layoutContent.sizeDelta;
            float width = CardStackWidth * CardStacks.Count;
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

            CardStack cardStack = CardStacks.Last();
            var cardModel = Instantiate(cardModelPrefab, cardStack.transform).GetOrAddComponent<CardModel>();
            cardModel.Value = card;

            OnAddCardModel(cardStack, cardModel);
        }

        private void OnAddCardModel(CardStack cardStack, CardModel cardModel)
        {
            if (cardStack == null || cardModel == null)
                return;

            cardModel.SecondaryDragAction = cardModel.UpdateParentCardStackScrollRect;
            cardModel.DoubleClickAction = DestroyCardModel;

            Consolidate();
            UpdateDeckStats();
        }

        private void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
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
            if (cardModel == null || cardModel.ParentCardStack == null)
                return;

            int cardStackIndex = CardStacks.IndexOf(cardModel.ParentCardStack);
            if (cardStackIndex > 0 && cardStackIndex < CardStacks.Count)
                scrollRect.horizontalNormalizedPosition = CardStacks.Count > 1
                    ? cardStackIndex / (CardStacks.Count - 1f)
                    : 0f;
        }

        [UsedImplicitly]
        public void Sort()
        {
            UnityDeck sortedDeck = CurrentDeck;
            sortedDeck.Sort();
            foreach (CardStack stack in CardStacks)
                stack.transform.DestroyAllChildren();
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
            newName = UnityExtensionMethods.GetSafeFileName(newName);
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
