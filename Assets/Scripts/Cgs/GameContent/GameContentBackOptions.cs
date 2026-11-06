/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using FinolDigital.Cgs.Json.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.GameContent
{
    public sealed class GameContentBackOptions : MonoBehaviour
    {
        private Dropdown _dropdown;
        private UnityCardGame _game;
        private List<string> _keys = new();

        public static void Bind(Dropdown dropdown, UnityCardGame game, string selected = "")
        {
            var binding = dropdown.GetComponent<GameContentBackOptions>();
            if (binding == null)
                binding = dropdown.gameObject.AddComponent<GameContentBackOptions>();
            binding._dropdown = dropdown;
            binding._game = game;
            binding._keys = new[] { "" }.Concat(game.CardBackFaceImageSprites.Keys).ToList();
            binding.Refresh(game);
            dropdown.SetValueWithoutNotify(System.Math.Max(0, binding._keys.IndexOf(selected)));
        }

        public static string SelectedKey(Dropdown dropdown)
        {
            if (dropdown == null)
                return "";
            var binding = dropdown.GetComponent<GameContentBackOptions>();
            return binding != null && dropdown.value < binding._keys.Count ? binding._keys[dropdown.value] : "";
        }

        private void OnEnable() => GameContentPresentation.Changed += Refresh;
        private void OnDisable() => GameContentPresentation.Changed -= Refresh;

        private void Refresh(UnityCardGame changed)
        {
            if (_dropdown == null || _game == null || (changed != null && changed != _game))
                return;
            var selected = _dropdown.value;
            var display = GameContentPresentation.Context(_game);
            _dropdown.options = _keys.Select(key => new Dropdown.OptionData(key.Length == 0 ? "" : display.CardBackName(key))).ToList();
            _dropdown.SetValueWithoutNotify(selected);
            _dropdown.RefreshShownValue();
        }
    }
}
