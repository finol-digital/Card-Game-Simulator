/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class LocalFileImportTests
    {
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
