/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using CardGameDef;
using CGS.Menu;

namespace CGS.Cards
{
    public class CardCreationMenu : Modal
    {
        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            // TODO:
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
