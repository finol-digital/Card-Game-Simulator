/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Cgs.UI
{
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public class UwpScrollSensitivityFix : MonoBehaviour
    {
        // Windows reports raw mouse wheel deltas of +/-120 per tick.
        // The Input System normalizes this to +/-1 on StandaloneWindows,
        // but not on UWP/WSA, so rescale to match the other platforms.
        private const float WindowsScrollDeltaPerTick = 120f;

#if UNITY_WSA && !UNITY_EDITOR
        private void Awake()
        {
            var inputModule = GetComponent<InputSystemUIInputModule>();
            inputModule.scrollDeltaPerTick /= WindowsScrollDeltaPerTick;
        }
#endif
    }
}
