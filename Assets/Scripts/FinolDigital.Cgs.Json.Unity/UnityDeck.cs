/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FinolDigital.Cgs.Json.Decks;
using UnityEngine;
using UnityExtensionMethods;
#if !UNITY_WEBGL
using Didstopia.PDFSharp;
using Didstopia.PDFSharp.Drawing;
using Didstopia.PDFSharp.Pdf;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
#endif

namespace FinolDigital.Cgs.Json.Unity
{
    public class UnityDeck : Deck
    {
        private const float PrintPdfWidth = 8.5f;
        private const float PrintPdfHeight = 11f;
        private const float PrintPdfMargin = 0.5f;
        private const int PrintPdfPixelsPerInch = 72;

        private AuthoredDeckFiles.Binding _authoredBinding;
        private string _authoredPath;
        private string _boundCanonicalName;
        public string AuthoredName => _authoredBinding != null && Name == _boundCanonicalName &&
            AuthoredDeckFiles.ReadBinding((UnityCardGame)SourceGame, _authoredPath)?.Generation == _authoredBinding.Generation
                ? _authoredBinding.Name : null;
        public void ClearAuthoredName() => _authoredBinding = null;

        public static UnityDeck LoadFile(UnityCardGame game, string path, string name)
        {
            lock (LocalizationFiles.Gate(game.GameDirectoryPath))
            {
                var deck = Parse(game, name, game.DeckFileType, File.ReadAllText(path));
                deck._authoredBinding = AuthoredDeckFiles.ReadBinding(game, path);
                deck._authoredPath = path;
                deck._boundCanonicalName = deck.Name;
                return deck;
            }
        }

        public string FilePath => ((UnityCardGame) SourceGame).DecksDirectoryPath + Path.DirectorySeparatorChar +
                                  UnityFileMethods.GetSafeFileName(Name + "." + FileType.ToString().ToLower());

        private string PrintPdfDirectory => ((UnityCardGame) SourceGame).DecksDirectoryPath + "/printpdf";

        private string PrintPdfFilePath =>
            PrintPdfDirectory + "/" + UnityFileMethods.GetSafeFileName(Name + ".pdf");

        public override IReadOnlyCollection<Card> Cards => new List<Card>(_cards);
        private readonly List<UnityCard> _cards;

        public IReadOnlyDictionary<string, int> MissingCards =>
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(_missingCards);

        private readonly Dictionary<string, int> _missingCards = new();

        public UnityDeck(UnityCardGame sourceGame, string name = DefaultName, DeckFileType fileType = DeckFileType.Txt,
            IReadOnlyCollection<UnityCard> cards = null) : base(sourceGame, name, fileType, cards)
        {
            SourceGame = sourceGame;
            _cards = cards != null ? new List<UnityCard>(cards) : new List<UnityCard>();
        }

        public static UnityDeck Parse(UnityCardGame cardGame, string deckName, DeckFileType deckFileType,
            string deckText)
        {
            var deck = new UnityDeck(cardGame, deckName, deckFileType);
            if (string.IsNullOrEmpty(deckText))
                return deck;

            var lines = deckText.Split('\n').Select(x => x.Trim()).ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                switch (deckFileType)
                {
                    case DeckFileType.Dec:
                        deck.LoadDec(line);
                        break;
                    case DeckFileType.Hsd:
                        deck.LoadHsd(line);
                        break;
                    case DeckFileType.Ydk:
                        if (line.Equals("!side"))
                            return deck;
                        deck.LoadYdk(line);
                        break;
                    case DeckFileType.Lor:
                        deck.LoadLor(line);
                        break;
                    case DeckFileType.Svd:
                        deck.LoadSvd(line);
                        break;
                    case DeckFileType.Txt:
                    default:
                        if (line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:") ||
                            line.Equals("# Sideboard"))
                        {
                            while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i]))
                                i++;
                        }
                        else
                            deck.LoadTxt(line);

