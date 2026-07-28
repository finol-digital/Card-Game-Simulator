/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests.PlayMode
{
    /// <summary>
    /// Open Sans and Liberation Sans carry no CJK glyphs. Desktop and mobile hide that by silently
    /// falling back to OS-installed fonts, but WebGL has no OS fonts, so Japanese game names came out
    /// blank there. These tests pin the bundled Noto Sans JP fallback in place on both text stacks.
    /// </summary>
    public class JapaneseFontTests
    {
        private const string Japanese = "遊戯王カードゲーム";

        private const string OpenSansPath = "Assets/TextMesh Pro/Fonts/Open Sans/OpenSans-Regular.ttf";
        private const string OpenSansBoldPath = "Assets/TextMesh Pro/Fonts/Open Sans/OpenSans-Bold.ttf";

        [Test]
        public void BundledJapaneseFont_CoversJapaneseText()
        {
            var font = GetJapaneseFallbackFontAsset().sourceFontFile;
            Assert.IsNotNull(font,
                "The Japanese TMP font asset must keep a runtime reference to its source font, " +
                "otherwise the glyphs are stripped from player builds.");

            foreach (var character in Japanese)
                Assert.IsTrue(font.HasCharacter(character),
                    $"Bundled Japanese font is missing '{character}'.");
        }

        [Test]
        public void TmpSettings_FallBackToFontCoveringJapanese()
        {
            var fallback = GetJapaneseFallbackFontAsset();

            Assert.AreEqual(AtlasPopulationMode.Dynamic, fallback.atlasPopulationMode,
                "The Japanese TMP fallback must use a dynamic atlas so glyphs rasterize on demand.");

            foreach (var character in Japanese)
                Assert.IsTrue(fallback.HasCharacter(character, false, true),
                    $"TextMeshPro cannot rasterize '{character}' from the Japanese fallback.");
        }

#if UNITY_EDITOR
        [Test]
        [TestCase(OpenSansPath)]
        [TestCase(OpenSansBoldPath)]
        public void OpenSans_FallsBackToFontCoveringJapanese(string fontPath)
        {
            var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(fontPath);
            Assert.IsNotNull(importer, $"Could not load the font importer for {fontPath}.");

            var fallbacks = importer.fontReferences;
            Assert.IsNotNull(fallbacks, $"{fontPath} has no fallback fonts.");

            var coversJapanese = fallbacks.Any(fallback =>
                fallback != null && Japanese.All(fallback.HasCharacter));
            Assert.IsTrue(coversJapanese,
                $"{fontPath} needs a fallback font covering Japanese, or WebGL renders it blank.");
        }
#endif

        private static TMP_FontAsset GetJapaneseFallbackFontAsset()
        {
            var fallbacks = TMP_Settings.fallbackFontAssets;
            Assert.IsNotNull(fallbacks, "TMP Settings has no fallback font assets.");

            var japaneseFallback = fallbacks.FirstOrDefault(fallback =>
                fallback != null && fallback.sourceFontFile != null &&
                Japanese.All(fallback.sourceFontFile.HasCharacter));
            Assert.IsNotNull(japaneseFallback,
                "TMP Settings needs a fallback font asset covering Japanese.");

            return japaneseFallback;
        }
    }
}
