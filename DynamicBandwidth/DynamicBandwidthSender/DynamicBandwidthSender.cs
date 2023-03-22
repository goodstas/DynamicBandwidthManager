using System.Text.Json;
using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;
using Prometheus;
using Redis.OM;
using StackExchange.Redis;

namespace DynamicBandwidthSender;

public class DynamicBandwidthSender : BackgroundService
{
    #region Constructor

    public DynamicBandwidthSender(ILogger<DynamicBandwidthSender> logger, IConfiguration config)
    {
        _logger = logger;

        // get connection string for Redis
        var connectionString = config.GetConnectionString("REDIS_CONNECTION_STRING");
        try
        {
            // connect to Redis
            _connection = ConnectionMultiplexer.Connect(connectionString);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, e.Message);
        }

        // initialize redis provider
        _provider = new($"redis://{connectionString}");

        // initialize metrics
        foreach (var dataType in DataTypes) MessageMetrics.Add(dataType, new(dataType));
    }

    #endregion

    #region Message Metric

    // a class to hold Messages Metrics for Prometheus
    private class MessageMetric
    {
        #region Fields
        // number of messages of current type in current chunk
        private readonly Gauge _count;

        // total number of bytes sent of current type in current chunk
        private readonly Gauge _size;
        #endregion

        #region Constructor

        // default constructor for Total Metric
        public MessageMetric()
        {
            _size = Metrics.CreateGauge("total_messages_size", "Number of bytes that has been sent");
            _count = Metrics.CreateGauge("total_messages_count", "Number of messages that has been sent");
        } 

        //constructor for specific Data Type
        internal MessageMetric(string Type)
        {
            var lower = Type.ToLower().Replace(" ", "_");
            _size = Metrics.CreateGauge($"{lower}_messages_size",
                $"Number of bytes that has been sent for type {Type}");
            _count = Metrics.CreateGauge($"{lower}_messages_count",
                $"Number of messages that has been sent for type {Type}");
        }
        #endregion

        #region Methods
        // set gauges values
        public void Set(DataStatistics statistics)
        {
            _size.Set(statistics.Size);
            _count.Set(statistics.Count);
        }

        public void Set(int count, int size)
        {
            _size.Set(size);
            _count.Set(count);
        } 
        #endregion
    }

    #endregion

    #region Fields

    //types of messages
    private static readonly string[] DataTypes = {"Track", "Plot", "Sensor Status", "Mission Status"};

    // metrics to display with Prometheus and Grafana
    private static readonly MessageMetric Total = new();
    private static readonly Dictionary<string, MessageMetric> MessageMetrics = new();

    // channel to subscribe to Redis in order to get chunks
    private const string ChunkChannel = "chunk-channel";

    // connection handler to Redis
    private static ConnectionMultiplexer? _connection;

    // logger 
    private readonly ILogger<DynamicBandwidthSender> _logger;

    // an instance of Redis Provider for fetching messages from redis
    private readonly RedisConnectionProvider _provider;

    // cancellation token for exit
    private CancellationToken _cancel;

    // an instance of Redis Subscriber for subscribe to get chunks
    private ISubscriber? _subscriber;

    #endregion

    #region Methods

    /// <summary>
    ///     main function of the service
    /// </summary>
    /// <param name="stoppingToken">stop when set to cancel</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancel = stoppingToken;

        if (_connection != null)
        {
            // subscribe to Redis for getting chunks
            _subscriber = _connection.GetSubscriber();
            await _subscriber.SubscribeAsync(ChunkChannel, SendMessages);
        }
    }

    /// <summary>
    ///     received chunk from Manger, send all the messages that are in it
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="json"></param>
    private async void SendMessages(RedisChannel channel, RedisValue json)
    {
        // got cancelled
        if (_cancel.IsCancellationRequested)
        {
            // unsubscribe from Redis
            if (_subscriber != null) await _subscriber.UnsubscribeAsync(ChunkChannel);
            return;
        }

        // parse the chunk
        var chunk = JsonSerializer.Deserialize<Chunk>(json);
        if (chunk == null) return;

        // fetch the messages
        var ids = chunk.MessagesIds.Select(id => $"{nameof(Message)}:{id}");
        var messages = await _provider.RedisCollection<Message>().FindByIdsAsync(ids);

        // it's a demo - just log messages types and sizes
        foreach (var message in messages)
            if (message.Value != null)
                _logger.Log(LogLevel.Debug,
                    $"Send message: {message.Value.DataType}, Size: {message.Value.Data.Length} bytes");

        // set metrics
        Total.Set(chunk.Count, chunk.Size);
        foreach (var dataType in chunk.MessagesStatistics.Keys.Where(dataType => MessageMetrics.ContainsKey(dataType)))
            MessageMetrics[dataType].Set(chunk.MessagesStatistics[dataType]);
    }

    #endregion
}