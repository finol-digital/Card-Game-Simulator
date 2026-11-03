/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace FinolDigital.Cgs.Json.Unity
{
    public sealed class GameLocalizationAssets
    {
        private readonly UnityCardGame _game;
        private readonly string _root;
        private readonly LocalizationCache _cache;
        private readonly Func<string, string, Action<FileDownloadResult>, IEnumerator> _download;
        private readonly HashSet<string> _pending = new();

        public GameLocalizationAssets(UnityCardGame game, LocalizationCache cache,
            Func<string, string, Action<FileDownloadResult>, IEnumerator> download = null)
        {
            _game = game;
            _root = game.GameDirectoryPath;
            _cache = cache;
            _download = download ?? ((uri, path, completed) => UnityFileMethods.SaveUrlToFile(uri, path, completed: completed));
        }

        public static string Target(params string[] path) => "/assets/" +
            string.Join("/", path.Select(part => part.Replace("~", "~0").Replace("/", "~1")));

        // The caller owns the returned sprite and texture. Canonical image objects are untouched.
        public Sprite Available(IReadOnlyList<LocalizationResourceCandidate> candidates, string target, bool orient = false)
        {
            foreach (var candidate in candidates)
            {
                try
                {
                    var bytes = candidate.Uri.IsFile
                        ? LocalizationFiles.ExistingPhysicalPath(candidate.Uri.LocalPath) == null ? null : File.ReadAllBytes(candidate.Uri.LocalPath)
                        : _cache.Read(_cache.Begin(candidate.Locale, candidate.Uri, target))?.Payload;
                    if (bytes == null)
                        continue;
                    var sprite = UnityFileMethods.CreateSprite(bytes);
                    if (sprite != null)
                        return orient ? Orient(sprite, _game) : sprite;
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Localized image unavailable: " + error.Message);
                }
            }
            return null;
        }

        public IEnumerator Refresh(IReadOnlyList<LocalizationResourceCandidate> candidates, string target, Action published)
        {
            var definition = _game.DefinitionGeneration;
            foreach (var candidate in candidates)
            {
                if (_game.AutoUpdate < 0 || _game.DefinitionGeneration != definition || _root != _game.GameDirectoryPath)
                    yield break;
                if (candidate.Uri.IsFile)
                    continue;
                var identity = target + "\n" + candidate.Locale + "\n" + candidate.Uri.AbsoluteUri + "\n" + definition;
                if (!_pending.Add(identity))
                {
                    // A second view shares the in-flight result but must not report
                    // completion while the first view's request is still loading.
                    while (_pending.Contains(identity) && _game.DefinitionGeneration == definition)
                        yield return null;
                    continue;
                }
                try
                {
                    var request = Begin(candidate, target);
                    if (request == null)
                        continue;
                    var cached = _cache.Read(request);
                    if (cached != null && _game.AutoUpdate > 0 &&
                        (DateTime.UtcNow - cached.PublishedUtc).TotalDays < _game.AutoUpdate)
                        continue;
                    var temporary = StagingPath();
                    if (temporary == null)
                        continue;
                    try
                    {
                        FileDownloadResult result = null;
                        yield return _download(candidate.Uri.AbsoluteUri, temporary, value => result = value);
                        if (result?.Succeeded == true && Publish(request, temporary, result))
                            published?.Invoke();
                    }
                    finally
                    {
                        DeleteStaging(temporary);
                    }
                }
                finally
                {
                    _pending.Remove(identity);
                }
            }
        }

        private LocalizationCache.Request Begin(LocalizationResourceCandidate candidate, string target)
        {
            try { return _cache.Begin(candidate.Locale, candidate.Uri, target); }
            catch (Exception error)
            {
                Debug.LogWarning("Localized image request unavailable: " + error.Message);
                return null;
            }
        }

        private string StagingPath()
        {
            try
            {
                if (LocalizationFiles.ExistingPhysicalPath(_root) == null)
                    return null;
                var directory = Path.Combine(_root, "localization", "staging");
                Directory.CreateDirectory(directory);
                return LocalizationFiles.ExistingPhysicalPath(directory) == null ? null :
                    Path.Combine(directory, Guid.NewGuid().ToString("N") + ".download");
            }
            catch (Exception error)
            {
                Debug.LogWarning("Localized image staging unavailable: " + error.Message);
                return null;
            }
        }

        private bool Publish(LocalizationCache.Request request, string path, FileDownloadResult result)
        {
            try
            {
                return _cache.Publish(request, File.ReadAllBytes(path), result.FinalDocumentUri, bytes =>
                {
                    var sprite = UnityFileMethods.CreateSprite(bytes);
                    if (sprite == null)
                        return false;
                    Release(sprite);
                    return true;
                });
            }
            catch (Exception error)
            {
                Debug.LogWarning("Localized image publication failed: " + error.Message);
                return false;
            }
        }

        private static void DeleteStaging(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception error) { Debug.LogWarning("Localized image staging cleanup failed: " + error.Message); }
        }

        public static void Release(Sprite sprite)
        {
            if (sprite == null)
                return;
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }

        private static Sprite Orient(Sprite sprite, UnityCardGame game)
        {
            var texture = sprite.texture;
            var size = game.CardSize;
            if (!((texture.width > texture.height && Math.Abs(size.X) < Math.Abs(size.Y)) ||
                (texture.width < texture.height && Math.Abs(size.X) > Math.Abs(size.Y))))
                return sprite;
            try
            {
                var rotated = UnityFileMethods.Rotate90Clockwise(texture);
                var result = Sprite.Create(rotated, new Rect(0, 0, rotated.width, rotated.height), new Vector2(0.5f, 0.5f));
                Release(sprite);
                return result;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Localized image orientation unavailable: " + error.Message);
                return sprite;
            }
        }
    }
}
