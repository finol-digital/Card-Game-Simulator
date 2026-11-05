/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using Cgs.GameContent;
using Cgs.Localization;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class GameContentPresentationTests
    {
        private static UnityCardGame Game(string name)
        {
            var game = new UnityCardGame(null, name);
            game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""en"",""locales"":{""es"":{""inline"":{""game"":{""name"":""Español""}}},""ja"":{""inline"":{""game"":{""name"":""日本語""}}}}}").Value;
            return game;
        }

        [Test]
        public void OptOutPresentationDoesNotInspectOptionalLegacyCollections()
        {
            var game = new UnityCardGame(null, "Original 日本語")
            {
                CardProperties = null, Enums = null, Extras = null
            };
            Assert.That(GameContentPresentation.Context(game, "es").GameName, Is.EqualTo(game.Name));
        }

        [Test]
        public void SourceOnlyManifestHasSafeDiagnostics()
        {
            var game = new UnityCardGame(null, "No catalogs")
            {
                Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""en""}").Value
            };
            Assert.That(game.Localization.IsUsable, Is.True);
            Assert.DoesNotThrow(() => GameContentPresentation.Diagnostics(game));
        }

        [Test]
        public void SetDescriptionsKeepLegacyCodesAndTranslateOnlyNames()
        {
            var game = new UnityCardGame(null, "Sets");
            var set = new Set("C", "Core");
            var unnamed = new Set("X", "X");
            var canonical = GameContentPresentation.Context(game, "es");
            Assert.That(GameContentPresentation.SetDescription(canonical, set), Is.EqualTo(set.ToString()));
            Assert.That(GameContentPresentation.SetDescription(canonical, unnamed), Is.EqualTo(unnamed.ToString()));
            game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""es"",""locales"":{""es"":{""inline"":{""sets"":{""C"":{""name"":""Básico""}}}}}}").Value;
            var translated = new LocalizationContext(new LocalizationTargets(game, Array.Empty<Card>(), new[] { set }), "es");
            Assert.That(GameContentPresentation.SetDescription(translated, set), Is.EqualTo("Básico (C)"));
        }

        [Test]
        public void ContextsReuseInventoryAndInvalidateWhenCanonicalCardsChange()
        {
            var game = Game("Base");
            var spanish = GameContentPresentation.Context(game, "es");
            Assert.That(GameContentPresentation.Context(game, "es"), Is.SameAs(spanish));
            Assert.That(GameContentPresentation.Context(game, "ja").GameName, Is.EqualTo("日本語"));
            var before = GameContentPresentation.Context(game, "es");
            game.Add(new UnityCard(game, "1", "Original", "BASE", new Dictionary<string, PropertyDefValuePair>(), false), false);
            Assert.That(GameContentPresentation.Context(game, "es"), Is.Not.SameAs(before));
            Assert.That(game.Name, Is.EqualTo("Base"));
            Assert.That(game.HasLoaded, Is.False);
            Assert.That(game.HasDownloaded, Is.False);
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 15;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(condition(), Is.True, "Presentation did not settle within 15 seconds.");
        }

        [UnityTest]
        public IEnumerator PageLoadsCoalesceInventoryRefreshUntilLoadingFinishes()
        {
            var game = Game("Paged load");
            var node = new GameObject("Load runner", typeof(GameContentImage));
            game.CoroutineRunner = node.GetComponent<GameContentImage>();
            var loading = typeof(UnityCardGame).GetProperty(nameof(UnityCardGame.IsLoading));
            var notify = typeof(UnityCardGame).GetMethod("NotifyContentChanged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var before = GameContentPresentation.Context(game, "es");
            var refreshes = 0;
            void Changed(UnityCardGame changed) { if (changed == game) refreshes++; }
            GameContentPresentation.Changed += Changed;
            try
            {
                loading.SetValue(game, true);
                for (var page = 0; page < 3; page++)
                {
                    game.Add(new UnityCard(game, page.ToString(), "Card " + page, "BASE", new Dictionary<string, PropertyDefValuePair>(), false), false);
                    notify.Invoke(game, null);
                    Assert.That(GameContentPresentation.Context(game, "es"), Is.SameAs(before));
                    yield return null;
                }
                Assert.That(refreshes, Is.Zero);
                loading.SetValue(game, false);
                yield return Until(() => refreshes == 1);
                Assert.That(GameContentPresentation.Context(game, "es"), Is.Not.SameAs(before));
            }
            finally
            {
                loading.SetValue(game, false);
                GameContentPresentation.Changed -= Changed;
                Object.DestroyImmediate(node);
            }
        }

        [Test]
        public void FailingViewDoesNotBlockOtherRefreshSubscribers()
        {
            var game = Game("Subscriber isolation");
            GameContentPresentation.Context(game);
            var observed = false;
            void Fail(UnityCardGame changed) => throw new InvalidOperationException("Broken view");
            void Observe(UnityCardGame changed) => observed = changed == game;
            GameContentPresentation.Changed += Fail;
            GameContentPresentation.Changed += Observe;
            try
            {
                LogAssert.Expect(LogType.Warning, "Game-content binding refresh failed: Broken view");
                typeof(UnityCardGame).GetMethod("NotifyContentChanged",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(game, null);
                Assert.That(observed, Is.True);
            }
            finally
            {
                GameContentPresentation.Changed -= Fail;
                GameContentPresentation.Changed -= Observe;
            }
        }

        [Test]
        public void RefreshObserversCaptureTheFinalSelectionGeneration()
        {
            var game = Game("generation-" + Guid.NewGuid().ToString("N"));
            game.AutoUpdate = -1;
            GameContentPresentation.Context(game);
            GameContentPresentation.Select(game);
            var generation = typeof(GameContentPresentation).GetField("_selection",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var observed = -1;
            void Changed(UnityCardGame changed) => observed = (int)generation.GetValue(null);
            GameContentPresentation.Changed += Changed;
            try
            {
                typeof(GameContentPresentation).GetMethod("OnLocaleChanged",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
                Assert.That(observed, Is.EqualTo(generation.GetValue(null)),
                    "An image request started by a locale notification must not be made stale after the notification.");
                System.IO.Directory.CreateDirectory(game.GameDirectoryPath);
                System.IO.File.WriteAllText(game.GameFilePath, JsonConvert.SerializeObject(game));
                game.ReadProperties();
                Assert.That(observed, Is.EqualTo(generation.GetValue(null)),
                    "A manifest reload must establish its selection generation before refreshing views.");
            }
            finally
            {
                GameContentPresentation.Changed -= Changed;
                GameContentPresentation.Select(null);
                if (System.IO.Directory.Exists(game.GameDirectoryPath))
                    System.IO.Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        [UnityTest]
        public IEnumerator LiveMessageRefreshesCanonicalArgumentsAndReleasesSubscription()
        {
            var hadPreference = PlayerPrefs.HasKey(CgsLocaleSelector.PreferenceKey);
            var preference = PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey);
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            var original = CgsLocalization.CurrentLocale;
            var game = Game("Canonical");
            var rendered = string.Empty;
            var resolutions = 0;
            var message = GameContentMessage.Create(game, display =>
            {
                resolutions++;
                return UiMessage.With("games.cards.loaded", display.GameName, ("gameName", display.GameName));
            });
            LocalizedMessageBinding binding = null;
            try
            {
                CgsLocalization.SelectLanguage("es");
                yield return Until(() => CgsLocalization.CurrentLocale == "es");
                binding = new LocalizedMessageBinding(message, text => rendered = text);
                yield return Until(() => rendered.Contains("Español"));
                CgsLocalization.SelectLanguage("ja");
                yield return Until(() => rendered.Contains("日本語"));
                Assert.That(game.Name, Is.EqualTo("Canonical"));
                binding.Dispose();
                var before = resolutions;
                CgsLocalization.SelectLanguage("es");
                yield return Until(() => CgsLocalization.CurrentLocale == "es");
                yield return null;
                Assert.That(resolutions, Is.EqualTo(before), "Disposed dialogs must release game-content observers.");
            }
            finally
            {
                binding?.Dispose();
                CgsLocalization.SelectLanguage(original);
                if (hadPreference)
                    PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, preference);
                else
                    PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
                PlayerPrefs.Save();
            }
            yield return Until(() => CgsLocalization.CurrentLocale == original);
        }

        [UnityTest]
        public IEnumerator ActiveHiddenAndReusedViewsUseLatestLocaleWithoutLoadingGames()
        {
            var hadPreference = PlayerPrefs.HasKey(CgsLocaleSelector.PreferenceKey);
            var preference = PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey);
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Assert.That(CgsLocalization.IsReady, Is.True);
            var original = CgsLocalization.CurrentLocale;
            var labelObject = new GameObject("Game label", typeof(RectTransform), typeof(Text));
            var first = Game("First");
            var second = Game("Second");
            var canonical = JsonConvert.SerializeObject(first);
            try
            {
                var label = labelObject.GetComponent<Text>();
                CgsLocalization.SelectLanguage("es");
                yield return Until(() => CgsLocalization.CurrentLocale == "es");
                GameContentText.Set(label, first, context => context.GameName);
                Assert.That(label.text, Is.EqualTo("Español"));
                labelObject.SetActive(false);
                CgsLocalization.SelectLanguage("ja");
                yield return Until(() => CgsLocalization.CurrentLocale == "ja");
                labelObject.SetActive(true);
                Assert.That(label.text, Is.EqualTo("日本語"));
                GameContentText.Set(label, second, context => context.GameName + " 2");
                CgsLocalization.SelectLanguage("fr");
                CgsLocalization.SelectLanguage("ja");
                CgsLocalization.SelectLanguage("es");
                yield return Until(() => label.text == "Español 2");
                labelObject.SetActive(false);
                LocalizedUiText.SetLiteral(label, "User-entered 日本語");
                CgsLocalization.SelectLanguage("ja");
                yield return Until(() => CgsLocalization.CurrentLocale == "ja");
                labelObject.SetActive(true);
                Assert.That(label.text, Is.EqualTo("User-entered 日本語"));
                Assert.That(JsonConvert.SerializeObject(first), Is.EqualTo(canonical));
                foreach (var game in new[] { first, second })
                {
                    Assert.That(game.HasLoaded, Is.False);
                    Assert.That(game.HasDownloaded, Is.False);
                    Assert.That(game.HasReadProperties, Is.False);
                    Assert.That(game.Cards, Is.Empty);
                }
            }
            finally
            {
                Object.DestroyImmediate(labelObject);
                CgsLocalization.SelectLanguage(original);
                if (hadPreference)
                    PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, preference);
                else
                    PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
                PlayerPrefs.Save();
            }
            yield return Until(() => CgsLocalization.CurrentLocale == original);
        }
    }
}
