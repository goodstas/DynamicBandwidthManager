using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Injector
{
    class RedisSender
    {
        //parameters
        bool stopSending = false;
        StackExchange.Redis.ConnectionMultiplexer Connection;
        StackExchange.Redis.IDatabase RedisDB;
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
        public void OpenConnection(string address)
        {
            Connection = StackExchange.Redis.ConnectionMultiplexer.Connect(address);
            RedisDB = Connection.GetDatabase();
            stopSending = false;
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
            //for each data type
            for (int i = 0; i < InjectionManager.Instance.DataTypeAmount; i++)
            {
                Injection injection = InjectionManager.Instance.GetInjection(i);
                //times message per seconds
                for (int j = 0; j < injection.MessagesPerSecond; j++)
                {
                    //send message
                    var data = new byte[injection.MessageSize];
                    var json = JsonSerializer.Serialize(data);
                    subscriber.Publish(injection.Channel, json);
                }
            }
        }

        //stop sending data
        public void StopSending()
        {
            stopSending = true;
            Thread.Sleep(1000);
        }
    }
}
