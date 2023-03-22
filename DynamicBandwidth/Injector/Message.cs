using System.Text.Json;
using System.Text.Json.Serialization;

namespace BandwidthManagerInterface
{
    public class Message 
    {
        public Message(Byte[] data)
        {
            Id = new Guid();
            DateTimeUTC = DateTime.UtcNow;
            MessageSizeb = Data.Length;
            Data = data;
        }

        public Guid Id { get; set; }
        public DateTime DateTimeUTC { get; set; }

        public double MessageSizeb { get; set; }

        [JsonIgnore]
        public Byte[] Data { get; set; }

        public short Priority { get; set; }
    }
}