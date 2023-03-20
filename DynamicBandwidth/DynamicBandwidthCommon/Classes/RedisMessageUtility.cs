using Redis.OM;
using Redis.OM.Searching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBandwidthCommon
{
    public class RedisMessageUtility
    {
        public (Message, MessageHeader) CreateNewMessage(byte[] data, MessagePriority priority)
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

        public async Task<string> SaveMessage(IRedisCollection<Message> messages, IRedisCollection<MessageHeader> messagesHeaders, IEnumerable<Message> newMessages,IEnumerable<MessageHeader> newMessageHeaders, int messageTTLInSec = -1, int messageHeaderTTLInSec = -1)
        {
            var error = string.Empty;

            try
            {
                List<string> messageKeys = null, newMessageHeadersKeys = null;
                TimeSpan ttl;

                if (messageTTLInSec > 0)
                {
                    ttl = new TimeSpan(0, 0, messageTTLInSec);
                    messageKeys = await messages.Insert(newMessages, ttl);
                }
                else
                {
                    messageKeys = await messages.Insert(newMessages);
                }

                if (messageHeaderTTLInSec > 0)
                {
                    ttl = new TimeSpan(0, 0, messageHeaderTTLInSec);
                    newMessageHeadersKeys = await messagesHeaders.Insert(newMessageHeaders, ttl);
                }
                else
                {
                    newMessageHeadersKeys = await messagesHeaders.Insert(newMessageHeaders);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return error;
        }
    }
}
