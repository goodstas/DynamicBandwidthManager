using DataHandlerBL;
using StackExchange.Redis;

namespace DataHandler
{
    public class Consumer : BackgroundService
    {
        private readonly ILogger<Consumer> _logger;

        private static readonly string ConnectionString = "localhost:6379";

        private static readonly ConnectionMultiplexer Connection = ConnectionMultiplexer.Connect(ConnectionString);

        private const string Channel = "test-channel";

        public Consumer(ILogger<Consumer> logger) { _logger = logger; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var subscriber = Connection.GetSubscriber();

            await subscriber.SubscribeAsync(Channel, (channel,data) =>
            {
                Manager.Instance.HandleMessage(data, Connection);

                _logger.LogInformation($"Received message");
            });
        }
    }
}
