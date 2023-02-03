using Unity.Netcode;

public class RoundScoreState : INetworkSerializable
{
    public int RoundNumber;
    public int[] Scores;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref RoundNumber);
        serializer.SerializeValue(ref Scores);
    }
}
