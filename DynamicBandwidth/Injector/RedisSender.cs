using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Prometheus;

namespace Injector
{
    class RedisSender
    {
        //parameters
        bool stopSending = false;
        StackExchange.Redis.ConnectionMultiplexer Connection;
        StackExchange.Redis.IDatabase RedisDB;
        sealed record Message(Guid Id, DateTime CreatedOnUtc, byte[] Data);

        //singleton for RedisSender
        #region RedisSender Singleton
        private static RedisSender? instance = null;
        private static readonly object Padlock = new object();
        private Counter recordsProcessed;

        private RedisSender()
        {
            // Prometheus
            var server = new KestrelMetricServer(port: 8083);
            server.Start();

            // Generate some sample data from fake business logic.
            recordsProcessed = Metrics.CreateCounter("sample_records_processed_total", "Total number of records processed.");
        }

        public static RedisSender Instance
        {
            get
            {
                lock (Padlock)
                {
                    return instance ??= new RedisSender();
                }
            }
        }
        #endregion

        //open connection with redis
        public void OpenConnection(string address)
        {
            //Redis
            //Connection = StackExchange.Redis.ConnectionMultiplexer.Connect(address);
            //RedisDB = Connection.GetDatabase();
            //stopSending = false;



        }

        //periodically send data as loaded from injection file
        public void PeriodicSendThread()
        {
            while (!stopSending)
            {
                Task.Factory.StartNew(() => { SendOneSecondInjection(); });
                Thread.Sleep(1000);
            }
        }

        //send 1 second injection data
        public void SendOneSecondInjection()
        {
            //var subscriber = Connection.GetSubscriber();
            //for each data type
            for (int i = 0; i < InjectionManager.Instance.DataTypeAmount; i++)
            {
                Injection injection = InjectionManager.Instance.GetInjection(i);
                //times message per seconds
                for (int j = 0; j < injection.MessagesPerSecond; j++)
                {
                    //send message
                    var guid = Guid.NewGuid();
                    var data = new byte[injection.MessageSize];
                    var message = new Message(guid, DateTime.UtcNow, data);
                    var json = JsonSerializer.Serialize(message);
                    //subscriber.Publish(injection.Channel, json);
                }
            }

            recordsProcessed.Inc();


        }

        //stop sending data
        public void StopSending()
        {
            stopSending = true;
            Thread.Sleep(1000);
        }
    }
}
