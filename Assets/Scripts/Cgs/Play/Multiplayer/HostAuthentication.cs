/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;

namespace Cgs.Play.Multiplayer
{
    public class HostAuthentication : Modal
    {
        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
    }
}
