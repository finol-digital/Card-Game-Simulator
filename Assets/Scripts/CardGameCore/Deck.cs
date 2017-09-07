using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Deck
{
    public string Name { get; set; }

    public List<Card> Cards { get; set; }

    public Deck(string name, string definition = "")
    {
        Name = name;
        Cards = new List<Card>();
        FromString(definition);
    }

    public void FromString(string definition)
    {
        if (definition == null)
            return;

        Cards.Clear();
        foreach (string line in definition.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
            if (line.Length == 0 || line [0] == '#')
                continue;

            int numCopies = 1;
            string cardName = line.Trim();
            if (cardName.Contains(" ") && int.TryParse(cardName.Split(' ') [0], out numCopies))
                cardName = cardName.Substring(cardName.IndexOf(' ') + 1);

            List<Card> matchingCards = CardGameManager.Current.Cards.Where((card) => string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase)).ToList<Card>();
            for (int i = 0; matchingCards.Count > 0 && i < numCopies; i++)
                Cards.Add(matchingCards [0]);
        }
    }

    public override string ToString()
    {
        string definition = "# " + CardGameManager.Current.Name + " Deck List: " + Name + System.Environment.NewLine;
        Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        foreach (Card card in Cards) {
            int currentCount = 0;
            cardCounts.TryGetValue(card.Name, out currentCount);
            currentCount++;
            cardCounts [card.Name] = currentCount;
        }

        foreach (string cardName in cardCounts.Keys)
            definition += cardCounts [cardName] + " " + cardName + System.Environment.NewLine;
        
        return definition;
    }

    public string FilePath {
        get {
            return CardGameManager.Current.DecksFilePath + "/" + Name + "." + CardGameManager.Current.DeckFileType;
        }
    }
}
