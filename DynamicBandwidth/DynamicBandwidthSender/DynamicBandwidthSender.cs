using System.Text.Json;
using DynamicBandwidthCommon;
using Redis.OM;
using StackExchange.Redis;

namespace DynamicBandwidthSender;

public class DynamicBandwidthSender : BackgroundService
{
    private static ConnectionMultiplexer? _connection;
    private readonly ILogger<DynamicBandwidthSender> _logger;
    private readonly RedisConnectionProvider _provider;
    private readonly string _chunkChannel = "chunk-channel";

    public DynamicBandwidthSender(ILogger<DynamicBandwidthSender> logger, IConfiguration config)
    {
        _logger = logger;

        var connectionString = config.GetConnectionString("REDIS_CONNECTION_STRING");
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _provider = new($"redis://{connectionString}");
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection != null)
        {
            var subscriber = _connection.GetSubscriber();
            await subscriber.SubscribeAsync(_chunkChannel, SendMessages);
        }
    }

    private async void SendMessages(RedisChannel channel, RedisValue json)
    {
        var ids = JsonSerializer.Deserialize<string[]>(json);
        if (ids == null) return;

        var messages = await _provider.RedisCollection<Message>().FindByIdsAsync(ids);

        foreach (var message in messages)
        {
            // create metrics and view in grafana
        }
    }
}