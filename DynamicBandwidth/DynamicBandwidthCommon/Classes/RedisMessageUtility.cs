using Redis.OM;
using Redis.OM.Searching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon
{
    public static class RedisMessageUtility
    {
        public static (Message, MessageHeader) CreateNewMessage(byte[] data, MessagePriority priority)
        {
            var message = new Message()
            {
                Id   = Ulid.NewUlid(),
                Data = data
            };

            var messageHeader = new MessageHeader()
            {
                Id        = message.Id,
                Priority  = priority,
                DataSize  = data.Length,
                TimeStamp = DateTime.Now.Ticks
            };

            return(message, messageHeader);
        }

        public static async Task<string> SaveMessage(IRedisCollection<Message> messages, IRedisCollection<MessageHeader> messagesHeaders, IEnumerable<Message> newMessages, IEnumerable<MessageHeader> newMessageHeaders)
        {
            var error = string.Empty;

            try
            {
                var messageKeys = await messages.Insert(newMessages);

                var newMessageHeadersKeys = await messagesHeaders.Insert(newMessageHeaders);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return error;
        }
    }
}
