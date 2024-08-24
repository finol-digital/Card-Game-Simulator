/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using UnityExtensionMethods;

namespace FinolDigital.Cgs.CardGameDef.Unity
{
    public class ImageQueueService
    {
        public const string SizeWarningMessage = "WARNING: Card image for {0} ({1}) is too large! \n" +
                                                 "Recommended Action: Delete the Card, compress the image file using a tool like " +
                                                 "https://www.iloveimg.com/compress-image " +
                                                 ", then re-import.";

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
#if UNITY_WEBGL
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(unityCard.ImageWebUrl)
                , output => newSprite = output);
#else
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(unityCard.ImageFilePath,
                    unityCard.ImageWebUrl.Replace(" ", "%20"))
                , output => newSprite = output);
            var fileInfo = new FileInfo(unityCard.ImageFilePath);
            if (fileInfo.Exists && fileInfo.Length > 2_000_000)
            {
                var sizeWarningMessage = string.Format(SizeWarningMessage, unityCard.Name, unityCard.Id);
                Debug.LogError(sizeWarningMessage);
            }
#endif
            unityCard.OnLoadImage(newSprite);
            _concurrentQueueCount--;
        }
    }
}
