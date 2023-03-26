
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
using DynamicBandwidthCommon;
using DynamicBandwidthCommon.Classes;

namespace Injector
{
    class RedisSender
    {
        //parameters
        bool stopSending = false;
        ConnectionMultiplexer Connection;
        IDatabase RedisDB;
        sealed record Message(byte[] Data);

        //singleton for RedisSender
        #region RedisSender Singleton
        private static RedisSender? instance = null;
        private static readonly object Padlock = new object();

        private RedisSender()
        {

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
        public void OpenConnection(string address, int prometheusPort)
        {
            //redis connection
            Connection = StackExchange.Redis.ConnectionMultiplexer.Connect(address);
            RedisDB = Connection.GetDatabase();
            stopSending = false;
            //prometheus connection
            var server = new KestrelMetricServer(port: prometheusPort);
            server.Start();
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
            var subscriber = Connection.GetSubscriber();
            int totalData = 0;
            //for each data type
            for (int i = 0; i < InjectionManager.Instance.DataTypeAmount; i++)
            {
                int totalTypeData = 0;
                Injection injection = InjectionManager.Instance.GetInjection(i);
                //times message per seconds
                for (int j = 0; j < injection.MessagesPerSecond; j++)
                {
                    //send message
                    Random random = new Random();
                    int deviationPercentage = random.Next(
                        -1 * InjectionManager.Instance.DataSizeDeviationPrecentage,
                        InjectionManager.Instance.DataSizeDeviationPrecentage);
                    int dataSizeToSend = injection.MessageSize * (100 - deviationPercentage) / 100;
                    var data = new byte[dataSizeToSend];
                    subscriber.Publish(injection.Channel, data);
                    totalTypeData += dataSizeToSend;
                }
                //send data to prometheus
                SendDataToPrometheus(totalTypeData, injection.Channel);
                totalData += totalTypeData;
            }
            //send total data to prometheus
            SendDataToPrometheus(totalData);
        }

        //stop sending data
        public void StopSending()
        {
            stopSending = true;
            Thread.Sleep(1000);
        }

        //send injection data to prometheus
        private void SendDataToPrometheus(int dataSize, string type = null)
        {
            if(type == null)
            {

            }
            else
            {

            }
        }
    }
}
