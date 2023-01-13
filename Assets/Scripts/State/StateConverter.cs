using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class StateConverter
{
    public static List<CardState> DeserializeCardList(string serializedCards)
    {
        return Regex.Matches(serializedCards, "([CDHS][0-9]{1,2})")
            .Select(card => new CardState(card.ToString().ToCharArray(0, 1)[0], int.Parse(card.ToString().Substring(1, card.ToString().Length - 1))))
            .ToList();
    }
    
    public static CardState DeserializeCardState(string serializedCard)
    {
        return DeserializeCardList(serializedCard)[0];
    }
    
    public static string SerializeCardList(List<CardState> cards)
    {
        return cards.Select(SerializeCardState).Aggregate("", (acc, c) => acc + c);
    }

    public static string SerializeCardState(CardState cardState)
    {
        return cardState.SuitKey + cardState.ValueKey.ToString();
    }
}