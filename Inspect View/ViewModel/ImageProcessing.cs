using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO.Ports;
using System.Windows.Input;


namespace Inspect_View
{
    //Here all logic for image processing can be found
    public partial class MainWindowViewModel
    {
        //Camera and frame processing objects
        /// <summary> Window used for selecting camera for connection </summary>
        private CameraList? cameraListWindow;
        /// <summary> Currently connected camera </summary>
        public VideoCapture? connectedCamera;
        /// <summary> Currently processed frame </summary>
        private Mat? currentFrame;


        //Serial port objects
        /// <summary>  </summary>
        private SerialPortList? serialPortListWindow;
        /// <summary>  </summary>//Window for selecting serial device for connection
        public SerialPort connectedPort;                                    //Currently connected serial device


        //Limiter objects
        /// <summary>  </summary>
        public List<ImageInspectionLimiter> imageInspectionLimiters;
        /// <summary>  </summary>//List of all limiters
        public ImageInspectionLimiter? selectedLimiter;                     //Currently selected limiter


        //Attributes needed for limiter dragging on image and resizing
        /// <summary> Position of limiter on starting drag and drop operation </summary>
        public Vector limiterDragFirstPos;
        /// <summary> Position where mouse cursor was relative to origin of limiter, while drag and drop operation started </summary>
        public Vector limiterDragRelativePos;
        /// <summary> Minimal drag distance value </summary> 
        public int dragMinVal;
        /// <summary> Is drag and drop operation for limiter in progress </summary>
        public bool isDragged;
        /// <summary> Limiter resize step (TODO: variable step size) </summary>
        public int resizeStep;


        /// <summary> Cache containing masked pixel count, for selected limiter, after last image refresh action </summary>
        private int maskedPixelsCountCache;


        /// <summary>
        /// Init image processing objects and attributes
        /// </summary>
        private void InitImageProcessing()
        {
            imageInspectionLimiters = new List<ImageInspectionLimiter>();
            connectedCamera = new VideoCapture();

            dragMinVal = 2;
            resizeStep = 10;
        }


        /// <summary>
        /// Delete a GDI object
        /// </summary>
        /// <param name="o">The poniter to the GDI object to be deleted</param>
        /// <returns></returns>
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert Emgu.CV Mat to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu.CV Mat</param>
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


        /// <summary>
        /// Get next frame from camera and save it to <c>currentFrame</c>
        /// </summary>
        public void TakePhoto()
        {
            if (connectedCamera != null && connectedCamera.IsOpened)
            {
                Mat frame = new();

                //Get frame twice, to get rid of old one sitting in buffer
                connectedCamera.Read(frame);
                connectedCamera.Read(frame);

                currentFrame = frame;

                RefreshImages();
            }
        }


