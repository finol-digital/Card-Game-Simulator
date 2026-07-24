/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Text;
using Cgs.Menu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PlayHelpMenu : Modal
    {
        private const string MobileHelpText = "Select Card: Tap a card.\n" +
                                              "View Card: Press-and-hold a card to zoom in on it.\n" +
                                              "Move Card: Drag a card with 1 finger.\n" +
                                              "Pan: Drag the play area with 2 fingers.\n" +
                                              "Zoom: Pinch with 2 fingers.\n" +
                                              "Rotate: Twist with 2 fingers, when rotation is enabled.";

        public Text helpText;

        public override void Show()
        {
            base.Show();
            helpText.text = Application.isMobilePlatform ? MobileHelpText : DesktopHelpText;
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
                Debug.LogError($"PlayHelpMenu: Input Action '{inputActionId}' not found.");
                return string.Empty;
            }

            var inputBinding = Gamepad.current != null
                ? InputBinding.MaskByGroup("Gamepad")
                : InputBinding.MaskByGroup("Keyboard&Mouse");
            return inputAction.GetBindingDisplayString(inputBinding)
                .Replace("| `", "").Replace("| Backspace", "")
                .Replace("Keypad ", "").Replace("Numpad ", "").Replace("Num ", "");
        }
    }
}
