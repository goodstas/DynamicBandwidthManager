﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;
using DynamicBandwidthDataHandler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace DataHandlerBL
{
    public class DynamicBandwidthDataHandlerService : BackgroundService
    {
        private ILogger                 _logger;
        private RedisMessageUtility     _redisMessageUtility;
        private RedisConnectionProvider _redisProvider;
        private ConnectionMultiplexer   _connectionMultiplexer;
        private ISubscriber             _redisSubscriber;

        private DynamicBandwidthDataHandlerConfiguration _config;

        private string _channelsList;

        public DynamicBandwidthDataHandlerService(IOptions<DynamicBandwidthDataHandlerCommandArgs> args, ILogger<DynamicBandwidthDataHandlerService> logger, RedisMessageUtility utility , IOptions<DynamicBandwidthDataHandlerConfiguration> config) 
        {
            _channelsList = args.Value.ChannelsList;

            _logger              = logger;
            _redisMessageUtility = utility;
            _config              = config.Value;

            _redisProvider = new RedisConnectionProvider($"redis://{_config.RedisConnectionString}");

            _connectionMultiplexer = ConnectionMultiplexer.Connect(_config.RedisConnectionString);

            _redisSubscriber = _connectionMultiplexer.GetSubscriber();

            var wasCreated = _redisProvider.Connection.CreateIndex(typeof(MessageHeader));
            wasCreated     = _redisProvider.Connection.CreateIndex(typeof(Message));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<Task> subscriptions = new List<Task>();

            Task currSubscriptionTask;

            var messagesCollection       = (RedisCollection<Message>)_redisProvider.RedisCollection<Message>();
            var messageHeadersCollection = (RedisCollection<MessageHeader>)_redisProvider.RedisCollection<MessageHeader>();

            var channels = ParseCommandArgsAndConfig();

            foreach (var dataType in channels)
            {
                _logger.LogInformation($"Subsribe to {dataType} channel.");

                _redisSubscriber.Subscribe(dataType, (channel, data) =>
                {
                    var (message, messageHeader) = _redisMessageUtility.CreateNewMessage(data, MessagePriority.Normal, channel);
                    
                    var messages = new List<Message>();
                    messages.Add(message);
                    
                    var messageHeaders = new List<MessageHeader>();
                    messageHeaders.Add(messageHeader);
                    
                    _redisMessageUtility.SaveMessage(messagesCollection, messageHeadersCollection,                       
                        messages, messageHeaders, _config.MessageTTLInSec, _config.MessageHeaderTTLInSec);

                    _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss.fff")} : Received message from channel {dataType}.");
                });
            }

           _logger.LogInformation("Subsription to all channels was completed");

           await Task.Delay(Timeout.Infinite);
        }

        private IEnumerable<string> ParseCommandArgsAndConfig()
        {
            var channels = new List<string>();

            if (!string.IsNullOrEmpty(_channelsList))
            {
                var currChannels = _channelsList.Split(" ");
                foreach (var channel in currChannels)
                {
                    if(_config.Channels.Contains(channel))
                        channels.Add(channel);
                }
            }
            else
            {
                channels.AddRange(_config.Channels);
            }

            return channels;
        }
    }
}
