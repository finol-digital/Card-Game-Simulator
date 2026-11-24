/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System;
using Cgs.Performance;
using Cgs.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cgs.Steam
{
    public sealed class SteamDeckSupport : MonoBehaviour, IFloatingTextKeyboard
    {
        private const string SteamUnavailableMessage = "Steam Deck features unavailable: ";
        public event Action Dismissed;
        private bool _initialized;
        private Callback<FloatingGamepadTextInputDismissed_t> _dismissed;
        private static bool _isDeckDisplaySet;
        private bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
#if !UNITY_EDITOR
            // Optional: other distributions never load the Steam API
            if (!SteamLaunch.IsDetected())
                return;
            // Choose the Deck's display before the first frame, so the menu is not resized once it is showing
            if (SteamLaunch.IsSteamDeck(Environment.GetEnvironmentVariable))
            {
                ResolutionManager.UseSteamDeckDisplay();
                ActiveInputDevice.EnableSteamDeck();
                _isDeckDisplaySet = true;
            }

            var host = new GameObject(nameof(SteamDeckSupport));
            DontDestroyOnLoad(host);
            host.AddComponent<SteamDeckSupport>();
#endif
        }

        private void Update()
        {
            if (_started)
            {
                if (_initialized)
                    SteamAPI.RunCallbacks();
                return;
            }

            // Steam is not needed to show or use the first screen, so it starts afterwards
            if (!SteamLaunch.IsReadyToStart(StartupTimeline.Session, SceneManager.GetActiveScene().buildIndex))
                return;
            _started = true;
            StartSteam();
        }

        private void StartSteam()
        {
            try
            {
                // Non-Steam builds must remain usable without a running Steam client.
                _initialized = SteamAPI.Init();
                if (!_initialized)
                {
                    Debug.LogWarning(SteamUnavailableMessage + "SteamAPI.Init failed");
                    return;
                }

                Debug.Log($"Steam initialized at {StartupTimeline.ElapsedMs:0} ms");
                if (SteamUtils.IsRunningOnSteamHardware() != ESteamHardwareType.k_ESteamHardwareTypeSteamDeck)
                    return;
                if (!_isDeckDisplaySet)
                    ResolutionManager.UseSteamDeckDisplay();
                ActiveInputDevice.EnableSteamDeck();
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
