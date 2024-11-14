using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Emgu.CV;
using System.Runtime.InteropServices;
using System.Drawing;
using Emgu.CV.Structure;
using System.Threading;

namespace Inspect_View
{
    /// <summary>
    /// Window for camera selection. 
    /// </summary>
    public partial class CameraList : Window
    {
        /// <summary> ViewModel for main window </summary>
        private MainWindowViewModel viewModel;

        public CameraList(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            
            this.viewModel = viewModel;
        }

        /// <summary>
        /// Validation for text box to include only numbers
        /// </summary>
        /// <param name="sender">Object sending event</param>
        /// <param name="e">Event arguments</param>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Button click event handler used for connecting to camera selected on list
        /// </summary>
        /// <param name="sender">Object sending event</param>
        /// <param name="e">Event arguments</param>
        private void ConnectCamera_Click(object sender, RoutedEventArgs e)
        {
            if (CameraListBox.SelectedIndex != -1)
            {
                if(viewModel.connectedCamera != null && viewModel.connectedCamera.IsOpened)
                {
                    viewModel.connectedCamera.Release();
                }

                viewModel.connectedCamera = new VideoCapture(CameraListBox.SelectedIndex, VideoCapture.API.DShow);
                viewModel.connectedCamera.Set(Emgu.CV.CvEnum.CapProp.Buffersize, 0);

                if (!viewModel.connectedCamera.IsOpened)
                {
                    MessageBox.Show(this, "Could not connect to selected camera", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }

        /// <summary>
        /// Button click event handler used for refreshing camera list
        /// </summary>
        /// <param name="sender">Object sending event</param>
        /// <param name="e">Event arguments</param>
        private void SearchCameras_Click(object sender, RoutedEventArgs e)
        {
            List<string> cameraList = new List<string>();

            //I could not find fool-proof method of getting camera list, that also found all cameras I have connected so I'm using OpenCV function to connect to camera and checking if connection is successfull
            //Also because in some cases camera numbers aren't in order, I'm giving user possibility to look for specific amount of cameras
            int maxCameras = Convert.ToInt32(CameraSearchNumber.Text);

            for (int i = 0; i < maxCameras; i++)
            {
                var camera = new VideoCapture(i, VideoCapture.API.DShow);

                if (camera.IsOpened)
                {
                    cameraList.Add("Camera " + i);
                    camera.Release();
                }
            }

            CameraListBox.ItemsSource = cameraList.ToArray();
        }
    }
}
