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
    /// Window for serial device selection. 
    /// </summary>
    public partial class SerialPortList : Window
    {
        /// <summary> ViewModel for main window </summary>
        private MainWindowViewModel viewModel;

        public SerialPortList(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            GetPortList();
            this.viewModel = viewModel;
        }

        /// <summary>
        /// Button click event handler used for connecting to serial device selected on list
        /// </summary>
        /// <param name="sender">Object sending event</param>
        /// <param name="e">Event arguments</param>
        private void ConnectPort_Click(object sender, RoutedEventArgs e)
        {
            if (PortListBox.SelectedIndex != -1)
            {
                if (viewModel.connectedPort != null && viewModel.connectedPort.IsOpen) viewModel.connectedPort.Close();
                viewModel.connectedPort = new()
                {
                    PortName = PortListBox.SelectedValue.ToString(),
                    BaudRate = 9600,

                    ReadTimeout = 5000,
                    WriteTimeout = 5000
                };

                viewModel.connectedPort.Open();

                if(!viewModel.connectedPort.IsOpen)
                {
                    MessageBox.Show(this, "Could not connect to selected COM port", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    //Discard serial port buffer in case there are data there already
                    viewModel.connectedPort.DiscardInBuffer();

                    viewModel.connectedPort.DataReceived += viewModel.RecieveData;

                    this.Close();
                }
            }
        }

        private void SearchPorts_Click(object sender, RoutedEventArgs e)
        {
            GetPortList();
        }

        /// <summary>
        /// Refresh serial device list
        /// </summary>
        private void GetPortList()
        {
            string[] ports = SerialPort.GetPortNames();
            PortListBox.ItemsSource = ports;
        }
    }
}
