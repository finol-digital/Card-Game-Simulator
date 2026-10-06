/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using Cgs.Localization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;

namespace Tests.PlayMode
{
    // Exercise the presenter without the unrelated global game/input manager.
    public class LocalizationTestDialog : Cgs.Menu.Dialog
    {
        protected override void Start() { }
        protected override void LateUpdate() { }
    }

    public class LocalizationPresentationTests
    {
        private Locale _original;
        private string _saved;
        private bool _hadSaved;
        private GameObject _object;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hadSaved = PlayerPrefs.HasKey(CgsLocaleSelector.PreferenceKey);
            _saved = PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey);
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Assert.IsTrue(CgsLocalization.IsReady);
            _original = LocalizationSettings.SelectedLocale;
            _object = new GameObject("Localized label", typeof(RectTransform), typeof(Text));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(_object);
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            if (_original != null)
            {
                CgsLocalization.SelectLanguage(_original.Identifier.Code);
                yield return Until(() => LocalizationSettings.SelectedLocale == _original);
                yield return LocalizationSettings.InitializationOperation;
                yield return null;
            }
            if (_hadSaved)
                PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, _saved);
            else
                PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
            PlayerPrefs.Save();
        }

        private static IEnumerator Until(System.Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 15;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(condition(), "Localized presentation did not settle within 15 seconds.");
        }

        [UnityTest]
        public IEnumerator ScoreboardLabels_ReuseBindingsAndTrackConnectionAndLocale()
        {
            var owner = new GameObject("Scoreboard test");
            owner.SetActive(false);
            var scoreboard = owner.AddComponent<Cgs.Play.Scoreboard>();
            var name = _object.GetComponent<Text>();
            var idObject = new GameObject("Room ID", typeof(RectTransform), typeof(Text));
            idObject.transform.SetParent(_object.transform);
            var id = idObject.GetComponent<Text>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(Cgs.Play.Scoreboard).GetField("roomNameText", flags).SetValue(scoreboard, name);
            typeof(Cgs.Play.Scoreboard).GetField("roomIdIpText", flags).SetValue(scoreboard, id);
            var refresh = typeof(Cgs.Play.Scoreboard).GetMethod("RefreshRoomLabels", flags);
            var bindingField = typeof(LocalizedUiText).GetField("_binding", flags);
            try
            {
                refresh.Invoke(scoreboard, new object[] { false, null, null });
                var presenter = name.GetComponent<LocalizedUiText>();
                var binding = bindingField.GetValue(presenter);
                Assert.IsNotNull(binding);
                for (var frame = 0; frame < 10; frame++)
                    refresh.Invoke(scoreboard, new object[] { false, null, null });
                Assert.AreSame(binding, bindingField.GetValue(presenter));
                CgsLocalization.SelectLanguage("es");
                yield return Until(() => name.text != Cgs.Play.Scoreboard.Offline);
                Assert.AreEqual(name.text, id.text);
                refresh.Invoke(scoreboard, new object[] { true, "Player room 日本語", "12345" });
                Assert.AreEqual("Player room 日本語", name.text);
                Assert.AreEqual("12345", id.text);
                Assert.IsNull(bindingField.GetValue(presenter));
                refresh.Invoke(scoreboard, new object[] { true, "Renamed room", "67890" });
                Assert.AreEqual("Renamed room", name.text);
                Assert.AreEqual("67890", id.text);
                refresh.Invoke(scoreboard, new object[] { false, null, null });
                CgsLocalization.SelectLanguage("en");
                yield return Until(() => name.text == Cgs.Play.Scoreboard.Offline);
                Assert.AreEqual(name.text, id.text);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [UnityTest]
        public IEnumerator VisibleAndReusedText_TracksLocaleAndArguments()
        {
            var text = _object.GetComponent<Text>();
            CgsLocalization.SelectLanguage("es");
            LocalizedUiText.Set(text, new UiMessage("settings.language", "Language"));
            yield return Until(() => text.text == "Idioma");
            _object.SetActive(false);
            CgsLocalization.SelectLanguage("de");
            _object.SetActive(true);
            yield return Until(() => text.text == "Sprache");
            LocalizedUiText.Set(text, UiMessage.With("cards.page", "1 / 3", ("page", 1), ("total", 3)));
            yield return Until(() => text.text == "1 / 3");
            LocalizedUiText.Set(text, UiMessage.With("cards.page", "2 / 3", ("page", 2), ("total", 3)));
            yield return Until(() => text.text == "2 / 3");
            CgsLocalization.SelectLanguage("fr");
            CgsLocalization.SelectLanguage("ja");
            CgsLocalization.SelectLanguage("es");
            LocalizedUiText.SetLiteral(text, "Player-entered 日本語");
            yield return null;
            yield return null;
            Assert.AreEqual("Player-entered 日本語", text.text);
        }

        [UnityTest]
        public IEnumerator Dialog_VisibleHiddenAndQueuedMessagesPreserveActionsAndInput()
        {
            var dialogObject = new GameObject("Localized dialog");
            dialogObject.SetActive(false);
            var dialog = dialogObject.AddComponent<LocalizationTestDialog>();
            var text = AddDialogChild<Text>(dialogObject, "Message");
            var yes = AddDialogChild<Button>(dialogObject, "Yes");
            var no = AddDialogChild<Button>(dialogObject, "No");
            var input = AddDialogChild<InputField>(dialogObject, "Player input");
            input.text = "Player 日本語";
            SetDialogField(dialog, "messageText", text);
            SetDialogField(dialog, "yesButton", yes);
            SetDialogField(dialog, "noButton", no);
            SetDialogField(dialog, "copyButton", AddDialogChild<Button>(dialogObject, "Copy").gameObject);
            SetDialogField(dialog, "shareButton", AddDialogChild<Button>(dialogObject, "Share").gameObject);
            try
            {
                var accepted = 0;
                CgsLocalization.SelectLanguage("es");
                dialog.Prompt(new UiMessage("settings.language", "Language"), () => accepted++);
                yield return Until(() => text.text == "Idioma");
                dialog.Show(new UiMessage("common.true", "true"));
                dialogObject.SetActive(false);
                CgsLocalization.SelectLanguage("de");
                dialogObject.SetActive(true);
                yield return Until(() => text.text == "Sprache");
                Assert.AreEqual("Player 日本語", input.text);
                yes.onClick.Invoke();
                Assert.AreEqual(1, accepted);
                Assert.IsTrue(dialogObject.activeSelf, "Queued message must remain available.");
                yield return Until(() => text.text != "Sprache" && text.text != "true");
                dialog.OkClose();
                Assert.IsFalse(dialogObject.activeSelf);
            }
            finally { Object.DestroyImmediate(dialogObject); }
        }

        private static T AddDialogChild<T>(GameObject parent, string name) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform);
            return child.AddComponent<T>();
        }

        private static void SetDialogField(Cgs.Menu.Dialog dialog, string field, object value)
            => typeof(Cgs.Menu.Dialog).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dialog, value);

        [UnityTest]
        public IEnumerator SettingsLanguage_FirstRowRefreshDoesNotNotifyOrChangeOtherPreferences()
        {
            var frameRate = Cgs.FrameRateManager.FrameRateIndex;
            var resolution = Cgs.ResolutionManager.ResolutionIndex;
            var developerMode = Cgs.Menu.Settings.DeveloperMode;
            Cgs.Menu.Settings.DeveloperMode = false;
            // Unity's test runner disables project actions. Enable them before the
            // real manager starts, so its recovery warning cannot open a new modal.
            var actionsEnabled = InputSystem.actions.enabled;
            InputSystem.actions.Enable();
            yield return SceneManager.LoadSceneAsync("Settings", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Settings");
            // The Editor's startup scene may show a welcome dialog. It must not
            // consume navigation intended for this independently loaded Settings UI.
            var activeModals = Object.FindObjectsByType<Cgs.Menu.Modal>(FindObjectsSortMode.None)
                .Where(modal => modal.gameObject.activeInHierarchy).ToArray();
            foreach (var modal in activeModals)
                modal.gameObject.SetActive(false);
            try
            {
                var settings = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Cgs.Menu.Settings>(true)).Single();
                var dropdown = (Dropdown)typeof(Cgs.Menu.Settings).GetField("languageDropdown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(settings);
                yield return Until(() => dropdown.interactable && dropdown.options.Count == 10);
                Assert.AreEqual(0, dropdown.transform.parent.GetSiblingIndex());
                CollectionAssert.AreEqual(CgsLocalization.Languages.Select(language => language.NativeName), dropdown.options.Select(option => option.text));
                var gamepad = InputSystem.AddDevice<Gamepad>();
                try
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.down });
                    yield return null;
                    yield return null;
                    Assert.AreEqual(dropdown.gameObject, EventSystem.current.currentSelectedGameObject,
                        "Initial gamepad navigation must focus Language. Selected="
                        + EventSystem.current.currentSelectedGameObject?.transform.parent?.name
                        + "; active=" + EventSystem.current.currentSelectedGameObject?.activeInHierarchy
                        + "; modal=" + Cgs.CardGameManager.Instance.ModalCanvas?.name
                        + "; move=" + InputSystem.actions.FindAction(Cgs.Tags.PlayerMove).ReadValue<Vector2>());
                    InputSystem.QueueStateEvent(gamepad, new GamepadState());
                    yield return null;
                    dropdown.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                    yield return null;
                    Assert.IsNotNull(dropdown.transform.Find("Dropdown List"), "Pointer selection opens the language list.");
                    dropdown.Hide();
                }
                finally { InputSystem.RemoveDevice(gamepad); }
                var callbacks = 0;
                dropdown.onValueChanged.AddListener(_ => callbacks++);
                CgsLocalization.SelectLanguage("fr");
                yield return Until(() => dropdown.value == 5);
                Assert.AreEqual(0, callbacks);
                dropdown.value = 1;
                yield return Until(() => LocalizationSettings.SelectedLocale.Identifier.Code == "es");
                Assert.AreEqual(1, callbacks);
                settings.gameObject.SetActive(false);
                CgsLocalization.SelectLanguage("ja");
                settings.gameObject.SetActive(true);
                yield return Until(() => dropdown.value == 7);
                Assert.AreEqual(1, callbacks);
                Assert.AreEqual(frameRate, Cgs.FrameRateManager.FrameRateIndex);
                Assert.AreEqual(resolution, Cgs.ResolutionManager.ResolutionIndex);
            }
            finally
            {
                foreach (var modal in activeModals)
                    if (modal != null)
                        modal.gameObject.SetActive(true);
                Cgs.Menu.Settings.DeveloperMode = developerMode;
                if (!actionsEnabled)
                    InputSystem.actions.Disable();
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator AuthoredLabel_MissingKeyFallsBackAndLiteralOwnershipSurvivesSwitch()
        {
            var text = _object.GetComponent<Text>();
            var authored = _object.AddComponent<CgsLocalizeStringEvent>();
            typeof(CgsLocalizeStringEvent).GetField("english", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(authored, "Readable label");
            authored.OnUpdateString.AddListener(value => text.text = value);
            authored.StringReference = new LocalizedString("CgsUi", "test.intentionally.missing");
            yield return Until(() => text.text == "Readable label");
            LocalizedUiText.SetLiteral(text, "Untranslated player value");
            CgsLocalization.SelectLanguage("fr");
            yield return null;
            yield return null;
            Assert.AreEqual("Untranslated player value", text.text);
        }

        [UnityTest]
        public IEnumerator MissingKey_RetainsReadableEnglish()
        {
            var text = _object.GetComponent<Text>();
            LocalizedUiText.Set(text, new UiMessage("test.intentionally.missing", "Readable English"));
            yield return null;
            yield return null;
            Assert.AreEqual("Readable English", text.text);
        }

        [UnityTest]
        public IEnumerator EmptyEntry_RetainsReadableEnglish()
        {
            CgsLocalization.SelectLanguage("es");
            yield return Until(() => LocalizationSettings.SelectedLocale.Identifier.Code == "es");
            yield return LocalizationSettings.InitializationOperation;
            var operation = LocalizationSettings.StringDatabase.GetTableAsync("CgsUi");
            yield return operation;
            var entry = operation.Result.GetEntry("settings.language");
            var previous = entry.Value;
            try
            {
                entry.Value = string.Empty;
                var text = _object.GetComponent<Text>();
                LocalizedUiText.Set(text, new UiMessage("settings.language", "Language"));
                yield return null;
                yield return null;
                Assert.AreEqual("Language", text.text);
            }
            finally { entry.Value = previous; }
        }

        [TestCase("de", "es", "ko-KR", "de")]
        [TestCase("unsupported", "es", "ko-KR", "es")]
        [TestCase(null, "removed", "es-MX", "es")]
        [TestCase(null, null, "zh-Hans-CN", "zh-Hans")]
        [TestCase(null, null, "zh-CN", "zh-Hans")]
        [TestCase(null, null, "zh-Hant-TW", "en")]
        [TestCase(null, null, "pt-PT", "en")]
        [TestCase(null, null, "pt-BR", "pt-BR")]
        public void ShippedStartupSelector_ResolvesPrecedenceAndUnsupportedPreferences(
            string commandLine, string saved, string device, string expected)
        {
            var selector = new CgsLocaleSelector(CgsLocalization.Languages);
            Assert.AreEqual(expected, selector.ResolveCode(commandLine, saved, device));
        }

        [UnityTest]
        public IEnumerator SavedChoice_IsReadByFreshAsynchronousLocaleInitialization()
        {
            Assert.IsTrue(CgsLocalization.SelectLanguage("fr"));
            yield return Until(() => LocalizationSettings.SelectedLocale.Identifier.Code == "fr");
            yield return LocalizationSettings.InitializationOperation;
            // Change only the current runtime authority. A fresh startup must read
            // the persisted choice, independently of the existing selected locale.
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales
                .Single(locale => locale.Identifier.Code == "en");
            yield return LocalizationSettings.InitializationOperation;
            var fresh = ScriptableObject.CreateInstance<LocalizationSettings>();
            fresh.GetStartupLocaleSelectors().Clear();
            fresh.GetStartupLocaleSelectors().Add(new CgsLocaleSelector(CgsLocalization.Languages));
            fresh.SetAvailableLocales(new LocalesProvider());
            // This exercises startup initialization in isolation; the active UI
            // keeps its original string/asset databases and subscriptions.
            fresh.SetAssetDatabase(null);
            fresh.SetStringDatabase(null);
            try
            {
                var initialization = fresh.GetInitializationOperation();
                yield return initialization;
                Assert.AreEqual(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded,
                    initialization.Status);
                Assert.AreEqual("fr", fresh.GetSelectedLocaleAsync().Result.Identifier.Code);
                Assert.AreEqual("en", LocalizationSettings.SelectedLocale.Identifier.Code);
                Assert.AreEqual("fr", PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey));
            }
            finally
            {
                ((System.IDisposable)fresh).Dispose();
                Object.DestroyImmediate(fresh);
            }
        }

        [Test]
        public void ExplicitChoice_PersistsOnlySupportedLocaleCode()
        {
            Assert.IsTrue(CgsLocalization.SelectLanguage("fr"));
            Assert.AreEqual("fr", PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey));
            Assert.IsFalse(CgsLocalization.SelectLanguage("pt-PT"));
            Assert.AreEqual("fr", PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey));
        }
    }
}
