namespace DynamicBandwidthCommon.Classes;

public class Chunk
{
    // number of messages in chunk
    public int Count { get; set; }

    // total size of chunk messages
    public int Size { get; set; } = 0;

    // statistics for each DataType for current chunk.
    // dictionary key is DataType
    public Dictionary<string, DataStatistics> MessagesStatistics { get; set; } = new();

    // list of Ids to take from redis
    public List<Ulid> MessagesIds { get; set; } = new();
}

public class DataStatistics
{
    // number of messages of current DataType
    public int Count { get; set; } = 0;

    // total size of messages for current DataType
    public int Size { get; set; } = 0;
}