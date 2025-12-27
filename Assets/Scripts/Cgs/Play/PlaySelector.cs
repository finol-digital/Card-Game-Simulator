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
            if (!(_moveAction?.WasPressedThisFrame() ?? false))
                return;

            if (IsBlocked)
                return;

            if (CardViewer.Instance.IsVisible || PlayableViewer.Instance.IsVisible)
                return;

            var handCards = PlayController.Instance.drawer.cardZoneRectTransforms[0]
                .GetComponentsInChildren<CardModel>(true);
            EventSystem.current.SetSelectedGameObject(handCards[0].gameObject);
        }
    }
}
