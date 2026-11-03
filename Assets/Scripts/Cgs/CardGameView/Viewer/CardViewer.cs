/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using Cgs.GameContent;
using Cgs.Localization;
using Cgs.Play;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Viewer
{
    public enum CardViewerMode
    {
        Minimal = 0,
        Expanded = 1,
        Maximal = 2
    }

    public class CardViewer : MonoBehaviour, ICardDisplay, ICardPresentationDisplay, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        private const string SetLabel = "Set";
        private const string IdLabel = "Id";
        private const string Delimiter = ": ";
        private const float PageAxisPressDeadZone = 0.5f;

        public static CardViewer Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                var cardViewerGameObject = GameObject.FindWithTag(Tags.CardViewer);
                if (cardViewerGameObject != null)
                    _instance = cardViewerGameObject.GetOrAddComponent<CardViewer>();
                return _instance;
            }
        }

        private static CardViewer _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        public CardViewerMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                Redisplay();
            }
        }

        private CardViewerMode _mode;

        [SerializeField] CanvasGroup preview;
        [SerializeField] CanvasGroup minimal;
        [SerializeField] CanvasGroup expanded;
        [SerializeField] CardActionPanel cardActionPanel;
        [SerializeField] CanvasGroup maximal;

        [SerializeField] ScrollRect maximalScrollRect;

        [SerializeField] RectTransform zoomPanel;

        [SerializeField] List<Text> previewNameText;
        [SerializeField] List<Text> previewIdText;
        [SerializeField] Button expandButton;
        [SerializeField] Button imageButton;
        [SerializeField] List<AspectRatioFitter> cardAspectRatioFitters;
        [SerializeField] List<Image> cardImages;
        [SerializeField] List<Text> nameTexts;
        [SerializeField] List<Text> uniqueIdTexts;
        [SerializeField] Text idText;
        [SerializeField] Text setText;
        [SerializeField] Text propertyTextTemplate;
        [SerializeField] List<Dropdown> propertySelectors;
        [SerializeField] List<Text> propertyValueTexts;
        [SerializeField] RectTransform buttonsPanel;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;

        public RectTransform ZoomPanel => zoomPanel;

        public RectTransform ButtonsPanel => buttonsPanel;

        public Button PreviousButton => previousButton;

        public Button NextButton => nextButton;

        private List<Text> PropertyTexts { get; } = new();
        private List<Dropdown.OptionData> PropertyOptions { get; } = new();
        private List<string> PropertyPaths { get; } = new();
        private List<string> PropertyLabels { get; } = new();

        private int PrimaryPropertyIndex
        {
            get
            {
                var primaryPropertyIndex = 0;
                for (var i = 0; i < PropertyOptions.Count; i++)
                    if (i < PropertyPaths.Count && PropertyPaths[i].Equals(CardGameManager.Current.CardPrimaryProperty))
                        primaryPropertyIndex = i;
                return primaryPropertyIndex;
            }
        }

        private string SelectedPropertyName
        {
            get
            {
                var selectedName = SetLabel;
                switch (SelectedPropertyIndex)
                {
                    case 1:
                        selectedName = IdLabel;
                        break;
                    case > 1 when SelectedPropertyIndex < PropertyOptions.Count:
                        selectedName = PropertyPaths[SelectedPropertyIndex];
                        break;
                }

                return selectedName;
            }
        }

        [UsedImplicitly]
        public int SelectedPropertyIndex
        {
            get => _selectedPropertyIndex;
            set
            {
                _selectedPropertyIndex = value;
                if (_selectedPropertyIndex < 0)
                    _selectedPropertyIndex = PropertyOptions.Count - 1;
                if (_selectedPropertyIndex >= PropertyOptions.Count)
                    _selectedPropertyIndex = 0;
                foreach (var propertySelector in propertySelectors)
                    propertySelector.value = _selectedPropertyIndex;
                ResetPropertyValueText();
            }
        }

        private int _selectedPropertyIndex;

        public CardModel PreviewCardModel
        {
            get => _previewCardModel;
            set
            {
                _previewCard?.UnregisterDisplay(this);
                _previewCard = null;

                _previewCardModel = value;
                HidePreviewPresentation();

                if (_previewCardModel == null)
                    return;

                _hoverPreview ??= new CardHoverPreview((RectTransform)transform);
                _hoverPreview.Show(_previewCardModel.GetComponent<Image>());
                var card = _previewCardModel.Value;
                _previewCard = card;
                card.RegisterDisplay(this);
                preview.alpha = 1;
                foreach (var previewName in previewNameText)
                    GameContentText.Set(previewName, card.Game, display => display.CardName(card));
                foreach (var previewId in previewIdText)
                    Cgs.Localization.LocalizedUiText.SetLiteral(previewId, card.Id);
            }
        }

        private CardModel _previewCardModel;
        private UnityCard _previewCard;
        private CardHoverPreview _hoverPreview;

        public CardModel SelectedCardModel
        {
            get => _selectedCardModel;
            set
            {
                PreviewCardModel = null;

                if (_selectedCardModel != null)
                {
                    _selectedCardModel.HighlightMode = HighlightMode.Off;
                    _selectedCardModel.Value.UnregisterDisplay(this);
                }

                _selectedCardModel = value;

                if (_selectedCardModel != null)
                    _selectedCardModel.Value.RegisterDisplay(this);
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);

                IsVisible = _selectedCardModel != null;
                ZoomTime = 0;

                ResetTexts();
            }
        }

        private CardModel _selectedCardModel;

        public bool Zoom
        {
            get => zoomPanel.gameObject.activeSelf;
            set
            {
                if (ZoomTime > 0.5f || value)
                    zoomPanel.gameObject.SetActive(value);
            }
        }

        public float ZoomTime { get; private set; }

        public bool IsActionable { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (!_isVisible && zoomPanel != null)
                    zoomPanel.gameObject.SetActive(false);
                if (SelectedCardModel != null)
                    SelectedCardModel.HighlightMode = _isVisible ? HighlightMode.Selected : HighlightMode.Off;
                Redisplay();
            }
        }

        private bool _isVisible;
        public bool WasVisible { get; private set; }

        private bool IsBlocked => !(IsVisible || Zoom) || SelectedCardModel == null ||
                                  CardGameManager.Instance.ModalCanvas != null;

        private InputAction _pageAction;

        private Vector2 _cardActionPanelPosition;

        protected void OnEnable()
        {
            GameContentPresentation.Changed += RefreshPropertyLabels;
            CardGameManager.Instance.OnSceneActions.Add(ResetInfo);

            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
            InputSystem.actions.FindAction(Tags.ViewerZoom).performed += InputZoom;
            InputSystem.actions.FindAction(Tags.ViewerLess).performed += InputLess;
            InputSystem.actions.FindAction(Tags.ViewerMore).performed += InputMore;
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed += InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed += InputSelectNext;
            RefreshPropertyLabels(null);
        }

        protected void Start()
        {
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            ResetInfo();
        }

        protected void Update()
        {
            if (Zoom)
                ZoomTime += Time.deltaTime;
            else
                ZoomTime = 0;
            WasVisible = IsVisible;

            if (_selectedCardModel == null && IsVisible)
                IsVisible = false;

            if (IsBlocked)
                return;

            if (Mode != CardViewerMode.Minimal && SelectedCardModel.IsFacedown)
                Mode = CardViewerMode.Minimal;
            expandButton.interactable = !SelectedCardModel.IsFacedown;
            if (imageButton.gameObject.activeSelf == SelectedCardModel.IsFacedown)
                imageButton.gameObject.SetActive(!SelectedCardModel.IsFacedown);
            nameTexts[0].color = SelectedCardModel.IsFacedown ? Color.clear : Color.white;

            if (EventSystem.current.currentSelectedGameObject == null && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);

            var pageVector = _pageAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if (Mode == CardViewerMode.Maximal)
            {
                if (Mathf.Abs(pageVector.y) > PageAxisPressDeadZone)
                {
                    maximalScrollRect.verticalNormalizedPosition = pageVector.y < 0
                        ? Mathf.Clamp01(maximalScrollRect.verticalNormalizedPosition - 0.1f)
                        : Mathf.Clamp01(maximalScrollRect.verticalNormalizedPosition + 0.1f);
                }
            }
            else if (SelectedCardModel != null && !SelectedCardModel.IsStatic && PlayController.Instance != null)
            {
                var parentCardZone = SelectedCardModel.ParentCardZone;
                if (parentCardZone == null)
                    return;

                var pagePressed = _pageAction?.WasPressedThisFrame() ?? false;
                switch (parentCardZone.Type)
                {
                    case CardZoneType.Vertical:
                        if (pagePressed && Mathf.Abs(pageVector.y) > PageAxisPressDeadZone)
                        {
                            switch (pageVector.y)
                            {
                                case > 0:
                                    SelectedCardModel.transform.SetSiblingIndex(Mathf.Min(
                                        SelectedCardModel.transform.GetSiblingIndex() + 1,
                                        SelectedCardModel.transform.parent.childCount - 1));
                                    break;
                                case < 0:
                                    SelectedCardModel.transform.SetSiblingIndex(
                                        Mathf.Max(SelectedCardModel.transform.GetSiblingIndex() - 1, 0));
                                    break;
                            }
                        }

                        break;
                    case CardZoneType.Horizontal:
                        if (pagePressed && Mathf.Abs(pageVector.x) > PageAxisPressDeadZone)
                        {
                            switch (pageVector.x)
                            {
                                case > 0:
                                    SelectedCardModel.transform.SetSiblingIndex(Mathf.Min(
                                        SelectedCardModel.transform.GetSiblingIndex() + 1,
                                        SelectedCardModel.transform.parent.childCount - 1));
                                    break;
                                case < 0:
                                    SelectedCardModel.transform.SetSiblingIndex(
                                        Mathf.Max(SelectedCardModel.transform.GetSiblingIndex() - 1, 0));
                                    break;
                            }
                        }

                        break;
                    default:
                    case CardZoneType.Area:
                        if (pageVector != Vector2.zero)
                        {
                            var delta = PlayController.PlayableMoveSpeed * Time.deltaTime;
                            SelectedCardModel.Position += pageVector * delta;
                        }

                        break;
                }
            }
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (!Zoom && Mode == CardViewerMode.Maximal)
                Mode = CardViewerMode.Expanded;
            else
                SelectedCardModel.DefaultAction?.Invoke(SelectedCardModel);
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (!Zoom && Mode == CardViewerMode.Maximal)
                Mode = CardViewerMode.Expanded;
            SelectedCardModel = null;
        }

        private void ResetInfo()
        {
            foreach (var cardAspectRatioFitter in cardAspectRatioFitters)
                cardAspectRatioFitter.aspectRatio = CardGameManager.Current.CardAspectRatio;
            foreach (var text in uniqueIdTexts)
                text.transform.parent.parent.gameObject.SetActive(!CardGameManager.Current.CardNameIsUnique);

            PropertyOptions.Clear();
            PropertyOptions.Add(new Dropdown.OptionData() { text = SetLabel });
            PropertyOptions.Add(new Dropdown.OptionData() { text = IdLabel });
            PropertyPaths.Clear();
            PropertyLabels.Clear();
            PropertyPaths.Add(SetLabel);
            PropertyPaths.Add(IdLabel);
            PropertyLabels.Add(SetLabel);
            PropertyLabels.Add(IdLabel);
            foreach (var propertyDef in CardGameManager.Current.CardProperties)
                AddProperty(propertyDef);
            RefreshPropertyLabels(null);

            foreach (var propertySelector in propertySelectors)
            {
                propertySelector.options = PropertyOptions;
                propertySelector.GetComponent<LocalizedDropdown>()?.RefreshBindings();
                propertySelector.value = PrimaryPropertyIndex;
                propertySelector.onValueChanged.Invoke(propertySelector.value);
            }

            if (SelectedCardModel == null)
                SetImageSprite(null);
        }

        private void AddProperty(PropertyDef propertyDef, string parentPrefix = "")
        {
            if (propertyDef == null || parentPrefix == null)
            {
                Debug.LogWarning("AddProperty::NullProperty");
                return;
            }

            if (propertyDef.Type is PropertyType.Object or PropertyType.ObjectList)
            {
                foreach (var childProperty in propertyDef.Properties)
                    AddProperty(childProperty, parentPrefix + propertyDef.Name + PropertyDef.ObjectDelimiter);
            }
            else
            {
                var displayName = !string.IsNullOrEmpty(propertyDef.Display) ? propertyDef.Display : propertyDef.Name;
                PropertyOptions.Add(new Dropdown.OptionData() { text = displayName });
                PropertyPaths.Add(parentPrefix + propertyDef.Name);
                PropertyLabels.Add(displayName);
            }
        }

        private void RefreshPropertyLabels(UnityCardGame changedGame)
        {
            var game = CardGameManager.Current;
            if (game == null || (changedGame != null && changedGame != game) || PropertyPaths.Count < 2)
                return;
            var display = GameContentPresentation.Context(game);
            for (var index = 2; index < PropertyPaths.Count; index++)
                PropertyOptions[index].text = display.PropertyLabel(PropertyPaths[index], PropertyLabels[index]);
            foreach (var selector in propertySelectors.Where(selector => selector != null))
                selector.RefreshShownValue();
        }

        private void InputSelectPrevious(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            DecrementProperty();
        }

        [UsedImplicitly]
        public void DecrementProperty()
        {
            SelectedPropertyIndex--;
        }

        private void InputSelectNext(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            IncrementProperty();
        }

        [UsedImplicitly]
        public void IncrementProperty()
        {
            SelectedPropertyIndex++;
        }

        // The preview and zoom show the card image as it is, without rotating it to the game's card orientation
        private UnityCard _imageCard;
        public void SetImageCard(UnityCard card)
        {
            _imageCard = card;
            SetImageSprite(card?.ImageSprite);
        }

        public bool UsesOrientedImage => false;

        public void SetImageSprite(Sprite imageSprite)
        {
            foreach (var cardImage in cardImages.Where(cardImage => cardImage != null))
                if (_imageCard != null)
                    GameContentImage.Set(cardImage, _imageCard.Game,
                        imageSprite != null ? imageSprite : CardGameManager.Current.CardBackImageSprite,
                        false, "cardImages", _imageCard.Id);
                else
                    GameContentImage.Set(cardImage, CardGameManager.Current, CardGameManager.Current.CardBackImageSprite,
                        false, "cardBackImageUrl");
            ResetTexts();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!Zoom)
                IsVisible = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!Zoom)
                IsVisible = false;
        }

        public void MaximizeOn(CardModel cardModel)
        {
            SelectedCardModel = cardModel;
            Mode = CardViewerMode.Maximal;
        }

        private void InputZoom(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedCardModel != null && imageButton.gameObject.activeSelf)
                Zoom = !Zoom;
        }

        public void ZoomOn(CardModel cardModel)
        {
            var isVisible = IsVisible;
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);
            SelectedCardModel = cardModel;
            IsVisible = isVisible;
            Zoom = true;
        }

        private void InputLess(InputAction.CallbackContext callbackContext)
        {
            // Gamepad does not have enough buttons for all card actions, so prefer rotation over less/more
            var block = callbackContext.control.device is Gamepad && SelectedCardModel != null &&
                        SelectedCardModel.ParentCardZone != null && SelectedCardModel.ParentCardZone.AllowsRotation;
            if (IsBlocked || block)
                return;

            Mode = Mode switch
            {
                CardViewerMode.Maximal => CardViewerMode.Expanded,
                CardViewerMode.Expanded => CardViewerMode.Minimal,
                _ => Mode
            };
        }

        private void InputMore(InputAction.CallbackContext callbackContext)
        {
            // Gamepad does not have enough buttons for all card actions, so prefer rotation over less/more
            var block = callbackContext.control.device is Gamepad && SelectedCardModel != null &&
                        SelectedCardModel.ParentCardZone != null && SelectedCardModel.ParentCardZone.AllowsRotation;
            if (IsBlocked || block)
                return;

            Mode = Mode switch
            {
                CardViewerMode.Minimal => CardViewerMode.Expanded,
                CardViewerMode.Expanded => CardViewerMode.Maximal,
                _ => Mode
            };
        }

        [UsedImplicitly]
        public void SetMode(int mode)
        {
            Mode = (CardViewerMode)mode;
        }

        private void Redisplay()
        {
            HidePreview();

            minimal.alpha = IsVisible && Mode == CardViewerMode.Minimal ? 1 : 0;
            minimal.interactable = IsVisible && Mode == CardViewerMode.Minimal;
            minimal.blocksRaycasts = IsVisible && Mode == CardViewerMode.Minimal;

            expanded.alpha = IsVisible && Mode == CardViewerMode.Expanded ? 1 : 0;
            expanded.interactable = IsVisible && Mode == CardViewerMode.Expanded;
            expanded.blocksRaycasts = IsVisible && Mode == CardViewerMode.Expanded;

            if (cardActionPanel.gameObject.activeSelf)
            {
                if (IsVisible && IsActionable && PlaySettings.ShowActionsMenu)
                    cardActionPanel.Show();
                else
                    cardActionPanel.Hide();
            }

            maximal.alpha = IsVisible && Mode == CardViewerMode.Maximal ? 1 : 0;
            maximal.interactable = IsVisible && Mode == CardViewerMode.Maximal;
            maximal.blocksRaycasts = IsVisible && Mode == CardViewerMode.Maximal;
        }

        private void ResetTexts()
        {
            if (SelectedCardModel == null || !IsVisible)
            {
                foreach (var nameText in nameTexts)
                    Cgs.Localization.LocalizedUiText.SetLiteral(nameText, string.Empty);
                foreach (var uniqueIdText in uniqueIdTexts)
                    Cgs.Localization.LocalizedUiText.SetLiteral(uniqueIdText, string.Empty);
                Cgs.Localization.LocalizedUiText.SetLiteral(idText, string.Empty);
                Cgs.Localization.LocalizedUiText.SetLiteral(setText, string.Empty);
                ResetPropertyValueText();
                return;
            }

            foreach (var nameText in nameTexts)
                GameContentText.Set(nameText, SelectedCardModel.Value.Game, display => display.CardName(SelectedCardModel.Value));
            foreach (var uniqueIdText in uniqueIdTexts)
                Cgs.Localization.LocalizedUiText.SetLiteral(uniqueIdText, SelectedCardModel.Id);
            Cgs.Localization.LocalizedUiText.Set(idText, Cgs.Localization.UiMessage.With("cards.id",
                IdLabel + Delimiter + SelectedCardModel.Id, ("cardId", SelectedCardModel.Id)));
            GameContentText.SetMessage(setText, SelectedCardModel.Value.Game, display =>
            {
                var card = SelectedCardModel.Value;
                var setName = card.Game.Sets.TryGetValue(card.SetCode, out var set) ? display.SetName(set) : card.SetCode;
                return UiMessage.With("cards.set", SetLabel + Delimiter + setName, ("setName", setName));
            });
            foreach (var propertyText in PropertyTexts)
                Destroy(propertyText.gameObject);
            PropertyTexts.Clear();
            for (var i = 2; i < PropertyOptions.Count; i++)
            {
                var newPropertyText = Instantiate(propertyTextTemplate.gameObject, maximalScrollRect.content)
                    .GetComponent<Text>();
                newPropertyText.gameObject.SetActive(true);
                var path = PropertyPaths[i];
                var label = PropertyLabels[i];
                var card = SelectedCardModel.Value;
                GameContentText.Set(newPropertyText, card.Game, display =>
                    display.PropertyLabel(path, label) + Delimiter + display.PropertyValue(card, path));
                PropertyTexts.Add(newPropertyText);
            }

            maximalScrollRect.verticalNormalizedPosition = 1;
            ResetPropertyValueText();
        }

        private void ResetPropertyValueText()
        {
            if (SelectedCardModel == null || !IsVisible)
            {
                foreach (var propertyValueText in propertyValueTexts)
                    Cgs.Localization.LocalizedUiText.SetLiteral(propertyValueText, string.Empty);
                return;
            }

            var card = SelectedCardModel.Value;
            var propertyIndex = SelectedPropertyIndex;
            var path = SelectedPropertyName;
            foreach (var propertyValueText in propertyValueTexts)
                GameContentText.Set(propertyValueText, card.Game, display =>
                {
                    if (propertyIndex > 1)
                        return display.PropertyValue(card, path);
                    if (propertyIndex == 1)
                        return card.Id;
                    return card.Game.Sets.TryGetValue(card.SetCode, out var set) ? display.SetName(set) : string.Empty;
                });
        }

        public void HidePreview()
        {
            PreviewCardModel = null;
        }

        private void HidePreviewPresentation()
        {
            preview.alpha = 0;
            _hoverPreview?.Hide();
        }

        protected void LateUpdate()
        {
            if (_previewCardModel == null || !_previewCardModel.isActiveAndEnabled || _previewCardModel.IsFacedown
                || IsVisible || Zoom || CgsNetPlayable.IsAnyDragging)
            {
                if (_previewCardModel != null || _previewCard != null || preview.alpha > 0)
                    HidePreview();
                return;
            }

            if (!Cgs.Menu.Settings.PreviewOnMouseOver
                || (PlayableViewer.Instance != null && PlayableViewer.Instance.IsVisible)
                || CardGameManager.Instance.ModalCanvas != null)
            {
                HidePreviewPresentation();
                return;
            }

            if (preview.alpha == 0)
            {
                preview.alpha = 1;
                _hoverPreview?.Show(_previewCardModel.GetComponent<Image>());
            }
            else
                _hoverPreview?.Refresh();
        }

        protected void OnDisable()
        {
            GameContentPresentation.Changed -= RefreshPropertyLabels;
            HidePreview();
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
            InputSystem.actions.FindAction(Tags.ViewerZoom).performed -= InputZoom;
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed -= InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed -= InputSelectNext;
            InputSystem.actions.FindAction(Tags.ViewerLess).performed -= InputLess;
            InputSystem.actions.FindAction(Tags.ViewerMore).performed -= InputMore;
        }
    }
}
