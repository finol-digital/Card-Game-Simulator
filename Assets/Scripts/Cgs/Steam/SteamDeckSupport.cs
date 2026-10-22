/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System;
using Cgs.UI;
using Steamworks;
using UnityEngine;

namespace Cgs.Steam
{
    public sealed class SteamDeckSupport : MonoBehaviour, IFloatingTextKeyboard
    {
        private const string SteamUnavailableMessage = "Steam Deck features unavailable: ";
        public event Action Dismissed;
        private bool _initialized;
        private Callback<FloatingGamepadTextInputDismissed_t> _dismissed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
#if !UNITY_EDITOR
            var host = new GameObject(nameof(SteamDeckSupport));
            DontDestroyOnLoad(host);
            host.AddComponent<SteamDeckSupport>();
#endif
        }

        private void Awake()
        {
            try
            {
                // Optional: non-Steam builds must remain usable without a running Steam client.
                _initialized = SteamAPI.Init();
                if (!_initialized)
                    return;
                if (!SteamUtils.IsSteamRunningOnSteamDeck())
                    return;
                ActiveInputDevice.EnableSteamDeck();
                ResolutionManager.UseSteamDeckDisplay();
                _dismissed = Callback<FloatingGamepadTextInputDismissed_t>.Create(_ => Dismissed?.Invoke());
                gameObject.AddComponent<FloatingTextKeyboard>().Configure(this);
            }
            catch (Exception exception) when (exception is DllNotFoundException
                                              || exception is EntryPointNotFoundException
                                              || exception is BadImageFormatException)
            {
                Debug.LogWarning(SteamUnavailableMessage + exception.Message);
            }
        }

        private void Update()
        {
            if (_initialized)
                SteamAPI.RunCallbacks();
        }

        public bool Show(bool multiline, RectInt fieldBounds)
        {
            return _initialized && SteamUtils.ShowFloatingGamepadTextInput(multiline
                    ? EFloatingGamepadTextInputMode.k_EFloatingGamepadTextInputModeModeMultipleLines
                    : EFloatingGamepadTextInputMode.k_EFloatingGamepadTextInputModeModeSingleLine,
                fieldBounds.x, fieldBounds.y, fieldBounds.width, fieldBounds.height);
        }

        public void Hide()
        {
            if (_initialized)
                SteamUtils.DismissFloatingGamepadTextInput();
        }

        private void OnDestroy()
        {
            var keyboard = GetComponent<FloatingTextKeyboard>();
            if (keyboard != null)
                keyboard.Configure(null);
            _dismissed?.Dispose();
            if (_initialized)
                SteamAPI.Shutdown();
            _initialized = false;
        }
    }
}
#endif
