using Emgu.CV;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using Emgu.CV.Structure;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Drawing;
using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using System.IO.Ports;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace Inspect_View
{
    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            viewModel = new MainWindowViewModel(this);
            DataContext = viewModel;

            Closing += viewModel.OnWindowClosing;
        }


        /// <summary>
        /// Temporary keyboard button handling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape:
                    if (viewModel.inspectionStart) break;
                    if (ToolCircle.IsChecked == true) ToolCircle.IsChecked = false;
                    else if (ToolRectangle.IsChecked == true) ToolRectangle.IsChecked = false;
                    break;

                case Key.Delete:
                    if (viewModel.inspectionStart) break;
                    if (viewModel.selectedLimiter != null)
                    {
                        viewModel.selectedLimiter.isSelected = false;
                        viewModel.imageInspectionLimiters.Remove(viewModel.selectedLimiter);
                        viewModel.colorDataPanel = false;

                        viewModel.RefreshImages();
                    }
                    break;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PixelCountBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(PixelCountBox.Text == "") PixelCountBox.Text = "0";
        }

        private void PixelDeltaBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PixelDeltaBox.Text == "") PixelDeltaBox.Text = "0";
        }

        private void PixelCountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (PixelCountBox.Text != "") viewModel.pixelCount = int.Parse(PixelCountBox.Text);
            }
            catch
            {
                viewModel.pixelCount = 0;
            }
        }

        private void PixelDeltaBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (PixelDeltaBox.Text != "") viewModel.pixelCountDelta = int.Parse(PixelDeltaBox.Text);
            }
            catch
            {
                viewModel.pixelCountDelta = 0;
            }
        }

        public void EnableAll(bool enable)
        {
            FileNew.IsEnabled = enable;
            FileOpen.IsEnabled = enable;
            FileSave.IsEnabled = enable;
            FileSaveAs.IsEnabled = enable;

            ConnectCamera.IsEnabled = enable;
            ConnectSerialPort.IsEnabled = enable;

            TakePhotoButton.IsEnabled = enable;
            ToolCircle.IsEnabled = enable;
            ToolRectangle.IsEnabled = enable;
            TestButton.IsEnabled = enable;

            MainGrid.IsEnabled = enable;
        }

        //Using events for mouse was a lot less complicated than making commands work
        //TODO Move mouse events to commands somehow?
        private void ViewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewModel.OnLeftMouseDown((System.Windows.Controls.Image)sender, e);
        }

        private void ViewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            viewModel.OnLeftMouseUp((System.Windows.Controls.Image)sender, e);
        }

        private void ViewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            viewModel.OnMouseWheel(e);
        }

        private void RedMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.selectedLimiter != null)
            {
                if (viewModel.colorMaskData.redMin > viewModel.colorMaskData.redMax)
                {
                    viewModel.colorMaskData.redMax = viewModel.colorMaskData.redMin;
                }

                viewModel.SetColorDataPanel();
                viewModel.RefreshImages();
            }
        }

        private void RedMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.selectedLimiter != null)
            {
                if (viewModel.colorMaskData.redMax < viewModel.colorMaskData.redMin)
                {
                    viewModel.colorMaskData.redMin = viewModel.colorMaskData.redMax;
                }

                viewModel.SetColorDataPanel();
                viewModel.RefreshImages();
            }
        }

        private void GreenMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.selectedLimiter != null)
            {
                if (viewModel.colorMaskData.greenMin > viewModel.colorMaskData.greenMax)
                {
                    viewModel.colorMaskData.greenMax = viewModel.colorMaskData.greenMin;
                }

                viewModel.SetColorDataPanel();
                viewModel.RefreshImages();
            }
        }

        private void GreenMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.selectedLimiter != null)
            {
                if (viewModel.colorMaskData.greenMax < viewModel.colorMaskData.greenMin)
                {
                    viewModel.colorMaskData.greenMin = viewModel.colorMaskData.greenMax;
                }

                viewModel.SetColorDataPanel();
                viewModel.RefreshImages();
            }
        }

        private void BlueMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.selectedLimiter != null)
            {
                if (viewModel.colorMaskData.blueMin > viewModel.colorMaskData.blueMax)
                {
                    viewModel.colorMaskData.blueMax = viewModel.colorMaskData.blueMin;
                }

                viewModel.SetColorDataPanel();
                viewModel.RefreshImages();
            }
        }

        private void BlueMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.selectedLimiter != null)
            {
                if (viewModel.colorMaskData.blueMax < viewModel.colorMaskData.blueMin)
                {
                    viewModel.colorMaskData.blueMin = viewModel.colorMaskData.blueMax;
                }

                viewModel.SetColorDataPanel();
                viewModel.RefreshImages();
            }
        }

        private void ColorDataName_TextChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.limiterName = ((TextBox)sender).Text;
        }
    }
}