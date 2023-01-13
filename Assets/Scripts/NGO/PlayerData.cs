using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerData : INetworkSerializable
{
    public string name;
    public ulong id;

    // private string _hand = "";

    public CardState[] Hand = Array.Empty<CardState>();
    // {
        // get => StateConverter.DeserializeCardList(_hand);
        // set => _hand = StateConverter.SerializeCardList(value);
    // }

    public PlayerData()
    {
    }

    public PlayerData(string name, ulong id)
    {
        this.name = name;
        this.id = id;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref Hand);
    }
}