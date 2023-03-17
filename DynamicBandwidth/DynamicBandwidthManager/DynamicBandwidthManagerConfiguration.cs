﻿using DynamicBandwidthCommon;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManagerConfiguration
    {
        public bool Enabled { get; set; }
        public uint TotalBytes  { get; set; }

        public uint PeriodInSec { get; set; }

        public Dictionary<DataType, DataTypeChunkConfiguration> ChunksConfiguration { get; set; }
    }

    public class DataTypeChunkConfiguration
    {
        public double RemainderPriority { get; set; }

        public uint SizeInBytes { get; set; }


    }
}
