/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.CardGameView.Viewer
{
    public delegate void CardAction(CardModel cardModel);

    public class CardActions : MonoBehaviour
    {
        public static IReadOnlyDictionary<FinolDigital.Cgs.CardGameDef.CardAction, CardAction> ActionsDictionary =>
            _actionsDictionary ??= new Dictionary<FinolDigital.Cgs.CardGameDef.CardAction, CardAction>
            {
                [FinolDigital.Cgs.CardGameDef.CardAction.Move] = Move,
                [FinolDigital.Cgs.CardGameDef.CardAction.Flip] = Flip,
                [FinolDigital.Cgs.CardGameDef.CardAction.Rotate] = Rotate,
                [FinolDigital.Cgs.CardGameDef.CardAction.Tap] = Tap,
                [FinolDigital.Cgs.CardGameDef.CardAction.Zoom] = Zoom
            };

        private static Dictionary<FinolDigital.Cgs.CardGameDef.CardAction, CardAction> _actionsDictionary;

        public static void Move(CardModel cardModel)
        {
            CardGameManager.Instance.Messenger.Show("Move Feature is Coming Soon!");
        }

        public static void Flip(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsFlip)
            {
                Debug.LogWarning("Ignoring flip request since the parent card zone does not support it.");
                return;
            }

            cardModel.IsFacedown = !cardModel.IsFacedown;
        }

        public static void Rotate(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsRotation)
            {
                Debug.LogWarning("Ignoring rotation request since the parent card zone does not support it.");
                return;
            }

            cardModel.Rotation *= Quaternion.Euler(0, 0, -CardGameManager.Current.GameCardRotationDegrees);
        }

        public static void Tap(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsRotation)
            {
                Debug.LogWarning("Ignoring rotation request since the parent card zone does not support it.");
                return;
            }

            var unTappedRotation = Quaternion.identity;
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.LocalPlayer != null)
                unTappedRotation = CgsNetManager.Instance.LocalPlayer.DefaultRotation;
            var isTapped = !unTappedRotation.Equals(cardModel.Rotation);
            var tappedRotation = unTappedRotation *
                                 Quaternion.Euler(0, 0, -CardGameManager.Current.GameCardRotationDegrees);
            cardModel.Rotation = isTapped ? unTappedRotation : tappedRotation;
        }

        public static void Zoom(CardModel cardModel)
        {
            if (CardViewer.Instance != null)
                CardViewer.Instance.ZoomOn(cardModel);
        }

        public List<Transform> buttonPanels;
        public List<Button> flipButtons;
        public List<Button> moveButtons;
        public List<Button> rotateButtons;
        public List<Button> tapButtons;
        public Transform zoomButton;

        public void Show()
        {
            foreach (var panel in buttonPanels)
                panel.gameObject.SetActive(true);
        }

        public void Update()
        {
            var isCardSelected = CardViewer.Instance != null && CardViewer.Instance.IsVisible &&
                                 CardViewer.Instance.SelectedCardModel != null;

            foreach (var flipButton in flipButtons)
            {
                flipButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsFlip;
                flipButton.transform.GetChild(0).GetComponent<Image>().color =
                    isCardSelected && CardGameManager.Current.GameDefaultCardAction ==
                    FinolDigital.Cgs.CardGameDef.CardAction.Flip
                        ? Color.green
                        : Color.white;
            }

            foreach (var rotateButton in rotateButtons)
            {
                rotateButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;
                rotateButton.transform.GetChild(0).GetComponent<Image>().color =
                    isCardSelected && CardGameManager.Current.GameDefaultCardAction ==
                    FinolDigital.Cgs.CardGameDef.CardAction.Rotate
                        ? Color.green
                        : Color.white;
            }

            foreach (var tapButton in tapButtons)
            {
                tapButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;
                tapButton.transform.GetChild(0).GetComponent<Image>().color =
                    isCardSelected && CardGameManager.Current.GameDefaultCardAction ==
                    FinolDigital.Cgs.CardGameDef.CardAction.Tap
                        ? Color.green
                        : Color.white;
            }

            var zoomButtonActive = isCardSelected && !CardViewer.Instance.nameVisibleButton.gameObject.activeSelf;
            if (zoomButtonActive != zoomButton.gameObject.activeSelf)
                zoomButton.gameObject.SetActive(zoomButtonActive);

            if (Inputs.IsFilter && flipButtons[0].interactable)
                Flip(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsNew && moveButtons[0].interactable)
                Move(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsLoad && rotateButtons[0].interactable)
                Rotate(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsSave && tapButtons[0].interactable)
                Tap(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsOption && CardViewer.Instance != null)
            {
                if (CardViewer.Instance.Zoom)
                    CardViewer.Instance.Zoom = false;
                else if (zoomButton.gameObject.activeSelf)
                    CardViewer.Instance.Zoom = !CardViewer.Instance.Zoom;
                else if (CardViewer.Instance.nameVisibleButton.gameObject.activeSelf)
                    CardViewer.Instance.ToggleIsNameVisible();
            }
        }

        [UsedImplicitly]
        public void Flip()
        {
            Flip(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Move()
        {
            Move(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Rotate()
        {
            Rotate(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Tap()
        {
            Tap(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Zoom()
        {
            Zoom(CardViewer.Instance.SelectedCardModel);
        }
    }
}
