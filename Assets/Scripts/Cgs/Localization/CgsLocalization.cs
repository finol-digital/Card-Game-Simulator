/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Cgs.Localization
{
    public static class CgsLocalization
    {
        public const string TableName = "CgsUi";
        private const string InitializationError = "CGS localization initialization failed; retaining English UI.";
        private static LocalizationSettings _settings;
        private static bool _started;
        private static int _generation;
        private static Locale _requestedLocale;
        private static LocaleChangeQueue _queue;

        public static bool IsReady { get; private set; }
        public static event Action Changed;
        public static IReadOnlyList<SupportedLanguage> Languages => Selector?.Languages ?? Array.Empty<SupportedLanguage>();
        private static CgsLocaleSelector Selector => _settings == null ? null :
            _settings.GetStartupLocaleSelectors().OfType<CgsLocaleSelector>().FirstOrDefault();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (_settings != null)
                _settings.OnSelectedLocaleChanged -= OnLocaleChanged;
            _settings = null;
            _started = false;
            IsReady = false;
            Changed = null;
            _generation++;
            _requestedLocale = null;
        }

        public static void Initialize()
        {
            if (_started)
                return;
            _started = true;
            try
            {
                _settings = LocalizationSettings.Instance;
                if (Application.isPlaying && _queue == null)
                {
                    var queueObject = new GameObject("CGS Localization");
                    UnityEngine.Object.DontDestroyOnLoad(queueObject);
                    _queue = queueObject.AddComponent<LocaleChangeQueue>();
                }
                var settings = _settings;
                var generation = _generation;
                var operation = LocalizationSettings.InitializationOperation;
                operation.Completed += result =>
                {
                    if (generation != _generation || settings == null || settings != _settings)
                        return;
                    IsReady = result.Status == AsyncOperationStatus.Succeeded && Selector != null;
                    if (IsReady)
                        settings.OnSelectedLocaleChanged += OnLocaleChanged;
                    else
                        Debug.LogError(InitializationError);
                    Changed?.Invoke();
                };
            }
            catch (Exception error)
            {
                Debug.LogError(InitializationError + " " + error.Message);
            }
        }

        private static void OnLocaleChanged(Locale locale) => Changed?.Invoke();

        // Native dialogs accept a one-time string rather than a live binding. Only
        // read an already loaded table; never block the browser/player to open one.
        public static string Text(UiMessage message)
        {
            if (!IsReady)
                return message.English;
            var operation = LocalizationSettings.StringDatabase.GetTableAsync(TableName);
            if (!operation.IsDone || operation.Status != AsyncOperationStatus.Succeeded)
                return message.English;
            var entry = operation.Result.GetEntry(message.Key);
            return entry == null || string.IsNullOrWhiteSpace(entry.Value)
                ? message.English : entry.GetLocalizedString(message.Arguments);
        }

        public static bool SelectLanguage(string code)
        {
            if (!IsReady || Selector.ExactCode(code) == null)
                return false;
            var locale = LocalizationSettings.AvailableLocales.Locales.FirstOrDefault(item =>
                string.Equals(item.Identifier.Code, code, StringComparison.OrdinalIgnoreCase));
            if (locale == null)
                return false;
            _requestedLocale = locale;
            PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, locale.Identifier.Code);
            PlayerPrefs.Save();
            return true;
        }

        internal static void ApplyPendingLanguage()
        {
            if (_requestedLocale == null || !IsReady
                || !LocalizationSettings.InitializationOperation.IsDone)
                return;
            var locale = _requestedLocale;
            _requestedLocale = null;
            LocalizationSettings.SelectedLocale = locale;
        }
    }
}
