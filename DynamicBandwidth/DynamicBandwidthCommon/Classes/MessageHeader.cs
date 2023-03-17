using Redis.OM.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon
{
    [Document(StorageType = StorageType.Json, Prefixes =new string[] { "MessageHeader" }, IndexName = "MessageHeader-idx")]
    public class MessageHeader
    {
        [RedisIdField]
        [Indexed]
        public Ulid Id { get; set; }

        public double DataSize { get; set; }

        [Indexed(Aggregatable =true, Sortable = true)]
        public MessagePriority Priority { get; set; }

        [Indexed(Sortable = true)]
        public long TimeStamp { get; set; }
    }
}
