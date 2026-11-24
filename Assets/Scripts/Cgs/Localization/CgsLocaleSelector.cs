/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[assembly: InternalsVisibleTo("PlayMode")]

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
        public const string EnglishCode = "en";
        private const string CommandLinePrefix = "-language=";

        // Tests replace the process arguments to check the -language= override
        internal static Func<string[]> CommandLineArgs = Environment.GetCommandLineArgs;

        [SerializeField] List<SupportedLanguage> languages = new();

        public IReadOnlyList<SupportedLanguage> Languages => languages;

        public CgsLocaleSelector(IEnumerable<SupportedLanguage> languages)
        {
            this.languages = languages.ToList();
        }

        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            var code = StartupCode();
            // The package calls selectors after loading locales. Avoid GetLocale's implicit fallback.
            return availableLocales.Locales.FirstOrDefault(locale => locale.Identifier.Code == code)
                   ?? availableLocales.Locales.FirstOrDefault(locale => locale.Identifier.Code == EnglishCode);
        }

        /// <summary>The startup language's code, from the same choices as at startup, without loading anything.</summary>
        public string StartupCode()
        {
            var commandLine = CommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(CommandLinePrefix, StringComparison.OrdinalIgnoreCase));
            var requested = commandLine?.Substring(CommandLinePrefix.Length);
            return ResolveCode(requested, PlayerPrefs.GetString(PreferenceKey, string.Empty), DeviceLanguage());
        }

        public string ResolveCode(string commandLine, string saved, string device)
        {
            return ExactCode(commandLine) ?? ExactCode(saved) ?? MatchDevice(device) ?? EnglishCode;
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

            // A singleton begins an extension or private-use sequence, not a script.
            var subtags = code.Replace('_', '-').Split('-');
            var extension = Array.FindIndex(subtags, 1, subtag => subtag.Length == 1);
            var candidate = string.Join("-", extension < 0 ? subtags : subtags.Take(extension));
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
