/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.Menu;
using Cgs.UI;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play
{
    [RequireComponent(typeof(Modal))]
    public class MoveMenu : SelectionPanel
    {
        public Button moveButton;

        private CardModel _selectedCardModel;
        private ICardContainer _selectedCardContainer;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();
        private Modal _menu;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private void Start()
        {
            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            var isMoveable = _selectedCardContainer != null && toggleGroup.AnyTogglesOn();
            if(moveButton.interactable != isMoveable)
                moveButton.interactable = isMoveable;

            if (Menu.IsBlocked)
                return;

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            if (Mathf.Abs(pageVertical) > 0)
            {
                var delta = pageVertical * Time.deltaTime;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);
            }

            if (!(_moveAction?.WasPressedThisFrame() ?? false))
                return;
            var moveVertical = _moveAction.ReadValue<Vector2>().y;
            switch (moveVertical)
            {
                case > 0:
                    SelectPrevious();
                    break;
                case < 0:
                    SelectNext();
                    break;
            }
        }

        public void Show(CardModel selectedCardModel)
        {
            _selectedCardModel = selectedCardModel;
            Menu.Show();
            BuildCardZoneSelectionOptions();
        }

        private void BuildCardZoneSelectionOptions()
        {
            Dictionary<ICardContainer, string> cardContainerOptions = new()
            {
                { PlayController.Instance, "Play" },
                { PlayController.Instance.drawer, "Hand" }
            };

            Rebuild(cardContainerOptions, SelectCardContainer, _selectedCardContainer);
        }

        [UsedImplicitly]
        public void SelectCardContainer(Toggle toggle, ICardContainer selectedCardContainer)
        {
            if (toggle != null && toggle.isOn && selectedCardContainer != _selectedCardContainer)
                _selectedCardContainer = selectedCardContainer;
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked || !moveButton.interactable)
                return;

            Move();
        }

        [UsedImplicitly]
        public void Move()
        {
            if (_selectedCardModel == null || _selectedCardContainer == null)
            {
                Debug.LogError("ERROR: Move: Missing selected card model or container.");
                return;
            }

            _selectedCardContainer.AddCard(_selectedCardModel.Value);
            _selectedCardModel.RequestDelete();

            Hide();
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Hide();
        }

        [UsedImplicitly]
        public void Hide()
        {
            Menu.Hide();
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
