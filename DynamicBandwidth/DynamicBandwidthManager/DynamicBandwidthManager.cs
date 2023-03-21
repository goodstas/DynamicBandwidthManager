using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;
using System.Diagnostics;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManager : BackgroundService
    {
        private ILogger<DynamicBandwidthManager>     _logger;
        private readonly TimeSpan                    _wakeUpPeriod;
        private DynamicBandwidthManagerConfiguration _config;

        private SortedList<double, string>           _remainderPriorities;

        private RedisMessageUtility     _redisMessageUtility;

        private RedisConnectionProvider _redisProvider;

        private ConnectionMultiplexer   _connectionMultiplexer;
        private ISubscriber             _redisPublisher;

        private Dictionary<string, Dictionary<MessagePriority, Queue<MessageHeader>>> _dataStorage;

        private long _lastScanTimeStamp = 0;
        private long _currScanTimeStamp = 0;

        public DynamicBandwidthManager(RedisMessageUtility redisMessageUtility , ILogger<DynamicBandwidthManager> logger, IOptions<DynamicBandwidthManagerConfiguration> config)
        {
            _config = config.Value;

            _redisProvider         = new RedisConnectionProvider($"redis://{_config.RedisConnectionString}");
            
            _connectionMultiplexer = ConnectionMultiplexer.Connect(_config.RedisConnectionString);
            _redisPublisher = _connectionMultiplexer.GetSubscriber();

            var wasCreated = _redisProvider.Connection.CreateIndex(typeof(MessageHeader));

            _redisMessageUtility   = redisMessageUtility;

            _logger = logger;
            
            _wakeUpPeriod = TimeSpan.FromSeconds(config.Value.PeriodInSec);

            IsEnabled = config.Value.Enabled;

            _dataStorage = new Dictionary<string, Dictionary<MessagePriority, Queue<MessageHeader>>>();

            foreach (var dataType in _config.DataTypes)
            {              
                if (!_dataStorage.ContainsKey(dataType))
                {
                    _dataStorage.Add(dataType, new Dictionary<MessagePriority, Queue<MessageHeader>>());
                }

                foreach (var priority in Enum.GetValues(typeof(MessagePriority)))
                {
                    if ((MessagePriority)priority == MessagePriority.None) continue;

                    if (!_dataStorage[dataType].ContainsKey((MessagePriority)priority))
                    {
                        _dataStorage[dataType].Add((MessagePriority)priority, new Queue<MessageHeader>());
                    }
                }
            }
            
            ParseConfig();
        }

        public bool IsEnabled { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopwatch   = new Stopwatch();

            var  sleepPeriod         = _wakeUpPeriod;
            long elapsedMilliseconds = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                stopwatch.Restart();
                try
                {
                    if (IsEnabled)
                    {
                        var now = DateTime.Now;
                        _currScanTimeStamp = now.Ticks;

                        _logger.LogInformation($"====================================================================================");
                        _logger.LogInformation($"Previous round took {elapsedMilliseconds} milliseconds");
                        _logger.LogInformation($"Start new round at: {now.ToString("HH:mm:ss.fff")}");

                        FillDataStorage();

                        //THE END
                        _lastScanTimeStamp = _currScanTimeStamp;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(
                          $"Failed to execute DynamicBandwidthManager with exception message {ex.Message}. Good luck next round!");
                }
                finally
                {
                    stopwatch.Stop();
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            
                    if (stopwatch.ElapsedTicks < _wakeUpPeriod.Ticks)
                    {
                        await Task.Delay(new TimeSpan(_wakeUpPeriod.Ticks - stopwatch.ElapsedTicks));
                    }
                }
            }
        }

        private void FillDataStorage()
        {
            var messageHeaders = _redisProvider.RedisCollection<MessageHeader>();

            foreach (var dataType in _config.DataTypes)
            {
                foreach (var priority in Enum.GetValues(typeof(MessagePriority)))
                {
                    var currMessagePriority = (int)(MessagePriority)priority;
                    if (currMessagePriority == (int)MessagePriority.None) continue;

                    var newMessageHeaders = messageHeaders.Where(header => header.DataType == dataType && header.Priority == currMessagePriority &&
                                                                 header.TimeStamp > _lastScanTimeStamp && header.TimeStamp <= _currScanTimeStamp)
                                                          .OrderBy(header => header.TimeStamp).Select(header => header);

                    var numofMessages = newMessageHeaders.ToList<MessageHeader>().Count;
                    foreach (var newMessageHeader in newMessageHeaders)
                    {
                        
                    }
                }
            }

        }

        private Chunk BuildChunk(List<MessageHeader> messageHeaders) 
        {
            var chunk = new Chunk();

            var ids = messageHeaders.Select(header => $"{nameof(Message)}:{header.Id}").ToList();
            chunk.MessagesIds = ids;

            return  chunk;
        }

        private async Task SendChunk(Chunk chunk)
        {
            var jsonChunk = JsonConvert.SerializeObject(chunk);

            await _redisPublisher.PublishAsync(_config.ChunkChannelName, jsonChunk, CommandFlags.FireAndForget);
        }

        #region Private Methods

        private void ParseConfig()
        {
            _remainderPriorities = new SortedList<double, string>();
            foreach (var chunkConfig in _config.ChunksConfiguration)
            {
                _remainderPriorities.Add(chunkConfig.Value.RemainderPriority, chunkConfig.Key);
            }
        }

        #endregion
    }

    record DynamicBandwidthManagerState(bool IsEnabled);
}
