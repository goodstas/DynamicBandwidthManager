using System.Text.Json;
using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;
using Prometheus;
using Redis.OM;
using StackExchange.Redis;

namespace DynamicBandwidthSender;

public class DynamicBandwidthSender : BackgroundService
{
    private static ConnectionMultiplexer? _connection;

    private static readonly string[] _dataTypes = {"Track", "Plot", "Sensor Status", "Mission Status"};

    private static readonly MessageMetric Total = new();
    private static readonly Dictionary<string,MessageMetric> MessageMetrics = new();

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

        foreach (var dataType in _dataTypes)
        {
            MessageMetrics.Add(dataType,new(dataType));
        }
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
                _logger.Log(LogLevel.Debug,
                    $"Send message: {message.Value.DataType}, Size: {message.Value.Data.Length} bytes");

        // create metrics and view in grafana

        Total.Set(chunk.Count,chunk.Size);

        foreach (var dataType in chunk.MessagesStatistics.Keys.Where(dataType => MessageMetrics.ContainsKey(dataType)))
        {
            MessageMetrics[dataType].Set(chunk.MessagesStatistics[dataType]);
        }
    }

    internal class MessageMetric
    {
        public MessageMetric()
        {
            _size = Metrics.CreateGauge("total_messages_size", "Number of bytes that has been sent");
            _count = Metrics.CreateGauge("total_messages_count", "Number of messages that has been sent");
        }


        internal MessageMetric(string Type)
        {
            var lower = Type.ToLower().Replace(" ","_");
            _size = Metrics.CreateGauge($"{lower}_messages_size",
                $"Number of bytes that has been sent for type {Type}");
            _count = Metrics.CreateGauge($"{lower}_messages_count",
                $"Number of messages that has been sent for type {Type}");
        }

        private readonly Gauge _size;
        private readonly Gauge _count;

        public void Set(DataStatistics statistics)
        {
            _size.Set(statistics.Size);
            _count.Set(statistics.Count);
        }

        public void Set(int count, int size)
        {
            _size.Set(size);
            _count.Set(count);        }
    }
}