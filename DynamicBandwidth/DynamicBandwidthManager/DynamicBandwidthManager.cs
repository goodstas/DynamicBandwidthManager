using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;
using System.Diagnostics;
using System.Runtime;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManager : BackgroundService
    {
        private ILogger<DynamicBandwidthManager>     _logger;
        private readonly TimeSpan                    _wakeUpPeriod;
        private DynamicBandwidthManagerConfiguration _config;

        private SortedList<double, string>           _dataTypesPriorities;

        private RedisMessageUtility     _redisMessageUtility;

        private RedisConnectionProvider _redisProvider;

        private ConnectionMultiplexer   _connectionMultiplexer;
        private ISubscriber             _redisPublisher;

        private Dictionary<string, PriorityQueue<MessageHeader, int>> _dataStorage;

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

            _dataStorage = new Dictionary<string, PriorityQueue<MessageHeader, int>>();

            foreach (var dataType in _config.ChunksConfiguration.Keys)
            {              
                if (!_dataStorage.ContainsKey(dataType))
                {
                    _dataStorage.Add(dataType, new PriorityQueue<MessageHeader,int>());
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

                        var chunk = CreateChunk();

                        await SendChunk(chunk);

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

            foreach (var dataType in _config.ChunksConfiguration.Keys)
            {
                foreach (var priority in Enum.GetValues(typeof(MessagePriority)))
                {
                    var messagePriority     = (MessagePriority)priority;
                    var currMessagePriority = (int)(MessagePriority)priority;
             
                    var newMessageHeaders = messageHeaders.Where(header => header.DataType == dataType && header.Priority == currMessagePriority &&
                                                                 header.TimeStamp > _lastScanTimeStamp && header.TimeStamp <= _currScanTimeStamp)
                                                          .OrderBy(header => header.TimeStamp).Select(header => header);
                                        
                    foreach (var newMessageHeader in newMessageHeaders)
                    {
                        _dataStorage[newMessageHeader.DataType].Enqueue(newMessageHeader, currMessagePriority);
                    }
                }
            }
        }

        private Chunk CreateChunk()
        {            
            var messgesList = new List<MessageHeader>();
            var totalSizeAccumulator = 0;
            var totalCount = 0;

            var chunkStatistics = new Dictionary<string, DataStatistics>();
            foreach (var dataType in _config.ChunksConfiguration.Keys)
            {
                chunkStatistics[dataType] = new DataStatistics();
            }

            var stopSqueezing = false;

            MessageHeader topMessage = null;
            int topMessagePriority   = 0;
            var stopWithDataType     = false;

            var leftBytes = 0;
            var prevLeftBytes = 0;

            bool isRound1 = true;

            var dataTypeSizeLimits = new Dictionary<string, int>();

            foreach (var dataType in _config.ChunksConfiguration.Keys)
            {
                dataTypeSizeLimits.Add(dataType, _config.ChunksConfiguration[dataType].SizeInBytes);
            }

            while (!stopSqueezing)
            {
                //we always take messages from the queue with bigger priority
                foreach (var dataType in _dataTypesPriorities.Values)
                {
                    stopWithDataType = false;
                    
                    //Because of using PriorityQueue (default ascending order) -> the lowest value gives higher priority
                    while (!stopWithDataType)
                    {
                        if (_dataStorage[dataType].TryPeek(out topMessage, out topMessagePriority))
                        {
                            if (topMessage.DataSize + chunkStatistics[dataType].Size > dataTypeSizeLimits[dataType])
                            {
                                stopWithDataType = true;
                            }
                            else
                            {
                                //next message  is already copied to the topMessage that's why we only need to dequeue it
                                _dataStorage[dataType].Dequeue();

                                //update statistics
                                chunkStatistics[dataType].Size += topMessage.DataSize;
                                chunkStatistics[dataType].Count += 1;

                                totalSizeAccumulator += topMessage.DataSize;
                                totalCount += 1;
                            }
                        }
                        else
                        {
                            stopWithDataType = true;
                        }
                    }

                    //squeezing rounds : recalculate left bytes after squeezing messages from each Data Type queue and update Size Limits
                    if (!isRound1)
                    {
                        leftBytes = CalculateLeftBytes(totalSizeAccumulator, dataTypeSizeLimits);
                    }
                }

                prevLeftBytes = leftBytes;
                leftBytes     = CalculateLeftBytes(totalSizeAccumulator, dataTypeSizeLimits);

                if (leftBytes <= 0) stopSqueezing = true;

                if (leftBytes == prevLeftBytes) stopSqueezing = true;

                if(isRound1) isRound1 = false;
            }
            
            var chunk = BuildChunk(messgesList, chunkStatistics, totalSizeAccumulator, totalCount);
            return chunk;
        }

        private int CalculateLeftBytes(int totalSizeAccumulator, Dictionary<string, int> dataTypeSizeLimits)
        {
            var leftBytes = _config.TotalBytes - totalSizeAccumulator;

            if (leftBytes < 0)
            {
                Console.WriteLine("Configuration Error : the value of summary for sizes of all data types is larger than total bytes number.");
                return 0;
            }
        
            foreach (var dataType in _config.ChunksConfiguration.Keys)
            {
                dataTypeSizeLimits[dataType] = leftBytes;
            }
        
            return leftBytes;
        }

        private Chunk BuildChunk(List<MessageHeader> messageHeaders, Dictionary<string, DataStatistics> statistics, int totalMessageSize, int totalMessageCount)
        {
            var chunk = new Chunk()
            {                
                Size  = totalMessageSize,
                Count = totalMessageCount,
                MessagesIds        = messageHeaders.Select(header => $"{nameof(Message)}:{header.Id}").ToList(),
                MessagesStatistics = statistics
            };

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
            _dataTypesPriorities = new SortedList<double, string>();
            foreach (var chunkConfig in _config.ChunksConfiguration)
            {
                _dataTypesPriorities.Add(chunkConfig.Value.RemainderPriority, chunkConfig.Key);
            }
        }

        #endregion
    }

    record DynamicBandwidthManagerState(bool IsEnabled);
}
