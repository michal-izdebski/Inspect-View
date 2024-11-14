using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Inspect_View
{
    // Command name            [Send by whom] Description
    // "IV_HANDSHAKE"          [Master] Send handshake - used to test connection between master and slave
    // "IV_INSPECT_OK"         [Master] Inspection successfull
    // "IV_INSPECT_NOK"        [Master] Inspection unsuccessfull
    // "IV_HANDSHAKE_OK"       [Slave] Respond to master handshake
    // "IV_DO_INSPECT"         [Slave] Send signal to do inspection 

    public partial class MainWindowViewModel
    {
        /// <summary>
        /// Handler used for recieving all data incoming from serial device. It is using different thread than main window one
        /// </summary>
        /// <param name="sender">Object sending event</param>
        /// <param name="e">Event arguments</param>
        public void RecieveData(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            String dataRecieved = "";

            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    dataRecieved += serialPort.ReadLine();
                }

                switch (dataRecieved)
                {
                    case "IV_HANDSHAKE_OK":
                        App.Current.Dispatcher.BeginInvoke((Action)(() => {
                            MessageBox.Show(mainWindow, "Serial port device sent HANDSHAKE_OK response", "Handshake ok", MessageBoxButton.OK, MessageBoxImage.Information);
                        }));
                        break;

                    case "IV_DO_INSPECT":
                        App.Current.Dispatcher.BeginInvoke((Action)(() => {
                            if(InspectAll())
                            {
                                serialPort.Write("IV_INSPECT_OK\n");
                            }
                            else
                            {
                                serialPort.Write("IV_INSPECT_NOK\n");
                            }
                        }));
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.BeginInvoke((Action)(() => {
                    MessageBox.Show(mainWindow, "Serial port device data recieve error\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));

                //Data recieve unsuccessfull - discard rest of data in buffer
                serialPort.DiscardInBuffer();
            }
        }
    }
}
