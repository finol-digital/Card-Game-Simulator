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
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public static Vector2 DockedPosition =>
            new(0, -(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y) - 10);

        private static bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.WasVisible ||
                                         CardViewer.Instance.Zoom ||
                                         PlayableViewer.Instance.IsVisible || PlayableViewer.Instance.WasVisible ||
                                         CardGameManager.Instance.ModalCanvas != null;

        public StackViewer viewer;
        public Dropdown cardStackDropdown;
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
        private int _previousCardStackCount;
        private string _previousCardStackNames = string.Empty;
        private bool _isRefreshingDropdown;

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(Resize);

            InputSystem.actions.FindAction(Tags.ViewerLess).performed += InputLess;
            InputSystem.actions.FindAction(Tags.ViewerMore).performed += InputMore;
            InputSystem.actions.FindAction(Tags.PlayGameAction0).performed += InputAction0;
            InputSystem.actions.FindAction(Tags.PlayGameAction1).performed += InputAction1;
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

            if (PlayController.Instance != null)
            {
                var allCardStacks = PlayController.Instance.AllCardStacks.ToList();
                var currentCount = allCardStacks.Count;
                var currentNames = string.Join(",", allCardStacks.Select(s => s.Name));
                if (currentCount != _previousCardStackCount || currentNames != _previousCardStackNames)
                    RefreshCardStackDropdown();
            }
        }

        private void Resize()
        {
            var cardHeight = CardGameManager.Current.CardSize.Y * CardGameManager.PixelsPerInch;
            panelRectTransform.sizeDelta = new Vector2(panelRectTransform.sizeDelta.x, HandleHeight + cardHeight);
            cardZonesRectTransform.sizeDelta = new Vector2(cardZonesRectTransform.sizeDelta.x, cardHeight);
            foreach (var cardZoneRectTransform in cardZoneRectTransforms)
                cardZoneRectTransform.sizeDelta = new Vector2(cardZoneRectTransform.sizeDelta.x, cardHeight);
        }

        private void Show()
        {
            panelRectTransform.anchoredPosition = ShownPosition;
            downButton.interactable = true;
            upButton.interactable = false;
        }

        private void InputLess(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            ViewLess();
        }

        [UsedImplicitly]
        public void ViewLess()
        {
            var pos = panelRectTransform.anchoredPosition;
            var dDocked = Vector2.Distance(pos, DockedPosition);
            var dMid = Vector2.Distance(pos, MidPosition);
            var dShown = Vector2.Distance(pos, ShownPosition);

            if (dShown <= dMid && dShown <= dDocked)
            {
                // Shown -> SemiShow
                SemiShow();
            }
            else if (dMid <= dShown && dMid <= dDocked)
            {
                // SemiShow -> Docked
                Dock();
            }
            else
            {
                // Hidden -> stay Docked
                Dock();
            }
        }

        private void InputMore(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            ViewMore();
        }

        [UsedImplicitly]
        public void ViewMore()
        {
            var pos = panelRectTransform.anchoredPosition;
            var dDocked = Vector2.Distance(pos, DockedPosition);
            var dMid = Vector2.Distance(pos, MidPosition);
            var dShown = Vector2.Distance(pos, ShownPosition);

            if (dDocked <= dMid && dDocked <= dShown)
            {
                // Docked -> SemiShow
                SemiShow();
            }
            else if (dMid <= dDocked && dMid <= dShown)
            {
                // SemiShow -> Shown
                Show();
            }
            else
            {
                // Shown -> stay Shown
                Show();
            }
        }

        public void SemiShow()
        {
            panelRectTransform.anchoredPosition = MidPosition;
            downButton.interactable = true;
            upButton.interactable = true;
        }

        public void RefreshCardStackDropdown()
        {
            if (PlayController.Instance == null)
                return;

            _isRefreshingDropdown = true;

            var allCardStacks = PlayController.Instance.AllCardStacks.ToList();
            _previousCardStackCount = allCardStacks.Count;
            _previousCardStackNames = string.Join(",", allCardStacks.Select(s => s.Name));

            cardStackDropdown.ClearOptions();
            cardStackDropdown.AddOptions(allCardStacks.Select(s => s.Name).ToList());

            var selectedIndex = -1;
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.IsOnline &&
                CgsNetManager.Instance.LocalPlayer != null)
            {
                var currentDeck = CgsNetManager.Instance.LocalPlayer.CurrentDeck;
                if (currentDeck != null)
                {
                    for (var i = 0; i < allCardStacks.Count; i++)
                    {
                        if (allCardStacks[i].GetComponent<NetworkObject>() != currentDeck)
                            continue;
                        selectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                var currentDeck = PlayController.Instance.CurrentDeckStack;
                if (currentDeck != null)
                {
                    for (var i = 0; i < allCardStacks.Count; i++)
                    {
                        if (allCardStacks[i] != currentDeck)
                            continue;
                        selectedIndex = i;
                        break;
                    }
                }
            }

            if (selectedIndex >= 0 && selectedIndex < allCardStacks.Count)
                cardStackDropdown.value = selectedIndex;
            else if (allCardStacks.Count > 0)
                cardStackDropdown.value = 0;

            cardStackDropdown.RefreshShownValue();

            _isRefreshingDropdown = false;
        }

        [UsedImplicitly]
        public void SetCardStack(int cardStackIndex)
        {
            if (_isRefreshingDropdown)
                return;

            if (PlayController.Instance == null)
                return;

            var allCardStacks = PlayController.Instance.AllCardStacks.ToList();
            if (cardStackIndex < 0 || cardStackIndex >= allCardStacks.Count)
                return;

            var selectedStack = allCardStacks[cardStackIndex];

            if (CgsNetManager.Instance != null && CgsNetManager.Instance.IsOnline &&
                CgsNetManager.Instance.LocalPlayer != null)
            {
                var networkObject = selectedStack.GetComponent<NetworkObject>();
                if (networkObject != null)
                    CgsNetManager.Instance.LocalPlayer.RequestSetCurrentDeck(networkObject);
            }
            else
            {
                PlayController.Instance.CurrentDeckStack = selectedStack;
            }
        }

        public void AddCard(UnityCard card)
        {
            viewer.AddCard(card);
        }

        private void InputAction0(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            Deal();
        }

        [UsedImplicitly]
        public void Deal()
        {
            PlayController.Instance.ShowDealer();
        }

        private void InputAction1(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            Draw();
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
            sizeDelta = new Vector2(sizeDelta.x + ((RectTransform)tabPrefab.transform).sizeDelta.x, sizeDelta.y);
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

            var cardZoneRectTransform = (RectTransform)Instantiate(cardZonePrefab, cardZonesRectTransform).transform;
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
            sizeDelta = new Vector2(sizeDelta.x - ((RectTransform)tabPrefab.transform).sizeDelta.x, sizeDelta.y);
            tabsRectTransform.sizeDelta = sizeDelta;

            if (CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveHand(tabIndex);
        }

        public void Clear()
        {
            foreach (var cardZone in cardZonesRectTransform.GetComponentsInChildren<CardZone>())
                cardZone.Clear();
        }

        private void Dock()
        {
            panelRectTransform.anchoredPosition = DockedPosition;
            downButton.interactable = false;
            upButton.interactable = true;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.ViewerLess).performed -= InputLess;
            InputSystem.actions.FindAction(Tags.ViewerMore).performed -= InputMore;
            InputSystem.actions.FindAction(Tags.PlayGameAction0).performed -= InputAction0;
            InputSystem.actions.FindAction(Tags.PlayGameAction1).performed -= InputAction1;
        }
    }
}
