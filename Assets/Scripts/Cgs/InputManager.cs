/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cgs
{
    public class InputManager : MonoBehaviour
    {
        public const KeyCode BluetoothReturn = (KeyCode)10;

        public const string PlayerCancel = "Player/Cancel";
        public const string PlayerFilter = "Player/Filter";
        public const string PlayerFocusBack = "Player/FocusBack";
        public const string PlayerFocusNext = "Player/FocusNext";
        public const string PlayerLoad = "Player/Load";
        public const string PlayerNew = "Player/New";
        public const string PlayerOption = "Player/Option";
        public const string PlayerSave = "Player/Save";
        public const string PlayerSort = "Player/Sort";
        private const string PlayerSubmit = "Player/Submit";

        private const string PlayerMove = "Player/Move";
        private const string PlayerPage = "Player/Page";

        private const float Tolerance = 0.1f;

        public static bool IsCancel => InputSystem.actions.FindAction(PlayerCancel).WasPressedThisFrame();

        public static bool IsFilter => InputSystem.actions.FindAction(PlayerFilter).WasPressedThisFrame();

        public static bool IsFocus => InputSystem.actions.FindAction(PlayerFocusBack).WasPressedThisFrame()
                                      || InputSystem.actions.FindAction(PlayerFocusNext).WasPressedThisFrame();

        public static bool IsFocusBack => InputSystem.actions.FindAction(PlayerFocusBack).WasPressedThisFrame();

        public static bool IsFocusNext => InputSystem.actions.FindAction(PlayerFocusNext).WasPressedThisFrame();

        public static bool IsHorizontal =>
            Math.Abs(InputSystem.actions.FindAction(PlayerMove).ReadValue<Vector2>().x) > Tolerance;

        public static bool IsLeft => InputSystem.actions.FindAction(PlayerMove).ReadValue<Vector2>().x < 0;
        public static bool IsRight => InputSystem.actions.FindAction(PlayerMove).ReadValue<Vector2>().x > 0;

        public static bool IsLoad => InputSystem.actions.FindAction(PlayerLoad).WasPressedThisFrame();

        public static bool IsNew => InputSystem.actions.FindAction(PlayerNew).WasPressedThisFrame();

        public static bool IsOption => InputSystem.actions.FindAction(PlayerOption).WasPressedThisFrame();

        public static float FPageVertical => InputSystem.actions.FindAction(PlayerPage).ReadValue<Vector2>().y;
        public static bool IsPageVertical => Math.Abs(FPageVertical) > Tolerance;
        public static bool IsPageDown => FPageVertical < 0;
        public static bool IsPageUp => FPageVertical > 0;

        public static float FPageHorizontal => InputSystem.actions.FindAction(PlayerPage).ReadValue<Vector2>().x;
        public static bool IsPageHorizontal => Math.Abs(FPageHorizontal) > Tolerance;
        public static bool IsPageLeft => FPageHorizontal < 0;
        public static bool IsPageRight => FPageHorizontal > 0;

        public static bool IsSave => InputSystem.actions.FindAction(PlayerSave).WasPressedThisFrame();

        public static bool IsSort => InputSystem.actions.FindAction(PlayerSort).WasPressedThisFrame();

        public static bool IsSubmit => InputSystem.actions.FindAction(PlayerSubmit).WasPressedThisFrame();

        public static bool IsVertical =>
            Math.Abs(InputSystem.actions.FindAction(PlayerMove).ReadValue<Vector2>().y) > Tolerance;

        public static bool IsDown => InputSystem.actions.FindAction(PlayerMove).ReadValue<Vector2>().y < 0;
        public static bool IsUp => InputSystem.actions.FindAction(PlayerMove).ReadValue<Vector2>().y > 0;


        // WasDirection set in CardGameManager.LateUpdate()
        public static bool WasFocusBack { get; set; }
        public static bool WasFocusNext { get; set; }
        public static bool WasDown { get; set; }
        public static bool WasUp { get; set; }
        public static bool WasLeft { get; set; }
        public static bool WasRight { get; set; }
        public static bool WasPageVertical { get; set; }
        public static bool WasPageUp { get; set; }
        public static bool WasPageDown { get; set; }
        public static bool WasPageHorizontal { get; set; }
        public static bool WasPageLeft { get; set; }
        public static bool WasPageRight { get; set; }


        public static char FilterFocusInput(char charToValidate)
        {
            if (charToValidate == '`')
                charToValidate = '\0';
            return charToValidate;
        }
    }
}
