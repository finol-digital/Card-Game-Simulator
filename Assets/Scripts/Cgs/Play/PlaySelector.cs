/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Cgs.Play
{
    public class PlaySelector : MonoBehaviour
    {
        private static bool IsBlocked =>
            CardGameManager.Instance.ModalCanvas != null || EventSystem.current.alreadySelecting;

        private InputAction _moveAction;

        private void Start()
        {
            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (_moveAction?.WasPressedThisFrame() ?? false)
                InputMove();
        }

        private void InputMove()
        {
            if (IsBlocked)
                return;

            var selectedPlayable = PlayableViewer.Instance.SelectedPlayable;
            if (selectedPlayable == null)
                selectedPlayable = CardViewer.Instance.SelectedCardModel;
            if (selectedPlayable == null || !PlayableViewer.Instance.IsVisible || !CardViewer.Instance.IsVisible)
            {
                var handCards = PlayController.Instance.drawer.cardZoneRectTransforms[0]
                    .GetComponentsInChildren<CardModel>();
                if (handCards.Length > 0)
                    EventSystem.current.SetSelectedGameObject(handCards[0].gameObject);
                else
                    Debug.LogWarning("No cards in hand to select.");
                PlayController.Instance.drawer.SemiShow();
                return;
            }

            // Expensive lookup that may be optimized later
            var allPlayables = PlayController.Instance.transform.GetComponentsInChildren<CgsNetPlayable>();
            var moveVector2 = _moveAction.ReadValue<Vector2>();
            if (moveVector2.y < 0) // down
            {
            }
            else if (moveVector2.y > 0) // up
            {
            }
            else if (moveVector2.x < 0) // left
            {
            }
            else if (moveVector2.x > 0) // right
            {
            }
        }
    }
}
