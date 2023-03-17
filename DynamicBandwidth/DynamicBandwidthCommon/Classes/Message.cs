﻿using Redis.OM.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon
{
    [Document(StorageType = StorageType.Json, Prefixes = new string[] { "Message" }, IndexName = "Message-idx")]
    public class Message
    {
        [RedisIdField]
        [Indexed]
        public Ulid Id { get; set; }

        public byte[] Data { get; set; }
    }
}
