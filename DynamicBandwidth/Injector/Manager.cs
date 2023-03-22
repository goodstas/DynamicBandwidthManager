using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BandwidthManagerInterface;
using StackExchange.Redis;

namespace DataHandlerBL
{
    public class Manager
    {
        private static Manager _manager;
        private  Manager() { }

        public static Manager Instance
        {
            get {
                if (_manager == null ) _manager = new Manager();
                return _manager;
            }
        }

        public void HandleMessage(Byte[] data, ConnectionMultiplexer Connection)
        {
            Message message = new Message(data);
            DAL.SaveData(Connection, message);
            
        }
    }
}
