using Redis.OM.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon
{
    public class Message
    {
        [RedisIdField]
        [Indexed]
        public Ulid Id { get; set; }

        public byte[] Data { get; set; }
    }
}
