/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play.Drawer
{
    public class CardDrawer : MonoBehaviour
    {
        public const string DefaultHandName = "Hand";
        public const string DefaultDrawerName = "Drawer";

        private const float HandleHeight = 100.0f;

        public static readonly Vector2 ShownPosition = Vector2.zero;

        public static Vector2 HiddenPosition =>
            new Vector2(0, -(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y) - 10);

        public StackViewer viewer;
        public Button downButton;
        public Button upButton;
        public RectTransform panelRectTransform;
        public RectTransform cardZonesRectTransform;
        public List<RectTransform> cardZoneRectTransforms;

        public Toggle firstToggle;
        public Toggle secondToggle;
        public List<Text> nameTexts;
        public List<Text> countTexts;

        private void OnEnable()
        {
            firstToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectTab(0);
            });
            secondToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectTab(1);
            });
            CardGameManager.Instance.OnSceneActions.Add(Resize);
            viewer.Sync(0, cardZoneRectTransforms[0].GetComponentInChildren<CardZone>(),
                nameTexts[0], countTexts[0]);
        }

        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            if (!Inputs.IsSort)
                return;

            if (upButton.interactable)
                Show();
            else
                Hide();
        }

        private void Resize()
        {
            float cardHeight = CardGameManager.Current.CardSize.Y * CardGameManager.PixelsPerInch;
            panelRectTransform.sizeDelta = new Vector2(panelRectTransform.sizeDelta.x, HandleHeight + cardHeight);
            cardZonesRectTransform.sizeDelta = new Vector2(cardZonesRectTransform.sizeDelta.x, cardHeight);
            foreach (RectTransform cardZoneRectTransform in cardZoneRectTransforms)
                cardZoneRectTransform.sizeDelta = new Vector2(cardZoneRectTransform.sizeDelta.x, cardHeight);
        }

        public void Show()
        {
            if (CgsNetManager.Instance.LocalPlayer.HandNames.Count < 2)
                CgsNetManager.Instance.LocalPlayer.RequestNewHand("Drawer");
            panelRectTransform.anchoredPosition = ShownPosition;
            downButton.interactable = true;
            upButton.interactable = false;
        }

        public void AddCard(UnityCard card)
        {
            viewer.AddCard(card);
        }

        [UsedImplicitly]
        public void SelectTab(int tabIndex)
        {
            for (var i = 0; i < cardZoneRectTransforms.Count; i++)
                cardZoneRectTransforms[i].gameObject.SetActive(i == tabIndex);
            CgsNetManager.Instance.LocalPlayer.RequestUseHand(tabIndex);
            var cardZone = cardZoneRectTransforms[tabIndex].GetComponentInChildren<CardZone>();
            viewer.Sync(tabIndex, cardZone,
                nameTexts[tabIndex], countTexts[tabIndex]);
            SyncNetwork(cardZone.GetComponentsInChildren<CardModel>().Select(cardModel => cardModel.Id).ToArray());
        }

        public void SyncNetwork(string[] cardIds)
        {
            if (!CgsNetManager.Instance.isNetworkActive || CgsNetManager.Instance.LocalPlayer == null)
                return;

            int index = CgsNetManager.Instance.LocalPlayer.CurrentHand;
            CgsNetManager.Instance.LocalPlayer.RequestSyncHand(index, cardIds);
        }

        public void Clear()
        {
            foreach (CardZone cardZone in cardZonesRectTransform.GetComponentsInChildren<CardZone>())
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
