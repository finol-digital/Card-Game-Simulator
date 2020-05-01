/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs
{
    public static class Inputs
    {
        public const float Tolerance = 0.1f;

        public const KeyCode BluetoothReturn = (KeyCode)10;
        public const string Cancel = "Cancel";
        public const string Filter = "Filter";
        public const string FocusBack = "FocusBack";
        public const string FocusNext = "FocusNext";
        public const string Horizontal = "Horizontal";
        public const string Load = "Load";
        public const string New = "New";
        public const string Option = "Option";
        public const string PageHorizontal = "PageHorizontal";
        public const string PageVertical = "PageVertical";
        public const string Save = "Save";
        public const string Sort = "Sort";
        public const string Submit = "Submit";
        public const string Vertical = "Vertical";

        public static char FilterFocusNameInput(char charToValidate)
        {
            if (charToValidate == '`')
                charToValidate = '\0';
            return charToValidate;
        }
    }
}
