/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityExtensionMethods;
#if !UNITY_WEBGL
using System;
using System.IO;
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

        private static int ConcurrentQueueSize => 5;

        public static ImageQueueService Instance { get; } = new();

        private readonly ConcurrentQueue<UnityCard> _queue = new();

        private int _concurrentQueueCount;

        public void Enqueue(UnityCard unityCard)
        {
            _queue.Enqueue(unityCard);
        }

        public void ProcessQueue(MonoBehaviour coroutineRunner)
        {
            if (_concurrentQueueCount >= ConcurrentQueueSize || !_queue.TryDequeue(out var unityCard))
                return;

            _concurrentQueueCount++;
            coroutineRunner.StartCoroutine(LoadImageSprite(unityCard));
        }

        private IEnumerator LoadImageSprite(UnityCard unityCard)
        {
            Sprite newSprite = null;

            try
            {
#if !UNITY_WEBGL
                yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                    UnityFileMethods.CreateAndOutputSpriteFromImageFile(unityCard.ImageFilePath,
                        unityCard.ImageWebUrl.Replace(" ", "%20"))
                    , output => newSprite = output);
                if (newSprite == null)
                    Debug.LogWarning(
                        $"Failed to load image for card: {unityCard.Name} ({unityCard.Id}) at {unityCard.ImageFilePath}");
                try
                {
                    var fileInfo = new FileInfo(unityCard.ImageFilePath);
                    if (fileInfo.Exists && fileInfo.Length > MaxImageFileSizeBytes)
                        Debug.LogWarning(string.Format(SizeWarningMessage, unityCard.Name, unityCard.Id));
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"Failed to check image file size for card: {unityCard.Name} ({unityCard.Id}): {e}");
                }
#else
                var url = unityCard.ImageWebUrl;
                if (url.StartsWith("https://") && !url.StartsWith("https://cgs.games/api/proxy/"))
                {
                    url = "https://cgs.games/api/proxy/" + url[8..];
                    Debug.Log("CGS Games WebGL url : " + url);
                }

                yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                    UnityFileMethods.CreateAndOutputSpriteFromImageFile(url)
                    , output => newSprite = output);
#endif

                unityCard.OnLoadImage(newSprite);
            }
            finally
            {
                _concurrentQueueCount--;
            }
        }
    }
}
