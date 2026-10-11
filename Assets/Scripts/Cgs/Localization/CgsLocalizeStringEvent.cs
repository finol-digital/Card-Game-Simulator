/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.Localization.Components;

namespace Cgs.Localization
{
    // Retains the package's public, persistent text event and Editor preview support.
    public sealed class CgsLocalizeStringEvent : LocalizeStringEvent
    {
        [SerializeField] string english;

        protected override void UpdateString(string value)
        {
            if (Application.isPlaying)
                LocalizedUiText.FitButtonLabel(GetComponent<UnityEngine.UI.Text>());
            var operation = StringReference.CurrentLoadingOperationHandle;
            var entry = operation.IsValid() && operation.IsDone ? operation.Result.Entry : null;
            base.UpdateString(entry == null || string.IsNullOrWhiteSpace(entry.Value) || string.IsNullOrWhiteSpace(value)
                ? english : value);
        }
    }
}
