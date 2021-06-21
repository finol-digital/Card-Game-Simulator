/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Play.Drawer
{
    public class TabController : MonoBehaviour
    {
        public void CreateTab()
        {
            CardGameManager.Instance.Messenger.Show("The Multiple Hands feature is coming soon!");
        }
    }
}
