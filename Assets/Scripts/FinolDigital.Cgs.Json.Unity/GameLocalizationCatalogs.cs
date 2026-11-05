/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExtensionMethods;

namespace FinolDigital.Cgs.Json.Unity
{
    public sealed class GameLocalizationCatalogs
    {
        private readonly UnityCardGame _game;
        private readonly string _root;
        private readonly LocalizationCache _cache;
        private readonly Func<string, string, Action<FileDownloadResult>, IEnumerator> _download;

        public GameLocalizationCatalogs(UnityCardGame game,
            Func<string, string, Action<FileDownloadResult>, IEnumerator> download = null)
        {
            _game = game;
            _root = game.GameDirectoryPath;
            _cache = new LocalizationCache(_root, game.Id);
            _download = download ?? ((uri, path, completed) =>
                UnityFileMethods.SaveUrlToFile(uri, path, completed: completed));
        }

        private GameLocalizationAssets _images;
        public GameLocalizationAssets Images => _images ??= new GameLocalizationAssets(_game, _cache, _download);

        public bool MatchesInstallation => _root == _game.GameDirectoryPath && _cache.IsCurrentOwner;

        private IEnumerable<(string locale, Uri uri)> Sources(string selectedLocale)
        {
            var manifest = _game.Localization;
            if (manifest == null || !manifest.IsUsable || manifest.Locales == null)
                yield break;
            foreach (var locale in LocalizationLocale.FallbackChain(selectedLocale, manifest.DefaultLocale))
            {
                var source = manifest.Locales.FirstOrDefault(pair => pair.Key.Equals(locale, StringComparison.OrdinalIgnoreCase)).Value;
                if (source?.Url == null)
                    continue;
                var uri = LocalizationOrigin.Resolve(source.Url, _game.DefinitionOrigin);
                if (uri != null)
                    yield return (locale, uri);
            }
        }

        public IReadOnlyDictionary<string, LocalizationCatalogDocument> Available(string selectedLocale)
        {
            var documents = new Dictionary<string, LocalizationCatalogDocument>(StringComparer.OrdinalIgnoreCase);
            foreach (var (locale, uri) in Sources(selectedLocale))
            {
                try
                {
                    byte[] bytes;
                    LocalizationOrigin origin;
                    if (uri.IsFile)
                    {
                        if (LocalizationFiles.ExistingPhysicalPath(uri.LocalPath, _root) == null)
                            continue;
                        bytes = File.ReadAllBytes(uri.LocalPath);
                        origin = LocalizationOrigin.Bundled(_root, uri.LocalPath, path => LocalizationFiles.ExistingPhysicalPath(path, _root));
                    }
                    else
                    {
                        var cached = _cache.Read(_cache.Begin(locale, uri));
                        if (cached == null)
                            continue;
                        bytes = cached.Payload;
                        origin = cached.FinalDocumentUri == null ? null : LocalizationOrigin.Remote(cached.FinalDocumentUri);
                    }
                    var catalog = Parse(bytes);
                    if (catalog != null)
                        documents[locale] = new LocalizationCatalogDocument(catalog, origin);
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Game-content catalog unavailable: " + error.Message);
                }
            }
            return documents;
        }

        public IEnumerator Refresh(string selectedLocale, Action published)
        {
            if (_game.AutoUpdate < 0 || !MatchesInstallation)
                yield break;
            // Capture sources now; a reload while yielding cannot repurpose this request.
            var definition = _game.DefinitionGeneration;
            foreach (var (locale, uri) in Sources(selectedLocale).ToArray())
            {
                if (_game.AutoUpdate < 0 || definition != _game.DefinitionGeneration || !MatchesInstallation)
                    yield break;
                if (uri.IsFile)
                    continue;
                var request = TryBegin(locale, uri);
                if (request == null)
                    yield break;
                var cached = _cache.Read(request);
                if (cached != null && _game.AutoUpdate > 0 &&
                    (DateTime.UtcNow - cached.PublishedUtc).TotalDays < _game.AutoUpdate)
                    continue;
                var stagingDirectory = Path.Combine(_root, "localization", "staging");
                if (LocalizationFiles.ExistingPhysicalPath(_root, _root) == null)
                    yield break;
                if (!CreateStaging(stagingDirectory))
                    yield break;
                var path = Path.Combine(stagingDirectory, Guid.NewGuid().ToString("N") + ".download");
                var didPublish = false;
                try
                {
                    FileDownloadResult result = null;
                    yield return _download(uri.AbsoluteUri, path, value => result = value);
                    if (result?.Succeeded == true)
                        didPublish = PublishFile(request, path, result);
                }
                finally
                {
                    TryDelete(path);
                }
                if (didPublish)
                    published?.Invoke();
            }
        }

        private LocalizationCache.Request TryBegin(string locale, Uri uri)
        {
            try
            {
                return _cache.Begin(locale, uri);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Game-content cache request unavailable: " + error.Message);
                return null;
            }
        }

        private bool CreateStaging(string directory)
        {
            try
            {
                if (LocalizationFiles.ConfinedPath(directory, _root) == null)
                    return false;
                Directory.CreateDirectory(directory);
                return LocalizationFiles.ExistingPhysicalPath(directory, _root) != null;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Game-content cache staging unavailable: " + error.Message);
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not remove staged game-content download: " + error.Message);
            }
        }

        private bool PublishFile(LocalizationCache.Request request, string path, FileDownloadResult result)
        {
            try
            {
                return _cache.Publish(request, File.ReadAllBytes(path), result.FinalDocumentUri, bytes => Parse(bytes) != null);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Game-content catalog refresh failed: " + error.Message);
                return false;
            }
        }

        private static LocalizationCatalog Parse(byte[] bytes)
        {
            return LocalizationJson.ReadCatalog(new UTF8Encoding(false, true).GetString(bytes).TrimStart('\ufeff')).Value;
        }
    }
}
