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
using UnityExtensionMethods;

namespace Cgs.Menu
{
    public class Dialog : Modal
    {
        [SerializeField] GameObject copyButton;
        [SerializeField] GameObject shareButton;
        [SerializeField] Text messageText;
        [SerializeField] Button noButton;
        [SerializeField] Button yesButton;
        private bool _ignoreClose;
        private string _shareText;
        private int _messageVersion;
        private bool _canCopy;

        protected struct Message : IEquatable<Message>
        {
            public string Text;
            public UnityAction NoAction;
            public UnityAction YesAction;
            public bool Unskippable;
            public bool CanCopy { get; set; }

            public bool Equals(Message other)
            {
                return string.Equals(Text, other.Text, StringComparison.Ordinal) && CanCopy == other.CanCopy;
            }

            public override bool Equals(object obj)
            {
                return obj is Message other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Text, CanCopy);
            }
        }

        protected Queue<Message> MessageQueue { get; } = new();

        private bool _isNewMessage;

        private InputAction _submitAction;
        private InputAction _noAction;
        private InputAction _copyShareAction;
        private InputAction _cancelAction;

        protected override void Start()
        {
            base.Start();

            UpdateCopyShareButtons();

            _submitAction = InputSystem.actions.FindAction(Tags.PlayerSubmit);
            _noAction = InputSystem.actions.FindAction(Tags.SubMenuNo);
            _copyShareAction = InputSystem.actions.FindAction(Tags.SubMenuCopyShare);
            _cancelAction = InputSystem.actions.FindAction(Tags.PlayerCancel);
        }

        // Popup needs to update last to consume the input over what it covers
        // Preserve the dialog's own input loop in place of Modal's focus bookkeeping.
        protected override void LateUpdate()
        {
            if (_isNewMessage)
            {
                _isNewMessage = false;
                return;
            }

            if (MoveAction != null && MoveAction.WasPressedThisFrame()
                                   && EventSystem.current.currentSelectedGameObject == null
                                   && yesButton.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
            }

            if (_submitAction != null && _submitAction.WasPressedThisFrame())
            {
                if (yesButton.gameObject.activeInHierarchy)
                    yesButton.onClick?.Invoke();
                else
                    OkClose();
            }
            else if (_noAction != null && _noAction.WasPressedThisFrame() && noButton.gameObject.activeInHierarchy)
                noButton.onClick?.Invoke();
            else if (_copyShareAction != null && _copyShareAction.WasPressedThisFrame())
                CopyShare();
            else if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
                OkClose();
        }

        public void Show(string text, bool unskippable = false)
        {
            Prompt(text, null, unskippable);
        }

        public void ShowStatus(string text)
        {
            // Native sharing may finish after this dialog has been destroyed.
            if (this)
                ShowMessage(new Message { Text = text });
        }

        public void Prompt(string text, UnityAction yesAction, bool unskippable = false)
        {
            Ask(text, null, yesAction, unskippable);
        }

        public void Ask(string text, UnityAction noAction, UnityAction yesAction, bool unskippable = false)
        {
            var message = new Message()
                { Text = text, NoAction = noAction, YesAction = yesAction, Unskippable = unskippable, CanCopy = true };
            ShowMessage(message);
        }

        private void ShowMessage(Message message)
        {
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
            _messageVersion++;
            _canCopy = message.CanCopy;
            UpdateCopyShareButtons();
            _shareText = message.Text ?? string.Empty;
            messageText.text = _shareText;
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
            if (!_canCopy)
                return;

            var messageVersion = _messageVersion;
            TextSharing.CopyOrShare(_shareText, feedback =>
            {
                if (this && gameObject.activeInHierarchy && messageVersion == _messageVersion)
                    messageText.text = feedback + "\n\n" + _shareText;
            });
        }

        private void UpdateCopyShareButtons()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            copyButton.SetActive(false);
            shareButton.SetActive(_canCopy);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            copyButton.SetActive(false);
            shareButton.SetActive(false);
#else
            copyButton.SetActive(_canCopy);
            shareButton.SetActive(false);
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
