/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.Localization;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using UnityEngine;

namespace Cgs.GameContent
{
    // Retains a canonical target, including while a pooled view is disabled.
    public sealed class GameContentText : MonoBehaviour, ILiteralTextBinding
    {
        private Component _target;
        private UnityCardGame _game;
        private Func<LocalizationContext, string> _resolve;
        private Func<LocalizationContext, UiMessage> _message;
        private bool _applying;

        public static void Set(Component target, UnityCardGame game, Func<LocalizationContext, string> resolve)
        {
            if (target == null)
                return;
            var binding = target.GetComponent<GameContentText>();
            if (binding == null)
                binding = target.gameObject.AddComponent<GameContentText>();
            if (binding == null)
                return;
            binding._target = target;
            binding._game = game;
            binding._resolve = resolve;
            binding._message = null;
            binding.Refresh(null);
        }

        public static void SetMessage(Component target, UnityCardGame game, Func<LocalizationContext, UiMessage> message)
        {
            if (target == null)
                return;
            var binding = target.GetComponent<GameContentText>();
            if (binding == null)
                binding = target.gameObject.AddComponent<GameContentText>();
            if (binding == null)
                return;
            binding._target = target;
            binding._game = game;
            binding._resolve = null;
            binding._message = message;
            binding.Refresh(null);
        }

        private void OnEnable()
        {
            GameContentPresentation.Changed += Refresh;
            Refresh(null);
        }

        private void OnDisable() => GameContentPresentation.Changed -= Refresh;

        public void ReleaseForAssignment()
        {
            if (_applying)
                return;
            _resolve = null;
            _message = null;
            _game = null;
        }

        private void Refresh(UnityCardGame changedGame)
        {
            if (_target == null || _game == null || (_resolve == null && _message == null) ||
                (changedGame != null && changedGame != _game))
                return;
            _applying = true;
            try
            {
                var context = GameContentPresentation.Context(_game);
                if (_message != null)
                    LocalizedUiText.Set(_target, _message(context));
                else
                    LocalizedUiText.SetLiteral(_target, _resolve(context));
            }
            finally
            {
                _applying = false;
            }
            if (_target.transform is RectTransform rect)
                UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rect);
        }
    }
}
