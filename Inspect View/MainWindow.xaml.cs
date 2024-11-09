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

namespace Inspect_View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String projectName;
        String projectPath;


        private CameraList? cameraListWindow;
        public VideoCapture? connectedCamera;
        private Mat? currentFrame;


        private SerialPortList? serialPortListWindow;
        public SerialPort connectedPort;


        public List<ImageInspectionLimiter> imageInspectionLimiters;
        ImageInspectionLimiter? selectedLimiter;

        public Vector limiterDragFirstPos;
        public Vector limiterDragRelativePos;
        public int dragMinVal;
        public bool isDragged;
        public int resizeStep;

        private int maskedPixelsCountCache;


        private bool isInspectionStarted;

        public MainWindow()
        {
            InitializeComponent();

            projectName = "";
            projectPath = "";

            imageInspectionLimiters = [];

            dragMinVal = 1;
            isDragged = false;
            resizeStep = 10;

            isInspectionStarted = false;
        }


        //TODO:
        //pozmieniać komentarze do konwersji

        /// <summary>
        /// Delete a GDI object
        /// </summary>
        /// <param name="o">The poniter to the GDI object to be deleted</param>
        /// <returns></returns>
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu CV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(Mat image)
        {
            Bitmap source = BitmapExtension.ToBitmap(image);

            IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(ptr); //release the HBitmap
            return bs;
        }


        public void TakePhoto()
        {
            if (connectedCamera != null && connectedCamera.IsOpened)
            {
                Mat frame = new();
                connectedCamera.Read(frame);
                connectedCamera.Read(frame);

                currentFrame = frame;

                RefreshImages();
            }
        }


        public void RefreshImages()
        {
            if (currentFrame != null)
            {
                Mat newFrame = currentFrame.Clone();

                foreach (ImageInspectionLimiter x in imageInspectionLimiters)
                {
                    MCvScalar circleColor1 = new(255, 255, 255);
                    MCvScalar circleColor2 = new(0, 0, 0);
                    if (x.isSelected)
                    {
                        circleColor1 = new MCvScalar(0, 0, 0);
                        circleColor2 = new MCvScalar(255, 255, 255);
                    }

                    if (x.rectangle == true)
                    {
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(x.position, new System.Drawing.Size(x.width, x.height));
                        CvInvoke.Rectangle(newFrame, rect, circleColor2, 2);
                        CvInvoke.Rectangle(newFrame, rect, circleColor1, 1);
                    }
                    else
                    {
                        RotatedRect rect = new RotatedRect(x.position, new SizeF(x.width, x.height), 0);
                        CvInvoke.Ellipse(newFrame, rect, circleColor2, 2);
                        CvInvoke.Ellipse(newFrame, rect, circleColor1, 1);
                    }
                }


                Image<Bgr, byte> frame = currentFrame.ToImage<Bgr, byte>();


                if (selectedLimiter != null)
                {
                    selectedLimiter.RefreshMask(currentFrame);

                    int blueMin = selectedLimiter.colorData.blueMin;
                    int blueMax = selectedLimiter.colorData.blueMax;
                    int redMin = selectedLimiter.colorData.redMin;
                    int redMax = selectedLimiter.colorData.redMax;
                    int greenMin = selectedLimiter.colorData.greenMin;
                    int greenMax = selectedLimiter.colorData.greenMax;

                    Image<Gray, byte> inRangeView = frame.InRange(new Bgr(blueMin, greenMin, redMin), new Bgr(blueMax, greenMax, redMax));

                    Image<Gray, byte> maskLimiter = selectedLimiter.limiterMask.ToImage<Gray, byte>();
                    Image<Gray, byte> inRangeMasked = inRangeView.Copy();

                    maskedPixelsCountCache = 0;

                    for (int i = 0; i < maskLimiter.Cols; i++)
                    {
                        for (int j = 0; j < maskLimiter.Rows; j++)
                        {
                            if (maskLimiter[j, i].Intensity == 0)
                            {
                                inRangeMasked[j, i] = new Gray(0);
                            }
                        }
                    }

                    for (int i = 0; i < inRangeMasked.Cols; i++)
                    {
                        for (int j = 0; j < inRangeMasked.Rows; j++)
                        {
                            if (inRangeMasked[j, i].Intensity != 0)
                            {
                                maskedPixelsCountCache++;
                            }
                        }
                    }

                    LimiterMaskLabel.Content = "Limiter Mask (" + maskedPixelsCountCache.ToString() + " pixels counted)";

                    MaskImage.Source = ToBitmapSource(inRangeView.Mat);
                    ViewLimiterMask.Source = ToBitmapSource(inRangeMasked.Mat);
                }

                ViewImage.Source = ToBitmapSource(newFrame);
            }
        }


        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ConnectCamera_Click(object sender, RoutedEventArgs e)
        {
            cameraListWindow = new CameraList(this);
            cameraListWindow.ShowDialog();
        }

        private void RedMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedLimiter != null)
            {
                if (RedMinSlider.Value > RedMaxSlider.Value)
                {
                    RedMaxSlider.Value = RedMinSlider.Value;

                    selectedLimiter.colorData.redMax = (int)RedMaxSlider.Value;
                }

                RedMinLabel.Content = ((int)RedMinSlider.Value).ToString();
                selectedLimiter.colorData.redMin = (int)RedMinSlider.Value;

                RefreshImages();
            }
        }

        private void RedMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedLimiter != null)
            {
                if (RedMaxSlider.Value < RedMinSlider.Value)
                {
                    RedMinSlider.Value = RedMaxSlider.Value;
                    selectedLimiter.colorData.redMin = (int)RedMinSlider.Value;
                }

                RedMaxLabel.Content = ((int)RedMaxSlider.Value).ToString();
                selectedLimiter.colorData.redMax = (int)RedMaxSlider.Value;

                RefreshImages();
            }
        }

        private void GreenMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedLimiter != null)
            {
                if (GreenMinSlider.Value > GreenMaxSlider.Value)
                {
                    GreenMaxSlider.Value = GreenMinSlider.Value;

                    selectedLimiter.colorData.greenMax = (int)GreenMaxSlider.Value;
                }

                GreenMinLabel.Content = ((int)GreenMinSlider.Value).ToString();
                selectedLimiter.colorData.greenMin = (int)GreenMinSlider.Value;

                RefreshImages();
            }
        }

        private void GreenMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedLimiter != null)
            {
                if (GreenMaxSlider.Value < GreenMinSlider.Value)
                {
                    GreenMinSlider.Value = GreenMaxSlider.Value;
                    selectedLimiter.colorData.greenMin = (int)GreenMinSlider.Value;
                }

                GreenMaxLabel.Content = ((int)GreenMaxSlider.Value).ToString();
                selectedLimiter.colorData.greenMax = (int)GreenMaxSlider.Value;

                RefreshImages();
            }
        }

        private void BlueMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedLimiter != null)
            {
                if (BlueMinSlider.Value > BlueMaxSlider.Value)
                {
                    BlueMaxSlider.Value = BlueMinSlider.Value;

                    selectedLimiter.colorData.blueMax = (int)BlueMaxSlider.Value;
                }

                BlueMinLabel.Content = ((int)BlueMinSlider.Value).ToString();
                selectedLimiter.colorData.blueMin = (int)BlueMinSlider.Value;

                RefreshImages();
            }
        }

        private void BlueMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedLimiter != null)
            {
                if (BlueMaxSlider.Value < BlueMinSlider.Value)
                {
                    BlueMinSlider.Value = BlueMaxSlider.Value;
                    selectedLimiter.colorData.blueMin = (int)BlueMinSlider.Value;
                }

                BlueMaxLabel.Content = ((int)BlueMaxSlider.Value).ToString();
                selectedLimiter.colorData.blueMax = (int)BlueMaxSlider.Value;

                RefreshImages();
            }
        }
        

        private void TakePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            TakePhoto();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (connectedCamera != null && connectedCamera.IsOpened) connectedCamera.Release();
            if (connectedPort != null && connectedPort.IsOpen) connectedPort.Close();

            currentFrame?.Dispose();
        }

        /// <summary>
        /// Get mouse position on Camera View converted from actual image coordinates to image coordinates
        /// </summary>
        /// <param name="e">MouseButtonEventArgs object from mouse event</param>
        /// <returns>Mouse position on image as Vector</returns>
        private Vector GetMouseLocalPos(MouseButtonEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(ViewImage);
            double width = ViewImage.ActualWidth;
            double height = ViewImage.ActualHeight;

            return new Vector((mousePos.X / width) * currentFrame.Width, (mousePos.Y / height) * currentFrame.Height);
        }

        private void ViewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isInspectionStarted) return;

            Vector clickPosition = GetMouseLocalPos(e);

            if (ToolCircle.IsChecked != true && ToolRectangle.IsChecked != true && currentFrame != null)
            {
                if(selectedLimiter != null)
                {
                    if (selectedLimiter.rectangle == false)
                    {
                        double xRadius = selectedLimiter.width / 2;
                        double yRadius = selectedLimiter.height / 2;
                        double isInRadius = Math.Pow(clickPosition.X - selectedLimiter.position.X, 2) / Math.Pow(xRadius, 2) + Math.Pow(clickPosition.Y - selectedLimiter.position.Y, 2) / Math.Pow(yRadius, 2);

                        if (isInRadius <= 1)
                        {
                            isDragged = true;

                            limiterDragFirstPos = new Vector(clickPosition.X, clickPosition.Y);
                            limiterDragRelativePos = new Vector(clickPosition.X - selectedLimiter.position.X, clickPosition.Y - selectedLimiter.position.Y);
                        }
                    }
                    else
                    {
                        if(clickPosition.X > selectedLimiter.position.X && clickPosition.Y > selectedLimiter.position.Y)
                        {
                            if(clickPosition.X < selectedLimiter.position.X + selectedLimiter.width && clickPosition.Y < selectedLimiter.position.Y + selectedLimiter.height)
                            {
                                isDragged = true;

                                limiterDragFirstPos = new Vector(clickPosition.X, clickPosition.Y);
                                limiterDragRelativePos = new Vector(clickPosition.X - selectedLimiter.position.X, clickPosition.Y - selectedLimiter.position.Y);
                            }
                        }
                    }

                    RefreshImages();
                }
            }
        }

        private void ViewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isInspectionStarted) return;

            Vector clickPosition = GetMouseLocalPos(e);

            if (ToolCircle.IsChecked == true && currentFrame != null)
            {
                imageInspectionLimiters.Add(new ImageInspectionLimiter(currentFrame, false, false, 50, 50, new System.Drawing.Point((int)clickPosition.X, (int)clickPosition.Y)));
            }
            else if (ToolRectangle.IsChecked == true && currentFrame != null)
            {
                imageInspectionLimiters.Add(new ImageInspectionLimiter(currentFrame, true, false, 50, 50, new System.Drawing.Point((int)clickPosition.X, (int)clickPosition.Y)));
            }
            else if (currentFrame != null && isDragged != true)
            {
                bool selecting = false;

                for (int i = 0; i < imageInspectionLimiters.Count; i++)
                {
                    if (imageInspectionLimiters[i].rectangle == false)
                    {
                        double xRadius = imageInspectionLimiters[i].width / 2;
                        double yRadius = imageInspectionLimiters[i].height / 2;

                        double isInRadius = Math.Pow(clickPosition.X - imageInspectionLimiters[i].position.X, 2) / Math.Pow(xRadius, 2) + Math.Pow(clickPosition.Y - imageInspectionLimiters[i].position.Y, 2) / Math.Pow(yRadius, 2);

                        if (isInRadius <= 1)
                        {
                            selecting = true;
                        }
                    }
                    else
                    {
                        if (clickPosition.X > imageInspectionLimiters[i].position.X && clickPosition.Y > imageInspectionLimiters[i].position.Y)
                        {
                            if (clickPosition.X < imageInspectionLimiters[i].position.X + imageInspectionLimiters[i].width && clickPosition.Y < imageInspectionLimiters[i].position.Y + imageInspectionLimiters[i].height)
                            {
                                selecting = true;
                            }
                        }
                    }

                    if(selecting)
                    {
                        if(selectedLimiter != null) selectedLimiter.isSelected = false;

                        imageInspectionLimiters[i].isSelected = true;
                        selectedLimiter = imageInspectionLimiters[i];

                        if (ColorDataPanel.IsEnabled == false)
                        {
                            ColorDataPanel.IsEnabled = true;
                        }

                        SetColorDataPanel();

                        break;
                    }
                    else if (selectedLimiter != null)
                    {
                        selectedLimiter.isSelected = false;
                        selectedLimiter = null;

                        if (ColorDataPanel.IsEnabled == true) ColorDataPanel.IsEnabled = false;
                    }
                }
            }
            else if (currentFrame != null && selectedLimiter != null)
            {
                if (Math.Sqrt(Math.Pow(limiterDragFirstPos.X - clickPosition.X, 2) + Math.Pow(limiterDragFirstPos.Y - clickPosition.Y, 2)) >= dragMinVal)
                {
                    isDragged = false;
                    selectedLimiter.position = new System.Drawing.Point((int)(clickPosition.X - limiterDragRelativePos.X), (int)(clickPosition.Y - limiterDragRelativePos.Y));
                }
            }

            RefreshImages();
         }

        private void SetColorDataPanel()
        {
            ColorDataName.Text = selectedLimiter.name;


            RedMinSlider.Value = selectedLimiter.colorData.redMin;
            RedMaxSlider.Value = selectedLimiter.colorData.redMax;

            RedMinLabel.Content = RedMinSlider.Value.ToString();
            RedMaxLabel.Content = RedMaxSlider.Value.ToString();


            GreenMinSlider.Value = selectedLimiter.colorData.greenMin;
            GreenMaxSlider.Value = selectedLimiter.colorData.greenMax;

            GreenMinLabel.Content = GreenMinSlider.Value.ToString();
            GreenMaxLabel.Content = GreenMaxSlider.Value.ToString();


            BlueMinSlider.Value = selectedLimiter.colorData.blueMin;
            BlueMaxSlider.Value = selectedLimiter.colorData.blueMax;

            BlueMinLabel.Content = BlueMinSlider.Value.ToString();
            BlueMaxLabel.Content = BlueMaxSlider.Value.ToString();


            PixelCountBox.Text = selectedLimiter.pixelCount.ToString();
            PixelDeltaBox.Text = selectedLimiter.delta.ToString();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape:
                    if (isInspectionStarted) break;
                    if (ToolCircle.IsChecked == true) ToolCircle.IsChecked = false;
                    else if (ToolRectangle.IsChecked == true) ToolRectangle.IsChecked = false;
                    break;

                case Key.Space:
                    if (isInspectionStarted) break;
                    TakePhoto();
                    break;

                case Key.Delete:
                    if (isInspectionStarted) break;
                    if (selectedLimiter != null)
                    {
                        selectedLimiter.isSelected = false;
                        imageInspectionLimiters.Remove(selectedLimiter);
                        ColorDataPanel.IsEnabled = false;

                        RefreshImages();
                    }
                    break;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (isInspectionStarted) return;
            if (selectedLimiter != null)
            {
                int delta = -1;
                if (e.Delta > 0) delta = 1;

                if (Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftShift))
                {
                    selectedLimiter.width += delta * resizeStep;
                }
                else if (!Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift))
                {
                    selectedLimiter.height += delta * resizeStep;
                }
                else
                {
                    selectedLimiter.width += delta * resizeStep;
                    selectedLimiter.height += delta * resizeStep;
                }

                if (selectedLimiter.width < 10) selectedLimiter.width = 10;
                if (selectedLimiter.height < 10) selectedLimiter.height = 10;

                if(currentFrame != null)
                {
                    if (selectedLimiter.width > currentFrame.Width) selectedLimiter.width = (int)currentFrame.Width;
                    if (selectedLimiter.height > currentFrame.Height) selectedLimiter.height = (int)currentFrame.Height;
                }

                RefreshImages();
            }
        }

        private void ColorDataName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(selectedLimiter != null) selectedLimiter.name = ColorDataName.Text;
        }

        private void ToolCircle_Click(object sender, RoutedEventArgs e)
        {
            if (ToolCircle.IsChecked == true) ToolRectangle.IsChecked = false;
        }

        private void ToolRectangle_Click(object sender, RoutedEventArgs e)
        {
            if (ToolRectangle.IsChecked == true) ToolCircle.IsChecked = false;
        }

        private void GetColorData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Continue with getting color data for " + selectedLimiter.name + "? All previous data will be overridden.", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (selectedLimiter != null && currentFrame != null)
                {
                    ColorStatistics redStats = selectedLimiter.GetColorStats(currentFrame, Inspect_View.Color.Red);
                    ColorStatistics greenStats = selectedLimiter.GetColorStats(currentFrame, Inspect_View.Color.Green);
                    ColorStatistics blueStats = selectedLimiter.GetColorStats(currentFrame, Inspect_View.Color.Blue);


                    int redMin, greenMin, blueMin;
                    int redMax, greenMax, blueMax;


                    redMin = (int)(redStats.avg - redStats.variation);
                    if (redMin < 0) redMin = 0;
                    redMax = (int)(redStats.avg + redStats.variation);
                    if (redMax > 255) redMax = 255;

                    selectedLimiter.colorData.redMin = redMin;
                    selectedLimiter.colorData.redMax = redMax;

                    RedMinSlider.Value = redMin;
                    RedMaxSlider.Value = redMax;


                    greenMin = (int)(greenStats.avg - greenStats.variation);
                    if (greenMin < 0) greenMin = 0;
                    greenMax = (int)(greenStats.avg + greenStats.variation);
                    if (greenMax > 255) greenMax = 255;

                    selectedLimiter.colorData.greenMin = greenMin;
                    selectedLimiter.colorData.greenMax = greenMax;

                    GreenMinSlider.Value = greenMin;
                    GreenMaxSlider.Value = greenMax;


                    blueMin = (int)(blueStats.avg - blueStats.variation);
                    if (blueMin < 0) blueMin = 0;
                    blueMax = (int)(blueStats.avg + blueStats.variation);
                    if (blueMax > 255) blueMax = 255;

                    selectedLimiter.colorData.blueMin = blueMin;
                    selectedLimiter.colorData.blueMax = blueMax;

                    BlueMinSlider.Value = blueMin;
                    BlueMaxSlider.Value = blueMax;


                    RefreshImages();


                    selectedLimiter.pixelCount = maskedPixelsCountCache;
                    selectedLimiter.delta = (int)(maskedPixelsCountCache * 0.05);

                    PixelCountBox.Text = selectedLimiter.pixelCount.ToString();
                    PixelDeltaBox.Text = selectedLimiter.delta.ToString();
                }
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
            if (PixelDeltaBox.Text == "") PixelCountBox.Text = "0";
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if(selectedLimiter != null)
            {
                RefreshImages();

                Uri uriSource;

                if (maskedPixelsCountCache < selectedLimiter.pixelCount - selectedLimiter.delta || maskedPixelsCountCache > selectedLimiter.pixelCount + selectedLimiter.delta)
                {
                    uriSource = new Uri("/img/nok.png", UriKind.Relative);
                }
                else
                {
                    uriSource = new Uri("/img/ok.png", UriKind.Relative);
                }

                NOK_OK.Source = new BitmapImage(uriSource);
            }
        }

        private void PixelCountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(selectedLimiter != null && PixelDeltaBox.Text != "") selectedLimiter.pixelCount = int.Parse(PixelCountBox.Text);
        }

        private void PixelDeltaBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(selectedLimiter != null && PixelDeltaBox.Text != "") selectedLimiter.delta = int.Parse(PixelDeltaBox.Text);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if(connectedCamera != null && connectedCamera.IsOpened && connectedPort != null && connectedPort.IsOpen)
            {
                if (StartButton.IsChecked == true)
                {
                    EnableAll(false);
                    ColorDataPanel.IsEnabled = false;
                    selectedLimiter = null;
                    isInspectionStarted = true;
                }
                else
                {
                    EnableAll(true);
                    isInspectionStarted = false;
                }
            }
            else StartButton.IsChecked = false;
        }

        private void EnableAll(bool enable)
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
        }

        private void FileSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            saveDialog.FileName = projectPath;
            saveDialog.DefaultExt = ".xml";
            saveDialog.Filter = "XML files (.xml)|*.xml";

            if (projectName != "")
            {
                SaveFile(saveDialog);
            }
            else if (saveDialog.ShowDialog() == true)
            {
                projectPath = saveDialog.FileName;
                SaveFile(saveDialog);
            }
        }

        private void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            saveDialog.FileName = "New project";
            saveDialog.DefaultExt = ".xml";
            saveDialog.Filter = "XML files (.xml)|*.xml";

            if (projectName != "") saveDialog.FileName = projectName;

            

            if (saveDialog.ShowDialog() == true)
            {
                projectPath = saveDialog.FileName;
                SaveFile(saveDialog);
            }
        }

        private void SaveFile(SaveFileDialog saveDialog)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<ImageInspectionLimiter>));
            TextWriter writer = new StreamWriter(System.IO.Path.GetFullPath(saveDialog.FileName), true);
            serializer.Serialize(writer, null);
            writer.Close();

            writer = new StreamWriter(saveDialog.FileName, false);

            serializer.Serialize(writer, imageInspectionLimiters);

            if (writer != null) writer.Close();
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();

            openDialog.DefaultExt = ".xml";
            openDialog.Filter = "XML files (.xml)|*.xml";

            if(openDialog.ShowDialog() == true)
            {
                NewProject();

                projectName = openDialog.SafeFileName;

                XmlSerializer serializer = new XmlSerializer(typeof(List<ImageInspectionLimiter>));
                TextReader reader = new StreamReader(openDialog.FileName);

                imageInspectionLimiters = (List<ImageInspectionLimiter>)serializer.Deserialize(reader);

                reader.Close();

                RefreshImages();
            }
        }

        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            NewProject();
        }

        private void NewProject()
        {
            projectName = "";

            if (connectedCamera != null && connectedCamera.IsOpened)
            {
                connectedCamera.Release();
            }

            currentFrame?.Dispose();
            currentFrame = null;

            selectedLimiter = null;
            imageInspectionLimiters.Clear();

            isDragged = false;

            ViewImage.Source = null;
            MaskImage.Source = null;
            ViewLimiterMask.Source = null;

            ImageInspectionLimiter.index = 0;

            ColorDataPanel.IsEnabled = false;
        }

        public void ZeroAllMasks()
        {
            foreach (ImageInspectionLimiter x in imageInspectionLimiters)
            {
                x.ZeroMask(currentFrame);
            }
        }

        private void ConnectSerialPort_Click(object sender, RoutedEventArgs e)
        {
            serialPortListWindow = new SerialPortList(this);
            serialPortListWindow.ShowDialog();
        }
    }
}