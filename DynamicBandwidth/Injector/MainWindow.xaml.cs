using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Injector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            startInjection.IsEnabled = false;
        }

        //read new injection file
        private void SelectInjectionClick(object sender, RoutedEventArgs e)
        {
            //ini file pick dialog
            string path = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files(.ini)|*.ini";
            if (openFileDialog.ShowDialog() == true)
            {
                path = openFileDialog.FileName;
                //create new injection list
                Task.Factory.StartNew(() => { InjectionManager.Instance.ReadNewInjectionFile(path, injectionsDataGrid); });
                startInjection.IsEnabled = true;
            }
        }

        //start injecting
        private void StartInjectionChecked(object sender, RoutedEventArgs e)
        {
            startInjection.Content = "Stop Injection";
            RedisSender.Instance.OpenConnection(InjectionManager.Instance.ReditAddress);
            Task.Factory.StartNew(() => { RedisSender.Instance.PeriodicSendThread(); });
        }

        //stop injecting
        private void StartInjectionUnchecked(object sender, RoutedEventArgs e)
        {
            startInjection.Content = "Start Injection";
            RedisSender.Instance.StopSending();
        }
    }
}
