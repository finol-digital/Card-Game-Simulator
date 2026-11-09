/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Cgs.Localization
{
    public sealed class LocalizedMessageBinding : IDisposable
    {
        private readonly UiMessage _message;
        private readonly Action<string> _apply;
        private LocalizedString _localized;
        private UiMessage _current;
        private readonly IDisposable _observation;
        private bool _disposed;

        public LocalizedMessageBinding(UiMessage message, Action<string> apply)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _current = message.Resolve();
            _apply(_current.English);
            _observation = message.Observe(OnReady);
            CgsLocalization.Changed += OnReady;
            CgsLocalization.Initialize();
            OnReady();
        }

        private void OnReady()
        {
            if (_disposed)
                return;
            _current = _message.Resolve();
            if (_localized != null)
                _localized.StringChanged -= Apply;
            _localized = null;
            if (!CgsLocalization.IsReady)
            {
                _apply(_current.English);
                return;
            }
            _localized = new LocalizedString(CgsLocalization.TableName, _current.Key)
            {
                Arguments = _current.Arguments,
                FallbackState = FallbackBehavior.UseFallback
            };
            _localized.StringChanged += Apply;
        }

        private void Apply(string text)
        {
            if (_disposed)
                return;
            // Unity substitutes a diagnostic string when an entry is absent. Inspect
            // the loaded entry rather than matching that diagnostic's English wording.
            var operation = _localized.CurrentLoadingOperationHandle;
            var entry = operation.IsValid() && operation.IsDone ? operation.Result.Entry : null;
            _apply(entry == null || string.IsNullOrWhiteSpace(entry.Value) || string.IsNullOrWhiteSpace(text)
                ? _current.English : text);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _observation?.Dispose();
            CgsLocalization.Changed -= OnReady;
            if (_localized != null)
                _localized.StringChanged -= Apply;
            _localized = null;
        }
    }
}
