public class DynamicBandwidthSenderConfiguration
{
    public string? RedisConnectionString { get; set; }
    public string? ChunkChannelName { get; set; }

    public string[]? DataTypes { get; set; }
}