                        break;
                }
            }

            return deck;
        }

        private void RecordMissingCard(string nameOrId, int count)
        {
            if (count <= 0)
                return;
            _missingCards.TryGetValue(nameOrId, out var previousCount);
            _missingCards[nameOrId] = previousCount + count;
        }

        private void LoadDec(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("SB:"))
                return;

            var cardCount = 1;
            var tokens = line.Split(' ').ToList();
            if (tokens.Count > 0 && int.TryParse(tokens[0], out var parsedCount))
            {
                cardCount = parsedCount;
                tokens.RemoveAt(0);
            }
            var cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            var cards =
                ((UnityCardGame) SourceGame).FilterCards(new CardSearchFilters() {Name = cardName});
            foreach (var card in cards)
            {
                if (!string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase))
                    continue;
                for (var i = 0; i < cardCount; i++)
                    _cards.Add(card);
                return;
            }
            RecordMissingCard(cardName, cardCount);
        }

        private void LoadHsd(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (line.StartsWith("#"))
            {
                if (line.StartsWith("###"))
                    Name = line[3..].Trim();
                return;
            }

            var bytes = Convert.FromBase64String(line);
            ulong offset = 3;

            var heroesCount = (int) Varint.Read(bytes, ref offset, out _);
            for (var i = 0; i < heroesCount; i++)
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, (int) Varint.Read(bytes, ref offset, out _), 1);

            var singleCardsCount = (int) Varint.Read(bytes, ref offset, out _);
            for (var i = 0; i < singleCardsCount; i++)
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, (int) Varint.Read(bytes, ref offset, out _), 1);

            var doubleCardsCount = (int) Varint.Read(bytes, ref offset, out _);
            for (var i = 0; i < doubleCardsCount; i++)
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, (int) Varint.Read(bytes, ref offset, out _), 2);

            var multiCardsCount = (int) Varint.Read(bytes, ref offset, out _);
            for (var i = 0; i < multiCardsCount; i++)
            {
                var id = (int) Varint.Read(bytes, ref offset, out _);
                var count = (int) Varint.Read(bytes, ref offset, out _);
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, id, count);
            }

            Sort();
        }

        private void AddCardsByPropertyInt(string propertyName, int propertyValue, int count)
        {
            var card = ((UnityCardGame) SourceGame).Cards.Values.FirstOrDefault(currCard =>
                currCard.GetPropertyValueInt(propertyName) == propertyValue);
            if (card == null)
                RecordMissingCard(propertyValue.ToString(System.Globalization.CultureInfo.InvariantCulture), count);
            for (var i = 0; card != null && i < count; i++)
                _cards.Add(card);
        }

        private void AddCardsByPropertyString(string propertyName, string propertyValue, int count)
        {
            var card = ((UnityCardGame) SourceGame).Cards.Values.FirstOrDefault(currCard =>
                currCard.GetPropertyValueString(propertyName).Equals(propertyValue));
            if (card == null)
                RecordMissingCard(propertyValue, count);
            for (var i = 0; card != null && i < count; i++)
                _cards.Add(card);
        }

        private void LoadLor(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (line.StartsWith("#"))
            {
                if (line.StartsWith("###"))
                    Name = line[3..].Trim();
                return;
            }

            var cardCodeAndCounts = LoRDeckEncoder.GetDeckFromCode(line);
            foreach (var cardCodeAndCount in cardCodeAndCounts)
            {
                if (!((UnityCardGame) SourceGame).Cards.TryGetValue(cardCodeAndCount.CardCode, out var card))
                {
                    RecordMissingCard(cardCodeAndCount.CardCode, cardCodeAndCount.Count);
                    continue;
                }
                for (var i = 0; i < cardCodeAndCount.Count; i++)
                    _cards.Add(card);
            }
        }

        // Accepts the SVD3 code that Starvoxel shares, a link containing it, or the one id per line saved for unfinished decks.
        private void LoadSvd(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                return;

            if (StarvoxelDeckEncoder.TryDecode(line, out var cardIds))
            {
                foreach (var cardId in cardIds)
                    AddCardsByPropertyInt(SourceGame.DeckFileAltId, cardId, 1);
                return;
            }

            if (int.TryParse(line, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var altId))
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, altId, 1);
            else if (((UnityCardGame) SourceGame).Cards.TryGetValue(line, out var card))
                _cards.Add(card);
            else
                RecordMissingCard(line, 1);
        }

        private void LoadYdk(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.Equals("!side"))
                return;

            AddCardsByPropertyString(SourceGame.DeckFileAltId, line, 1);
        }

        private void LoadTxt(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//") ||
                line.Equals("Sideboard", StringComparison.OrdinalIgnoreCase) || line.Equals("Sideboard:"))
                return;

            var cardCount = 1;
            var cardName = line;
            var cardId = string.Empty;
            var cardSet = string.Empty;
            var cardBack = string.Empty;
            if (line.Contains(" "))
            {
                var tokens = line.Split(' ').ToList();
                if (tokens.Count > 0 &&
                    int.TryParse(
                        (tokens[0].StartsWith("x") || tokens[0].EndsWith("x")) ? tokens[0].Replace("x", "") : tokens[0],
                        out var parsedCount))
                {
                    cardCount = parsedCount;
                    tokens.RemoveAt(0);
                }

                if (tokens.Count > 0 && tokens[0].StartsWith("[") && tokens[0].EndsWith("]"))
                {
                    cardId = tokens[0].Substring(1, tokens[0].Length - 2);
                    tokens.RemoveAt(0);
                }

                if (tokens.Count > 0 && line.Contains(" (") && line.EndsWith(")"))
                {
                    var indexOfParens = line.LastIndexOf(" (", StringComparison.Ordinal);
                    var inParens = line.Substring(indexOfParens + 2, line.Length - (indexOfParens + 3));
                    if (((UnityCardGame) SourceGame).Sets.ContainsKey(inParens))
                    {
                        cardSet = inParens;
                        var cardSetTokenCount = cardSet.Count(f => f == ' ') + 1;
                        for (var i = 0; i < cardSetTokenCount; i++)
                            tokens.RemoveAt(tokens.Count - 1);
                        line = line[..indexOfParens].Trim();
                    }
                }

                if (tokens.Count > 0 && line.Contains(" <") && line.EndsWith(">"))
                {
                    var indexOfParens = line.LastIndexOf(" <", StringComparison.Ordinal);
                    var inParens = line.Substring(indexOfParens + 2, line.Length - (indexOfParens + 3));
                    if (((UnityCardGame) SourceGame).CardBackFaceImageSprites.ContainsKey(inParens))
                    {
                        cardBack = inParens;
                        var cardBackTokenCount = cardBack.Count(f => f == ' ') + 1;
                        for (var i = 0; i < cardBackTokenCount; i++)
                            tokens.RemoveAt(tokens.Count - 1);
                    }
                }

                if (tokens.Count > 0 && !SourceGame.CardNameIsUnique && string.IsNullOrEmpty(cardId)
                    && ((UnityCardGame) SourceGame).Cards.ContainsKey(tokens[0]))
                {
                    cardId = tokens[0];
                    cardName = string.Empty;
                }
                else
                {
                    cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
                }
            }

            var cards = ((UnityCardGame) SourceGame).FilterCards(new CardSearchFilters()
                {Id = cardId, Name = cardName, SetCode = cardSet, BackFaceId = cardBack});
            foreach (var card in cards)
            {
                if (!card.Id.Equals(cardId) &&
                    (!string.Equals(card.Name.Trim(), cardName, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrEmpty(cardSet) &&
                      !cardSet.Equals(card.SetCode, StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrEmpty(cardBack) &&
                     !cardBack.Equals(card.BackFaceId, StringComparison.OrdinalIgnoreCase)))
                    continue;
                for (var i = 0; i < cardCount; i++)
                    _cards.Add(card);
                return;
            }
            RecordMissingCard(string.IsNullOrEmpty(cardId) ? cardName : cardId, cardCount);
        }

        public void Add(UnityCard card)
        {
            _cards.Add(card);
        }

        public void Sort()
        {
            _cards.Sort();
        }

        // NOTE: CAN THROW EXCEPTION
        public Uri PrintPdf()
        {
#if UNITY_WEBGL
            return PrintJpegPdf();
#else
            if (!Directory.Exists(PrintPdfDirectory))
                Directory.CreateDirectory(PrintPdfDirectory);

            ImageSource.ImageSourceImpl = new ImageSharpImageSource();
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = Name;

            var cardsPerRow = (int) Math.Floor((PrintPdfWidth - PrintPdfMargin * 2) / SourceGame.CardSize.X);
            var rowsPerPage = (int) Math.Floor((PrintPdfHeight - PrintPdfMargin * 2) / SourceGame.CardSize.Y);
            var cardsPerPage = cardsPerRow * rowsPerPage;
            PdfPage page = null;
            XGraphics gfx = null;
            double px = PrintPdfMargin * PrintPdfPixelsPerInch, py = PrintPdfMargin * PrintPdfPixelsPerInch;
            var webpPngCache = new Dictionary<string, byte[]>();
            for (var cardNumber = 0; cardNumber < Cards.Count; cardNumber++)
            {
                if (page == null || cardNumber % cardsPerPage == 0)
                {
                    page = pdfDocument.AddPage();
                    page.Size = PageSize.Letter;
                    gfx = XGraphics.FromPdfPage(page);
                    py = PrintPdfMargin * PrintPdfPixelsPerInch;
                }

                var imageFilePath = _cards[cardNumber].ImageFilePath;
                XImage xImage;
                try
                {
                    if (UnityFileMethods.IsWebpFile(imageFilePath))
                    {
                        // Texture2D.LoadImage() only supports png and jpg, so webp must be decoded and re-encoded
                        if (!webpPngCache.TryGetValue(imageFilePath, out var pngBytes))
                        {
                            var texture = UnityFileMethods.DecodeWebpReadable(File.ReadAllBytes(imageFilePath));
                            if (texture == null)
                            {
                                const string DecodeErrorMessage = "Unable to decode webp image: ";
                                throw new InvalidOperationException(DecodeErrorMessage + imageFilePath);
                            }
                            pngBytes = texture.EncodeToPNG();
                            UnityEngine.Object.Destroy(texture);
                            webpPngCache[imageFilePath] = pngBytes;
                        }

                        xImage = XImage.FromImageSource(ImageSource.FromBinary(imageFilePath, () => pngBytes, false));
                    }
                    else
                    {
                        xImage = XImage.FromFile(imageFilePath);
                    }
                }
                catch (Exception e)
                {
                    const string FileOpErrorMessage = "Failed to load image file for PDF rendering: ";
                    Debug.LogError(FileOpErrorMessage + imageFilePath + " - " + e.Message);
                    throw;
                }
                gfx.DrawImage(xImage, px, py, SourceGame.CardSize.X * PrintPdfPixelsPerInch,
                    SourceGame.CardSize.Y * PrintPdfPixelsPerInch);
                px += SourceGame.CardSize.X * PrintPdfPixelsPerInch;

                if ((cardNumber + 1) % cardsPerRow != 0)
                    continue;

                px = PrintPdfMargin * PrintPdfPixelsPerInch;
                py += SourceGame.CardSize.Y * PrintPdfPixelsPerInch;
            }

            pdfDocument.Save(PrintPdfFilePath);
            pdfDocument.Close();
            pdfDocument.Dispose();

            // Deck names can contain URI characters such as #, so build the URI from the path itself.
            return new Uri(Path.GetFullPath(PrintPdfFilePath));
#endif
        }

        /// <summary>
        /// Prints with the same layout as <see cref="PrintPdf"/> without PdfSharp, which Web players exclude:
        /// each card image is embedded once as a JPEG.
        /// </summary>
        // NOTE: CAN THROW EXCEPTION
        public Uri PrintJpegPdf()
        {
            if (!Directory.Exists(PrintPdfDirectory))
                Directory.CreateDirectory(PrintPdfDirectory);

            var cardWidth = SourceGame.CardSize.X;
            var cardHeight = SourceGame.CardSize.Y;
            var cardsPerRow = (int) Math.Floor((PrintPdfWidth - PrintPdfMargin * 2) / cardWidth);
            var rowsPerPage = (int) Math.Floor((PrintPdfHeight - PrintPdfMargin * 2) / cardHeight);
            var cardsPerPage = cardsPerRow * rowsPerPage;
            if (cardsPerPage < 1)
                throw new InvalidOperationException("The cards are too large to print on a letter-size page.");

            var pdf = new JpegPdfWriter(PrintPdfWidth * PrintPdfPixelsPerInch, PrintPdfHeight * PrintPdfPixelsPerInch);
            for (var cardNumber = 0; cardNumber < _cards.Count; cardNumber++)
            {
                if (cardNumber % cardsPerPage == 0)
                    pdf.AddPage();

                var imageFilePath = _cards[cardNumber].ImageFilePath;
                if (!pdf.HasImage(imageFilePath))
                    AddJpegImage(pdf, imageFilePath);

                var slot = cardNumber % cardsPerPage;
                pdf.DrawImage(imageFilePath,
                    (PrintPdfMargin + slot % cardsPerRow * cardWidth) * PrintPdfPixelsPerInch,
                    (PrintPdfMargin + slot / cardsPerRow * cardHeight) * PrintPdfPixelsPerInch,
                    cardWidth * PrintPdfPixelsPerInch, cardHeight * PrintPdfPixelsPerInch);
            }

            if (pdf.PageCount == 0)
                pdf.AddPage();
            pdf.Save(PrintPdfFilePath);
            // Deck names can contain URI characters such as #, so build the URI from the path itself.
            return new Uri(Path.GetFullPath(PrintPdfFilePath));
        }

        private static void AddJpegImage(JpegPdfWriter pdf, string imageFilePath)
        {
            Texture2D texture = null;
            try
            {
                var bytes = File.ReadAllBytes(imageFilePath);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    // Texture2D.LoadImage() reads only png and jpg; some hosts serve webp under those names.
                    UnityEngine.Object.DestroyImmediate(texture);
                    texture = UnityFileMethods.DecodeWebpReadable(bytes);
                }

                if (texture == null)
                    throw new InvalidOperationException("Unable to decode image: " + imageFilePath);

                // JPEG has no transparency, so show transparent areas as white paper.
                var pixels = texture.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                {
                    var alpha = pixels[i].a;
                    if (alpha == byte.MaxValue)
                        continue;
                    var paper = byte.MaxValue * (byte.MaxValue - alpha);
                    pixels[i] = new Color32((byte) ((pixels[i].r * alpha + paper) / byte.MaxValue),
                        (byte) ((pixels[i].g * alpha + paper) / byte.MaxValue),
                        (byte) ((pixels[i].b * alpha + paper) / byte.MaxValue), byte.MaxValue);
                }

                texture.SetPixels32(pixels);
                pdf.AddImage(imageFilePath, texture.EncodeToJPG(90), texture.width, texture.height);
            }
            catch (Exception e)
            {
                const string FileOpErrorMessage = "Failed to load image file for PDF rendering: ";
                Debug.LogError(FileOpErrorMessage + imageFilePath + " - " + e.Message);
                throw;
            }
            finally
            {
                // Export runs within one frame, so free each decoded image now rather than at the frame's end.
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
