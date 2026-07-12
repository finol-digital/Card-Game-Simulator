/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
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
        private const string DefaultZoneName = "Zone";

        public Button moveButton;

        private CardModel _selectedCardModel;
        private ICardContainer _selectedCardContainer;

        // Card containers are Unity objects that may have been destroyed since they were selected
        private bool IsSelectedCardContainerAvailable =>
            _selectedCardContainer is Object unityObject && unityObject != null;

        private ICardContainer CurrentCardContainer
        {
            get
            {
                var parentCardZone = _selectedCardModel == null ? null : _selectedCardModel.ParentCardZone;
                if (parentCardZone == null)
                    return null;

                if (parentCardZone == PlayController.Instance.playAreaCardZone)
                    return PlayController.Instance;

                if (parentCardZone.transform.IsChildOf(PlayController.Instance.drawer.cardZonesRectTransform))
                    return PlayController.Instance.drawer;

                var stackViewer = parentCardZone.GetComponentInParent<StackViewer>();
                if (stackViewer != null)
                    return PlayController.Instance.AllCardStacks.FirstOrDefault(cardStack =>
                        cardStack.Viewer == stackViewer);

                return parentCardZone;
            }
        }

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
            var isMoveable = IsSelectedCardContainerAvailable && toggleGroup.AnyTogglesOn();
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

            foreach (var cardZone in PlayController.Instance.AllCardZones)
                if (cardZone != PlayController.Instance.playAreaCardZone)
                    cardContainerOptions.Add(cardZone,
                        string.IsNullOrEmpty(cardZone.Name) ? DefaultZoneName : cardZone.Name);

            foreach (var cardStack in PlayController.Instance.AllCardStacks)
            {
                var stackName = string.IsNullOrEmpty(cardStack.Name)
                    ? PlayController.DefaultStackName
                    : cardStack.Name;
                var ownerName = GetOwnerName(cardStack);
                cardContainerOptions.Add(cardStack,
                    string.IsNullOrEmpty(ownerName) ? stackName : $"{stackName} ({ownerName})");
            }

            // The card should not be able to be moved to the container it is already in
            var currentCardContainer = CurrentCardContainer;
            if (currentCardContainer != null)
                cardContainerOptions.Remove(currentCardContainer);

            Rebuild(cardContainerOptions, SelectCardContainer, _selectedCardContainer);
        }

        private static string GetOwnerName(CardStack cardStack)
        {
            if (CgsNetManager.Instance == null || !CgsNetManager.Instance.IsOnline || !cardStack.IsSpawned)
                return string.Empty;

            var owner = GameObject.FindGameObjectsWithTag("Player")
                .Select(player => player.GetComponent<CgsNetPlayer>())
                .FirstOrDefault(player => player != null && player.OwnerClientId == cardStack.OwnerClientId);
            return owner == null ? string.Empty : owner.Name;
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
            if (_selectedCardModel == null || !IsSelectedCardContainerAvailable)
            {
                Debug.LogError("ERROR: Move: Missing selected card model or container.");
                return;
            }

            switch (_selectedCardContainer)
            {
                case CardZone cardZone:
                    cardZone.AddCard(_selectedCardModel.Value, _selectedCardModel.IsFacedown);
                    break;
                case PlayController playController:
                    playController.AddCard(_selectedCardModel.Value, _selectedCardModel.IsFacedown);
                    break;
                default:
                    _selectedCardContainer.AddCard(_selectedCardModel.Value);
                    break;
            }

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
