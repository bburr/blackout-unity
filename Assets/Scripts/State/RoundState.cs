using System;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class RoundState : INetworkSerializable
{
    public static int NumPlayers;
    
    public int RoundNumber;
    public int NumTricks;
    public bool IsNumTricksAscending;
    public int PlayerIndexToBet;
    public int PlayerIndexToPlay;
    public int[] Bets = new int[NumPlayers];
    public CardState TrumpCard;
    public TrickState CurrentTrick;
    public TrickState[] CompletedTricks;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref RoundNumber);
        serializer.SerializeValue(ref NumTricks);
        serializer.SerializeValue(ref IsNumTricksAscending);
        serializer.SerializeValue(ref TrumpCard);
        serializer.SerializeValue(ref PlayerIndexToBet);
        serializer.SerializeValue(ref PlayerIndexToPlay);
        serializer.SerializeValue(ref Bets);
        serializer.SerializeValue(ref CurrentTrick);
        serializer.SerializeValue(ref CompletedTricks);
    }
}