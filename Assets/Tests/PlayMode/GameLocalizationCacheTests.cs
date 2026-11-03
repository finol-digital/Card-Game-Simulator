/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Text;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class GameLocalizationCacheTests
    {
        private string _root;
        private string _relocated;
        private static readonly Uri Source = new("https://example.invalid/es.json");
        private static readonly Uri NewOrigin = new("https://cdn.example.invalid/new/es.json");
        private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
        private static bool Usable(byte[] bytes) => bytes.Length > 0;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cgs-cache-" + Guid.NewGuid().ToString("N"));
            _relocated = _root + "-imported";
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var root in new[] { _root, _relocated })
            {
                LocalizationCache.Invalidate(root);
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [TestCase(null)]
        [TestCase("/assets/cardImages/1")]
        public void LaterInitiatedSuccessSurvivesOlderCompletionReopenAndRelocation(string target)
        {
            var cache = new LocalizationCache(_root, "canonical game");
            var older = cache.Begin("ES", Source, target);
            var newer = cache.Begin("es", Source, target);
            Assert.That(cache.Publish(newer, Bytes("new"), NewOrigin, Usable, new JObject { ["etag"] = "new" }), Is.True);
            var statePath = Path.Combine(_root, "localization", "state.json");
            var before = File.ReadAllText(statePath);
            Assert.That(cache.Publish(older, Bytes("old"), Source, Usable, new JObject { ["etag"] = "old" }), Is.False);
            Assert.That(File.ReadAllText(statePath), Is.EqualTo(before));
            var reopened = new LocalizationCache(_root, "canonical game");
            var entry = reopened.Read(reopened.Begin("es", Source, target));
            Assert.That(Encoding.UTF8.GetString(entry.Payload), Is.EqualTo("new"));
            Assert.That(entry.FinalDocumentUri, Is.EqualTo(NewOrigin));
            Assert.That(entry.Ticket, Is.EqualTo(2));
            LocalizationFiles.CopyPackageSnapshot(_root, _relocated);
            var imported = new LocalizationCache(_relocated, "canonical game");
            var importedEntry = imported.Read(imported.Begin("es", Source, target));
            Assert.That(Encoding.UTF8.GetString(importedEntry.Payload), Is.EqualTo("new"));
            Assert.That(importedEntry.FinalDocumentUri, Is.EqualTo(NewOrigin));
            Assert.That(File.ReadAllText(Path.Combine(_relocated, "localization", "state.json")), Does.Not.Contain(_root));
        }

        [Test]
        public void ExportZipIncludesPublishedCatalogAndOriginButNotPendingDownloads()
        {
            var cache = new LocalizationCache(_root, "game");
            var request = cache.Begin("es", Source);
            Assert.That(cache.Publish(request, Bytes("published"), NewOrigin, Usable), Is.True);
            var staging = Path.Combine(_root, "localization", "staging");
            Directory.CreateDirectory(staging);
            File.WriteAllText(Path.Combine(staging, "pending.download"), "incomplete");
            File.WriteAllText(Path.Combine(_root, "cgs.json.pending.download"), "incomplete");
            LocalizationFiles.CopyPackageSnapshot(_root, _relocated);
            var zipDirectory = Path.Combine(_root, "export");
            UnityFileMethods.CreateZip(_relocated, zipDirectory, "game.cgs.zip");
            Directory.Delete(_relocated, true);
            UnityFileMethods.ExtractZip(Path.Combine(zipDirectory, "game.cgs.zip"), _relocated);
            var reopened = new LocalizationCache(_relocated, "game");
            var entry = reopened.Read(reopened.Begin("es", Source));
            Assert.That(Encoding.UTF8.GetString(entry.Payload), Is.EqualTo("published"));
            Assert.That(entry.FinalDocumentUri, Is.EqualTo(NewOrigin));
            Assert.That(Directory.GetFiles(_relocated, "*.download", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void FailedNewerRequestDoesNotSuppressOlderSuccessOrReplaceLastGood()
        {
            var cache = new LocalizationCache(_root, "game");
            var older = cache.Begin("es", Source);
            var newer = cache.Begin("es", Source);
            Assert.That(cache.Publish(newer, Array.Empty<byte>(), NewOrigin, Usable), Is.False);
            Assert.That(cache.Publish(older, Bytes("good"), Source, Usable), Is.True);
            var failedRefresh = cache.Begin("es", Source);
            Assert.That(cache.Publish(failedRefresh, Bytes("bad"), NewOrigin, _ => false), Is.False);
            Assert.That(Encoding.UTF8.GetString(cache.Read(failedRefresh).Payload), Is.EqualTo("good"));
            Assert.That(cache.Publish(older, Bytes("duplicate"), NewOrigin, Usable), Is.False);
        }

        [Test]
        public void ReplacementRevokesOldOwnersAndDifferentKeysPublishIndependently()
        {
            var old = new LocalizationCache(_root, "game");
            var pending = old.Begin("es", Source);
            LocalizationCache.Invalidate(_root);
            var current = new LocalizationCache(_root, "game");
            var spanish = current.Begin("es", Source);
            var japanese = current.Begin("ja", Source);
            Assert.That(current.Publish(japanese, Bytes("ja"), Source, Usable), Is.True);
            Assert.That(current.Publish(spanish, Bytes("es"), Source, Usable), Is.True);
            Assert.That(old.Publish(pending, Bytes("stale"), Source, Usable), Is.False);
            Assert.That(Encoding.UTF8.GetString(current.Read(spanish).Payload), Is.EqualTo("es"));
            Assert.That(Encoding.UTF8.GetString(current.Read(japanese).Payload), Is.EqualTo("ja"));
            var otherGame = new LocalizationCache(_root, "different canonical game");
            Assert.That(otherGame.Read(otherGame.Begin("es", Source)), Is.Null);
        }

        [Test]
        public void TamperedPayloadOrEscapingMappingIsUnavailable()
        {
            var cache = new LocalizationCache(_root, "game");
            var request = cache.Begin("es", Source);
            Assert.That(cache.Publish(request, Bytes("good"), Source, Usable), Is.True);
            var entry = cache.Read(request);
            File.WriteAllText(entry.FilePath, "changed externally");
            Assert.That(cache.Read(request), Is.Null);
            var statePath = Path.Combine(_root, "localization", "state.json");
            var state = JObject.Parse(File.ReadAllText(statePath));
            foreach (var property in ((JObject)state["entries"]).Properties())
                property.Value["file"] = "../../private.json";
            File.WriteAllText(statePath, state.ToString());
            Assert.That(cache.Read(request), Is.Null);
        }
    }
}