        /// <summary>
        /// Recreate <c>Camera View</c>, <c>Masked View</c> and <c>Limiter Masked View</c> images
        /// </summary>
        public void RefreshImages()
        {
            if (currentFrame != null)
            {
                Mat newFrame = currentFrame.Clone();

                //Draw all limiters on Camera View
                foreach (ImageInspectionLimiter x in imageInspectionLimiters)
                {
                    MCvScalar circleColor1 = new(255, 255, 255);
                    MCvScalar circleColor2 = new(0, 0, 0);
                    if (x.isSelected)
                    {
                        //If limiter is selected - inverse colors
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

                    //Create Masked View by checking whitch pixels are in range of color data of selected limiter
                    Image<Gray, byte> inRangeView = frame.InRange(new Bgr(blueMin, greenMin, redMin), new Bgr(blueMax, greenMax, redMax));

                    Image<Gray, byte> maskLimiter = selectedLimiter.limiterMask.ToImage<Gray, byte>();
                    Image<Gray, byte> inRangeMasked = inRangeView.Copy();

                    maskedPixelsCountCache = 0;

                    //Eliminate all masked pixels, that are not inside limiter - this creates Limiter Masked View
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

                    //Count and cache in range pixels inside limiter
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

                    limiterMaskedViewPixelCount = maskedPixelsCountCache.ToString() + " pixels counted";

                    maskedView = ToBitmapSource(inRangeView.Mat);
                    limiterMaskedView = ToBitmapSource(inRangeMasked.Mat);
                }

                cameraView = ToBitmapSource(newFrame);
            }
        }

        /// <summary>
        /// Zero internal masks for all limiters
        /// </summary>
        public void ZeroAllMasks()
        {
            foreach (ImageInspectionLimiter x in imageInspectionLimiters)
            {
                x.ZeroMask(currentFrame);
            }
        }

        /// <summary>
        /// Get mouse position on Image control, converted from Image control coordinates to actual image coordinates
        /// </summary>
        /// <param name="e">MouseButtonEventArgs object from mouse event</param>
        /// <returns>Mouse position on image as Vector</returns>
        private Vector GetMouseLocalPos(System.Windows.Controls.Image image, MouseButtonEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(image);
            double width = image.ActualWidth;
            double height = image.ActualHeight;

            return new Vector((mousePos.X / width) * currentFrame.Width, (mousePos.Y / height) * currentFrame.Height);
        }

        /// <summary>
        /// Set Color Data Panel data for selected limiter
        /// </summary>
        public void SetColorDataPanel()
        {
            if(colorMaskData != selectedLimiter.colorData) colorMaskData = selectedLimiter.colorData;

            limiterName = selectedLimiter.name;

            redMinLabel = colorMaskData.redMin.ToString();
            redMaxLabel = colorMaskData.redMax.ToString();
            greenMinLabel = colorMaskData.greenMin.ToString();
            greenMaxLabel = colorMaskData.greenMax.ToString();
            blueMinLabel = colorMaskData.blueMin.ToString();
            blueMaxLabel = colorMaskData.blueMax.ToString();

            OnPropertyChanged("colorMaskData");

            pixelCount = selectedLimiter.pixelCount;
            pixelCountDelta = selectedLimiter.delta;
        }

        /// <summary>
        /// Get color data for selected limiter, by calculating standard deviation of red, green and blue color pixels inside selected limiter. 
        /// It gives good approximation of color data, that are needed for inspection, without getting uwanted colors. User might need to correct data after
        /// </summary>
        private void GetColorData()
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

                    colorMaskData.redMin = redMin;
                    colorMaskData.redMax = redMax;


                    greenMin = (int)(greenStats.avg - greenStats.variation);
                    if (greenMin < 0) greenMin = 0;
                    greenMax = (int)(greenStats.avg + greenStats.variation);
                    if (greenMax > 255) greenMax = 255;

                    colorMaskData.greenMin = greenMin;
                    colorMaskData.greenMax = greenMax;


                    blueMin = (int)(blueStats.avg - blueStats.variation);
                    if (blueMin < 0) blueMin = 0;
                    blueMax = (int)(blueStats.avg + blueStats.variation);
                    if (blueMax > 255) blueMax = 255;

                    colorMaskData.blueMin = blueMin;
                    colorMaskData.blueMax = blueMax;


                    OnPropertyChanged("colorMaskData");
                    RefreshImages();


                    pixelCount = maskedPixelsCountCache;
                    pixelCountDelta = (int)(maskedPixelsCountCache * 0.05);
                }
            }
        }


        /// <summary>
        /// Do inspection for all image limiters
        /// </summary>
        /// <returns>True if frame passed inspection, false if it failed</returns>
        private bool InspectAll()
        {
            bool isOk = true;

            //Get new photo for inspection
            TakePhoto();

            //Do inspection for all limiters, if even one limiter returns NOK, means inspection failed
            foreach (ImageInspectionLimiter x in imageInspectionLimiters)
            {
                isOk = DoInspection(x);
                if (!isOk) break;
            }

            return isOk;
        }


        /// <summary>
        /// Do inspection for one limiter
        /// </summary>
        /// <param name="limiter">Limiter for inspection</param>
        /// <returns>True if frame passed inspection, false if it failed</returns>
        private bool DoInspection(ImageInspectionLimiter limiter)
        {
            if (limiter != null)
            {
                if (selectedLimiter != null) selectedLimiter.isSelected = false;
                selectedLimiter = limiter;
                selectedLimiter.isSelected = true;
                RefreshImages();

                Uri uriSource;
                bool isOk = false;
                
                //If limiter masked pixel count is less than expected in color data (+/- delta) then return false and change icon to NOK
                if (maskedPixelsCountCache < limiter.pixelCount - limiter.delta || maskedPixelsCountCache > limiter.pixelCount + limiter.delta)
                {
                    uriSource = new Uri("/img/nok.png", UriKind.Relative);
                }
                //Else return true and change icon to OK
                else
                {
                    uriSource = new Uri("/img/ok.png", UriKind.Relative);
                    isOk = true;
                }

                NOK_OK_Icon = new BitmapImage(uriSource);
                return isOk;
            }

            return false;
        }
    }
}
