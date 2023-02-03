using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerData : INetworkSerializable
{
    public string name;
    public ulong id;

    public CardState[] Hand = Array.Empty<CardState>();

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