/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cgs.Menu;
using Cgs.UI.ScrollRects;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PlayHelpMenu : Modal
    {
        private const string MissingInputActionErrorFormat = "PlayHelpMenu: Input Action '{0}' not found.";

        private const string MissingBindingErrorFormat = "PlayHelpMenu: Input Action '{0}' has no '{1}' binding.";

        private const string GamepadGroup = "Gamepad";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";

        private const string MobileHelpText = "Select Card: Tap a card.\n" +
                                              "View Card: Press-and-hold a card to zoom in on it.\n" +
                                              "Move Card: Drag a card with 1 finger.\n" +
                                              "Pan: Drag the play area with 2 fingers.\n" +
                                              "Zoom: Pinch with 2 fingers.\n" +
                                              "Rotate: Twist with 2 fingers, when rotation is enabled.";

        private static readonly string[] PartDisplayOrder = {"up", "left", "down", "right"};

        public Text helpText;
        public Toggle rotationToggle;
        public Toggle zoomToggle;

        private static RotateZoomableScrollRect PlayArea =>
            PlayController.Instance != null ? PlayController.Instance.playArea : null;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameHelp).performed += InputCancel;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        public override void Show()
        {
            base.Show();
            helpText.text = Application.isMobilePlatform ? MobileHelpText : DesktopHelpText;
            SyncToggles();
        }

        private void Update()
        {
            SyncToggles();
        }

        // The play area can also be toggled through the keyboard shortcut or the play mat lock toggles.
        private void SyncToggles()
        {
            var playArea = PlayArea;
            if (playArea == null)
                return;

            if (rotationToggle != null && rotationToggle.isOn != playArea.RotationEnabled)
                rotationToggle.SetIsOnWithoutNotify(playArea.RotationEnabled);
            if (zoomToggle != null && zoomToggle.isOn != playArea.ZoomEnabled)
                zoomToggle.SetIsOnWithoutNotify(playArea.ZoomEnabled);
        }

        [UsedImplicitly]
        public void SetRotationEnabled(bool isRotationEnabled)
        {
            if (PlayArea != null)
                PlayArea.RotationEnabled = isRotationEnabled;
        }

        [UsedImplicitly]
        public void SetZoomEnabled(bool isZoomEnabled)
        {
            if (PlayArea != null)
                PlayArea.ZoomEnabled = isZoomEnabled;
        }

        private static string DesktopHelpText
        {
            get
            {
                var move = GetBindingDisplayString(Tags.PlayerMove);
                var page = GetBindingDisplayString(Tags.PlayerPage);
                var toggle = GetBindingDisplayString(Tags.PlayGameToggleZoomRotation);

                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Select Card: Left-Click a card, or move the selection with {move}.");
                stringBuilder.AppendLine(
                    $"Move Card: Drag a card with Left-Click, or use {page} while a card is selected.");
                stringBuilder.AppendLine($"Pan: Drag with Middle-Click or Shift+Left-Click, or use {page}.");
                stringBuilder.AppendLine($"Zoom: Ctrl+Scroll Wheel, or press {toggle} to toggle zooming with {page}.");
                stringBuilder.Append($"Rotate: Ctrl+Right-Click Drag, or press {toggle} to toggle rotating with {page}.");
                return stringBuilder.ToString();
            }
        }

        private static string GetBindingDisplayString(string inputActionId)
        {
            var inputAction = InputSystem.actions.FindAction(inputActionId);
            if (inputAction == null)
            {
                Debug.LogError(string.Format(MissingInputActionErrorFormat, inputActionId));
                return string.Empty;
            }

            var group = Gamepad.current != null ? GamepadGroup : KeyboardMouseGroup;
            var bindings = inputAction.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isPartOfComposite)
                    continue;

                // Composite bindings, i.e. Move and Page, do not have groups, so their parts are checked instead.
                string displayString;
                if (bindings[i].isComposite)
                    displayString = GetCompositeDisplayString(inputAction, i, group);
                else if (IsInGroup(bindings[i], group))
                    displayString = inputAction.GetBindingDisplayString(i);
                else
                    continue;

                if (!string.IsNullOrEmpty(displayString))
                    return Simplify(displayString);
            }

            Debug.LogError(string.Format(MissingBindingErrorFormat, inputActionId, group));
            return string.Empty;
        }

        private static string GetCompositeDisplayString(InputAction inputAction, int compositeIndex, string group)
        {
            var bindings = inputAction.bindings;
            var partNames = new List<string>();
            var partDisplayStrings = new Dictionary<string, string>();
            var partParentPaths = new Dictionary<string, string>();
            for (var i = compositeIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
            {
                if (!IsInGroup(bindings[i], group))
                    continue;

                // Only the last binding of each part is displayed, i.e. Up instead of both W and Up.
                var partName = bindings[i].name;
                if (!partDisplayStrings.ContainsKey(partName))
                    partNames.Add(partName);
                partDisplayStrings[partName] = inputAction.GetBindingDisplayString(i, out var deviceLayoutName,
                    out var controlPath);
                var separatorIndex = controlPath?.IndexOf('/') ?? -1;
                partParentPaths[partName] = controlPath != null && separatorIndex > 0
                    ? $"<{deviceLayoutName}>/{controlPath[..separatorIndex]}"
                    : null;
            }

            if (partNames.Count == 0)
                return string.Empty;

            // Parts of the same control, i.e. Right Stick Up/Right Stick Down/..., display as just that control.
            var parentPath = partParentPaths[partNames[0]];
            if (!string.IsNullOrEmpty(parentPath) && partNames.TrueForAll(partName =>
                    parentPath == partParentPaths[partName]))
                return InputControlPath.ToHumanReadableString(parentPath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);

            return string.Join("/",
                partNames.OrderBy(GetPartDisplayOrder).Select(partName => partDisplayStrings[partName]));
        }

        private static int GetPartDisplayOrder(string partName)
        {
            var partDisplayOrder = Array.IndexOf(PartDisplayOrder, partName);
            return partDisplayOrder >= 0 ? partDisplayOrder : PartDisplayOrder.Length;
        }

        private static bool IsInGroup(InputBinding inputBinding, string group)
        {
            return !string.IsNullOrEmpty(inputBinding.groups) && Array.IndexOf(
                inputBinding.groups.Split(InputBinding.Separator), group) >= 0;
        }

        private static string Simplify(string displayString)
        {
            return displayString.Replace("Keypad ", "").Replace("Numpad ", "").Replace("Num ", "");
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Hide();
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameHelp).performed -= InputCancel;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
