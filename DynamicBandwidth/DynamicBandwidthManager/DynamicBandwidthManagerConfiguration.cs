using DynamicBandwidthCommon;
using System.Net;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManagerConfiguration
    {
        public string RedisConnectionString { get; set; }
        public bool Enabled { get; set; }
        public int TotalBytes  { get; set; }

        public uint PeriodInSec { get; set; }

        public Dictionary<string, DataTypeChunkConfiguration> ChunksConfiguration { get; set; }

        public string ChunkChannelName { get; set; }
    }

    public class DataTypeChunkConfiguration
    {
        public double RemainderPriority { get; set; }

        public int SizeInBytes { get; set; }


    }
}
