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

            CgsNetPlayable selectedPlayable = null;
            if (CardViewer.Instance.IsVisible)
                selectedPlayable = CardViewer.Instance.SelectedCardModel;
            else if (PlayableViewer.Instance.IsVisible)
                selectedPlayable = PlayableViewer.Instance.SelectedPlayable;

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found for navigation.");
                return;
            }

            var selectedPlayablePosition = selectedPlayable?.transform.position ?? Vector3.zero;
            var startPosition = mainCamera.WorldToScreenPoint(selectedPlayablePosition);
            var moveDirection = _moveAction.ReadValue<Vector2>().normalized;
            CgsNetPlayable bestTarget = null;
            var bestScore = float.NegativeInfinity;

            // Expensive lookup that may be optimized later
            var allPlayables = PlayController.Instance.transform.GetComponentsInChildren<CgsNetPlayable>();
            foreach (var targetPlayable in allPlayables)
            {
                if (targetPlayable == selectedPlayable || targetPlayable is CardZone)
                    continue;
                var targetPosition = mainCamera.WorldToScreenPoint(targetPlayable.transform.position);
                var targetDirection = ((Vector2)targetPosition - (Vector2)startPosition).normalized;
                var dot = Vector2.Dot(moveDirection, targetDirection);
                if (dot <= 0.5f) // Only consider playables roughly in the input direction
                    continue;
                var distance = ((Vector2)targetPosition - (Vector2)startPosition).magnitude;
                // Score: prefer higher dot (closer to direction), then closer distance
                var score = dot * 1000f - distance;
                if (score <= bestScore)
                    continue;
                bestScore = score;
                bestTarget = targetPlayable;
            }

            if (bestTarget != null)
            {
                EventSystem.current.SetSelectedGameObject(bestTarget.gameObject);
            }
            // else: no valid target in that direction, do nothing
        }
    }
}
