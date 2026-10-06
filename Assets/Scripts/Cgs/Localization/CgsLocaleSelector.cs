/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Cgs.Localization
{
    [Serializable]
    public sealed class SupportedLanguage
    {
        [SerializeField] string code;
        [SerializeField] string nativeName;
        [SerializeField] string[] aliases;

        public string Code => code;
        public string NativeName => nativeName;
        public IReadOnlyList<string> Aliases => aliases;

        public SupportedLanguage(string code, string nativeName, string[] aliases)
        {
            this.code = code;
            this.nativeName = nativeName;
            this.aliases = aliases ?? Array.Empty<string>();
        }
    }

    [Serializable]
    public sealed class CgsLocaleSelector : IStartupLocaleSelector
    {
        public const string PreferenceKey = "LanguageCode";
        private const string CommandLinePrefix = "-language=";

        [SerializeField] List<SupportedLanguage> languages = new();

        public IReadOnlyList<SupportedLanguage> Languages => languages;

        public CgsLocaleSelector(IEnumerable<SupportedLanguage> languages)
        {
            this.languages = languages.ToList();
        }

        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            var commandLine = Environment.GetCommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(CommandLinePrefix, StringComparison.OrdinalIgnoreCase));
            var requested = commandLine?.Substring(CommandLinePrefix.Length);
            var code = ResolveCode(requested, PlayerPrefs.GetString(PreferenceKey, string.Empty),
                DeviceLanguage());
            // The package calls selectors after loading locales. Avoid GetLocale's implicit fallback.
            return availableLocales.Locales.FirstOrDefault(locale => locale.Identifier.Code == code)
                   ?? availableLocales.Locales.FirstOrDefault(locale => locale.Identifier.Code == "en");
        }

        public string ResolveCode(string commandLine, string saved, string device)
        {
            return ExactCode(commandLine) ?? ExactCode(saved) ?? MatchDevice(device) ?? "en";
        }

        private static string DeviceLanguage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return CgsBrowserLanguage();
#elif UNITY_IOS && !UNITY_EDITOR
            return getPreferredLanguage();
#elif UNITY_ANDROID && !UNITY_EDITOR
            using var localeClass = new AndroidJavaClass("java.util.Locale");
            using var locale = localeClass.CallStatic<AndroidJavaObject>("getDefault");
            return locale.Call<string>("toLanguageTag");
#else
            var culture = CultureInfo.CurrentUICulture.Name;
            return string.IsNullOrWhiteSpace(culture) ? new LocaleIdentifier(Application.systemLanguage).Code : culture;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string CgsBrowserLanguage();
#elif UNITY_IOS && !UNITY_EDITOR
        // Exported by the installed Unity Localization iOS plug-in.
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string getPreferredLanguage();
#endif

        public string ExactCode(string code)
        {
            return languages.FirstOrDefault(language =>
                string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))?.Code;
        }

        private string MatchDevice(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var candidate = code.Replace('_', '-');
            while (!string.IsNullOrEmpty(candidate))
            {
                var exact = ExactCode(candidate);
                if (exact != null)
                    return exact;
                var alias = languages.FirstOrDefault(language => language.Aliases.Any(value =>
                    string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase)));
                if (alias != null)
                    return alias.Code;
                var lastDash = candidate.LastIndexOf('-');
                if (lastDash < 0)
                    return null;
                // Never discard an explicit script in order to match a different script.
                if (candidate.Length - lastDash - 1 == 4 && candidate.Substring(lastDash + 1).All(char.IsLetter))
                    return null;
                candidate = candidate.Substring(0, lastDash);
            }
            return null;
        }
    }
}
