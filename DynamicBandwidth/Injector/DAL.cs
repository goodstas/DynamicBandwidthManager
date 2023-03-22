using System.Text.Json;
using BandwidthManagerInterface;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DataHandlerBL
{
    public class DAL
    {
        private static ILogger _logger;

        public DAL(ILogger logger)
        {
            _logger = logger;
        }

        public static bool SaveData(ConnectionMultiplexer Connection, Message message)
        {
      

            IDatabase RedisDB = Connection.GetDatabase((int)RedisDBInstance.HeaderDB);
            string json = JsonSerializer.Serialize<Message>(message);
            var added =  RedisDB.StringSet(message.Id.ToString(), json);

            if (added)
            {
                _logger.LogInformation($"New message with ID={message.Id} was successfully saved in HeaderDB.");
            }
            else
            {
                _logger.LogWarning($"The specified key {message.Id} is already exists");
                return false;
            }

             RedisDB = Connection.GetDatabase((int)RedisDBInstance.DataDB);
            added = RedisDB.StringSet(message.Id.ToString(), message.Data);

            if (added)
            {
                _logger.LogInformation($"New message with ID={message.Id} was successfully saved in DataDB.");
            }
            else
            {
                _logger.LogWarning($"The specified key {message.Id} is already exists");
                return false;
            }

            return true;

        }
    }
}