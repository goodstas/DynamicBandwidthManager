using Redis.OM.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon
{
    [Document(StorageType = StorageType.Json, Prefixes = new string[] { nameof(Message) }, IndexName = $"{nameof(Message)}-idx")]
    public class Message
    {
        [RedisIdField]
        [Indexed]
        public Ulid Id { get; set; }

        public string DataType { get; set; }

        public byte[] Data { get; set; }
    }
}
