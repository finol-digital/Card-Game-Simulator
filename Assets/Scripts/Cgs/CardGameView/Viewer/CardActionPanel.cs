/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Viewer
{
    public delegate void CardAction(CardModel cardModel);

    public class CardActionPanel : MonoBehaviour
    {
        public static IReadOnlyDictionary<FinolDigital.Cgs.Json.CardAction, CardAction> CardActionDictionary =>
            _cardActionDictionary ??= new Dictionary<FinolDigital.Cgs.Json.CardAction, CardAction>
            {
                [FinolDigital.Cgs.Json.CardAction.Move] = Move,
                [FinolDigital.Cgs.Json.CardAction.Rotate] = Rotate,
                [FinolDigital.Cgs.Json.CardAction.Tap] = Tap,
                [FinolDigital.Cgs.Json.CardAction.Flip] = Flip,
                [FinolDigital.Cgs.Json.CardAction.Discard] = Discard
            };

        private static Dictionary<FinolDigital.Cgs.Json.CardAction, CardAction> _cardActionDictionary;

        public Button moveButton;
        public Button rotateButton;
        public Button tapButton;
        public Button flipButton;
        public Button discardButton;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        }

        public void Show()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (CardViewer.Instance.SelectedCardModel == null)
                return;

            rotateButton.interactable =
                CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;
            rotateButton.transform.GetChild(0).GetChild(0).GetComponent<Image>().color =
                CardViewer.Instance.SelectedCardModel.DefaultAction == Rotate
                    ? Color.green
                    : Color.white;

            tapButton.interactable =
                CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;
            tapButton.transform.GetChild(0).GetChild(0).GetComponent<Image>().color =
                CardViewer.Instance.SelectedCardModel.DefaultAction == Tap
                    ? Color.green
                    : Color.white;

            flipButton.interactable =
                CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsFlip;
            flipButton.transform.GetChild(0).GetChild(0).GetComponent<Image>().color =
                CardViewer.Instance.SelectedCardModel.DefaultAction == Flip
                    ? Color.green
                    : Color.white;
        }

        public void Update()
        {
            if (!_canvasGroup.interactable)
                return;

            if (Inputs.IsNew && moveButton.interactable)
                Move(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsLoad && rotateButton.interactable)
                Rotate(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsSave && tapButton.interactable)
                Tap(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsFilter && flipButton.interactable)
                Flip(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsOption && discardButton.interactable)
                Discard(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Move()
        {
            Move(CardViewer.Instance.SelectedCardModel);
        }

        public static void Move(CardModel cardModel)
        {
            CardGameManager.Instance.Messenger.Show("Move Feature is Coming Soon!");
        }

        [UsedImplicitly]
        public void Rotate()
        {
            Rotate(CardViewer.Instance.SelectedCardModel);
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

        [UsedImplicitly]
        public void Tap()
        {
            Tap(CardViewer.Instance.SelectedCardModel);
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

        [UsedImplicitly]
        public void Flip()
        {
            Flip(CardViewer.Instance.SelectedCardModel);
        }

        public static void Flip(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsFlip)
            {
                Debug.LogWarning("Ignoring flip request since the parent card zone does not support it.");
                return;
            }

            if (cardModel.Value.IsBackFaceCard && !string.IsNullOrEmpty(cardModel.Value.BackFaceId)
                                               && CardGameManager.Current.Cards.TryGetValue(cardModel.Value.BackFaceId,
                                                   out var backFaceCard))
                cardModel.Value = backFaceCard;
            else
                cardModel.IsFacedown = !cardModel.IsFacedown;
        }

        [UsedImplicitly]
        public void Discard()
        {
            Discard(CardViewer.Instance.SelectedCardModel);
        }

        public static void Discard(CardModel cardModel)
        {
            cardModel.PromptDelete();
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
