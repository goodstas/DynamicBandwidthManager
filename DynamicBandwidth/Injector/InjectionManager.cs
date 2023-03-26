using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Injector
{
    class Injection
    {
        public string Channel { get; set; }
        public int MessageSize { get; set; }
        public int MessagesPerSecond { get; set; }
    }

    class InjectionManager
    {
        //parameters
        public string RedisAddress { get; set; }
        public int PrometheusPort { get; set; }
        public int DataTypeAmount { get; set; }
        public int DataSizeDeviationPrecentage { get; set; }
        List<Injection> injectionList = new List<Injection>();

        //singleton for InjectionManager
        #region InjectionManager Singleton
        private static InjectionManager? instance = null;
        private static readonly object Padlock = new object();

        private InjectionManager()
        {

        }

        public static InjectionManager Instance
        {
            get
            {
                lock (Padlock)
                {
                    return instance ??= new InjectionManager();
                }
            }
        }
        #endregion

        //read new injection parameters from file
        public void ReadNewInjectionFile(string path, DataGrid injectionDataGrid)
        {
            injectionList.Clear();
            IniFile iniFile = new IniFile(path);
            RedisAddress = iniFile.Read("RedisAddress", "General");
            PrometheusPort = int.Parse(iniFile.Read("PrometheusPort", "General"));
            DataTypeAmount = int.Parse(iniFile.Read("DataTypeAmount", "General"));
            DataSizeDeviationPrecentage = int.Parse(iniFile.Read("DataSizeDeviationPrecentage", "General"));
            for (int i = 1; i <= DataTypeAmount; i++)
            {
                Injection injection = new Injection();
                injection.Channel = iniFile.Read("Channel", "DataType" + i);
                injection.MessageSize = int.Parse(iniFile.Read("MessageSize", "DataType" + i));
                injection.MessagesPerSecond = int.Parse(iniFile.Read("MessagesPerSecond", "DataType" + i));
                injectionList.Add(injection);
            }
            //update gui
            injectionDataGrid.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                injectionDataGrid.ItemsSource = null;
                injectionDataGrid.ItemsSource = injectionList;
            }));
        }

        //get injection from index
        public Injection GetInjection(int index)
        {
            return injectionList[index];
        }
    }
}
