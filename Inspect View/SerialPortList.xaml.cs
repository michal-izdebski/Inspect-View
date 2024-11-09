using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;


using System.IO.Ports;
using Emgu.CV;


namespace Inspect_View
{
    /// <summary>
    /// Logika interakcji dla klasy SerialPortList.xaml
    /// </summary>
    public partial class SerialPortList : Window
    {
        private MainWindow mainWindow;

        public SerialPortList(MainWindow mainWindow)
        {
            InitializeComponent();

            GetPortList();
            this.mainWindow = mainWindow;
        }

        private void ConnectPort_Click(object sender, RoutedEventArgs e)
        {
            if (PortListBox.SelectedIndex != -1)
            {
                if (mainWindow.connectedPort != null && mainWindow.connectedPort.IsOpen) mainWindow.connectedPort.Close();
                mainWindow.connectedPort = new()
                {
                    PortName = PortListBox.SelectedValue.ToString(),
                    BaudRate = 9600
                };

                mainWindow.connectedPort.Open();

                if(!mainWindow.connectedPort.IsOpen)
                {
                    MessageBox.Show(this, "Could not connect to selected COM port", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else this.Close();
            }
        }

        private void SearchPorts_Click(object sender, RoutedEventArgs e)
        {
            GetPortList();
        }

        private void GetPortList()
        {
            string[] ports = SerialPort.GetPortNames();
            PortListBox.ItemsSource = ports;
        }
    }
}
