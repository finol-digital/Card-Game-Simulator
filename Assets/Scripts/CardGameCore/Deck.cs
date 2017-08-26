using System.Collections;
using System.Collections.Generic;

public class Deck
{
    private List<Card> cards = new List<Card>();

    // TODO: some methods to shuffle, peek, etc.

    public List<Card> Cards {
        get {
            return cards;
        }
    }
}
