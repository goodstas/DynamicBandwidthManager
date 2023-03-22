using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlerBL
{
    public class DataHandlerConfig
    {
        public string RedisConnectionMultiplexer { get; set; }
       public  List<Channel> Channels { get; set; }

        public int RunningChannelID { get; set; }
    }

    public class Channel
    {
       public int ChannelID { get; set; }
        public string ChannelName { get; set; }
    }
}
