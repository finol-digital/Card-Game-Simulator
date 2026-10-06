/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace Cgs.Localization
{
    public sealed class UiMessage
    {
        public string Key { get; }
        public string English { get; }
        public IList<object> Arguments { get; }

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
            Key = key;
            English = english ?? string.Empty;
            Arguments = arguments == null ? Array.Empty<object>() : new object[] { new Dictionary<string, object>(arguments) };
        }
    }
}
