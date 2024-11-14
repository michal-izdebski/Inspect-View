using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Inspect_View
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        MainWindow mainWindow;

        String projectName;
        String projectPath;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            NOK_OK_Icon = new BitmapImage(new Uri(@"/Img/ok.png", UriKind.Relative));

            connectedPort = new System.IO.Ports.SerialPort();

            ConnectCameraCommand = new RelayCommand(OnConnectCamera);
            ClickCircleToolCommand = new RelayCommand(OnCircleTool);
            ClickRectangleToolCommand = new RelayCommand(OnRectangleTool);
            ClickTakePhotoCommand = new RelayCommand(OnClickTakePhoto);
            GetColorDataCommand = new RelayCommand(OnGetColorData);
            ConnectSerialPortCommand = new RelayCommand(OnConnectSerialPort);
            TestSerialPortCommand = new RelayCommand(OnTestSerialPort);
            DoTestCommand = new RelayCommand(OnDoTest);
            NewFileCommand = new RelayCommand(OnNewFile);
            OpenFileCommand = new RelayCommand(OnOpenFile);
            SaveFileCommand = new RelayCommand(OnSaveFile);
            SaveFileAsCommand = new RelayCommand(OnSaveFileAs);
            ProgramExitCommand = new RelayCommand(OnProgramExit);
            StartInspectionCommand = new RelayCommand(OnStartInspection);

            InitImageProcessing();

            colorDataPanel = false;
            colorMaskData = new ColorMaskData();

            limiterName = "Limiter #0";

            redMinLabel = "0";
            redMaxLabel = "255";
            greenMinLabel = "0";
            greenMaxLabel = "255";
            blueMinLabel = "0";
            blueMaxLabel = "255";

            limiterMaskedViewPixelCount = "0 pixels counted";

            pixelCount = 0;
            pixelCountDelta = 0;

            projectName = "";
            projectPath = "";

            inspectionStart = false;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private ImageSource _NOK_OK_Icon;
        public ImageSource NOK_OK_Icon
        {
            get {  return _NOK_OK_Icon; }
            set
            {
                _NOK_OK_Icon = value;
                OnPropertyChanged("NOK_OK_Icon");
            }
        }

        private ImageSource _cameraView;
        public ImageSource cameraView
        {
            get { return _cameraView; }
            set
            {
                _cameraView = value;
                OnPropertyChanged("cameraView");
            }
        }

        private ImageSource _maskedView;
        public ImageSource maskedView
        {
            get { return _maskedView; }
            set
            {
                _maskedView = value;
                OnPropertyChanged("maskedView");
            }
        }

        private ImageSource _limiterMaskedView;
        public ImageSource limiterMaskedView
        {
            get { return _limiterMaskedView; }
            set
            {
                _limiterMaskedView = value;
                OnPropertyChanged("limiterMaskedView");
            }
        }

        private String _limiterMaskedViewPixelCount;
        public String limiterMaskedViewPixelCount
        {
            get { return _limiterMaskedViewPixelCount; }
            set
            {
                _limiterMaskedViewPixelCount = value;
                OnPropertyChanged("limiterMaskedViewPixelCount");
            }
        }

        private bool _colorDataPanel;
        public bool colorDataPanel
        {
            get { return _colorDataPanel; }
            set
            {
                _colorDataPanel = value;
                OnPropertyChanged("colorDataPanel");
            }
        }

        private String _limiterName;
        public String limiterName
        {
            get { return _limiterName; }
            set
            {
                _limiterName = value;
                if(selectedLimiter != null) selectedLimiter.name = value;
                OnPropertyChanged("limiterName");
            }
        }

        private String _redMinLabel;
        public String redMinLabel
        {
            get { return _redMinLabel; }
            set
            {
                _redMinLabel = value;
                OnPropertyChanged("redMinLabel");
            }
        }
        private String _redMaxLabel;
        public String redMaxLabel
        {
            get { return _redMaxLabel; }
            set
            {
                _redMaxLabel = value;
                OnPropertyChanged("redMaxLabel");
            }
        }
        private String _greenMinLabel;
        public String greenMinLabel
        {
            get { return _greenMinLabel; }
            set
            {
                _greenMinLabel = value;
                OnPropertyChanged("greenMinLabel");
            }
        }
        private String _greenMaxLabel;
        public String greenMaxLabel
        {
            get { return _greenMaxLabel; }
            set
            {
                _greenMaxLabel = value;
                OnPropertyChanged("greenMaxLabel");
            }
        }
        private String _blueMinLabel;
        public String blueMinLabel
        {
            get { return _blueMinLabel; }
            set
            {
                _blueMinLabel = value;
                OnPropertyChanged("blueMinLabel");
            }
        }
        private String _blueMaxLabel;
        public String blueMaxLabel
        {
            get { return _blueMaxLabel; }
            set
            {
                _blueMaxLabel = value;
                OnPropertyChanged("blueMaxLabel");
            }
        }

        private int _pixelCount;
        public int pixelCount
        {
            get { return _pixelCount; }
            set
            {
                _pixelCount = value;
                if (selectedLimiter != null) selectedLimiter.pixelCount = _pixelCount;
                OnPropertyChanged("pixelCount");
            }
        }

        private int _pixelCountDelta;
        public int pixelCountDelta
        {
            get { return _pixelCountDelta; }
            set
            {
                _pixelCountDelta = value;
                if (selectedLimiter != null) selectedLimiter.delta = _pixelCountDelta;
                OnPropertyChanged("pixelCountDelta");
            }
        }

        public ColorMaskData colorMaskData {  get; set; }

        private bool _circleTool;
        public bool circleTool
        {
            get { return _circleTool; }
            set
            {
                _circleTool = value;
                OnPropertyChanged("circleTool");
            }
        }

        private bool _rectangleTool;
        public bool rectangleTool
        {
            get { return _rectangleTool; }
            set
            {
                _rectangleTool = value;
                OnPropertyChanged("rectangleTool");
            }
        }


        private bool _inspectionStart;
        public bool inspectionStart
        {
            get { return _inspectionStart; }
            set
            {
                _inspectionStart = value;
                OnPropertyChanged("inspectionStart");
            }
        }


        public ICommand ConnectCameraCommand { get; set; }
        public ICommand ClickCircleToolCommand { get; set; }
        public ICommand ClickRectangleToolCommand { get; set; }
        public ICommand ClickTakePhotoCommand { get; set; }
        public ICommand GetColorDataCommand { get; set; }
        public ICommand ConnectSerialPortCommand { get; set; }
        public ICommand TestSerialPortCommand { get; set; }
        public ICommand DoTestCommand {  get; set; }
        public ICommand NewFileCommand { get; set; }
        public ICommand OpenFileCommand { get; set; }
        public ICommand SaveFileCommand { get; set; }
        public ICommand SaveFileAsCommand { get; set; }
        public ICommand ProgramExitCommand { get; set; }
        public ICommand StartInspectionCommand { get; set; }


        /// <summary>
        /// Disconnect devices and dispose resources in case of main window closing
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (connectedCamera != null && connectedCamera.IsOpened) connectedCamera.Release();
            if (connectedPort != null && connectedPort.IsOpen) connectedPort.Close();

            currentFrame?.Dispose();
        }


        /// <summary>
        /// Creates CameraList dialog and lets you connect to camera
        /// </summary>
        private void OnConnectCamera()
        {
            cameraListWindow = new CameraList(this);
            if (cameraListWindow.ShowDialog() == true)
            {
                //Load one frame from camera
                TakePhoto();

                //In case project with limiters was opened and new camera was connected, there is need to zero all limiter masks
                ZeroAllMasks();
            }
        }

        /// <summary>
        /// Creates SerialPortList dialog and lets you connect to serial device
        /// </summary>
        private void OnConnectSerialPort()
        {
            serialPortListWindow = new SerialPortList(this);
            serialPortListWindow.ShowDialog();
        }

        /// <summary>
        /// Sends test message ("IV_HANDSHAKE\n") to connected serial device
        /// </summary>
        private void OnTestSerialPort()
        {
            try
            {
                connectedPort.Write("IV_HANDSHAKE\n");
            }
            catch (System.InvalidOperationException)
            {
                MessageBox.Show(mainWindow, "There is no serial device connected", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnClickTakePhoto() { TakePhoto(); }

        private void OnCircleTool()
        {
            if (circleTool == true) rectangleTool = false;
        }
        private void OnRectangleTool()
        {
            if (rectangleTool == true) circleTool = false;
        }

        /// <summary>
        /// On "Test" button click, do inspection for selected limiter
        /// Needed for setting up color data, for automated testing
        /// </summary>
        private void OnDoTest()
        { 
            DoInspection(selectedLimiter); 
        }

        /// <summary>
        /// On "Start inspection" toggle button click, enables/disables all controls, except exit button and "Start inspection" button
        /// </summary>
        public void OnStartInspection()
        {
            if (connectedCamera != null && connectedCamera.IsOpened && connectedPort != null && connectedPort.IsOpen && imageInspectionLimiters != null && imageInspectionLimiters.Count > 0)
            {
                if (inspectionStart == true)
                {
                    mainWindow.EnableAll(false);
                }
                else
                {
                    mainWindow.EnableAll(true);
                }
            }
            else inspectionStart = false;
        }

        private void OnGetColorData()
        {
            GetColorData();
        }


        private void OnNewFile()
        {
            ClearProject();
        }

        private void OnOpenFile()
        {
            OpenFileDialog openDialog = new OpenFileDialog();

            openDialog.DefaultExt = ".xml";
            openDialog.Filter = "XML files (.xml)|*.xml";

            if (openDialog.ShowDialog() == true)
            {
                ClearProject();

                projectName = openDialog.SafeFileName;

                XmlSerializer serializer = new XmlSerializer(typeof(List<ImageInspectionLimiter>));
                TextReader reader = new StreamReader(openDialog.FileName);

                imageInspectionLimiters = (List<ImageInspectionLimiter>)serializer.Deserialize(reader);

                reader.Close();

                RefreshImages();
            }
        }

        private void OnSaveFile()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            saveDialog.FileName = "New project";
            saveDialog.DefaultExt = ".xml";
            saveDialog.Filter = "XML files (.xml)|*.xml";

            if (projectName != "")
            {
                saveDialog.FileName = projectName;
                SaveFile(saveDialog);
            }
            else if (saveDialog.ShowDialog() == true)
            {
                projectPath = saveDialog.FileName;
                SaveFile(saveDialog);
            }
        }

        private void OnSaveFileAs()
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

        private void OnProgramExit()
        {
            System.Windows.Application.Current.Shutdown();
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

        private void ClearProject()
        {
            projectName = "";

            selectedLimiter = null;
            imageInspectionLimiters.Clear();

            isDragged = false;

            cameraView = null;
            maskedView = null;
            limiterMaskedView = null;

            ImageInspectionLimiter.index = 0;

            colorDataPanel = false;
        }

        /// <summary>
        /// Logic for left mouse button down, on image
        /// Used with OnLeftMouseUp for selecting, creating and moving limiters
        /// </summary>
        /// <param name="image">Image control</param>
        /// <param name="e">Event arguments</param>
        public void OnLeftMouseDown(System.Windows.Controls.Image image, MouseButtonEventArgs e)
        {
            //If inspection is in progress ignore mouse click
            if (inspectionStart) return;

            //Get mouse click position in actual image coordinates
            Vector clickPosition = GetMouseLocalPos(image, e);

            //If circle nor rectangle tools are selected, means that user probably is trying to select limiter or drag it around
            if (circleTool != true && rectangleTool != true && currentFrame != null)
            {
                if (selectedLimiter != null)
                {
                    //Only selected limiter can be moved
                    //This part of code checks what type of limiter is selected and saves all data needed for drag and drop operation
                    if (selectedLimiter.rectangle == false)
                    {
                        //Circle limiter can be resised as ellipse
                        //This part of code is checking if mouse click position is inside ellipse
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
                        //If selected limiter is rectangle then check if mouse click position is inside said rectangle
                        if (clickPosition.X > selectedLimiter.position.X && clickPosition.Y > selectedLimiter.position.Y)
                        {
                            if (clickPosition.X < selectedLimiter.position.X + selectedLimiter.width && clickPosition.Y < selectedLimiter.position.Y + selectedLimiter.height)
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

        /// <summary>
        /// Logic for left mouse button up, on image
        /// Used with OnLeftMouseDown for selecting, creating and moving limiters
        /// </summary>
        /// <param name="image">Image control</param>
        /// <param name="e">Event arguments</param>
        public void OnLeftMouseUp(System.Windows.Controls.Image image, MouseButtonEventArgs e)
        {
            //If inspection is in progress ignore mouse click
            if (inspectionStart) return;

            //Get mouse click position in actual image coordinates
            Vector clickPosition = GetMouseLocalPos(image, e);

            if (circleTool == true && currentFrame != null)
            {
                //Add circle limiter if circle tool is selected
                imageInspectionLimiters.Add(new ImageInspectionLimiter(currentFrame, false, false, 50, 50, new System.Drawing.Point((int)clickPosition.X, (int)clickPosition.Y)));
            }
            else if (rectangleTool == true && currentFrame != null)
            {
                //Add rectangle limiter if rectangle tool is selected
                imageInspectionLimiters.Add(new ImageInspectionLimiter(currentFrame, true, false, 50, 50, new System.Drawing.Point((int)clickPosition.X, (int)clickPosition.Y)));
            }
            //This part of code is checking if any limiter was selected
            else if (currentFrame != null && isDragged != true)
            {
                bool selecting = false;

                for (int i = 0; i < imageInspectionLimiters.Count; i++)
                {
                    //For every limiter check if it's rectangle or circle and check if mouse click position is inside it
                    //If there is limiter that was clicked set "selecting" as true
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

                    //If limiter is being selected, enable color data panel and set data for it
                    //Else deselect currently selected limiter
                    if (selecting)
                    {
                        if (selectedLimiter != null) selectedLimiter.isSelected = false;

                        imageInspectionLimiters[i].isSelected = true;
                        selectedLimiter = imageInspectionLimiters[i];

                        if (colorDataPanel == false)
                        {
                            colorDataPanel = true;
                        }

                        SetColorDataPanel();

                        break;
                    }
                    else if (selectedLimiter != null)
                    {
                        selectedLimiter.isSelected = false;
                        selectedLimiter = null;

                        if (colorDataPanel == true) colorDataPanel = false;
                    }
                }
            }
            //If there is drag and drop operation active, check if limiter was dragged minimal distance (dragMinVal)
            //If yes, move limiter
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

        /// <summary>
        /// Handle mouse wheel operations, for limiter resizing
        /// Scroll wheel                - resize limiter
        /// Left CTRL + Scroll wheel    - resize limiter width
        /// Left SHIFT + Scroll wheel   - resize limiter height
        /// </summary>
        /// <param name="e">Mouse wheel arguments</param>
        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (inspectionStart) return;
            if (selectedLimiter != null)
            {
                //Resizing is done with fixed step, insted of value given from mouse wheel event, because of that directio of scroll is checked and limiter is resized based on that
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

                //Limiter size is fixed to width and height of current frame taken from camera
                if (currentFrame != null)
                {
                    if (selectedLimiter.width > currentFrame.Width) selectedLimiter.width = (int)currentFrame.Width;
                    if (selectedLimiter.height > currentFrame.Height) selectedLimiter.height = (int)currentFrame.Height;
                }

                RefreshImages();
            }
        }
    }
}
