/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef.Decks;
using Didstopia.PDFSharp;
using Didstopia.PDFSharp.Drawing;
using Didstopia.PDFSharp.Pdf;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;

namespace CardGameDef.Unity
{
    public class UnityDeck : Deck
    {
        private const float PrintPdfWidth = 8.5f;
        private const float PrintPdfHeight = 11f;
        private const float PrintPdfMargin = 0.5f;
        private const int PrintPdfPixelsPerInch = 72;

        public string FilePath => ((UnityCardGame) SourceGame).DecksDirectoryPath + Path.DirectorySeparatorChar +
                                  UnityFileMethods.GetSafeFileName(Name + "." + FileType.ToString().ToLower());

        private string PrintPdfDirectory => ((UnityCardGame) SourceGame).DecksDirectoryPath + "/printpdf";

        private string PrintPdfFilePath =>
            PrintPdfDirectory + "/" + UnityFileMethods.GetSafeFileName(Name + ".pdf");

        public override IReadOnlyCollection<Card> Cards => new List<Card>(_cards);
        private readonly List<UnityCard> _cards;

        public UnityDeck(UnityCardGame sourceGame, string name = DefaultName, DeckFileType fileType = DeckFileType.Txt,
            IReadOnlyCollection<UnityCard> cards = null) : base(sourceGame, name, fileType, cards)
        {
            SourceGame = sourceGame ?? UnityCardGame.UnityInvalid;
            _cards = cards != null ? new List<UnityCard>(cards) : new List<UnityCard>();
        }

        public static UnityDeck Parse(UnityCardGame cardGame, string deckName, DeckFileType deckFileType,
            string deckText)
        {
            var deck = new UnityDeck(cardGame, deckName, deckFileType);
            if (string.IsNullOrEmpty(deckText))
                return deck;

            foreach (string line in deckText.Split('\n').Select(x => x.Trim()))
            {
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
                    case DeckFileType.Txt:
                    default:
                        if (line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:"))
                            return deck;
                        deck.LoadTxt(line);
                        break;
                }
            }

            return deck;
        }

        private void LoadDec(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("SB:"))
                return;

