/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using JetBrains.Annotations;
using Mirror.Authenticators;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cgs.Play.Multiplayer
{
    public class HostAuthentication : Modal
    {
        public InputField roomNameInputField;
        public InputField passwordInputField;

        private UnityAction _callback;

        private void Update()
        {
            if (!IsFocused)
                return;

            if (Inputs.IsFocus)
                FocusInputField();
            else if (Inputs.IsCancel)
                Hide();
            else if (Inputs.IsSubmit)
                Submit();
        }

        public void Show(UnityAction callback)
        {
            Show();

            CgsNetManager.Instance.RoomName = CardGameManager.Current.Name;
            roomNameInputField.text = CardGameManager.Current.Name;

            _callback = callback;
        }

        [UsedImplicitly]
        public void SetRoomName(string roomName)
        {
            CgsNetManager.Instance.RoomName = roomName;
        }

        [UsedImplicitly]
        public void SetPassword(string password)
        {
            ((BasicAuthenticator) CgsNetManager.Instance.authenticator).password = password;
        }

        [UsedImplicitly]
        public void Submit()
        {
            _callback?.Invoke();

            Hide();
        }
    }
}
