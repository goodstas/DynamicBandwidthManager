using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon.Classes
{
    public class Chunk
    {
        public int Size { get; set; }
        public Dictionary<string, int> MessagesStatistics { get; set; } = new Dictionary<string, int>();
        public List<string> MessagesIds { get; set; } = new List<string>();
    }
}
