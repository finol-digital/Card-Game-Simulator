/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Play.Multiplayer;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Drawer
{
    public class CardDrawer : MonoBehaviour
    {
        public const string DefaultHandName = "Hand";
        public const string DefaultDrawerName = "Drawer";
        public static string RemoveDrawerPrompt(int n) => $"Remove Drawer {n}?";

        private const float HandleHeight = 100.0f;

        public static readonly Vector2 ShownPosition = Vector2.zero;

        private static Vector2 MidPosition =>
            new(0, -(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y) / 2 - 10);

        public static Vector2 HiddenPosition =>
            new(0, -(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y) - 10);

        public StackViewer viewer;
        public Button downButton;
        public Button upButton;
        public RectTransform panelRectTransform;
        public RectTransform cardZonesRectTransform;
        public List<RectTransform> cardZoneRectTransforms;

        public Toggle handToggle;
        public Text handNameText;
        public Text handCountText;

        public RectTransform tabsRectTransform;

        public GameObject tabPrefab;
        public GameObject cardZonePrefab;

        private readonly List<Toggle> _toggles = new();
        private readonly List<Text> _nameTexts = new();
        private readonly List<Text> _countTexts = new();

        private int _previousOverlapSpacing;

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(Resize);
        }

        private void Awake()
        {
            _toggles.Add(handToggle);
            _nameTexts.Add(handNameText);
            _countTexts.Add(handCountText);
        }

        private void Start()
        {
            _toggles[0].GetComponent<CardDropArea>().Index = 0;
            _toggles[0].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectTab(0);
            });
        }

        private void Update()
        {
            if (PlaySettings.StackViewerOverlap != _previousOverlapSpacing)
                viewer.ApplyOverlapSpacing();
            _previousOverlapSpacing = PlaySettings.StackViewerOverlap;

/*TODO:
            if (CardGameManager.Instance.ModalCanvas != null)
                return;
            if (InputManager.IsVertical)
            {
                if (InputManager.IsUp && !InputManager.WasUp)
                    Show();
                else if (InputManager.IsDown && !InputManager.WasDown)
                    Hide();
            }

            if (InputManager.IsFocusBack && !InputManager.WasFocusBack)
                Deal();

            if (InputManager.IsFocusNext && !InputManager.WasFocusNext)
                Draw();*/
        }

        private void Resize()
        {
            var cardHeight = CardGameManager.Current.CardSize.Y * CardGameManager.PixelsPerInch;
            panelRectTransform.sizeDelta = new Vector2(panelRectTransform.sizeDelta.x, HandleHeight + cardHeight);
            cardZonesRectTransform.sizeDelta = new Vector2(cardZonesRectTransform.sizeDelta.x, cardHeight);
            foreach (var cardZoneRectTransform in cardZoneRectTransforms)
                cardZoneRectTransform.sizeDelta = new Vector2(cardZoneRectTransform.sizeDelta.x, cardHeight);
        }

        [UsedImplicitly]
        public void Show()
        {
            panelRectTransform.anchoredPosition = ShownPosition;
            downButton.interactable = true;
            upButton.interactable = false;
        }

        [UsedImplicitly]
        public void SemiShow()
        {
            panelRectTransform.anchoredPosition = MidPosition;
            downButton.interactable = true;
            upButton.interactable = true;
        }

        public void AddCard(UnityCard card)
        {
            viewer.AddCard(card);
        }

        [UsedImplicitly]
        public void Deal()
        {
            PlayController.Instance.ShowDealer();
        }

        [UsedImplicitly]
        public void Draw()
        {
            PlayController.Instance.DealHand(1);
        }

        [UsedImplicitly]
        public void AddTab()
        {
            var sizeDelta = tabsRectTransform.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x + ((RectTransform) tabPrefab.transform).sizeDelta.x, sizeDelta.y);
            tabsRectTransform.sizeDelta = sizeDelta;

            var tabTemplate = Instantiate(tabPrefab, tabsRectTransform).GetComponent<TabTemplate>();
            var tabIndex = tabsRectTransform.childCount - 2;
            tabTemplate.transform.SetSiblingIndex(tabIndex);
            tabTemplate.TabIndex = tabIndex;

            _toggles.Add(tabTemplate.toggle);
            _toggles[tabIndex].group = _toggles[0].group;
            _toggles[tabIndex].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectTab(tabTemplate.TabIndex);
            });

            _nameTexts.Add(tabTemplate.nameText);
            _countTexts.Add(tabTemplate.countText);

            tabTemplate.removeButton.onClick.AddListener(() => PromptRemoveTab(tabTemplate.TabIndex));
            tabTemplate.drawerHandle.cardDrawer = this;

            var tabCardDropArea = _toggles[tabIndex].GetComponent<CardDropArea>();
            tabCardDropArea.DropHandler = viewer;
            tabCardDropArea.Index = tabIndex;
            viewer.drops.Add(tabCardDropArea);
            tabTemplate.TabCardDropArea = tabCardDropArea;

            var cardZoneRectTransform = (RectTransform) Instantiate(cardZonePrefab, cardZonesRectTransform).transform;
            var cardHeight = CardGameManager.Current.CardSize.Y * CardGameManager.PixelsPerInch;
            cardZoneRectTransform.sizeDelta = new Vector2(cardZoneRectTransform.sizeDelta.x, cardHeight);
            cardZoneRectTransforms.Add(cardZoneRectTransform);

            var cardZoneCardDropArea = cardZoneRectTransform.GetComponent<CardDropArea>();
            cardZoneCardDropArea.DropHandler = viewer;
            viewer.drops.Add(cardZoneCardDropArea);
            tabTemplate.CardZoneCardDropArea = cardZoneCardDropArea;

            if (CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.RequestNewHand(DefaultDrawerName);
        }

        [UsedImplicitly]
        public void SelectTab(int tabIndex)
        {
            if (tabIndex >= cardZoneRectTransforms.Count)
            {
                Debug.LogWarning($"SelectTab {tabIndex} but not created yet.");
                return;
            }

            for (var i = 0; i < cardZoneRectTransforms.Count; i++)
                cardZoneRectTransforms[i].gameObject.SetActive(i == tabIndex);

            var cardZone = cardZoneRectTransforms[tabIndex].GetComponentInChildren<CardZone>();
            var localCardIds = cardZone.GetComponentsInChildren<CardModel>().Select(cardModel => cardModel.Id).ToList();
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.IsOnline &&
                CgsNetManager.Instance.LocalPlayer != null)
            {
                CgsNetManager.Instance.LocalPlayer.RequestUseHand(tabIndex);
                var handCards = CgsNetManager.Instance.LocalPlayer.HandCards;
                if (tabIndex >= handCards.Count)
                {
                    Debug.Log($"SelectTab {tabIndex} but not on server yet.");
                    cardZone.transform.DestroyAllChildren();
                    return;
                }

                var serverCards = handCards[tabIndex];
                var serverCardIds = serverCards.Select(unityCard => unityCard.Id).ToList();
                if (!localCardIds.SequenceEqual(serverCardIds))
                {
                    cardZone.transform.DestroyAllChildren();
                    foreach (var unityCard in serverCards)
                    {
                        var cardModel = Instantiate(viewer.cardModelPrefab, cardZone.transform)
                            .GetOrAddComponent<CardModel>();
                        cardModel.Value = unityCard;
                        var cardTransform = cardModel.transform;
                        cardTransform.SetAsFirstSibling();
                        cardTransform.rotation = Quaternion.identity;
                        cardModel.IsFacedown = false;
                        cardModel.DefaultAction = CardActionPanel.Flip;
                    }
                }
            }

            viewer.Sync(tabIndex, cardZone, _nameTexts[tabIndex], _countTexts[tabIndex]);

            if (!_toggles[tabIndex].isOn)
                _toggles[tabIndex].isOn = true;
        }

        public void SyncHand(int handIndex, CgsNetString[] cardIds)
        {
            _countTexts[handIndex].text = cardIds.Length.ToString();
        }

        private void PromptRemoveTab(int tabIndex)
        {
            CardGameManager.Instance.Messenger.Prompt(RemoveDrawerPrompt(tabIndex), () => RemoveTab(tabIndex));
        }

        private void RemoveTab(int tabIndex)
        {
            if (tabIndex < 1)
            {
                Debug.LogWarning($"RemoveTab {tabIndex}!");
                return;
            }

            if (tabIndex >= cardZoneRectTransforms.Count)
            {
                Debug.LogWarning($"RemoveTab {tabIndex} but not created yet.");
                return;
            }

            SelectTab(0);

            var tabRectTransform = tabsRectTransform.GetChild(tabIndex);
            var tabTemplate = tabRectTransform.GetComponent<TabTemplate>();

            _toggles.Remove(tabTemplate.toggle);
            _countTexts.Remove(tabTemplate.countText);
            _nameTexts.Remove(tabTemplate.nameText);

            viewer.drops.Remove(tabTemplate.CardZoneCardDropArea);
            viewer.drops.Remove(tabTemplate.TabCardDropArea);

            var cardZoneRectTransform = cardZoneRectTransforms[tabIndex];
            cardZoneRectTransforms.Remove(cardZoneRectTransform);
            Destroy(cardZoneRectTransform.gameObject);

            foreach (var tabTemplateToEdit in tabsRectTransform.GetComponentsInChildren<TabTemplate>())
            {
                if (tabTemplateToEdit.TabIndex >= tabIndex)
                {
                    tabTemplateToEdit.TabIndex -= 1;
                }
            }

            Destroy(tabRectTransform.gameObject);

            var sizeDelta = tabsRectTransform.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x - ((RectTransform) tabPrefab.transform).sizeDelta.x, sizeDelta.y);
            tabsRectTransform.sizeDelta = sizeDelta;

            if (CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveHand(tabIndex);
        }

        public void Clear()
        {
            foreach (var cardZone in cardZonesRectTransform.GetComponentsInChildren<CardZone>())
                cardZone.Clear();
        }

        public void Hide()
        {
            panelRectTransform.anchoredPosition = HiddenPosition;
            downButton.interactable = false;
            upButton.interactable = true;
        }
    }
}
