/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class ImageOrientationTests
    {
        private static UnityCard CreateCardWithImage(Float2 cardSize, int imageWidth, int imageHeight)
        {
            var game = new UnityCardGame(null, "orientation_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>(), CardSize = cardSize
            };
            var card = new UnityCard(game, "orientation_card", "Orientation Card", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);
            var texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
            texture.Apply();
            card.ImageSprite = Sprite.Create(texture, new Rect(0, 0, imageWidth, imageHeight), new Vector2(0.5f, 0.5f));
            return card;
        }

        [Test]
        public void OrientedImageSprite_RotatesLandscapeImageForPortraitCard()
        {
            var card = CreateCardWithImage(new Float2(2.5f, 3.5f), 4, 2);

            // The unrotated image is preserved for displays that show the image as-is, like the card viewer
            Assert.AreEqual(4, card.ImageSprite.texture.width);
            Assert.AreEqual(2, card.ImageSprite.texture.height);

            Assert.AreNotSame(card.ImageSprite, card.OrientedImageSprite);
            Assert.AreEqual(2, card.OrientedImageSprite.texture.width);
            Assert.AreEqual(4, card.OrientedImageSprite.texture.height);

            card.ImageSprite = null;
        }

        [Test]
        public void OrientedImageSprite_ReusesImageWhenOrientationAlreadyMatches()
        {
            var card = CreateCardWithImage(new Float2(2.5f, 3.5f), 2, 4);

            Assert.AreSame(card.ImageSprite, card.OrientedImageSprite);

            card.ImageSprite = null;
        }

        [Test]
        public void OrientedImageSprite_RotatesPortraitImageForLandscapeCard()
        {
            var card = CreateCardWithImage(new Float2(3.5f, 2.5f), 2, 4);

            Assert.AreNotSame(card.ImageSprite, card.OrientedImageSprite);
            Assert.AreEqual(4, card.OrientedImageSprite.texture.width);
            Assert.AreEqual(2, card.OrientedImageSprite.texture.height);

            card.ImageSprite = null;
        }

        [Test]
        public void Rotate90Clockwise_SwapsDimensionsAndMapsPixels()
        {
            var texture = new Texture2D(3, 2, TextureFormat.RGBA32, false);
            var bottomLeft = new Color32(255, 0, 0, 255);
            var topLeft = new Color32(0, 255, 0, 255);
            var topRight = new Color32(0, 0, 255, 255);
            var pixels = new Color32[6];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            pixels[0] = bottomLeft; // (0, 0)
            pixels[3] = topLeft; // (0, 1)
            pixels[5] = topRight; // (2, 1)
            texture.SetPixels32(pixels);
            texture.Apply();

            var rotatedTexture = UnityFileMethods.Rotate90Clockwise(texture);

            Assert.AreEqual(2, rotatedTexture.width);
            Assert.AreEqual(3, rotatedTexture.height);
            // Rotating clockwise moves the left edge to the top and the top edge to the right
            Assert.AreEqual(bottomLeft, (Color32)rotatedTexture.GetPixel(0, 2));
            Assert.AreEqual(topLeft, (Color32)rotatedTexture.GetPixel(1, 2));
            Assert.AreEqual(topRight, (Color32)rotatedTexture.GetPixel(1, 0));

            UnityEngine.Object.Destroy(texture);
            UnityEngine.Object.Destroy(rotatedTexture);
        }
    }
}
