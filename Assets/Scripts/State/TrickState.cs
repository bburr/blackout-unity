using System;
using Unity.Netcode;

public struct TrickState : INetworkSerializable
{
    public CardState LeadingCard;
    public CardState[] PlayedCards;
    public int TrickWinnerIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LeadingCard);
        serializer.SerializeValue(ref PlayedCards);
        serializer.SerializeValue(ref TrickWinnerIndex);
    }
}
