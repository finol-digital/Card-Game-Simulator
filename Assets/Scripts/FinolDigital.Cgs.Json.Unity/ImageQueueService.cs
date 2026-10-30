/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityExtensionMethods;
#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace FinolDigital.Cgs.Json.Unity
{
    public class ImageQueueService
    {
        public const string SizeWarningMessage = "WARNING: Card image for {0} ({1}) is too large! \n" +
                                                 "Recommended Action: Delete the Card, compress the image file using a tool like " +
                                                 "https://www.iloveimg.com/compress-image " +
                                                 ", then re-import.";

        public const int MaxImageFileSizeBytes = 2_000_000; // 2 MB
        private const int ConcurrentQueueSize = 5;
        private const int RequestTimeoutSeconds = 15;
        private const string LoadErrorMessage = "Failed to load card image: ";
        private const string CacheErrorMessage = "Failed to cache card image: ";

        public static ImageQueueService Instance { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = new ImageQueueService();
        }

        private readonly LinkedList<UnityCard> _queue = new();
        private readonly Dictionary<UnityCard, LinkedListNode<UnityCard>> _pending = new();
        private readonly HashSet<UnityCard> _active = new();

        public void Enqueue(UnityCard unityCard)
        {
            if (unityCard == null || _pending.ContainsKey(unityCard) || _active.Contains(unityCard))
                return;

            _pending.Add(unityCard, _queue.AddLast(unityCard));
        }

        public void CancelPending(UnityCard unityCard)
        {
            if (unityCard == null || !_pending.TryGetValue(unityCard, out var node))
                return;

            _queue.Remove(node);
            _pending.Remove(unityCard);
            unityCard.OnLoadImage(null);
        }

        public void ProcessQueue(MonoBehaviour coroutineRunner)
        {
            if (coroutineRunner == null || !coroutineRunner.isActiveAndEnabled)
                return;

            while (_active.Count < ConcurrentQueueSize && _queue.First != null)
            {
                var card = _queue.First.Value;
                _queue.RemoveFirst();
                _pending.Remove(card);
                if (!card.HasImageDisplays)
                {
                    card.OnLoadImage(null);
                    continue;
                }

                _active.Add(card);
                coroutineRunner.StartCoroutine(RunLoad(card));
            }
        }

        // Drive nested iterators ourselves so cancellation and exceptions always dispose requests
        // and release the slot, even when a nested file/decoder coroutine fails.
        private IEnumerator RunLoad(UnityCard card)
        {
            var iterators = new Stack<IEnumerator>();
            try
            {
                iterators.Push(LoadImageSprite(card));
                while (iterators.Count > 0 && card.HasImageDisplays)
                {
                    var iterator = iterators.Peek();
                    bool moved;
                    object current = null;
                    try
                    {
                        moved = iterator.MoveNext();
                        if (moved)
                            current = iterator.Current;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(LoadErrorMessage + card.Id + ": " + exception.Message);
                        yield break;
                    }

                    if (!moved)
                    {
                        iterators.Pop();
                        (iterator as IDisposable)?.Dispose();
                    }
                    else if (current is IEnumerator nested)
                        iterators.Push(nested);
                    else if (current is AsyncOperation operation)
                    {
                        while (!operation.isDone && card.HasImageDisplays)
                            yield return null;
                    }
                    else
                        yield return current;
                }
            }
            finally
            {
                while (iterators.Count > 0)
                {
                    try
                    {
                        (iterators.Pop() as IDisposable)?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(LoadErrorMessage + card.Id + ": " + exception.Message);
                    }
                }

                _active.Remove(card);
                card.OnLoadImage(null);
                if (!card.HasImageDisplays)
                    card.ImageSprite = null;
            }
        }

        protected virtual IEnumerator LoadImageSprite(UnityCard card)
        {
            Sprite sprite = null;
            try
            {
                if (File.Exists(card.ImageFilePath))
                {
                    var invalidImage = false;
                    yield return UnityFileMethods.CreateSpriteFromFileAsync(card.ImageFilePath,
                        value => sprite = value, () => invalidImage = true);
                    if (sprite != null)
                        yield break;
                    // Never retain a previous HTML error page or incomplete image as a cache hit.
                    // A failed read alone does not mean a locally imported file is corrupt.
                    if (invalidImage)
                        TryDeleteCacheFile(card.ImageFilePath);
                }

                if (!Uri.TryCreate(card.ImageWebUrl, UriKind.Absolute, out var uri))
                    yield break;

                if (uri.IsFile)
                {
                    // Browser imports live in the virtual filesystem, outside fetch's file: access.
#if UNITY_WEBGL
                    var bytes = File.ReadAllBytes(uri.LocalPath);
#else
                    var loadTask = Task.Run(() => File.ReadAllBytes(uri.LocalPath));
                    while (!loadTask.IsCompleted)
                        yield return null;
                    if (loadTask.IsFaulted)
                    {
                        Debug.LogWarning(LoadErrorMessage + card.Id + ": " + loadTask.Exception);
                        yield break;
                    }
                    var bytes = loadTask.Result;
#endif
                    sprite = UnityFileMethods.CreateSprite(bytes);
                    if (sprite != null)
                        CacheImage(card, bytes);
                    yield break;
                }

                var url = uri.AbsoluteUri;
                var canProxy = uri.Scheme == Uri.UriSchemeHttps &&
                               !url.StartsWith(UnityFileMethods.CgsGamesProxyPrefix, StringComparison.Ordinal);
                for (var attempt = 0; attempt < (canProxy ? 2 : 1); attempt++)
                {
                    var requestUrl = attempt == 0 ? url : UnityFileMethods.ToCgsGamesProxyUrl(url);
                    using var request = CreateImageRequest(requestUrl);
                    request.timeout = RequestTimeoutSeconds;
                    try
                    {
                        yield return request.SendWebRequest();
                        if (request.result != UnityWebRequest.Result.Success)
                            continue;

                        var bytes = request.downloadHandler.data;
                        sprite = UnityFileMethods.CreateSprite(bytes);
                        if (sprite == null)
                            continue;

                        // Cache only decoded images, on both native and browser clients.
                        CacheImage(card, bytes);
                        yield break;
                    }
                    finally
                    {
                        if (!request.isDone)
                            request.Abort();
                    }
                }

                Debug.LogWarning(LoadErrorMessage + card.Id + " at " + url);
            }
            finally
            {
                card.OnLoadImage(sprite);
            }
        }

        protected virtual UnityWebRequest CreateImageRequest(string url) => UnityWebRequest.Get(url);

        private static void TryDeleteCacheFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(CacheErrorMessage + path + ": " + exception.Message);
            }
        }

        private static void CacheImage(UnityCard card, byte[] bytes)
        {
            if (bytes.Length > MaxImageFileSizeBytes)
                Debug.LogWarning(string.Format(SizeWarningMessage, card.Name, card.Id));
            var temporaryPath = card.ImageFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(card.ImageFilePath));
                File.WriteAllBytes(temporaryPath, bytes);
                if (File.Exists(card.ImageFilePath))
                    File.Replace(temporaryPath, card.ImageFilePath, null);
                else
                    File.Move(temporaryPath, card.ImageFilePath);
            }
            catch (Exception exception)
            {
                // A full or unavailable cache must not prevent displaying a downloaded image.
                Debug.LogWarning(CacheErrorMessage + card.Id + ": " + exception.Message);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    TryDeleteCacheFile(temporaryPath);
            }
        }
    }
}
