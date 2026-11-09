/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace Cgs.Localization
{
    public sealed class UiMessage
    {
        private readonly Func<UiMessage> _resolve;
        private readonly Func<Action, IDisposable> _observe;

        // Re-evaluate authored arguments without replacing a dialog or its actions.
        public static UiMessage Live(Func<UiMessage> resolve, Func<Action, IDisposable> observe)
        {
            if (resolve == null)
                throw new ArgumentNullException(nameof(resolve));
            return new UiMessage(resolve, observe);
        }

        private UiMessage(Func<UiMessage> resolve, Func<Action, IDisposable> observe)
        {
            _resolve = resolve;
            _observe = observe;
        }

        internal UiMessage Resolve() => _resolve == null ? this : _resolve();
        internal IDisposable Observe(Action changed) => _observe?.Invoke(changed);

        private readonly string _key;
        private readonly string _english;
        private readonly IList<object> _arguments;
        public string Key => _resolve == null ? _key : Resolve().Key;
        public string English => _resolve == null ? _english : Resolve().English;
        public IList<object> Arguments => _resolve == null ? _arguments : Resolve().Arguments;

        public static UiMessage With(string key, string english, params (string name, object value)[] arguments)
        {
            var values = new Dictionary<string, object>();
            foreach (var (name, value) in arguments)
                values.Add(name, value);
            return new UiMessage(key, english, values);
        }

        // English is already formatted and remains readable if localization cannot initialize.
        public UiMessage(string key, string english, IDictionary<string, object> arguments = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A localized message requires a semantic key.", nameof(key));
            _key = key;
            _english = english ?? string.Empty;
            _arguments = arguments == null ? Array.Empty<object>() : new object[] { new Dictionary<string, object>(arguments) };
        }
    }
}
