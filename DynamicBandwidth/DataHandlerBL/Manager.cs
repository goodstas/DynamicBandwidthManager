using System;
using System.Collections.Generic;
using System.Diagnostics;
using DynamicBandwidthCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace DataHandlerBL
{
    public class Manager : BackgroundService
    {
        private static Manager _manager;
        RedisMessageUtility _utility = new RedisMessageUtility();
        ILogger _logger;
        IConnectionMultiplexer _connectMulti;
        RedisConnectionProvider _redisProvider;
        IConfiguration _config;
        public  Manager(ILogger logger, RedisMessageUtility utility , RedisConnectionProvider redisProvider, IConnectionMultiplexer connectMulti,IConfiguration config) 
        {
            _logger = logger;
            _utility = utility;
            _connectMulti = connectMulti;
            _redisProvider = redisProvider;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopwatch = new Stopwatch();

            var sleepPeriod = 1000;
            long elapsedMilliseconds = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                stopwatch.Restart();
                try
                {

                    _logger.LogInformation($"====================================================================================");
                    _logger.LogInformation($"Previous round took {elapsedMilliseconds} milliseconds");
                    _logger.LogInformation($"Start new round at: {DateTime.Now.ToString("HH:mm:ss.fff")}");
                   
                    ISubscriber subscriber = _connectMulti.GetSubscriber();
                    string channel = _config.GetValue(typeof(string), "CHANNEL").ToString();

                    await subscriber.SubscribeAsync(channel, (channel, data) =>
                    {
                        (Message, MessageHeader) messageCreated = _utility.CreateNewMessage(data,MessagePriority.Normal);
                        List<Message> messages = new List<Message>();
                        messages.Add(messageCreated.Item1);
                        List<MessageHeader> messageHeaders = new List<MessageHeader>();
                        messageHeaders.Add(messageCreated.Item2);
                        _utility.SaveMessage((RedisCollection<Message>)_redisProvider.RedisCollection<Message>(),
                            (RedisCollection<MessageHeader>)_redisProvider.RedisCollection<MessageHeader>(),
                            messages, messageHeaders
                            );

                        _logger.LogInformation($"Received message");
                    });

                }
                catch (Exception ex)
                {
                    _logger.LogInformation(
                          $"Failed to execute DataHandler with exception message {ex.Message}. Good luck next round!");
                }
                finally
                {
                    stopwatch.Stop();
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    if (stopwatch.ElapsedTicks < sleepPeriod)
                    {
                        await Task.Delay(new TimeSpan(sleepPeriod - stopwatch.ElapsedTicks));
                    }
                }
            }
        }

    }
}
