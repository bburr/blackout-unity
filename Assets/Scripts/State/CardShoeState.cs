using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardShoeState
{
    private Queue<CardState> _cards;

    public CardShoeState(Queue<CardState> cards)
    {
        _cards = cards;
    }

    public CardShoeState(int numDecks = 1)
    {
        var allCards = new List<CardState>();
        
        for (var i = 0; i < numDecks; i++)
        {
            foreach (var suitKey in CardState.ListSuits().Keys)
            {
                for (var valueKey = 0; valueKey < CardState.ListValues().Length; valueKey++)
                {
                    allCards.Add(new CardState(suitKey, valueKey));
                }
            }
        }

        _cards = new Queue<CardState>(allCards.OrderBy(a => Random.value));
    }

    public CardState DealCard()
    {
        return _cards.Dequeue();
    }
}