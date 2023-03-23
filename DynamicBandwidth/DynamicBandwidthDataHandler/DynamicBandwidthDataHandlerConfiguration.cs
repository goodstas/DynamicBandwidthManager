namespace DynamicBandwidthDataHandler
{
    public class DynamicBandwidthDataHandlerConfiguration
    {
        public string RedisConnectionString { get; set; }
        public List<string> Channels { get; set; }

        public int MessageTTLInSec { get; set; }

        public int MessageHeaderTTLInSec { get; set; }
    }
}
