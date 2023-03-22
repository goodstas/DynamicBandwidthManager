using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;
using Prometheus;
using Redis.OM;
using StackExchange.Redis;
using System.Text.Json;

namespace DynamicBandwidthSender;

public class DynamicBandwidthSender : BackgroundService
{
    private static ConnectionMultiplexer? _connection;
    private readonly string _chunkChannel = "chunk-channel";
    private readonly ILogger<DynamicBandwidthSender> _logger;
    private readonly RedisConnectionProvider _provider;

    public DynamicBandwidthSender(ILogger<DynamicBandwidthSender> logger, IConfiguration config)
    {
        _logger = logger;

        var connectionString = config.GetConnectionString("REDIS_CONNECTION_STRING");
        try
        {
            _connection = ConnectionMultiplexer.Connect(connectionString);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, e.Message);
        }

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
        var chunk = JsonSerializer.Deserialize<Chunk>(json);
        if (chunk == null) return;

        var ids = chunk.MessagesIds.Select(id => $"{nameof(Message)}:{id}");

        var messages = await _provider.RedisCollection<Message>().FindByIdsAsync(ids);

        foreach (var message in messages)
            if (message.Value != null)
                _logger.Log(LogLevel.Debug, $"Send message: {message.Value.DataType}, Size: {message.Value.Data.Length} bytes");

        // create metrics and view in grafana

    }
}