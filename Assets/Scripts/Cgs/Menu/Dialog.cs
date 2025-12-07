/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class Dialog : Modal
    {
        public GameObject copyButton;
        public GameObject shareButton;
        public Text messageText;
        public Button noButton;
        public Button yesButton;
        private bool _ignoreClose;

        protected struct Message : IEquatable<Message>
        {
            public string Text;
            public UnityAction NoAction;
            public UnityAction YesAction;
            public bool Unskippable;

            public bool Equals(Message other)
            {
                return !string.IsNullOrEmpty(Text) && Text.Equals(other.Text);
            }
        }

        protected Queue<Message> MessageQueue { get; } = new();

        private bool _isNewMessage;

        private InputAction _submitAction;
        private InputAction _shareAction;
        private InputAction _cancelAction;

        protected override void Start()
        {
            base.Start();

#if UNITY_IOS || UNITY_ANDROID
            copyButton.SetActive(false);
            shareButton.SetActive(true);
#elif UNITY_STANDALONE_OSX
            copyButton.SetActive(false);
            shareButton.SetActive(false);
#else
            copyButton.SetActive(true);
            shareButton.SetActive(false);
#endif

            _submitAction = InputSystem.actions.FindAction(Tags.PlayerSubmit);
            _shareAction = InputSystem.actions.FindAction(Tags.SubMenuMenu);
            _cancelAction = InputSystem.actions.FindAction(Tags.PlayerCancel);
        }

        // Popup needs to update last to consume the input over what it covers
        private void LateUpdate()
        {
            if (_isNewMessage)
            {
                _isNewMessage = false;
                return;
            }

            if (MoveAction.WasPressedThisFrame() && EventSystem.current.currentSelectedGameObject == null &&
                yesButton.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
            }

            if (_submitAction.WasPressedThisFrame())
            {
                if (yesButton.gameObject.activeInHierarchy)
                    yesButton.onClick?.Invoke();
                else
                    OkClose();
            }
            else if (FocusNextAction.WasPressedThisFrame() && noButton.gameObject.activeInHierarchy)
                noButton.onClick?.Invoke();
            else if (_shareAction.WasPressedThisFrame())
                CopyShare();
            else if (_cancelAction.WasPressedThisFrame())
                OkClose();
        }

        public void Show(string text, bool unskippable = false)
        {
            Prompt(text, null, unskippable);
        }

        public void Prompt(string text, UnityAction yesAction, bool unskippable = false)
        {
            Ask(text, null, yesAction, unskippable);
        }

        public void Ask(string text, UnityAction noAction, UnityAction yesAction, bool unskippable = false)
        {
            var message = new Message()
                { Text = text, NoAction = noAction, YesAction = yesAction, Unskippable = unskippable };
            if (gameObject.activeSelf)
            {
                if (!MessageQueue.Contains(message))
                    MessageQueue.Enqueue(message);
                return;
            }

            gameObject.SetActive(true);
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);
            transform.SetAsLastSibling();
            foreach (var canvasScaler in GetComponentsInChildren<CanvasScaler>())
                canvasScaler.referenceResolution = ResolutionManager.Resolution;

            DisplayMessage(message);
        }

        private void DisplayMessage(Message message)
        {
            messageText.text = message.Text ?? string.Empty;
            noButton.gameObject.SetActive(message.YesAction != null);

            yesButton.onClick.RemoveAllListeners();
            if (message.YesAction != null)
                yesButton.onClick.AddListener(message.YesAction);
            yesButton.onClick.AddListener(OkClose);

            noButton.onClick.RemoveAllListeners();
            if (message.NoAction != null)
                noButton.onClick.AddListener(message.NoAction);
            noButton.onClick.AddListener(OkClose);

            _ignoreClose = message.Unskippable;

            _isNewMessage = true;
        }

        [UsedImplicitly]
        public void CopyShare()
        {
            var shareText = messageText.text;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            (new NativeShare()).SetText(shareText).Share();
#else
            UniClipboard.SetText(shareText);
#endif
        }

        [UsedImplicitly]
        public void IgnoreableClose()
        {
            if (_ignoreClose)
            {
                Debug.Log("IgnoreableClose");
                return;
            }

            OkClose();
        }

        [UsedImplicitly]
        public void OkClose()
        {
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting &&
                EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);

            if (MessageQueue.Count > 0)
                DisplayMessage(MessageQueue.Dequeue());
            else
                gameObject.SetActive(false);
        }
    }
}
