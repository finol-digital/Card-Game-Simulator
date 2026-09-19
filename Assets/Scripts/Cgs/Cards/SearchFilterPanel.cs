/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class SearchFilterPanel : MonoBehaviour
    {
        [SerializeField] Text nameLabelText;
        [SerializeField] InputField stringInputField;
        [SerializeField] Text stringPlaceHolderText;
        [SerializeField] InputField integerMinInputField;
        [SerializeField] InputField integerMaxInputField;
        [SerializeField] RectTransform toggleGroupContainer;
        [SerializeField] Toggle toggle;

        public Text NameLabelText => nameLabelText;

        public InputField StringInputField => stringInputField;

        public Text StringPlaceHolderText => stringPlaceHolderText;

        public InputField IntegerMinInputField => integerMinInputField;

        public InputField IntegerMaxInputField => integerMaxInputField;

        public RectTransform ToggleGroupContainer => toggleGroupContainer;

        public Toggle Toggle => toggle;
    }
}
