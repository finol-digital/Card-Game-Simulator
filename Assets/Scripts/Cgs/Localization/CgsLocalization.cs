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
        private static AsyncOperationHandle<LocalizationSettings> _initialization;
        private static bool _initializationCompleted;
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
            _initialization = default;
            _initializationCompleted = false;
            IsReady = false;
            Changed = null;
            _generation++;
            _requestedLocale = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            try
            {
                if (Application.isPlaying && _queue == null)
                {
                    var queueObject = new GameObject("CGS Localization");
                    UnityEngine.Object.DontDestroyOnLoad(queueObject);
                    _queue = queueObject.AddComponent<LocaleChangeQueue>();
                }

                var settings = LocalizationSettings.Instance;
                if (_settings == settings && IsReady)
                    return;
                var operation = settings.GetInitializationOperation();
                if (_settings != settings || !_initialization.Equals(operation))
                {
                    if (_settings != null)
                        _settings.OnSelectedLocaleChanged -= OnLocaleChanged;
                    _settings = settings;
                    _initialization = operation;
                    _initializationCompleted = false;
                    IsReady = false;
                    var generation = ++_generation;
                    operation.Completed += result => CompleteInitialization(result, settings, generation);
                }

                // Completed callbacks can be deferred, and Editor startup can replace
                // a pending operation. Reconcile the current handle instead of latching
                // permanently onto the first initialization attempt.
                if (operation.IsDone)
                    CompleteInitialization(operation, settings, _generation);
            }
            catch (Exception error)
            {
                Debug.LogError(InitializationError + " " + error.Message);
            }
        }

        private static void CompleteInitialization(AsyncOperationHandle<LocalizationSettings> result,
            LocalizationSettings settings, int generation)
        {
            if (generation != _generation || settings == null || settings != _settings
                || _initializationCompleted)
                return;
            _initializationCompleted = true;
            IsReady = result.Status == AsyncOperationStatus.Succeeded && Selector != null;
            if (IsReady)
                settings.OnSelectedLocaleChanged += OnLocaleChanged;
            else
                Debug.LogError($"{InitializationError} Status: {result.Status}; " +
                    $"startup selector available: {Selector != null}; {result.OperationException}");
            Changed?.Invoke();
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
