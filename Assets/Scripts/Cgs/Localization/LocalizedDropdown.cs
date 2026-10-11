/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Localization
{
    [RequireComponent(typeof(Dropdown))]
    public sealed class LocalizedDropdown : MonoBehaviour
    {
        [Serializable]
        public struct Option
        {
            public int index;
            public string key;
            public string english;
        }

        [SerializeField] Option[] options = Array.Empty<Option>();
        private readonly List<LocalizedMessageBinding> _bindings = new();

        private void OnEnable()
        {
            var dropdown = GetComponent<Dropdown>();
            foreach (var option in options)
            {
                var captured = option;
                _bindings.Add(new LocalizedMessageBinding(new UiMessage(option.key, option.english), value =>
                {
                    if (!dropdown || captured.index < 0 || captured.index >= dropdown.options.Count)
                        return;
                    dropdown.options[captured.index].text = value;
                    dropdown.RefreshShownValue();
                }));
            }
        }

        private void OnDisable()
        {
            foreach (var binding in _bindings)
                binding.Dispose();
            _bindings.Clear();
        }
    }
}
