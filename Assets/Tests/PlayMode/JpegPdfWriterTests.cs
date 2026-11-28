/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class JpegPdfWriterTests
    {
        private const double LetterWidth = 612;
        private const double LetterHeight = 792;

        [Test]
        public void WritesValidCrossReferencesAndSharesRepeatedImages()
        {
            var writer = new JpegPdfWriter(LetterWidth, LetterHeight);
            var red = Jpeg(Color.red, 4, 6);
            writer.AddPage();
            writer.AddImage("red", red, 4, 6);
            writer.DrawImage("red", 36, 36, 180, 252);
            writer.DrawImage("red", 216, 36, 180, 252);
            writer.AddPage();
            writer.AddImage("red", Jpeg(Color.blue, 4, 6), 4, 6); // Same key: the first image is kept.
            writer.DrawImage("red", 36, 36, 180, 252);

            var pdf = Latin1(writer.ToBytes());

            Assert.AreEqual(2, writer.PageCount);
            Assert.AreEqual(1, writer.ImageCount);
            AssertCrossReferencesPointToObjects(pdf);
            Assert.AreEqual(2, Regex.Matches(pdf, @"/Type /Page\b").Count);
            Assert.AreEqual(1, Regex.Matches(pdf, "/Subtype /Image").Count);
            StringAssert.Contains("/Count 2", pdf);
            StringAssert.Contains("/Filter /DCTDecode /Length " + red.Length + " ", pdf);
            // PDF coordinates start at the bottom left: 792 - 36 - 252 = 504.
            StringAssert.Contains("q 180 0 0 252 36 504 cm /Im0 Do Q", pdf);
            StringAssert.Contains("q 180 0 0 252 216 504 cm /Im0 Do Q", pdf);
        }

        [Test]
        public void RejectsDataThatIsNotJpeg()
        {
            var writer = new JpegPdfWriter(LetterWidth, LetterHeight);
            Assert.Throws<ArgumentException>(() => writer.AddImage("png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, 1, 1));
        }

        [Test]
        public void RequiresAPageBeforeDrawingAndBeforeSaving()
        {
            var writer = new JpegPdfWriter(LetterWidth, LetterHeight);
            writer.AddImage("red", Jpeg(Color.red, 2, 2), 2, 2);
            Assert.Throws<InvalidOperationException>(() => writer.DrawImage("red", 0, 0, 1, 1));
            Assert.Throws<InvalidOperationException>(() => writer.ToBytes());
        }

        [Test]
        public void DeckPrintsCardsInRowsAcrossPages()
        {
            var game = new UnityCardGame(null, "jpeg_pdf_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>(), CardSize = new Float2(2.5f, 3.5f)
            };
            try
            {
                var ace = CardWithImage(game, "ace", new Color32(255, 0, 0, 255));
                var king = CardWithImage(game, "king", new Color32(0, 0, 255, 0)); // Fully transparent.
                // A letter page fits 3 x 2 poker-size cards, so 7 cards need 2 pages.
                var cards = new List<UnityCard> { ace, ace, ace, ace, king, king, king };
                var deck = new UnityDeck(game, "Jpeg Pdf Test", DeckFileType.Txt, cards);

                var uri = deck.PrintJpegPdf();

                Assert.IsTrue(File.Exists(uri.LocalPath));
                var pdf = Latin1(File.ReadAllBytes(uri.LocalPath));
                AssertCrossReferencesPointToObjects(pdf);
                Assert.AreEqual(2, Regex.Matches(pdf, @"/Type /Page\b").Count);
                Assert.AreEqual(2, Regex.Matches(pdf, "/Subtype /Image").Count, "Repeated cards share an image.");
                Assert.AreEqual(7, Regex.Matches(pdf, " Do Q").Count);
                // Same positions as the PdfSharp layout: 0.5 inch margins, then one card width or height apart.
                StringAssert.Contains("q 180 0 0 252 36 504 cm /Im0 Do Q", pdf);
                StringAssert.Contains("q 180 0 0 252 396 504 cm /Im0 Do Q", pdf);
                StringAssert.Contains("q 180 0 0 252 36 252 cm /Im0 Do Q", pdf);
                StringAssert.Contains("q 180 0 0 252 216 252 cm /Im1 Do Q", pdf);

                // Transparent areas print as white paper rather than black.
                var kingImage = DecodeImage(pdf, 1);
                var pixel = kingImage.GetPixel(1, 1);
                UnityEngine.Object.Destroy(kingImage);
                Assert.Greater(pixel.r, 0.9f);
                Assert.Greater(pixel.g, 0.9f);
                Assert.Greater(pixel.b, 0.9f);
            }
            finally
            {
                if (Directory.Exists(game.GameDirectoryPath))
                    Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        private static UnityCard CardWithImage(UnityCardGame game, string id, Color32 color)
        {
            var card = new UnityCard(game, id, id, Set.DefaultCode, new Dictionary<string, PropertyDefValuePair>(),
                false);
            var texture = new Texture2D(5, 7, TextureFormat.RGBA32, false);
            texture.SetPixels32(Enumerable.Repeat(color, 5 * 7).ToArray());
            Directory.CreateDirectory(Path.GetDirectoryName(card.ImageFilePath));
            File.WriteAllBytes(card.ImageFilePath, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            return card;
        }

        private static byte[] Jpeg(Color color, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.SetPixels(Enumerable.Repeat(color, width * height).ToArray());
            var jpeg = texture.EncodeToJPG();
            UnityEngine.Object.Destroy(texture);
            return jpeg;
        }

        private static Texture2D DecodeImage(string pdf, int imageNumber)
        {
            var header = Regex.Matches(pdf, @"/Filter /DCTDecode /Length (\d+) >>\nstream\n")[imageNumber];
            var length = int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture);
            var bytes = pdf.Substring(header.Index + header.Length, length).Select(c => (byte)c).ToArray();
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(bytes));
            return texture;
        }

        private static void AssertCrossReferencesPointToObjects(string pdf)
        {
            StringAssert.StartsWith("%PDF-1.4\n", pdf);
            StringAssert.EndsWith("%%EOF\n", pdf);
            var startXref = Regex.Match(pdf, @"startxref\n(\d+)\n%%EOF\n$");
            Assert.IsTrue(startXref.Success);
            var xref = int.Parse(startXref.Groups[1].Value, CultureInfo.InvariantCulture);
            StringAssert.StartsWith("xref\n0 ", pdf.Substring(xref));

            var size = int.Parse(Regex.Match(pdf, @"/Size (\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
            var entries = Regex.Matches(pdf.Substring(xref), @"(\d{10}) (\d{5}) ([nf]) \n");
            Assert.AreEqual(size, entries.Count);
            for (var number = 1; number < size; number++)
            {
                var offset = int.Parse(entries[number].Groups[1].Value, CultureInfo.InvariantCulture);
                StringAssert.StartsWith(number + " 0 obj\n", pdf.Substring(offset),
                    "Cross-reference entry " + number + " must point to its object.");
            }
        }

        // One character per byte, so offsets in the string match offsets in the file.
        private static string Latin1(byte[] bytes) =>
            new string(bytes.Select(value => (char)value).ToArray());
    }
}
