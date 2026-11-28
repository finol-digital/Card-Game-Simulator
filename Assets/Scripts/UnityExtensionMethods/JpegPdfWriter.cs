/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace UnityExtensionMethods
{
    /// <summary>
    /// Writes a PDF of pages that show JPEG images. JPEG data is embedded unchanged, so the writer needs no image
    /// or PDF library; Web players use it because PdfSharp is excluded from WebGL builds.
    /// </summary>
    public sealed class JpegPdfWriter
    {
        public const double PointsPerInch = 72;

        private readonly double _pageWidth;
        private readonly double _pageHeight;
        private readonly List<StringBuilder> _pages = new();
        private readonly List<HashSet<int>> _pageImages = new();
        private readonly List<JpegImage> _images = new();
        private readonly Dictionary<string, int> _imageIndexes = new();

        private readonly struct JpegImage
        {
            public readonly byte[] Data;
            public readonly int Width;
            public readonly int Height;

            public JpegImage(byte[] data, int width, int height)
            {
                Data = data;
                Width = width;
                Height = height;
            }
        }

        public JpegPdfWriter(double pageWidthPoints, double pageHeightPoints)
        {
            if (pageWidthPoints <= 0 || pageHeightPoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageWidthPoints), "Pages need a positive size.");
            _pageWidth = pageWidthPoints;
            _pageHeight = pageHeightPoints;
        }

        public int PageCount => _pages.Count;

        public int ImageCount => _images.Count;

        public void AddPage()
        {
            _pages.Add(new StringBuilder());
            _pageImages.Add(new HashSet<int>());
        }

        public bool HasImage(string key) => _imageIndexes.ContainsKey(key);

        /// <summary>Stores an RGB JPEG once under <paramref name="key"/>, so repeated cards share it.</summary>
        public void AddImage(string key, byte[] jpeg, int pixelWidth, int pixelHeight)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Images need a key.", nameof(key));
            if (jpeg is not { Length: > 3 } || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
                throw new ArgumentException("The image is not JPEG data.", nameof(jpeg));
            if (pixelWidth <= 0 || pixelHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(pixelWidth), "Images need a positive size.");
            if (_imageIndexes.ContainsKey(key))
                return;

            _imageIndexes[key] = _images.Count;
            _images.Add(new JpegImage(jpeg, pixelWidth, pixelHeight));
        }

        /// <summary>Draws a stored image on the last page; positions are in points from the page's top left.</summary>
        public void DrawImage(string key, double left, double top, double width, double height)
        {
            if (_pages.Count == 0)
                throw new InvalidOperationException("Add a page before drawing.");
            if (!_imageIndexes.TryGetValue(key, out var index))
                throw new KeyNotFoundException("Add the image before drawing it: " + key);

            var bottom = _pageHeight - top - height;
            _pages[^1].Append("q ").Append(Number(width)).Append(" 0 0 ").Append(Number(height)).Append(' ')
                .Append(Number(left)).Append(' ').Append(Number(bottom)).Append(" cm /Im").Append(index)
                .Append(" Do Q\n");
            _pageImages[^1].Add(index);
        }

        public void Save(string path) => File.WriteAllBytes(path, ToBytes());

        public byte[] ToBytes()
        {
            if (_pages.Count == 0)
                throw new InvalidOperationException("A PDF needs at least one page.");

            // Objects: 1 catalog, 2 page tree, then each image, then a page and its contents per page.
            const int firstImageObject = 3;
            var firstPageObject = firstImageObject + _images.Count;
            var objectCount = firstPageObject + _pages.Count * 2 - 1;
            var offsets = new long[objectCount + 1];

            using var output = new MemoryStream();
            Write(output, "%PDF-1.4\n%âãÏÓ\n");

            BeginObject(output, offsets, 1);
            Write(output, "<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            BeginObject(output, offsets, 2);
            var kids = new StringBuilder();
            for (var page = 0; page < _pages.Count; page++)
                kids.Append(firstPageObject + page * 2).Append(" 0 R ");
            Write(output, "<< /Type /Pages /Kids [" + kids.ToString().TrimEnd() + "] /Count " + _pages.Count +
                          " >>\nendobj\n");

            for (var image = 0; image < _images.Count; image++)
            {
                var jpeg = _images[image];
                BeginObject(output, offsets, firstImageObject + image);
                Write(output, "<< /Type /XObject /Subtype /Image /Width " + jpeg.Width + " /Height " + jpeg.Height +
                              " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " +
                              jpeg.Data.Length + " >>\nstream\n");
                output.Write(jpeg.Data, 0, jpeg.Data.Length);
                Write(output, "\nendstream\nendobj\n");
            }

            for (var page = 0; page < _pages.Count; page++)
            {
                var pageObject = firstPageObject + page * 2;
                var xObjects = new StringBuilder();
                foreach (var image in _pageImages[page])
                    xObjects.Append("/Im").Append(image).Append(' ').Append(firstImageObject + image).Append(" 0 R ");

                BeginObject(output, offsets, pageObject);
                Write(output, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + Number(_pageWidth) + " " +
                              Number(_pageHeight) + "] /Resources << /XObject << " + xObjects + ">> >> /Contents " +
                              (pageObject + 1) + " 0 R >>\nendobj\n");

                var contents = Encoding.ASCII.GetBytes(_pages[page].ToString());
                BeginObject(output, offsets, pageObject + 1);
                Write(output, "<< /Length " + contents.Length + " >>\nstream\n");
                output.Write(contents, 0, contents.Length);
                Write(output, "\nendstream\nendobj\n");
            }

            var crossReference = output.Position;
            var table = new StringBuilder();
            table.Append("xref\n0 ").Append(objectCount + 1).Append("\n0000000000 65535 f \n");
            for (var number = 1; number <= objectCount; number++)
                table.Append(offsets[number].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            table.Append("trailer\n<< /Size ").Append(objectCount + 1).Append(" /Root 1 0 R >>\nstartxref\n")
                .Append(crossReference).Append("\n%%EOF\n");
            Write(output, table.ToString());
            return output.ToArray();
        }

        private static void BeginObject(Stream output, long[] offsets, int number)
        {
            offsets[number] = output.Position;
            Write(output, number + " 0 obj\n");
        }

        private static void Write(Stream output, string text)
        {
            // One byte per character keeps the binary comment's bytes; everything else is ASCII.
            var bytes = new byte[text.Length];
            for (var i = 0; i < text.Length; i++)
                bytes[i] = text[i] <= 0xFF ? (byte)text[i] : (byte)'?';
            output.Write(bytes, 0, bytes.Length);
        }

        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
