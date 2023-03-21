using DynamicBandwidthCommon;
using System.Net;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManagerConfiguration
    {
        public string RedisConnectionString { get; set; }
        public bool Enabled { get; set; }
        public uint TotalBytes  { get; set; }

        public uint PeriodInSec { get; set; }

        public Dictionary<string, DataTypeChunkConfiguration> ChunksConfiguration { get; set; }

        public string ChunkChannelName { get; set; }

        public List<string> DataTypes { get; set; }
    }

    public class DataTypeChunkConfiguration
    {
        public double RemainderPriority { get; set; }

        public uint SizeInBytes { get; set; }


    }
}
