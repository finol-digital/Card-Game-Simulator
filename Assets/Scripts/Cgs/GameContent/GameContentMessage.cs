/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.Localization;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;

namespace Cgs.GameContent
{
    public static class GameContentMessage
    {
        public static UiMessage Create(UnityCardGame game, Func<LocalizationContext, UiMessage> resolve)
        {
            return UiMessage.Live(() => resolve(GameContentPresentation.Context(game)),
                changed => new Observation(game, changed));
        }

        private sealed class Observation : IDisposable
        {
            private readonly UnityCardGame _game;
            private readonly Action _changed;

            public Observation(UnityCardGame game, Action changed)
            {
                _game = game;
                _changed = changed;
                GameContentPresentation.Changed += OnChanged;
            }

            private void OnChanged(UnityCardGame game)
            {
                if (game == null || game == _game)
                    _changed();
            }

            public void Dispose() => GameContentPresentation.Changed -= OnChanged;
        }
    }
}
