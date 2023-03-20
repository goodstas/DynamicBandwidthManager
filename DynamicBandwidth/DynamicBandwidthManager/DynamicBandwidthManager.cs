using DynamicBandwidthCommon;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManager : BackgroundService
    {
        private ILogger<DynamicBandwidthManager>     _logger;
        private readonly TimeSpan                    _wakeUpPeriod;
        private DynamicBandwidthManagerConfiguration _config;

        private SortedList<double, DataType> _remainderPriorities;

        private RedisMessageUtility _redisMessageUtility;

        private long _lastTimeScan;

        public DynamicBandwidthManager(RedisMessageUtility redisMessageUtility , ILogger<DynamicBandwidthManager> logger, IOptions<DynamicBandwidthManagerConfiguration> config)
        {
            _redisMessageUtility = redisMessageUtility;
            _logger = logger;
            _config = config.Value;
            _wakeUpPeriod = TimeSpan.FromSeconds(config.Value.PeriodInSec);
            IsEnabled = config.Value.Enabled;

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
                        _logger.LogInformation($"====================================================================================");
                        _logger.LogInformation($"Previous round took {elapsedMilliseconds} milliseconds");
                        _logger.LogInformation($"Start new round at: {DateTime.Now.ToString("HH:mm:ss.fff")}");
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

        #region Private Methods

        private void ParseConfig()
        {
            _remainderPriorities = new SortedList<double, DataType>();
            foreach (var chunkConfig in _config.ChunksConfiguration)
            {
                _remainderPriorities.Add(chunkConfig.Value.RemainderPriority, chunkConfig.Key);
            }
        }

        #endregion
    }

    record DynamicBandwidthManagerState(bool IsEnabled);
}
