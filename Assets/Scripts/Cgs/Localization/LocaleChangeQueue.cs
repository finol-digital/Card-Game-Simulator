/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Localization
{
    // Unity unloads its previous tables on each locale assignment. Coalesce rapid
    // selections until that assignment's asynchronous preload and events settle.
    public sealed class LocaleChangeQueue : MonoBehaviour
    {
#pragma warning disable S2325 // Unity requires an instance method for lifecycle callbacks.
        private void LateUpdate()
        {
            if (!CgsLocalization.IsReady)
                CgsLocalization.Initialize();
            CgsLocalization.ApplyPendingLanguage();
        }
#pragma warning restore S2325
    }
}
