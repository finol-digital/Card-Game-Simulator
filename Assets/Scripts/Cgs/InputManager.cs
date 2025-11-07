/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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

        private static InputManager _instance;

        public static bool IsCancel => _instance?._cancelAction?.WasPressedThisFrame() ?? false;
        private InputAction _cancelAction;

        public static bool IsFilter => _instance?._filterAction?.WasPressedThisFrame() ?? false;
        private InputAction _filterAction;

        public static bool IsFocus => IsFocusBack || IsFocusNext;

        public static bool IsFocusBack => _instance?._focusBackAction?.WasPressedThisFrame() ?? false;
        private InputAction _focusBackAction;

        public static bool IsFocusNext => _instance?._focusNextAction?.WasPressedThisFrame() ?? false;
        private InputAction _focusNextAction;

        public static bool IsLoad => _instance?._loadAction?.WasPressedThisFrame() ?? false;
        private InputAction _loadAction;

        public static bool IsNew => _instance?._newAction?.WasPressedThisFrame() ?? false;
        private InputAction _newAction;

        public static bool IsOption => _instance?._optionAction?.WasPressedThisFrame() ?? false;
        private InputAction _optionAction;

        public static bool IsSave => _instance?._saveAction?.WasPressedThisFrame() ?? false;
        private InputAction _saveAction;

        public static bool IsSort => _instance?._sortAction?.WasPressedThisFrame() ?? false;
        private InputAction _sortAction;

        public static bool IsSubmit => _instance?._submitAction?.WasPressedThisFrame() ?? false;
        private InputAction _submitAction;

        private InputAction _moveAction;

        public static bool IsHorizontal => IsLeft || IsRight;
        private static float FMoveHorizontal => _instance?._moveAction?.ReadValue<Vector2>().x ?? 0;

        public static bool IsLeft => FMoveHorizontal < 0;
        public static bool IsRight => FMoveHorizontal > 0;

        public static bool IsVertical => IsDown || IsUp;
        private static float FMoveVertical => _instance?._moveAction?.ReadValue<Vector2>().y ?? 0;

        public static bool IsDown => FMoveVertical < 0;
        public static bool IsUp => FMoveVertical > 0;

        private InputAction _pageAction;

        public static bool IsPageHorizontal => IsPageLeft || IsPageRight;
        public static float FPageHorizontal => _instance?._pageAction?.ReadValue<Vector2>().x ?? 0;

        public static bool IsPageLeft => FPageHorizontal < 0;
        public static bool IsPageRight => FPageHorizontal > 0;

        public static bool IsPageVertical => IsPageDown || IsPageUp;
        public static float FPageVertical => _instance?._pageAction?.ReadValue<Vector2>().y ?? 0;

        public static bool IsPageDown => FPageVertical < 0;
        public static bool IsPageUp => FPageVertical > 0;

        public static bool WasFocusBack { get; private set; }
        public static bool WasFocusNext { get; private set; }
        public static bool WasDown { get; private set; }
        public static bool WasUp { get; private set; }
        public static bool WasLeft { get; private set; }
        public static bool WasRight { get; private set; }
        public static bool WasPageVertical { get; private set; }
        public static bool WasPageUp { get; private set; }
        public static bool WasPageDown { get; private set; }
        public static bool WasPageHorizontal { get; private set; }
        public static bool WasPageLeft { get; private set; }
        public static bool WasPageRight { get; private set; }

        public static char FilterFocusInput(char charToValidate)
        {
            if (charToValidate == '`')
                charToValidate = '\0';
            return charToValidate;
        }

        private void Start()
        {
            _instance = this;
            _cancelAction = InputSystem.actions.FindAction(PlayerCancel);
            _filterAction = InputSystem.actions.FindAction(PlayerFilter);
            _focusBackAction = InputSystem.actions.FindAction(PlayerFocusBack);
            _focusNextAction = InputSystem.actions.FindAction(PlayerFocusNext);
            _loadAction = InputSystem.actions.FindAction(PlayerLoad);
            _newAction = InputSystem.actions.FindAction(PlayerNew);
            _optionAction = InputSystem.actions.FindAction(PlayerOption);
            _saveAction = InputSystem.actions.FindAction(PlayerSave);
            _sortAction = InputSystem.actions.FindAction(PlayerSort);
            _submitAction = InputSystem.actions.FindAction(PlayerSubmit);
            _moveAction = InputSystem.actions.FindAction(PlayerMove);
            _pageAction = InputSystem.actions.FindAction(PlayerPage);
        }

        private void LateUpdate()
        {
            WasFocusBack = IsFocusBack;
            WasFocusNext = IsFocusNext;
            WasDown = IsDown;
            WasUp = IsUp;
            WasLeft = IsLeft;
            WasRight = IsRight;
            WasPageVertical = IsPageVertical;
            WasPageDown = IsPageDown;
            WasPageUp = IsPageUp;
            WasPageHorizontal = IsPageHorizontal;
            WasPageLeft = IsPageLeft;
            WasPageRight = IsPageRight;
        }
    }
}
