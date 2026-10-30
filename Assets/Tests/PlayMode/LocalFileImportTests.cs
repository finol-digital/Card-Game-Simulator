/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class LocalFileImportTests
    {
        private sealed class FileOnlyQueue : ImageQueueService
        {
            protected override UnityWebRequest CreateImageRequest(string url)
            {
                Assert.Fail("Local image URLs must use the filesystem, not a network request.");
                return null;
            }
        }

        private sealed class RetryQueue : ImageQueueService
        {
            public readonly List<string> RequestedUrls = new();
            public string FirstResponseUrl;
            public string ImageFileUrl;

            protected override UnityWebRequest CreateImageRequest(string url)
            {
                RequestedUrls.Add(url);
                return UnityWebRequest.Get(RequestedUrls.Count == 1 ? FirstResponseUrl : ImageFileUrl);
            }
        }

        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Application.temporaryCachePath, "LocalImport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [UnityTest]
        public IEnumerator CopiesSelectedFileWithSpacesPlusAndUnicodeWithoutNetworkRequests()
        {
            var source = Path.Combine(_directory, "card + 日本語.png");
            var target = Path.Combine(_directory, "saved", "card.png");
            var bytes = new byte[] { 0, 255, 80, 75 };
            File.WriteAllBytes(source, bytes);

            yield return UnityFileMethods.SaveUrlToFile(new Uri(source).AbsoluteUri, target);

            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(target));
            yield return UnityFileMethods.SaveUrlToFile(new Uri(target).AbsoluteUri, target);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(target));
        }

        [UnityTest]
        public IEnumerator LoadsImportedCardImageWithoutRemoteUrl()
        {
            var game = new UnityCardGame(null, "LocalCard-" + Guid.NewGuid().ToString("N"));
            var card = new UnityCard(game, "First_Card", "First Card", "Browser Cards", null, false);
            Directory.CreateDirectory(Path.GetDirectoryName(card.ImageFilePath));
            var texture = new Texture2D(2, 3);
            File.WriteAllBytes(card.ImageFilePath, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            try
            {
                Assert.IsTrue(string.IsNullOrEmpty(card.ImageWebUrl));
                var loader = typeof(ImageQueueService).GetMethod("LoadImageSprite",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                yield return (IEnumerator)loader.Invoke(new FileOnlyQueue(), new object[] { card });
                Assert.IsNotNull(card.ImageSprite);
                Assert.AreEqual(2, card.ImageSprite.texture.width);
                Assert.AreEqual(3, card.ImageSprite.texture.height);
            }
            finally
            {
                card.ImageSprite = null;
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        [UnityTest]
        public IEnumerator InvalidCachedCardImageFallsBackToItsUrl()
        {
            var game = new UnityCardGame(null, "DamagedCard-" + Guid.NewGuid().ToString("N"));
            var source = Path.Combine(_directory, "fallback.png");
            var texture = new Texture2D(3, 2);
            File.WriteAllBytes(source, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            var card = new UnityCard(game, "Card", "Card", "Set", null, false)
            {
                ImageWebUrl = new Uri(source).AbsoluteUri
            };
            Directory.CreateDirectory(Path.GetDirectoryName(card.ImageFilePath));
            File.WriteAllBytes(card.ImageFilePath, Array.Empty<byte>());
            try
            {
                var loader = typeof(ImageQueueService).GetMethod("LoadImageSprite",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                yield return (IEnumerator)loader.Invoke(new FileOnlyQueue(), new object[] { card });
                CollectionAssert.AreEqual(File.ReadAllBytes(source), File.ReadAllBytes(card.ImageFilePath),
                    "The damaged cache entry must be replaced with the downloaded image.");
                Assert.IsNotNull(card.ImageSprite, "The fallback URL must still be loaded.");
                Assert.AreEqual(3, card.ImageSprite.texture.width);
                Assert.AreEqual(2, card.ImageSprite.texture.height);

                card.ImageSprite = null;
                File.Delete(source);
                yield return (IEnumerator)loader.Invoke(new FileOnlyQueue(), new object[] { card });
                Assert.IsNotNull(card.ImageSprite, "Revisiting must use the cache even if the source is unavailable.");
            }
            finally
            {
                card.ImageSprite = null;
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

#if !UNITY_WEBGL
        [UnityTest]
        public IEnumerator FailedReadPreservesLocallyImportedImage()
        {
            var game = new UnityCardGame(null, "LockedCard-" + Guid.NewGuid().ToString("N"));
            var card = new UnityCard(game, "Card", "Card", "Set", null, false);
            Directory.CreateDirectory(Path.GetDirectoryName(card.ImageFilePath));
            var texture = new Texture2D(2, 3);
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(card.ImageFilePath, bytes);
            UnityEngine.Object.Destroy(texture);
            try
            {
                using (File.Open(card.ImageFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    LogAssert.Expect(LogType.Warning, new Regex("CreateSpriteFromFileAsync::ReadFailed:"));
                    var loader = typeof(ImageQueueService).GetMethod("LoadImageSprite",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    yield return (IEnumerator)loader.Invoke(new FileOnlyQueue(), new object[] { card });
                }

                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(card.ImageFilePath));
            }
            finally
            {
                card.ImageSprite = null;
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        [UnityTest]
        public IEnumerator FailedHttpsRequestUsesProxyAndCachesSuccessfulImage()
        {
            yield return VerifyProxyFallback(new Uri(Path.Combine(_directory, "missing.png")).AbsoluteUri);
        }

        [UnityTest]
        public IEnumerator MalformedWebpResponseUsesProxyAndCachesSuccessfulImage()
        {
            var malformedImage = Path.Combine(_directory, "malformed.webp");
            File.WriteAllBytes(malformedImage, System.Text.Encoding.ASCII.GetBytes("RIFFxxxxWEBP"));
            LogAssert.Expect(LogType.Error, "[WebPDecoderWrapper] Get file info failed");
            yield return VerifyProxyFallback(new Uri(malformedImage).AbsoluteUri);
        }

        private IEnumerator VerifyProxyFallback(string firstResponseUrl)
        {
            var game = new UnityCardGame(null, "ProxyCard-" + Guid.NewGuid().ToString("N"));
            var card = new UnityCard(game, "Card", "Card", "Set", null, false)
            {
                ImageWebUrl = "https://images.example/card.png"
            };
            var source = Path.Combine(_directory, "proxy.png");
            var texture = new Texture2D(3, 2);
            File.WriteAllBytes(source, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            var queue = new RetryQueue
            {
                FirstResponseUrl = firstResponseUrl,
                ImageFileUrl = new Uri(source).AbsoluteUri
            };
            try
            {
                var loader = typeof(ImageQueueService).GetMethod("LoadImageSprite",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                yield return (IEnumerator)loader.Invoke(queue, new object[] { card });
                CollectionAssert.AreEqual(new[]
                {
                    card.ImageWebUrl, UnityFileMethods.ToCgsGamesProxyUrl(card.ImageWebUrl)
                }, queue.RequestedUrls);
                Assert.IsNotNull(card.ImageSprite);
                CollectionAssert.AreEqual(File.ReadAllBytes(source), File.ReadAllBytes(card.ImageFilePath));
                Assert.IsEmpty(Directory.GetFiles(Path.GetDirectoryName(card.ImageFilePath), "*.tmp"));
            }
            finally
            {
                card.ImageSprite = null;
                if (Directory.Exists(game.GameDirectoryPath))
                    Directory.Delete(game.GameDirectoryPath, true);
            }
        }
#endif

        [UnityTest]
        public IEnumerator PreviewsSelectedImageFromLocalFileUri()
        {
            var path = Path.Combine(_directory, "card + 日本語.png");
            var texture = new Texture2D(2, 3);
            texture.SetPixel(0, 0, Color.red);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            Sprite sprite = null;
            try
            {
                yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                    UnityFileMethods.CreateAndOutputSpriteFromImageFile(new Uri(path).AbsoluteUri),
                    value => sprite = value);
                Assert.IsNotNull(sprite);
                Assert.AreEqual(2, sprite.texture.width);
                Assert.AreEqual(3, sprite.texture.height);
            }
            finally
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
            }
        }
    }
}
