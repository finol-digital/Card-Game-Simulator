using System;
using System.Collections;
using System.Collections.Generic;

public class Deck
{
    public string Name { get; set; }

    private List<Card> cards;

    public Deck(string name, string definition = "")
    {
        Name = name;
        cards = new List<Card>();
        DefineFromString(definition);
    }

    public void DefineFromString(string definition)
    {
        cards.Clear();
        foreach (string line in definition.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
            if (line.Length == 0 || line [0] == '#')
                continue;

            int numCopies = 1;
            string cardName = line.Trim();
            if (cardName.Contains(" ") && int.TryParse(cardName.Split(' ') [0], out numCopies))
                cardName = cardName.Substring(cardName.IndexOf(' ') + 1);

            foreach (Card card in CardGameManager.CurrentCardGame.FilterCards("", cardName, "", new Dictionary<string, string>())) {
                for (int i = 0; i < numCopies; i++)
                    cards.Add(card);
                break;
            }

        }
    }

    public override string ToString()
    {
        string definition = "# " + CardGameManager.CurrentGameName + " Deck List" + System.Environment.NewLine;
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

    public List<Card> Cards {
        get {
            return cards;
        }
    }
}
