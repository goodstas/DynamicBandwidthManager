using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace DynamicBandwidth
{
    public class DynamicBandwidthManager : BackgroundService
    {
        private ILogger<DynamicBandwidthManager> _logger;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(1);

        public DynamicBandwidthManager(ILogger<DynamicBandwidthManager> logger)
        {
            _logger = logger;
            IsEnabled = true;
        }

        public bool IsEnabled { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopwatch   = new Stopwatch();

            var  sleepPeriod         = _period;
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
            
                    if (stopwatch.ElapsedTicks < _period.Ticks)
                    {
                        await Task.Delay(new TimeSpan(_period.Ticks - stopwatch.ElapsedTicks));
                    }
                }
            }
        }
    }

    record DynamicBandwidthManagerState(bool IsEnabled);
}
