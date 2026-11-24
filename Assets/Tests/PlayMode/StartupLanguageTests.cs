/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Cgs;
using Cgs.Localization;
using Cgs.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>
    /// The project's localization settings, started fresh as at launch. Initialization waits for a gate that the
    /// test opens, or fails, to control when the startup language arrives.
    /// </summary>
    public class StartupLanguageTestSettings : LocalizationSettings
    {
        private ResourceManager _resourceManager;
        private AsyncOperationHandle<LocalizationSettings> _gate;
        private AsyncOperationHandle<LocalizationSettings> _startup;

        public void HoldUntil(ResourceManager resourceManager, AsyncOperationHandle<LocalizationSettings> gate)
        {
            _resourceManager = resourceManager;
            _gate = gate;
        }

        public override AsyncOperationHandle<LocalizationSettings> GetInitializationOperation()
        {
            if (!_gate.IsValid())
                return base.GetInitializationOperation();
            // One handle throughout, so text and fonts that wait on it receive the language once it is ready
            if (!_startup.IsValid())
                _startup = _resourceManager.CreateChainOperation(_gate, gate =>
                    gate.Status == AsyncOperationStatus.Succeeded
                        ? base.GetInitializationOperation()
                        : _resourceManager.CreateCompletedOperation<LocalizationSettings>(this,
                            "Expected startup localization failure."));
            return _startup;
        }

        public void ReleaseHold()
        {
            if (_startup.IsValid())
                _resourceManager.Release(_startup);
            _startup = default;
            _gate = default;
        }
    }

    /// <summary>The first Main Menu after launch shows the startup language, or readable English when it is late.</summary>
    public class StartupLanguageTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const string UiTable = "CgsUi";
        private const string FontTable = "CgsFonts";
        private const float GenerousWaitSeconds = 60;
        private const int TimeoutFrames = 3000;

        private sealed class PendingInitialization : AsyncOperationBase<LocalizationSettings>
        {
            protected override void Execute() { }

            public void Finish(LocalizationSettings settings, bool success) =>
                Complete(settings, success, success ? null : "Expected startup localization failure.");
        }

        private LocalizationSettings _original;
        private string _originalLocale;
        private StartupLanguageTestSettings _settings;
        private ResourceManager _resourceManager;
        private PendingInitialization _pending;
        private AsyncOperationHandle<LocalizationSettings> _pendingHandle;
        private Func<string> _launchDeepLink;
        private Func<string[]> _commandLineArgs;
        private CultureInfo _culture;
        private bool _hadSavedLanguage;
        private string _savedLanguage;
        private int _hasSeenWelcome;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return Until(() => CgsLocalization.IsReady);
            _original = LocalizationSettings.Instance;
            _originalLocale = CgsLocalization.CurrentLocale;
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _commandLineArgs = CgsLocaleSelector.CommandLineArgs;
            _culture = CultureInfo.CurrentUICulture;
            _hadSavedLanguage = PlayerPrefs.HasKey(CgsLocaleSelector.PreferenceKey);
            _savedLanguage = PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey);
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CardGameManager.LaunchDeepLink = _launchDeepLink;
            CgsLocaleSelector.CommandLineArgs = _commandLineArgs;
            CultureInfo.CurrentUICulture = _culture;
            if (_hadSavedLanguage)
                PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, _savedLanguage);
            else
                PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            PlayerPrefs.Save();
            MainMenu.ResetStartupLanguageWait();

            // Leave an empty scene, then start the project's own settings again
            var empty = SceneManager.CreateScene("StartupLanguageTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);

            LocalizationSettings.Instance = _original;
            if (_settings != null)
            {
                _settings.ReleaseHold();
                Object.DestroyImmediate(_settings);
            }
            if (_pendingHandle.IsValid())
                _resourceManager.Release(_pendingHandle);
            _resourceManager?.Dispose();
            _original.ResetState();
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return Until(() => CgsLocalization.IsReady);
            if (CgsLocalization.CurrentLocale != _originalLocale)
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales
                    .First(locale => locale.Identifier.Code == _originalLocale);
            yield return Until(() => CgsLocalization.CurrentLocale == _originalLocale);
        }

        [UnityTest]
        [TestCase(null, "ko", "en-US", "ko", ExpectedResult = null, TestName = "SavedKorean")]
        [TestCase(null, "ja", "en-US", "ja", ExpectedResult = null, TestName = "SavedJapanese")]
        [TestCase("ja", "ko", "en-US", "ja", ExpectedResult = null, TestName = "CommandLineOverridesSaved")]
        [TestCase(null, null, "ja-JP", "ja", ExpectedResult = null, TestName = "DeviceLocale")]
        [TestCase(null, null, "zh-Hans-CN", "zh-Hans", ExpectedResult = null, TestName = "DeviceSimplifiedChinese")]
        public IEnumerator FirstVisibleMainMenuUsesTheStartupLanguage(string commandLine, string saved,
            string device, string expected)
        {
            yield return Launch(commandLine, saved, device, GenerousWaitSeconds);
            var menu = Menu();
            for (var frame = 0; frame < 3; frame++)
            {
                Assert.IsTrue(menu.IsWaitingForLanguage, "The menu waits, hidden, while the language loads");
                Assert.IsFalse(MenuButtonsInteractable(menu), "The hidden menu ignores input");
                yield return null;
            }

            Release();
            yield return UntilShown(menu);

            Assert.IsTrue(CgsLocalization.IsReady);
            Assert.AreEqual(expected, CgsLocalization.CurrentLocale);
            AssertLabelsInLanguage(menu, expected);
            AssertFontsInLanguage(menu, expected);
            Assert.IsTrue(MenuButtonsInteractable(menu));
        }

        [UnityTest]
        [TestCase(null, null, "pt-PT", ExpectedResult = null, TestName = "UnsupportedDeviceFallsBackToEnglish")]
        [TestCase("en", "ko", "ko-KR", ExpectedResult = null, TestName = "CommandLineEnglish")]
        public IEnumerator EnglishStartupShowsTheMenuWithoutWaiting(string commandLine, string saved, string device)
        {
            yield return Launch(commandLine, saved, device, GenerousWaitSeconds);
            var menu = Menu();

            // The menu is authored in English, so it does not wait for the language to load
            Assert.IsFalse(menu.IsWaitingForLanguage);
            Assert.IsFalse(CgsLocalization.IsInitializationComplete);
            AssertLabelsInEnglish(menu);
            Assert.IsTrue(MenuButtonsInteractable(menu));

            Release();
            yield return Until(() => CgsLocalization.IsReady);
            yield return null;
            Assert.AreEqual("en", CgsLocalization.CurrentLocale);
            AssertLabelsInLanguage(menu, "en");
            AssertFontsInLanguage(menu, "en");
        }

        [UnityTest]
        public IEnumerator DelayedLanguageShowsEnglishThenRefreshesInPlace()
        {
            const float bound = 0.25f;
            yield return Launch(null, "ko", "en-US", bound);
            var menu = Menu();
            var started = Time.realtimeSinceStartup;
            LogAssert.Expect(LogType.Warning, new Regex("startup language was not ready after 250 ms"));

            yield return UntilShown(menu);

            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, bound * 0.9f, "The menu waits up to the bound");
            Assert.IsFalse(CgsLocalization.IsInitializationComplete);
            AssertLabelsInEnglish(menu);
            Assert.IsTrue(MenuButtonsInteractable(menu), "The English menu is usable");

            var labels = Labels(menu).Select(label => label.GetComponent<Text>()).ToArray();
            Release();
            yield return Until(() => CgsLocalization.IsReady && LabelsMatch(menu, "ko"));

            AssertLabelsInLanguage(menu, "ko");
            AssertFontsInLanguage(menu, "ko");
            Assert.AreSame(menu, Menu(), "The menu refreshes in place");
            CollectionAssert.AreEqual(labels, Labels(menu).Select(label => label.GetComponent<Text>()).ToArray());
            Assert.IsFalse(menu.IsWaitingForLanguage);
        }

        [UnityTest]
        public IEnumerator FailedLanguageShowsUsableEnglishWithADiagnostic()
        {
            yield return Launch(null, "ko", "en-US", GenerousWaitSeconds);
            var menu = Menu();
            yield return null;
            Assert.IsTrue(menu.IsWaitingForLanguage);

            // The gate, the failed startup, and the chain joining them each report the failure, then CGS does
            for (var report = 0; report < 3; report++)
                LogAssert.Expect(LogType.Error, new Regex("Expected startup localization failure"));
            LogAssert.Expect(LogType.Error, new Regex("CGS localization initialization failed"));
            _pending.Finish(_settings, false);
            yield return UntilShown(menu);

            Assert.IsTrue(CgsLocalization.IsInitializationComplete);
            Assert.IsFalse(CgsLocalization.IsReady);
            AssertLabelsInEnglish(menu);
            Assert.IsTrue(MenuButtonsInteractable(menu));
        }

        // Starts localization afresh, held before the startup locale is chosen, then opens the Main Menu as at launch
        private IEnumerator Launch(string commandLine, string saved, string device, float waitSeconds)
        {
            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
                Object.Destroy(manager.gameObject);
            yield return null;

            CgsLocaleSelector.CommandLineArgs = () => commandLine == null
                ? new[] { "Unity" }
                : new[] { "Unity", "-language=" + commandLine };
            if (saved == null)
                PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
            else
                PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, saved);
            CultureInfo.CurrentUICulture = new CultureInfo(device);

            var languages = CgsLocalization.Languages.ToArray();
            _settings = ScriptableObject.CreateInstance<StartupLanguageTestSettings>();
            _settings.SetAvailableLocales(_original.GetAvailableLocales());
            _settings.SetStringDatabase(_original.GetStringDatabase());
            _settings.SetAssetDatabase(_original.GetAssetDatabase());
            _settings.GetStartupLocaleSelectors().Clear();
            _settings.GetStartupLocaleSelectors().Add(new CgsLocaleSelector(languages));
            _settings.ResetState();
            _resourceManager = new ResourceManager();
            _pending = new PendingInitialization();
            _pendingHandle = _resourceManager.StartOperation(_pending, default);
            _settings.HoldUntil(_resourceManager, _pendingHandle);
            LocalizationSettings.Instance = _settings;
            CgsLocalization.Initialize();
            Assert.IsFalse(CgsLocalization.IsInitializationComplete);

            MainMenu.ResetStartupLanguageWait();
            MainMenu.StartupLanguageWaitSeconds = waitSeconds;
            CardGameManager.LaunchDeepLink = () => null;
            SceneManager.LoadScene(Tags.MainMenuSceneIndex);
            yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }

        // Lets the real initialization run, which chooses the startup locale and preloads its text and fonts
        private void Release() => _pending.Finish(_settings, true);

        private static MainMenu Menu() => Object.FindObjectsByType<MainMenu>().Single();

        private static CgsLocalizeStringEvent[] Labels(MainMenu menu) =>
            menu.GetComponentsInChildren<CgsLocalizeStringEvent>(true)
                .Where(label => label.GetComponent<Text>() != null).ToArray();

        private static bool MenuButtonsInteractable(MainMenu menu) =>
            menu.GetComponentsInChildren<Button>().All(button => button.IsInteractable());

        private static string Expected(CgsLocalizeStringEvent label, string locale)
        {
            if (locale == "en")
                return label.English;
            var table = LocalizationSettings.StringDatabase.GetTable(UiTable);
            var entry = table.GetEntryFromReference(label.StringReference.TableEntryReference);
            Assert.IsNotNull(entry, $"{label.name} has a {locale} entry");
            Assert.AreEqual(locale, table.LocaleIdentifier.Code);
            return entry.GetLocalizedString();
        }

        private static bool LabelsMatch(MainMenu menu, string locale) =>
            Labels(menu).All(label => label.GetComponent<Text>().text == Expected(label, locale));

        private static void AssertLabelsInLanguage(MainMenu menu, string locale)
        {
            var labels = Labels(menu);
            Assert.IsNotEmpty(labels);
            foreach (var label in labels)
            {
                var expected = Expected(label, locale);
                Assert.AreEqual(expected, label.GetComponent<Text>().text, label.name);
                if (locale != "en")
                    Assert.AreNotEqual(label.English, expected, $"{label.name} is translated into {locale}");
            }
        }

        private static void AssertLabelsInEnglish(MainMenu menu)
        {
            var labels = Labels(menu);
            Assert.IsNotEmpty(labels);
            foreach (var label in labels)
                Assert.AreEqual(label.English, label.GetComponent<Text>().text, label.name);
        }

        private static void AssertFontsInLanguage(MainMenu menu, string locale)
        {
            var english = LocalizationSettings.AvailableLocales.Locales.Single(item => item.Identifier.Code == "en");
            var fonts = menu.GetComponentsInChildren<LocalizedUiFont>(true)
                .Where(font => font.GetComponent<Text>() != null).ToArray();
            Assert.IsNotEmpty(fonts);
            foreach (var binding in fonts)
            {
                var expected = LocalizationSettings.AssetDatabase.GetLocalizedAsset<Font>(FontTable, binding.Key);
                Assert.IsNotNull(expected, $"{binding.Key} font for {locale}");
                Assert.AreSame(expected, binding.GetComponent<Text>().font, binding.name);
                if (locale is "ko" or "ja" or "zh-Hans")
                    Assert.AreNotSame(LocalizationSettings.AssetDatabase.GetLocalizedAsset<Font>(FontTable,
                        binding.Key, english), expected, $"{locale} uses its own {binding.Key} font");
            }
        }

        private static IEnumerator UntilShown(MainMenu menu)
        {
            yield return Until(() => !menu.IsWaitingForLanguage);
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            for (var frame = 0; frame < TimeoutFrames && !condition(); frame++)
                yield return null;
            Assert.IsTrue(condition(), "Timed out");
        }
    }
}
