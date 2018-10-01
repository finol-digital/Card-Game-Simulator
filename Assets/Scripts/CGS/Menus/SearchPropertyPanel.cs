/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameDef;
using UnityEngine;
using UnityEngine.UI;

namespace CGS.Menus
{
    public class SearchPropertyPanel : MonoBehaviour
    {
        public Text nameLabelText;
        public InputField stringInputField;
        public Text stringPlaceHolderText;
        public InputField integerMinInputField;
        public InputField integerMaxInputField;
        public RectTransform enumContent;
        public Toggle enumToggle;
    }
}
