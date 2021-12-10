/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using UnityEngine;
using UnityEngine.Events;

namespace Cgs.Cards
{
    public class SetImportMenu : Modal
    {
        private UnityAction _onCreationCallback;

        public void Show(UnityAction onCreationCallback)
        {
            Show();
            _onCreationCallback = onCreationCallback;
        }

    }
}
