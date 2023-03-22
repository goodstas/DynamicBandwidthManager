using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon.Classes
{
    public class Chunk
    {
        public int Count { get; set; }
        public int Size { get; set; } = 0;
        public Dictionary<string, DataStatistics> MessagesStatistics { get; set; } = new Dictionary<string, DataStatistics>();
        public List<Ulid> MessagesIds { get; set; } = new List<Ulid>();
    }

    public class DataStatistics
    {
        public int Count { get; set; } = 0;

        public int Size { get; set; } = 0;
    }
}
