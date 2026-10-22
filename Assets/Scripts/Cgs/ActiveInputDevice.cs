/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Cgs
{
    public static class ActiveInputDevice
    {
        public static event Action Changed;
        public static bool UseGamepad { get; private set; }
        public static bool IsSteamDeck { get; private set; }
        public static bool IsOnScreenKeyboardOpen { get; set; }
        public static string BindingGroup => UseGamepad ? "Gamepad" : "Keyboard&Mouse";

        private static IDisposable _buttonListener;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _buttonListener?.Dispose();
            _buttonListener = null;
            InputSystem.onActionChange -= OnActionChange;
            InputSystem.onDeviceChange -= OnDeviceChange;
            Changed = null;
            IsSteamDeck = false;
            IsOnScreenKeyboardOpen = false;
            UseGamepad = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SetGamepad(IsSteamDeck || Gamepad.current != null);
            _buttonListener = InputSystem.onAnyButtonPress.Call(OnButtonPress);
            InputSystem.onActionChange += OnActionChange;
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public static void EnableSteamDeck()
        {
            IsSteamDeck = true;
            SetGamepad(true);
        }

        private static void OnButtonPress(InputControl control)
        {
            if (control.device is Gamepad)
                SetGamepad(true);
            else if (!IsOnScreenKeyboardOpen && (control.device is Keyboard || control.device is Mouse))
                SetGamepad(false);
        }

        private static void OnActionChange(object value, InputActionChange change)
        {
            // Mouse motion (including Deck trackpad/gyro) never changes the prompts.
            // A deliberate stick movement also restores controller prompts; ignore drift.
            if (change == InputActionChange.ActionPerformed && value is InputAction action
                && action.activeControl?.device is Gamepad && action.activeControl.EvaluateMagnitude() > 0.5f)
                SetGamepad(true);
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not Gamepad)
                return;
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected
                || change == InputDeviceChange.Enabled)
                SetGamepad(true);
            else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected
                     || change == InputDeviceChange.Disabled)
                SetGamepad(IsSteamDeck || Gamepad.current != null && Gamepad.current.enabled);
        }

        private static void SetGamepad(bool value)
        {
            if (UseGamepad == value)
                return;
            UseGamepad = value;
            Changed?.Invoke();
        }

        public static string FormatBinding(string display)
        {
            if (!UseGamepad || !IsSteamDeck)
                return display;
            // Steam Input's standard gamepad layout uses these physical Deck labels.
            return display.Replace("Button South", "A").Replace("Button East", "B")
                .Replace("Button West", "X").Replace("Button North", "Y")
                .Replace("Left Shoulder", "L1").Replace("Right Shoulder", "R1")
                .Replace("Left Trigger", "L2").Replace("Right Trigger", "R2")
                .Replace("LB", "L1").Replace("RB", "R1").Replace("LT", "L2").Replace("RT", "R2")
                .Replace("Select", "View").Replace("Start", "Menu");
        }
    }
}