            var cardCount = 1;
            List<string> tokens = line.Split(' ').ToList();
            if (tokens.Count > 0 && int.TryParse(tokens[0], out cardCount))
                tokens.RemoveAt(0);
            string cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            IEnumerable<UnityCard> cards =
                ((UnityCardGame) SourceGame).FilterCards(new CardSearchFilters() {Name = cardName});
            foreach (UnityCard card in cards)
            {
                if (!string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase))
                    continue;
                for (var i = 0; i < cardCount; i++)
                    _cards.Add(card);
                break;
            }
        }

        private void LoadHsd(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (line.StartsWith("#"))
            {
                if (line.StartsWith("###"))
                    Name = line.Substring(3).Trim();
                return;
            }

            byte[] bytes = Convert.FromBase64String(line);
            ulong offset = 3;

            var heroesCount = (int) Varint.Read(bytes, ref offset, out int _);
            for (var i = 0; i < heroesCount; i++)
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, (int) Varint.Read(bytes, ref offset, out int _), 1);

            var singleCardsCount = (int) Varint.Read(bytes, ref offset, out int _);
            for (var i = 0; i < singleCardsCount; i++)
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, (int) Varint.Read(bytes, ref offset, out int _), 1);

            var doubleCardsCount = (int) Varint.Read(bytes, ref offset, out int _);
            for (var i = 0; i < doubleCardsCount; i++)
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, (int) Varint.Read(bytes, ref offset, out int _), 2);

            var multiCardsCount = (int) Varint.Read(bytes, ref offset, out int _);
            for (var i = 0; i < multiCardsCount; i++)
            {
                var id = (int) Varint.Read(bytes, ref offset, out int _);
                var count = (int) Varint.Read(bytes, ref offset, out int _);
                AddCardsByPropertyInt(SourceGame.DeckFileAltId, id, count);
            }

            Sort();
        }

        private void AddCardsByPropertyInt(string propertyName, int propertyValue, int count)
        {
            UnityCard card = ((UnityCardGame) SourceGame).Cards.Values.FirstOrDefault(currCard =>
                currCard.GetPropertyValueInt(propertyName) == propertyValue);
            for (var i = 0; card != null && i < count; i++)
                _cards.Add(card);
        }

        private void AddCardsByPropertyString(string propertyName, string propertyValue, int count)
        {
            UnityCard card = ((UnityCardGame) SourceGame).Cards.Values.FirstOrDefault(currCard =>
                currCard.GetPropertyValueString(propertyName).Equals(propertyValue));
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
                    Name = line.Substring(3).Trim();
                return;
            }

            List<CardCodeAndCount> cardCodeAndCounts = LoRDeckEncoder.GetDeckFromCode(line);
            foreach (CardCodeAndCount cardCount in cardCodeAndCounts)
            {
                if (!((UnityCardGame) SourceGame).Cards.TryGetValue(cardCount.CardCode, out UnityCard card))
                    continue;
                for (var i = 0; i < cardCount.Count; i++)
                    _cards.Add(card);
            }
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
            string cardName = line;
            var cardId = string.Empty;
            var cardSet = string.Empty;
            if (line.Contains(" "))
            {
                List<string> tokens = line.Split(' ').ToList();
                if (tokens.Count > 0 &&
                    int.TryParse(
                        (tokens[0].StartsWith("x") || tokens[0].EndsWith("x")) ? tokens[0].Replace("x", "") : tokens[0],
                        out cardCount))
                    tokens.RemoveAt(0);

                if (tokens.Count > 0 && tokens[0].StartsWith("[") && tokens[0].EndsWith("]"))
                {
                    cardId = tokens[0].Substring(1, tokens[0].Length - 2);
                    tokens.RemoveAt(0);
                }

                if (tokens.Count > 0 && tokens[tokens.Count - 1].StartsWith("(") &&
                    tokens[tokens.Count - 1].EndsWith(")"))
                {
                    string inParens = tokens[tokens.Count - 1].Substring(1, tokens[tokens.Count - 1].Length - 2);
                    if (((UnityCardGame) SourceGame).Sets.ContainsKey(inParens))
                    {
                        cardSet = inParens;
                        tokens.RemoveAt(tokens.Count - 1);
                    }
                }

                cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            }

            IEnumerable<UnityCard> cards = ((UnityCardGame) SourceGame).FilterCards(new CardSearchFilters()
                {Id = cardId, Name = cardName, SetCode = cardSet});
            foreach (UnityCard card in cards)
            {
                if (!card.Id.Equals(cardId) &&
                    (!string.Equals(card.Name.Trim(), cardName, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrEmpty(cardSet) &&
                      !cardSet.Equals(card.SetCode, StringComparison.OrdinalIgnoreCase))))
                    continue;
                for (var i = 0; i < cardCount; i++)
                    _cards.Add(card);
                break;
            }
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
            if (!Directory.Exists(PrintPdfDirectory))
                Directory.CreateDirectory(PrintPdfDirectory);

            ImageSource.ImageSourceImpl = new ImageSharpImageSource();
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = Name;

            var cardsPerRow = (int) Math.Floor((PrintPdfWidth - PrintPdfMargin * 2) / SourceGame.CardSize.X);
            var rowsPerPage = (int) Math.Floor((PrintPdfHeight - PrintPdfMargin * 2) / SourceGame.CardSize.Y);
            int cardsPerPage = cardsPerRow * rowsPerPage;
            PdfPage page = null;
            XGraphics gfx = null;
            double px = PrintPdfMargin * PrintPdfPixelsPerInch, py = PrintPdfMargin * PrintPdfPixelsPerInch;
            for (var cardNumber = 0; cardNumber < Cards.Count; cardNumber++)
            {
                if (page == null || cardNumber % cardsPerPage == 0)
                {
                    page = pdfDocument.AddPage();
                    page.Size = PageSize.Letter;
                    gfx = XGraphics.FromPdfPage(page);
                    py = PrintPdfMargin * PrintPdfPixelsPerInch;
                }

                XImage image = XImage.FromFile(_cards[cardNumber].ImageFilePath);
                gfx.DrawImage(image, px, py, SourceGame.CardSize.X * PrintPdfPixelsPerInch,
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

            return new Uri(UnityFileMethods.FilePrefix + PrintPdfFilePath);
        }
    }
}
