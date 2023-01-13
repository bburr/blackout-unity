using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct CardState : IEquatable<CardState>, INetworkSerializable
{
    public char SuitKey;
    public int ValueKey;

    private static Dictionary<char, string> _suits;
    
    public CardState(char suitKey, int valueKey)
    {
        SuitKey = suitKey;
        ValueKey = valueKey;
    }

    public bool Equals(CardState other)
    {
        return SuitKey == other.SuitKey && ValueKey == other.ValueKey;
    }

    public override string ToString()
    {
        return $"{ValueKey} of {SuitKey}";
    }

    public bool IsEmpty()
    {
        return SuitKey == default(char);
    }

    public static Dictionary<char, string> ListSuits()
    {
        return _suits ??= new Dictionary<char, string>
        {
            { 'D', "Diamonds" },
            { 'C', "Clubs" },
            { 'H', "Hearts" },
            { 'S', "Spades" },
        };
    }

    public static string[] ListValues()
    {
        return new []
        {
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "J",
            "Q",
            "K",
            "A",
        };
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SuitKey);
        serializer.SerializeValue(ref ValueKey);
    }
}
