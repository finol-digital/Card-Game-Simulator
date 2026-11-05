/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using Cgs.GameContent;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using System.Reflection;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class GameLocalizationAssetTests
    {
        private UnityCardGame _game;
        private string _relocated;
        private const string Source = "https://example.invalid/banner.png";
        private const string Target = "/assets/bannerImageUrl";

        [SetUp]
        public void SetUp()
        {
            _game = new UnityCardGame(null, "images-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_game.GameDirectoryPath);
            _relocated = _game.GameDirectoryPath + "-offline";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var root in new[] { _game.GameDirectoryPath, _relocated })
            {
                LocalizationCache.Invalidate(root);
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static byte[] ImageBytes(int width, int height)
        {
            var texture = new Texture2D(width, height);
            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return bytes;
        }

        private LocalizationContext Manifest(string source, string regional = null)
        {
            var locales = new JObject
            {
                ["es"] = new JObject { ["inline"] = new JObject { ["assets"] = new JObject { ["bannerImageUrl"] = source } } }
            };
            if (regional != null)
                locales["es-MX"] = new JObject { ["inline"] = new JObject { ["assets"] = new JObject { ["bannerImageUrl"] = regional } } };
            File.WriteAllText(_game.GameFilePath, new JObject
            {
                ["name"] = _game.Name, ["autoUpdate"] = -1,
                ["localization"] = new JObject { ["defaultLocale"] = "es", ["locales"] = locales }
            }.ToString());
            _game.ReadProperties();
            Assert.That(_game.HasReadProperties, Is.True, _game.Error);
            return new LocalizationContext(new LocalizationTargets(_game, _game.Cards.Values), "es-MX",
                definitionOrigin: _game.DefinitionOrigin);
        }

        private static void Run(IEnumerator routine)
        {
            while (routine.MoveNext())
                if (routine.Current is IEnumerator nested)
                    Run(nested);
        }

        [Test]
        public void MissingRegionalArtFallsBackAndOrientationDoesNotChangeCanonicalGeometry()
        {
            var bytes = ImageBytes(4, 2);
            File.WriteAllBytes(Path.Combine(_game.GameDirectoryPath, "es.png"), bytes);
            var context = Manifest("es.png", "missing.png");
            var size = _game.CardSize;
            var cache = new LocalizationCache(_game.GameDirectoryPath, _game.Id);
            var assets = new GameLocalizationAssets(_game, cache);
            var original = assets.Available(context.AssetCandidates("bannerImageUrl"), Target);
            var oriented = assets.Available(context.AssetCandidates("bannerImageUrl"), Target, true);
            try
            {
                Assert.That(original.texture.width, Is.EqualTo(4));
                Assert.That(oriented.texture.width, Is.EqualTo(2));
                Assert.That(oriented.texture.height, Is.EqualTo(4));
                Assert.That(_game.CardSize, Is.EqualTo(size));
                Assert.That(File.ReadAllBytes(Path.Combine(_game.GameDirectoryPath, "es.png")), Is.EqualTo(bytes));
            }
            finally
            {
                GameLocalizationAssets.Release(original);
                GameLocalizationAssets.Release(oriented);
            }
        }

        [Test]
        public void FailedImageRefreshKeepsDecodedCacheAndOfflinePackageCopy()
        {
            var context = Manifest(Source);
            var cache = new LocalizationCache(_game.GameDirectoryPath, _game.Id);
            var request = cache.Begin("es", new Uri(Source), Target);
            Assert.That(cache.Publish(request, ImageBytes(3, 2), new Uri(Source), bytes => bytes.Length > 0), Is.True);
            var requests = 0;
            IEnumerator Download(string source, string path, Action<FileDownloadResult> completed)
            {
                requests++;
                File.WriteAllBytes(path, Array.Empty<byte>());
                completed(new FileDownloadResult(true, new Uri(source), true));
                yield break;
            }
            var assets = new GameLocalizationAssets(_game, cache, Download);
            Run(assets.Refresh(context.AssetCandidates("bannerImageUrl"), Target, () => Assert.Fail("Disabled update published")));
            Assert.That(requests, Is.Zero);
            _game.AutoUpdate = 0;
            Run(assets.Refresh(context.AssetCandidates("bannerImageUrl"), Target, () => Assert.Fail("Empty image published")));
            Assert.That(requests, Is.EqualTo(1));
            LocalizationFiles.CopyPackageSnapshot(_game.GameDirectoryPath, _relocated);
            var offline = new GameLocalizationAssets(_game, new LocalizationCache(_relocated, _game.Id));
            var sprite = offline.Available(context.AssetCandidates("bannerImageUrl"), Target);
            try { Assert.That(sprite.texture.width, Is.EqualTo(3)); }
            finally { GameLocalizationAssets.Release(sprite); }
            Assert.That(File.Exists(_game.BannerImageFilePath), Is.False, "Localized bytes must not enter the canonical artwork cache.");
        }

        [Test]
        public void ZeroDayPolicyRefreshesOncePerSelectionCycleAndAfterDefinitionReload()
        {
            var context = Manifest(Source);
            _game.AutoUpdate = 0;
            var requests = 0;
            IEnumerator Download(string source, string path, Action<FileDownloadResult> completed)
            {
                requests++;
                File.WriteAllBytes(path, ImageBytes(2, 3));
                completed(new FileDownloadResult(true, new Uri(source), true));
                yield break;
            }
            var assets = new GameLocalizationAssets(_game, new LocalizationCache(_game.GameDirectoryPath, _game.Id), Download);
            for (var page = 0; page < 5; page++)
                Run(assets.Refresh(context.AssetCandidates("bannerImageUrl"), Target, null));
            Assert.That(requests, Is.EqualTo(1));
            assets.BeginRefreshCycle();
            Run(assets.Refresh(context.AssetCandidates("bannerImageUrl"), Target, null));
            Assert.That(requests, Is.EqualTo(2));
            context = Manifest(Source);
            _game.AutoUpdate = 0;
            Run(assets.Refresh(context.AssetCandidates("bannerImageUrl"), Target, null));
            Assert.That(requests, Is.EqualTo(3));
        }

        [Test]
        public void ActiveImageBindingReturnsToOriginalOnFeatureRemovalWithoutChangingRect()
        {
            File.WriteAllBytes(Path.Combine(_game.GameDirectoryPath, "es.png"), ImageBytes(4, 2));
            Manifest("es.png");
            var canonical = UnityFileMethods.CreateSprite(ImageBytes(2, 3));
            var node = new GameObject("Localized art", typeof(RectTransform), typeof(Image));
            try
            {
                var rect = node.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 300);
                var image = node.GetComponent<Image>();
                GameContentImage.Set(image, _game, canonical, false, "bannerImageUrl");
                Assert.That(image.sprite.texture.width, Is.EqualTo(4));
                Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(200, 300)));
                File.WriteAllText(_game.GameFilePath, new JObject { ["name"] = _game.Name }.ToString());
                _game.ReadProperties();
                Assert.That(image.sprite, Is.SameAs(canonical));
                Assert.That(canonical.texture.height, Is.EqualTo(3));
                Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(200, 300)));
            }
            finally
            {
                Object.DestroyImmediate(node);
                GameLocalizationAssets.Release(canonical);
            }
        }

        [UnityTest]
        public IEnumerator BindingKeepsUsableArtUntilSharedRefreshSettles()
        {
            File.WriteAllBytes(Path.Combine(_game.GameDirectoryPath, "es.png"), ImageBytes(4, 2));
            Manifest("es.png");
            var node = new GameObject("Image lifetime", typeof(RectTransform), typeof(Image), typeof(GameContentImage));
            var second = new GameObject("Second image", typeof(RectTransform), typeof(Image), typeof(GameContentImage));
            var finish = false;
            var requests = 0;
            var succeeded = true;
            IEnumerator Download(string source, string path, Action<FileDownloadResult> completed)
            {
                requests++;
                while (!finish)
                    yield return null;
                if (succeeded)
                    File.WriteAllBytes(path, ImageBytes(6, 3));
                completed(new FileDownloadResult(succeeded, new Uri(source), true));
            }
            try
            {
                var image = node.GetComponent<Image>();
                var other = second.GetComponent<Image>();
                GameContentImage.Set(image, _game, null, false, "bannerImageUrl");
                GameContentImage.Set(other, _game, null, false, "bannerImageUrl");
                var previous = image.sprite;
                Assert.That(previous, Is.Not.Null);
                _game.CoroutineRunner = node.GetComponent<GameContentImage>();
                var table = typeof(GameContentPresentation).GetField("_entries", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                var args = new object[] { _game, null };
                Assert.That((bool)table.GetType().GetMethod("TryGetValue").Invoke(table, args), Is.True);
                args[1].GetType().GetField("_catalogs", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(args[1], new GameLocalizationCatalogs(_game, Download));
                GameContentPresentation.Select(_game);
                void Reload(string source)
                {
                    var json = JObject.Parse(File.ReadAllText(_game.GameFilePath));
                    json["autoUpdate"] = 0;
                    json["localization"]["locales"]["es"]["inline"]["assets"]["bannerImageUrl"] = source;
                    File.WriteAllText(_game.GameFilePath, json.ToString());
                    _game.ReadProperties();
                }
                Reload(Source);
                yield return null;
                Assert.That(requests, Is.EqualTo(1), "Views must share the pending download.");
                Assert.That(image.sprite, Is.SameAs(previous), "No blank frame while the next variant loads.");
                Assert.That(other.sprite, Is.Not.Null);
                finish = true;
                var deadline = Time.realtimeSinceStartup + 20;
                while (image.sprite == previous && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(image.sprite.texture.width, Is.EqualTo(6));
                yield return null;
                Assert.That(other.sprite.texture.width, Is.EqualTo(6));
                finish = false;
                succeeded = false;
                Reload("https://example.invalid/missing.png");
                yield return null;
                Assert.That(image.sprite, Is.Not.Null);
                finish = true;
                deadline = Time.realtimeSinceStartup + 20;
                while ((image.sprite != null || other.sprite != null) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(image.sprite, Is.Null, "Settled failure uses the canonical fallback.");
                Assert.That(other.sprite, Is.Null, "A joining view must also settle after failure.");
            }
            finally
            {
                GameContentPresentation.Select(null);
                Object.DestroyImmediate(node);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void NewerImageSuccessSurvivesAnOlderSameUriManifestCompletion()
        {
            var context = Manifest(Source);
            _game.AutoUpdate = 0;
            var cache = new LocalizationCache(_game.GameDirectoryPath, _game.Id);
            var width = 2;
            IEnumerator Download(string source, string path, Action<FileDownloadResult> completed)
            {
                var startedWidth = width;
                yield return null;
                File.WriteAllBytes(path, ImageBytes(startedWidth, 3));
                completed(new FileDownloadResult(true, new Uri(source), true));
            }
            var images = new GameLocalizationAssets(_game, cache, Download);
            var older = images.Refresh(context.AssetCandidates("bannerImageUrl"), Target, null);
            Assert.That(older.MoveNext(), Is.True);
            var oldDownload = (IEnumerator)older.Current;
            Assert.That(oldDownload.MoveNext(), Is.True);
            width = 4;
            context = Manifest(Source);
            _game.AutoUpdate = 0;
            Run(images.Refresh(context.AssetCandidates("bannerImageUrl"), Target, null));
            Run(oldDownload);
            Run(older);
            var sprite = images.Available(context.AssetCandidates("bannerImageUrl"), Target);
            try { Assert.That(sprite.texture.width, Is.EqualTo(4)); }
            finally { GameLocalizationAssets.Release(sprite); }
        }

        [Test]
        public void ReplacingPackageRevokesCatalogOwnerEvenWhenDirectoryIsUnchanged()
        {
            Manifest(Source);
            var catalogs = new GameLocalizationCatalogs(_game);
            Assert.That(catalogs.MatchesInstallation, Is.True);
            LocalizationCache.Invalidate(_game.GameDirectoryPath);
            Assert.That(catalogs.MatchesInstallation, Is.False);
            Assert.That(new GameLocalizationCatalogs(_game).MatchesInstallation, Is.True);
        }

        [Test]
        public void ExternalRulebookIsSelectedAsLinkWithoutCreatingCacheFiles()
        {
            _game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""es"",""locales"":{""es"":{""inline"":{""assets"":{""rulesUrl"":""https://example.invalid/reglas""}}}}}").Value;
            var original = _game.RulesUrl;
            Assert.That(GameContentPresentation.RulesUri(_game).AbsoluteUri, Is.EqualTo("https://example.invalid/reglas"));
            Assert.That(_game.RulesUrl, Is.EqualTo(original));
            Assert.That(Directory.GetFiles(_game.GameDirectoryPath, "*", SearchOption.AllDirectories), Is.Empty);
        }
    }
}
