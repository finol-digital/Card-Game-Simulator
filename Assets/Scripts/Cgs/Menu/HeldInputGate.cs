/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Cgs.Menu
{
    /// <summary>
    /// Ignores keys, buttons, and touches that were already held when a screen became interactive, until they are
    /// released. A press that starts on or after that frame is accepted immediately, so there is no fixed delay.
    /// </summary>
    public class HeldInputGate
    {
        private readonly HashSet<ButtonControl> _held = new();
        private int _pointerReleaseFrame = -1;

        /// <summary>Records every control that is held now and was not pressed during this frame.</summary>
        public void Arm()
        {
            _held.Clear();
            _pointerReleaseFrame = -1;
            foreach (var device in InputSystem.devices)
            {
                foreach (var control in device.allControls)
                    if (control is ButtonControl { isPressed: true, wasPressedThisFrame: false } button)
                        _held.Add(button);
            }
        }

        /// <summary>
        /// False while <paramref name="control"/>, a control within it, or the control containing it is still held
        /// from before <see cref="Arm"/>.
        /// </summary>
        public bool Allows(InputControl control)
        {
            ForgetReleased();
            return control == null || !_held.Any(held => IsWithin(held, control) || IsWithin(control, held));
        }

        /// <summary>
        /// False while a mouse button, pen, or touch held from before <see cref="Arm"/> is still down, and during the
        /// frame it is released, because the UI turns that release into a click.
        /// </summary>
        public bool AllowsPointerAction
        {
            get
            {
                ForgetReleased();
                return Time.frameCount != _pointerReleaseFrame && !_held.Any(held => held.device is Pointer);
            }
        }

        /// <summary>
        /// Call every frame, so a release is noticed in the frame it happens and a later, fresh click is not mistaken
        /// for it.
        /// </summary>
        public void Refresh() => ForgetReleased();

        private void ForgetReleased()
        {
            if (_held.RemoveWhere(held => held.device is Pointer && IsReleased(held)) > 0)
                _pointerReleaseFrame = Time.frameCount;
            _held.RemoveWhere(IsReleased);
        }

        private static bool IsReleased(ButtonControl control) => !control.device.added || !control.isPressed;

        private static bool IsWithin(InputControl control, InputControl container)
        {
            for (var current = control; current != null; current = current.parent)
                if (current == container)
                    return true;
            return false;
        }
    }
}
