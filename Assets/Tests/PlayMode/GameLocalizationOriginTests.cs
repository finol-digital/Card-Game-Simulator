/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class GameLocalizationOriginTests
    {
        private readonly List<string> _directories = new();

#if UNITY_EDITOR_LINUX
        [DllImport("libc", EntryPoint = "symlink", SetLastError = true)]
        private static extern int CreateSymbolicLink(string target, string link);

        [Test]
        public void BundledResourcesRejectSymlinkAncestors()
        {
            var game = Game();
            var outside = Game();
            File.WriteAllText(game.GameFilePath, new JObject { ["name"] = game.Name }.ToString());
            File.WriteAllText(Path.Combine(outside.GameDirectoryPath, "es.json"), "{}");
            Assert.That(CreateSymbolicLink(outside.GameDirectoryPath, Path.Combine(game.GameDirectoryPath, "locales")), Is.Zero);
            game.ReadProperties();
            Assert.That(LocalizationOrigin.Resolve("locales/es.json", game.DefinitionOrigin), Is.Null);
        }
        [Test]
        public void TrustedRootBelowLinkedStorageAncestorSupportsOriginsAndCaches()
        {
            var physical = Game().GameDirectoryPath;
            var container = Game().GameDirectoryPath;
            var alias = Path.Combine(container, "linked-storage");
            Assert.That(CreateSymbolicLink(physical, alias), Is.Zero);
            var root = Path.Combine(alias, "game");
            Directory.CreateDirectory(root);
            var document = Path.Combine(root, "cgs.json");
            File.WriteAllText(document, "{}");
            var remote = FileDownloadResult.FromResponse("https://example.invalid/game/cgs.json", false);
            Assert.That(LocalizationFiles.RecordDefinitionOrigin(root, document, remote), Is.True);
            Assert.That(LocalizationFiles.ReadDefinitionOrigin(root, document).RemoteUrl, Is.EqualTo(remote.FinalDocumentUri));
            var cache = new LocalizationCache(root, "game");
            var request = cache.Begin("es", new Uri("https://example.invalid/es.json"));
            Assert.That(cache.Publish(request, new byte[] { 1 }, null, bytes => bytes.Length > 0), Is.True);
            Assert.That(cache.Read(request).Payload, Is.EqualTo(new byte[] { 1 }));
            File.WriteAllText(Path.Combine(root, "es.json"), "{}");
            var bundled = LocalizationOrigin.Bundled(root, document, path => LocalizationFiles.ExistingPhysicalPath(path, root));
            Assert.That(LocalizationOrigin.Resolve("es.json", bundled), Is.Not.Null);
            Assert.That(CreateSymbolicLink(physical, Path.Combine(root, "escape")), Is.Zero);
            Assert.That(LocalizationFiles.ConfinedPath(Path.Combine(root, "escape", "new.json"), root), Is.Null);
            Directory.Delete(Path.Combine(root, "escape"));
            Directory.Delete(alias);
        }

#endif

        private UnityCardGame Game()
        {
            var game = new UnityCardGame(null, "Origin-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(game.GameDirectoryPath);
            _directories.Add(game.GameDirectoryPath);
            return game;
        }

        private static string Download(UnityCardGame game)
        {
            var path = Path.Combine(game.GameDirectoryPath, "download.tmp");
            File.WriteAllText(path, new JObject
            {
                ["name"] = game.Name,
                ["autoUpdateUrl"] = "https://updates.example.invalid/different/cgs.json",
                ["localization"] = new JObject { ["defaultLocale"] = "en", ["locales"] = new JObject { ["es"] = new JObject { ["url"] = "locales/es.json" } } }
            }.ToString());
            return path;
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var directory in _directories)
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            _directories.Clear();
        }

        [Test]
        public void FinalResponseOriginSurvivesManifestOverrideRestartAndRelocation()
        {
            var game = Game();
            var final = "https://cdn.example.invalid/redirected/game/cgs.json";
            Assert.That(game.InstallDownloadedDefinition(Download(game), FileDownloadResult.FromResponse(final, false)), Is.True);
            game.ReadProperties();
            _directories.Add(game.GameDirectoryPath);
            Assert.That(game.HasReadProperties, Is.True, game.Error);
            Assert.That(game.DefinitionOrigin.RemoteUrl.AbsoluteUri, Is.EqualTo(final));
            Assert.That(game.AutoUpdateUrl.Host, Is.EqualTo("updates.example.invalid"));
            var restarted = new UnityCardGame(null, game.Id);
            restarted.ReadProperties();
            Assert.That(LocalizationOrigin.Resolve("locales/es.json", restarted.DefinitionOrigin).AbsoluteUri,
                Is.EqualTo("https://cdn.example.invalid/redirected/game/locales/es.json"));
            var relocated = Game().GameDirectoryPath;
            foreach (var path in Directory.GetFiles(game.GameDirectoryPath, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(game.GameDirectoryPath, path);
                var target = Path.Combine(relocated, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(path, target);
            }
            var origin = LocalizationFiles.ReadDefinitionOrigin(relocated, Path.Combine(relocated, "cgs.json"));
            Assert.That(origin.RemoteUrl.AbsoluteUri, Is.EqualTo(final));
            var state = File.ReadAllText(Path.Combine(relocated, "localization", "state.json"));
            Assert.That(state, Does.Not.Contain(game.GameDirectoryPath));
        }

        [Test]
        public void ProxyTransportNeverBecomesAnUpstreamBase()
        {
            var game = Game();
            var response = FileDownloadResult.FromResponse("https://cgs.games/api/proxy/cdn.example.invalid/a/cgs.json", true);
            Assert.That(response.FinalDocumentUri, Is.Null);
            Assert.That(game.InstallDownloadedDefinition(Download(game), response), Is.True);
            game.ReadProperties();
            _directories.Add(game.GameDirectoryPath);
            Assert.That(game.DefinitionOrigin, Is.Null);
            Assert.That(LocalizationOrigin.Resolve("locales/es.json", game.DefinitionOrigin), Is.Null);
            Assert.That(LocalizationOrigin.Resolve("https://cdn.example.invalid/es.json", game.DefinitionOrigin), Is.Not.Null);
        }

        [Test]
        public void ProxyFinalOriginSurvivesInstallAndResolvesRelativeCatalogs()
        {
            var game = Game();
            const string final = "https://cdn.example.invalid/redirected/game/cgs.json";
            var response = FileDownloadResult.FromResponse(
                "https://cgs.games/api/proxy/example.invalid/start", true, final);
            Assert.That(response.FinalDocumentUri.AbsoluteUri, Is.EqualTo(final));
            Assert.That(game.InstallDownloadedDefinition(Download(game), response), Is.True);
            game.ReadProperties();
            _directories.Add(game.GameDirectoryPath);
            Assert.That(LocalizationOrigin.Resolve("locales/es.json", game.DefinitionOrigin).AbsoluteUri,
                Is.EqualTo("https://cdn.example.invalid/redirected/game/locales/es.json"));
        }

        [TestCase(null)]
        [TestCase("/relative/cgs.json")]
        [TestCase("file:///tmp/cgs.json")]
        [TestCase("https://user:password@example.invalid/cgs.json")]
        [TestCase("https://example.invalid/\nspoof")]
        public void MissingOrInvalidProxyHeaderNeverSuppliesAnOrigin(string header)
        {
            Assert.That(FileDownloadResult.FromResponse(
                "https://cgs.games/api/proxy/example.invalid/cgs.json", true, header).FinalDocumentUri, Is.Null);
        }

        [TestCase("https://cgs.games.attacker.invalid/api/proxy/example.invalid/a")]
        [TestCase("http://cgs.games/api/proxy/example.invalid/a")]
        [TestCase("https://cgs.games:444/api/proxy/example.invalid/a")]
        [TestCase("https://cgs.games/other/example.invalid/a")]
        public void RedirectAwayFromTrustedProxyDoesNotAuthenticateHeader(string transport)
        {
            Assert.That(FileDownloadResult.FromResponse(transport, true,
                "https://cdn.example.invalid/cgs.json").FinalDocumentUri, Is.Null);
        }

        [Test]
        public void DirectDownloadsIgnoreProxyHeader()
        {
            const string direct = "https://example.invalid/cgs.json";
            Assert.That(FileDownloadResult.FromResponse(direct, false,
                "https://spoof.invalid/cgs.json").FinalDocumentUri.AbsoluteUri, Is.EqualTo(direct));
        }

        [Test]
        public void OlderBundledGameUsesActualLocationAndConfinesDecodedPaths()
        {
            var game = Game();
            File.WriteAllText(game.GameFilePath, new JObject { ["name"] = game.Name }.ToString());
            var catalog = Path.Combine(game.GameDirectoryPath, "es # 日本語.json");
            File.WriteAllText(catalog, "{}");
            game.ReadProperties();
            var uri = LocalizationOrigin.Resolve("es%20%23%20%E6%97%A5%E6%9C%AC%E8%AA%9E.json", game.DefinitionOrigin);
            Assert.That(uri, Is.Not.Null, "A percent-encoded filename must remain valid beside encoded Unicode.");
            Assert.That(uri.LocalPath, Is.EqualTo(catalog));
            Assert.That(LocalizationOrigin.Resolve("%2e%2e/outside.json", game.DefinitionOrigin), Is.Null);
            Assert.That(LocalizationOrigin.Resolve("file:///tmp/private.json", game.DefinitionOrigin), Is.Null);
            Assert.That(Directory.Exists(Path.Combine(game.GameDirectoryPath, "localization")), Is.False);
        }

        [Test]
        public void FailedDownloadOrInvalidDefinitionCannotReplaceInstalledDefinition()
        {
            var game = Game();
            File.WriteAllText(game.GameFilePath, new JObject { ["name"] = game.Name }.ToString());
            var before = File.ReadAllText(game.GameFilePath);
            var downloaded = Download(game);
            Assert.That(game.InstallDownloadedDefinition(downloaded, new FileDownloadResult(false, null, true)), Is.False);
            File.WriteAllText(downloaded, "{\"copyright\":\"Missing name\"}");
            Assert.That(game.InstallDownloadedDefinition(downloaded, FileDownloadResult.FromResponse("https://example.invalid/cgs.json", false)), Is.False);
            Assert.That(File.ReadAllText(game.GameFilePath), Is.EqualTo(before));
            Assert.That(Directory.Exists(Path.Combine(game.GameDirectoryPath, "localization")), Is.False);
        }

        [Test]
        public void EditingBundledDefinitionKeepsItsActualLocalOrigin()
        {
            var game = Game();
            File.WriteAllText(game.GameFilePath, new JObject { ["name"] = game.Name }.ToString());
            Assert.That(LocalizationFiles.RecordDefinitionOrigin(game.GameDirectoryPath, game.GameFilePath,
                new FileDownloadResult(true, null, false)), Is.True);
            File.WriteAllText(Path.Combine(game.GameDirectoryPath, "es.json"), "{}");
            File.WriteAllText(game.GameFilePath, new JObject { ["name"] = game.Name, ["copyright"] = "Edited locally" }.ToString());
            game.ReadProperties();
            Assert.That(LocalizationOrigin.Resolve("es.json", game.DefinitionOrigin).LocalPath,
                Is.EqualTo(Path.Combine(game.GameDirectoryPath, "es.json")));
        }

        [Test]
        public void OriginWriteFailureStillInstallsBaseDefinitionWithoutGuessingAnOrigin()
        {
            var game = Game();
            File.WriteAllText(Path.Combine(game.GameDirectoryPath, "localization"), "blocks optional metadata directory");
            var downloaded = Download(game);
            Assert.That(game.InstallDownloadedDefinition(downloaded,
                FileDownloadResult.FromResponse("https://example.invalid/game/cgs.json", false)), Is.True);
            Assert.That(File.Exists(game.GameFilePath), Is.True);
            game.ReadProperties();
            _directories.Add(game.GameDirectoryPath);
            Assert.That(game.HasReadProperties, Is.True, game.Error);
            Assert.That(game.DefinitionOrigin, Is.Null);
            Assert.That(LocalizationFiles.ReadDefinitionOrigin(game.GameDirectoryPath, game.GameFilePath), Is.Null);
        }

        [Test]
        public void OriginCannotBeReusedForAnUnrelatedDocument()
        {
            var game = Game();
            Assert.That(game.InstallDownloadedDefinition(Download(game), FileDownloadResult.FromResponse("https://example.invalid/a/cgs.json", false)), Is.True);
            File.WriteAllText(game.GameFilePath, new JObject { ["name"] = game.Name }.ToString());
            game.ReadProperties();
            Assert.That(game.HasReadProperties, Is.True);
            Assert.That(game.DefinitionOrigin, Is.Null);
        }
    }
}
