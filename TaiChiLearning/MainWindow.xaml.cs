namespace TaiChiLearning
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Forms;
    using System.Linq;
    using Microsoft.Kinect;
    using TaiChiLearning.DTW;
    using TaiChiLearning.Recorder;
    using TaiChiLearning.Replay;
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;
    using System.Windows.Media;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.ComponentModel;
    using Microsoft.Speech.Synthesis;
    using System.Globalization;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // We want to control how depth data gets converted into false-color data
        // for more intuitive visualization, so we keep 32-bit color frame buffer versions of
        // these, to be updated whenever we receive and process a 16-bit frame.

        /// <summary>
        /// Handle the color stream
        /// </summary>
        readonly ColorStreamManager RealTimeColorManager = new ColorStreamManager();
        readonly ColorStreamManager ReplayColorManager = new ColorStreamManager();
        readonly ColorStreamManager PlayBackColorManager = new ColorStreamManager();
        //SkeletonDrawManager LearningSkeleton;
        SkeletonDrawManager RealTimeSkeleton;
        SkeletonDrawManager ReplaySkeleton;
        //SkeletonDrawManager PlayBackSkeleton;

        /// <summary>
        /// The red index
        /// </summary>
        private const int RedIdx = 2;

        /// <summary>
        /// The green index
        /// </summary>
        private const int GreenIdx = 1;

        /// <summary>
        /// The blue index
        /// </summary>
        private const int BlueIdx = 0;

        /// <summary>
        /// How many skeleton frames to ignore (_flipFlop)
        /// 1 = capture every frame, 2 = capture every second frame etc.
        /// </summary>
        private const int Ignore = 2;

        /// <summary>
        /// The minumum number of frames in the _video buffer before we attempt to start matching gestures
        /// </summary>
        private const int CaptureCountdownSeconds = 5;

        /// <summary>
        /// Dictionary of all the joints Kinect SDK is capable of tracking. You might not want always to use them all but they are included here for thouroughness.
       
        /// number of joints that we need
        private const int dimension = 16;

        /// <summary>
        /// Flag to show whether or not the Tai Chi learning system is capturing a new pose
        /// </summary>
        private static bool _capturing = false;

        /// <summary>
        /// Flag to show whether the mode is training mode or challenge mode
        /// </summary>
        private static bool _training = true;

        /// <summary>
        /// Flag to show whether the mode is replaying
        /// </summary>
        private static bool _replaying = false;

        /// <summary>
        /// speech recognition mode
        /// </summary>
        private static bool _speechrecognition = false;

        /// <summary>
        /// flag to show whether it is playing back or not
        /// </summary>
        private static bool _playingback = false;

        /// <summary>
        /// Flag to show whether or not the the system is in Learning Mode
        /// </summary>
        private static bool _learning = false;

        private static bool _startspeech = false;

        /// <summary>
        /// Dynamic Time Warping object
        /// </summary>
        private DtwForTaiChiLearning _dtw;

        /// <summary>
        /// The 'last time' DateTime. Used for calculating frames per second
        /// </summary>
        private DateTime _lastTime = DateTime.MaxValue;

        /// <summary>
        /// The Natural User Interface runtime
        /// </summary>
        private KinectSensor  _nui;

        ///<summary>
	    ///and the speech recognition engine (SRE)
        ///</summary>
        private SpeechRecognitionEngine speechRecognizer;

        ///<summary>
        ///text to speech
        ///</summary>
        private SpeechSynthesizer synthesizer;

        /// <summary>
        /// ArrayList of master's and learner motion
        /// </summary>
        private ArrayList _masterseq = new ArrayList();
        private ArrayList _learnerseq = new ArrayList();

        // Kinect recorder
        private static KinectRecorder _recorder;
        private static KinectRecorder _colorrecorder;

        private static Stream _recordskeletonstream;
        private static Stream _recordcolorstream;
        private static Stream _learnerskeletonstream;
        private static Stream _learnercolorstream;

        private KinectReplay _replay;
        private KinectReplay _colorreplay;
        private KinectReplay _playback;
        private KinectReplay _colorplayback;

        private string _temppath = ".\\";

        /// 
        private DateTime _captureCountdown = DateTime.Now;

        /// 
        private System.Windows.Forms.Timer _captureCountdownTimer;

        private static Point[] _MasterAngle;

        private static Point[] _LearnerAngle;

        public static int[] detection = new int [dimension];

        private static double[] angles = new double [dimension];

        private static int _finalframeno;

        ///Difficulty
        private static double threshold = 40.0;

        /// <summary>
        /// Property change event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The replay rate 
        /// </summary>
        private int selectedFPS = 30;
        //private static double rateinmsec = 1000.0/SelectedFPS;

        public static readonly DependencyProperty selectedFPSProperty =
    DependencyProperty.Register("selectedFPS", typeof(string), typeof(MainWindow), new PropertyMetadata(null));
        
        public int SelectedFPS
        {
            get
            {
                return (int)this.selectedFPS;
            }

            set
            {
                this.selectedFPS = (int)(value);
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedFPS"));
                }
            }
        }

        /// <summary>
        /// The replay rate 
        /// </summary>
        private static double _dTWresult = 0.0;
        //private static double rateinmsec = 1000.0/SelectedFPS;

        //Get the speech recognizer (SR)
        private static RecognizerInfo GetKinectRecognizer()
        {
            /*
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
            */
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Initialize()
        {
            if (_nui == null)
                return;
            _lastTime = DateTime.Now;

            //test _video = new ArrayList();

            RealTimeSkeleton = new SkeletonDrawManager(RealTimeSkeletonCanvas, _nui);
            ReplaySkeleton = new SkeletonDrawManager(MasterSkeletonCanvas, _nui);
            //PlayBackSkeleton = new SkeletonDrawManager(PlayBackSkeletonCanvas, _nui);
            //LearningSkeleton = new SkeletonDrawManager(LearningSkeletonCanvas, _nui);

            _dtw = new DtwForTaiChiLearning(dimension * 2, 0.6, 2, 2, 10);
            // If you want to see the RGB stream then include this
            _nui.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            _nui.ColorFrameReady += NuiColorFrameReady;

            //test Skeleton3DDataExtract.Skeleton3DdataCoordReady += NuiSkeleton3DdataCoordReady;
            _nui.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            });
            _nui.SkeletonFrameReady += NuiSkeletonFrameReady;
            //_nui.SkeletonFrameReady += SkeletonExtractSkeletonFrameReady; we don't need the viewable data so far !!

            RealTimeImage.DataContext = RealTimeColorManager;
            ReplayImage.DataContext = ReplayColorManager;
            //PlayBackImage.DataContext = PlayBackColorManager;

            string path = ".\\Records\\" + "@1stMotion" + "\\";
            if (File.Exists(@path + "frame_number"))
            {
                using (FileStream fs = File.OpenRead(@path + "frame_number"))
                {
                    BinaryReader reader = new BinaryReader(fs);
                    _finalframeno = reader.ReadInt32();
                }
            }

            _nui.Start();
            CreateSpeechRecognizer();
            //text tp speech
            synthesizer = new SpeechSynthesizer();
            synthesizer.Volume = 100;//聲音大小(0 ~ 100)      
            synthesizer.Rate = -2;//聲音速度(-10 ~ 10)

        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (_nui == null)
                    {
                        _nui = e.Sensor;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (_nui == e.Sensor)
                    {
                        Clean();
                        System.Windows.MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (_nui == e.Sensor)
                    {
                        Clean();
                        System.Windows.MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    System.Windows.MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }

        private void Clean()
        {
            /* voice control
            if (audioManager != null)
            {
                audioManager.Dispose();
                audioManager = null;
            }

            if (voiceCommander != null)
            {
                voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
                voiceCommander.Stop();
                voiceCommander = null;
            }
            */

            if (_nui != null)
            {
                _nui.SkeletonFrameReady -= NuiSkeletonFrameReady;
                _nui.ColorFrameReady -= NuiColorFrameReady;
                _nui.Stop();
                _nui = null;
            }

            if (_recordcolorstream != null)
                _recordcolorstream.Close();
            if (_recordskeletonstream != null)
                _recordskeletonstream.Close();
        }

        /// <summary>
        /// Runds every time a skeleton frame is ready. Updates the skeleton canvas with new joint and polyline locations.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Skeleton Frame Event Args</param>
        private void NuiSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!_playingback)
            {
                int length;
                Point[] temppt = new Point[dimension];
                using (var frame = e.OpenSkeletonFrame())
                {
                    if (frame == null) return;
                    var skeletons = new Skeleton[frame.SkeletonArrayLength];
                    length = frame.SkeletonArrayLength;
                    frame.CopySkeletonDataTo(skeletons);


                    //DrawSkeleton(skeletons, LearnerSkeletonCanvas);
                    RealTimeSkeleton.DrawSkeleton(skeletons);

                    if (_learning && _training && _capturing)
                    {
                        //RealTimeSkeleton.DrawSkeleton(skeletons);
                        var brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                        int[] DetectionTemp = new int[dimension];
                        DetectionTemp = detection;

                        foreach (var data in skeletons)
                        {
                            temppt = Skeleton3DDataExtract.ProcessData(data);
                            if (temppt[4].X >= 0)
                                _LearnerAngle = temppt;
                            if (_LearnerAngle != null)
                            {
                                for (int i = 0; i < dimension; i++)
                                {
                                    if (DetectionTemp[i] > 0)
                                    {
                                        RealTimeSkeleton.DrawCorrection(data, DetectionTemp[i], angles[i], i);
                                    }
                                }
                            }
                            _learnerseq.Add(temppt);
                        }
                    }

                    if (_capturing)
                    {
                        if (_recorder == null) return;
                        _recorder.Record(frame);
                        if (!_learning) _finalframeno = frame.FrameNumber;
                    }
                }
            }

            else return; // do nothing in playing back mode
        }

        /// <summary>
        /// Called every time a video (RGB) frame is ready
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Image Frame Ready Event Args</param>
        /// 
        private void NuiColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            using (var image = e.OpenColorImageFrame())
            {
                if (image == null) return; // sometimes frame image comes null, so skip it.

                RealTimeColorManager.Update(image);
                if (_capturing)
                {
                    if (image == null)
                        return;
                    if(_colorrecorder != null)
                    _colorrecorder.Record(image);
                }
            }
        }

        /// <summary>
        /// Handle the replay color iamge frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            var image = e.ColorImageFrame;

            if (image == null) return; // sometimes frame image comes null, so skip it.
            ReplayColorManager.Update(image);
        }

        /// <summary>
        /// handle the play back color image frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void playback_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            var image = e.ColorImageFrame;

            if (image == null) return; // sometimes frame image comes null, so skip it.
            PlayBackColorManager.Update(image);
        }

        /// <summary>
        /// handle the replay skeleton frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons;
            var frame = e.SkeletonFrame;
            if (frame == null) return;
            skeletons = new Skeleton[frame.ArrayLength];
            skeletons = frame.Skeletons;
            Point[] temppt = new Point[dimension];

            //DrawSkeleton(skeletons, MasterSkeletonCanvas);
            ReplaySkeleton.DrawSkeleton(skeletons);

            /// get the joint angle data of master
            /// then make comparison
            if (_learning || _playingback)
            {
                foreach (var data in skeletons)
                {
                    temppt = Skeleton3DDataExtract.ProcessData(data);
                    if (temppt[4].X >= 0)
                        _MasterAngle = temppt;
                    //Console.WriteLine(_MasterAngle[4].X);
                    if (_LearnerAngle != null && _MasterAngle != null)
                    {
                        angles = MotionDetection.Detect(_LearnerAngle, _MasterAngle, dimension, threshold, detection);
                    }
                }
                _masterseq.Add(temppt);
            }

            if (_finalframeno <= frame.FrameNumber)
            {
                if (_learning)
                {
                    this.tcStopLearningClick(null, null);
                }
                else if (_playingback)
                {
                    this.tcStopPlayBackClick(null, null);
                }
                else
                {
                    this.tcStopReplayClick(null, null);
                }
            }
        }

        /// <summary>
        /// handle the replay skeleton frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void playback_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons;
            var frame = e.SkeletonFrame;
            if (frame == null) return;
            skeletons = new Skeleton[frame.ArrayLength];
            skeletons = frame.Skeletons;
            Point[] temppt = new Point[dimension];

            //DrawSkeleton(skeletons, MasterSkeletonCanvas);
            RealTimeSkeleton.DrawSkeleton(skeletons);

            /// get the joint angle data of master
            /// then make comparison
            var brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            int[] DetectionTemp = new int[dimension];
            DetectionTemp = detection;

            foreach (var data in skeletons)
            {
                temppt = Skeleton3DDataExtract.ProcessData(data);
                if (temppt[4].X >= 0)
                    _LearnerAngle = temppt;
                if (_LearnerAngle != null)
                {
                    for (int i = 0; i < dimension; i++)
                    {
                        if (DetectionTemp[i] > 0)
                        {
                            RealTimeSkeleton.DrawCorrection(data, DetectionTemp[i], angles[i], i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Runs after the window is loaded
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //listen to any status change for Kinects
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
                foreach (KinectSensor kinect in KinectSensor.KinectSensors)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        _nui = kinect;
                        break;
                    }
                }

                if (KinectSensor.KinectSensors.Count == 0)
                    System.Windows.MessageBox.Show("No Kinect found");
                else
                    Initialize();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Runs some tidy-up code when the window is closed. This is especially important for our NUI instance because the Kinect SDK is very picky about this having been disposed of nicely.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event Args</param>
        private void WindowClosed(object sender, EventArgs e)
        {
            if(_recordcolorstream!=null) _recordcolorstream.Close();
            if (_recordskeletonstream != null) _recordskeletonstream.Close();
            Debug.WriteLine("Stopping NUI");
            _nui.Stop();
            Debug.WriteLine("NUI stopped");
            Environment.Exit(0);
        }


        /// <summary>
        /// The method fired by the countdown timer. Either updates the countdown or fires the StartCapture method if the timer expires
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event Args</param>
        private void CaptureCountdown(object sender, EventArgs e)
        {
            if (sender == _captureCountdownTimer)
            {
                if (DateTime.Now < _captureCountdown)
                {
                    status.Text = "Wait " + ((_captureCountdown - DateTime.Now).Seconds + 1) + " seconds";
                }
                else
                {
                    _captureCountdownTimer.Stop();
                    
                    if (_learning)
                    {
                        status.Text = "Recognizing motion";
                        StartRegcon();
                        StartCapture();
                    }
                    else
                    {
                        status.Text = "Recording motion";
                        StartCapture();
                    }
                }
            }
        }

        /// <summary>
        /// Capture mode. Sets our control variables and button enabled states
        /// </summary>
        private void StartCapture()
        {
            _capturing = true;
            
            // Clear the _video buffer and start from the beginning
            //test _video = new ArrayList();
            if (File.Exists(@_temppath + "skeleton")) while (FileDelete(@_temppath + "skeleton")) ;
            if (File.Exists(@_temppath + "colorStream")) while (FileDelete(@_temppath + "colorStream")) ;
            if (_learning)
            {
                if (_learnercolorstream != null)
                    _learnercolorstream.Close();
                if (_learnerskeletonstream != null)
                    _learnerskeletonstream.Close();
                _learnerskeletonstream = File.Create(@_temppath + "skeleton");
                _learnercolorstream = File.Create(@_temppath + "colorStream");
                _recorder = new KinectRecorder(KinectRecordOptions.Skeletons, _learnerskeletonstream);
                _colorrecorder = new KinectRecorder(KinectRecordOptions.Color, _learnercolorstream);
            }
            else
            {
                // Set the buttons enabled state
                //dtwRead.IsEnabled = false;
                //tcCapture.IsEnabled = false;
                tcStore.IsEnabled = true;
                // Set the capturing? flag
                status.Text = "Recording motion " + gestureList.Text;
                if (_recordcolorstream != null)
                    _recordcolorstream.Close();
                if (_recordskeletonstream != null)
                    _recordskeletonstream.Close();
                _recordskeletonstream = File.Create(@_temppath + "skeleton");
                _recordcolorstream = File.Create(@_temppath + "colorStream");
                _recorder = new KinectRecorder(KinectRecordOptions.Skeletons, _recordskeletonstream);
                _colorrecorder = new KinectRecorder(KinectRecordOptions.Color, _recordcolorstream);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void StartRegcon()
        {
            _learning = true;
            _capturing = true;

            tcCapture.IsEnabled = false;
            tcStartLearning.IsEnabled = false;
            tcReplay.IsEnabled = false;
            tcStopLearning.IsEnabled = true;
            string path = ".\\Records\\" + gestureList.Text + "\\";
            readLastFrame(path);

            if (_recordskeletonstream != null)
                _recordskeletonstream.Close();
            _recordskeletonstream = File.OpenRead(@path + "skeleton");
            _replay = new KinectReplay(_recordskeletonstream);
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            _replay.Start(1000.0 / this.SelectedFPS);

            if (_recordcolorstream != null)
                _recordcolorstream.Close();
            _recordcolorstream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(_recordcolorstream);
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            _colorreplay.Start(1000.0 / this.SelectedFPS);

            _captureCountdownTimer.Dispose();

            status.Text = "Learning " + gestureList.Text;
        }
        


        /// <summary>
        /// Starts a countdown timer to enable the player to get in position to record gestures
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void tcCaptureClick(object sender, RoutedEventArgs e)
        {
            _learning = false;
            //dtwRead.IsEnabled = false;
            //tcCapture.IsEnabled = false;
            tcStore.IsEnabled = false;
            tcReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = false;
            _captureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);

            _captureCountdownTimer = new System.Windows.Forms.Timer();
            _captureCountdownTimer.Interval = 50;
            _captureCountdownTimer.Start();
            _captureCountdownTimer.Tick += CaptureCountdown;
        }

        /// <summary>
        /// Stores our gesture to the DTW sequences list
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void tcStoreClick(object sender, RoutedEventArgs e)
        {
            // Set the buttons enabled state
            //dtwRead.IsEnabled = false;
            //tcCapture.IsEnabled = true;
            tcStore.IsEnabled = false;
            tcReplay.IsEnabled = true;
            tcStartLearning.IsEnabled = true;
            // Set the capturing? flag
            _learning = false;
            _capturing = false;
            _recorder = null;
            _colorrecorder = null;

            const string message = "Are you sure that you would like to store the TaiChi motion?";
            const string caption = "Confirmation";
            var result = System.Windows.Forms.MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);

            // If the no button was pressed ... 
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                status.Text = "Remembering " + gestureList.Text + ", please stay there until the saving process finished :)";

                string path = ".\\Records\\" + gestureList.Text + "\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (_recordcolorstream != null)
                    _recordcolorstream.Close();
                if (_recordskeletonstream != null)
                    _recordskeletonstream.Close();

                if (File.Exists(@path + "skeleton")) while (FileDelete(@path + "skeleton")) ;
                if (File.Exists(@path + "colorStream")) while (FileDelete(@path + "colorStream")) ;
                if (File.Exists(@path + "frame_number")) while (FileDelete(@path + "frame_number")) ;

                File.Move(@_temppath + "skeleton", @path + "skeleton");
                File.Move(@_temppath + "colorStream", @path + "colorStream");

                /*
                while (IsFileClosed(@_temppath + "colorStream") && IsFileClosed(@path + "colorStream"))
                {
                    Thread.Sleep(1000);
                }
                 * */

                using (FileStream fs = File.Create(@path + "frame_number"))
                {
                    using (BinaryWriter sw = new BinaryWriter(fs))
                    {
                        sw.Write(_finalframeno);
                    }
                }
                status.Text = gestureList.Text + " added";
            }
            else
            {
                _recordskeletonstream.Close();
                _recordcolorstream.Close();
                return;
            }
        }

        //Replay the saved skeleton
        private void tcReplayClick (object sender, RoutedEventArgs e) 
        {
            _replaying = true;
            _learning = false;
            tcCapture.IsEnabled = false;
            tcStartLearning.IsEnabled = false;
            tcReplay.IsEnabled = false;
            status.Text = "Replaying master motion " + gestureList.Text;
            string path = ".\\Records\\" + gestureList.Text + "\\";
            readLastFrame(path);

            if (_recordskeletonstream != null)
                _recordskeletonstream.Close();
            _recordskeletonstream = File.OpenRead(@path + "skeleton");
            _replay = new KinectReplay(_recordskeletonstream);
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            _replay.Start(1000.0/this.SelectedFPS);

            if (_recordcolorstream != null)
                _recordcolorstream.Close();
            _recordcolorstream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(_recordcolorstream);
            //recordColorStream.Close();
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            _colorreplay.Start(1000.0/this.SelectedFPS);

            tcStopReplay.IsEnabled = true;
        }

        private void tcStopReplayClick(object sender, RoutedEventArgs e)
        {
            _replaying = false;
            status.Text = "Stopped replay";
            tcCapture.IsEnabled = true;
            tcStopReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = true;
            tcReplay.IsEnabled = true;
            _replay.Stop();
            _colorreplay.Stop();

            MasterSkeletonCanvas.Children.Clear();
            //ReplayImage.Source = null;
        }

        private void tcStartLearningClick(object sender, RoutedEventArgs e)
        {
            _learning = true;
            _capturing = false;
            _captureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);

            _captureCountdownTimer = new System.Windows.Forms.Timer();
            _captureCountdownTimer.Interval = 50;
            _captureCountdownTimer.Start();
            _captureCountdownTimer.Tick += CaptureCountdown;
        }
        
        /// <summary>
        /// Event handler for the stop learning click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tcStopLearningClick(object sender, RoutedEventArgs e)
        {
            status.Text = "Stopped learning";
            tcCapture.IsEnabled = true;
            tcStopReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = true;
            tcStopLearning.IsEnabled = false;
            tcReplay.IsEnabled = true;
            _learning = false;
            _capturing = false;
            _replay.Stop();
            _colorreplay.Stop();
            MasterSkeletonCanvas.Children.Clear();
            RealTimeSkeletonCanvas.Children.Clear();
            _recorder = null;
            _colorrecorder = null;

            //_dTWresult = _dtw.DtwCompution(_masterseq, _learnerseq, _temppath);
            
            const string message = "Are you satisfied with your performance this time, save or not?";
            const string caption = "Confirmation";
            var result = System.Windows.Forms.MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);
            // If the no button was pressed ... 
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                status.Text = "Remembering " + gestureList.Text + ", please stay there until the saving process finished :)";

                string path = ".\\Learnings\\" + gestureList.Text + "\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (_learnercolorstream != null)
                    _learnercolorstream.Close();
                if (_learnerskeletonstream != null)
                    _learnerskeletonstream.Close();

                if (File.Exists(@path + "skeleton")) while (FileDelete(@path + "skeleton")) ;
                if (File.Exists(@path + "colorStream")) while (FileDelete(@path + "colorStream")) ;

                File.Move(@_temppath + "skeleton", @path + "skeleton");
                File.Move(@_temppath + "colorStream", @path + "colorStream");
            }
            else
            {
                _learnerskeletonstream.Close();
                _learnercolorstream.Close();
            }
        }

        private void tcPlayBack_Click(object sender, RoutedEventArgs e)
        {
            _learning = false;
            _capturing = false;
            _playingback = true;

            tcCapture.IsEnabled = false;
            tcStartLearning.IsEnabled = false;
            tcReplay.IsEnabled = false;
            tcStopLearning.IsEnabled = false;
            tcStopPlayBack.IsEnabled = true;

            RealTimeImage.DataContext = PlayBackColorManager;

            string path = ".\\Records\\" + gestureList.Text + "\\";
            readLastFrame(path);

            if (_recordskeletonstream != null)
                _recordskeletonstream.Close();
            _recordskeletonstream = File.OpenRead(@path + "skeleton");
            _replay = new KinectReplay(_recordskeletonstream);
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            

            if (_recordcolorstream != null)
                _recordcolorstream.Close();
            _recordcolorstream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(_recordcolorstream);
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            

            string path2 = ".\\Learnings\\" + gestureList.Text + "\\";
            readLastFrame(path);

            if (_learnerskeletonstream != null)
                _learnerskeletonstream.Close();
            _learnerskeletonstream = File.OpenRead(@path2 + "skeleton");
            _playback = new KinectReplay(_learnerskeletonstream);
            _playback.SkeletonFrameReady += playback_SkeletonFrameReady;
            

            if (_learnercolorstream != null)
                _learnercolorstream.Close();
            _learnercolorstream = File.OpenRead(@path2 + "colorStream");
            _colorplayback = new KinectReplay(_learnercolorstream);
            _colorplayback.ColorImageFrameReady += playback_ColorImageFrameReady;

            _playback.Start(1000.0 / this.SelectedFPS);
            _colorplayback.Start(1000.0 / this.SelectedFPS);
            _colorreplay.Start(1000.0 / this.SelectedFPS);
            _replay.Start(1000.0 / this.SelectedFPS);

            status.Text = "Playing back " + gestureList.Text;
        }
        /// <summary>
        /// Event handler for the stop play back button, which will stop the playing back action
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tcStopPlayBackClick(object sender, RoutedEventArgs e)
        {
            _learning = false;
            _capturing = false;
            _playingback = false;

            RealTimeImage.DataContext = RealTimeColorManager;

            status.Text = "Stopped playing back";
            tcCapture.IsEnabled = true;
            tcStopReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = true;
            tcReplay.IsEnabled = true;
            tcStopPlayBack.IsEnabled = false;
            tcPlayBack.IsEnabled = true;
            _replay.Stop();
            _colorreplay.Stop();
            _playback.Stop();
            _colorplayback.Stop();

            MasterSkeletonCanvas.Children.Clear();
            RealTimeSkeletonCanvas.Children.Clear();
        }

        private void CreateSpeechRecognizer()
        {
            //set recognizer info
            RecognizerInfo ri = GetKinectRecognizer();
            //create instance of SRE
            if (null != ri)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); 
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

                //SpeechRecognitionEngine speechRecognizer;
                speechRecognizer = new SpeechRecognitionEngine(ri.Id);
                /*
                var grammar = new Choices();
                Choices speechaction = new Choices(new string[] { "RECORD", "STORE RECORD", "REPLAY", "STOP REPLAY", "LEARN", "FINISH", "PLAYBACK", "STOP PLAYBACK" });
                grammar.Add("KINECT");
                var gb = new GrammarBuilder();
                gb.Culture = ri.Culture;
                gb.Append(grammar);
                gb.Append(speechaction);
                var g = new Grammar(gb);
                speechRecognizer.LoadGrammar(g);
                 * /
/*
                //Now we need to add the words we want our program to recognise
                //  Create lists of alternative choices.
                Choices speechaction = new Choices(new string[] { "RECORD", "STORE RECORD", "REPLAY", "STOP REPLAY", "LEARN", "FINISH", "PLAYBACK", "STOP PLAYBACK"  });

                // Create a GrammarBuilder object and assemble the grammar components.
                GrammarBuilder actionMenu = new GrammarBuilder("KINECT");
                //actionMenu.Append("KINECT");
                actionMenu.Append(speechaction);
                //actionMenu.Append("KINECT");


                // Build a Grammar object from the GrammarBuilder.
                Grammar grammar = new Grammar(actionMenu);
                grammar.Name = "button";
                speechRecognizer.LoadGrammar(grammar);
                
*/
                //var grammar_start = new Choices();
                //grammar_start.Add("Kinect");

                var grammar = new Choices();
                grammar.Add(new SemanticResultValue("Record", "RECORD"));
                grammar.Add(new SemanticResultValue("Store", "STORE"));
                grammar.Add(new SemanticResultValue("Replay", "REPLAY"));
                grammar.Add(new SemanticResultValue("Stop", "STOP"));
                grammar.Add(new SemanticResultValue("Learn", "LEARN"));
                grammar.Add(new SemanticResultValue("Finish", "FINISH"));
                /*
                 * var directions = new Choices();
                * directions.Add(new SemanticResultValue("forward", "FORWARD"));
                * directions.Add(new SemanticResultValue("forwards", "FORWARD"));
                * directions.Add(new SemanticResultValue("straight", "FORWARD"));
                * directions.Add(new SemanticResultValue("backward", "BACKWARD"));
                * directions.Add(new SemanticResultValue("backwards", "BACKWARD"));
                * directions.Add(new SemanticResultValue("back", "BACKWARD"));
                * directions.Add(new SemanticResultValue("turn left", "LEFT"));
                * directions.Add(new SemanticResultValue("turn right", "RIGHT"));
                */



                
                //set culture - language, country/region
                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(grammar);
                //gb.Append(grammar_start);

                //set up the grammar builder
                var g = new Grammar(gb);
                speechRecognizer.LoadGrammar(g);
                
                //Set events for recognizing, hypothesising and rejecting speech
                //speechRecognizer.SpeechRecognized += SreSpeechStartRecogn;
                speechRecognizer.SpeechRecognized += SreSpeechRecognized;
                speechRecognizer.SpeechHypothesized += SreSpeechHypothesized;
                speechRecognizer.SpeechRecognitionRejected += SreSpeechRecognitionRejected;

                //test voice
                var audioSource = _nui.AudioSource;
                //Set the beam angle mode - the direction the audio beam is pointing
                //we want it to be set to adaptive
                audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
                //start the audiosource 
                var kinectStream = audioSource.Start();
                //configure incoming audio stream
                speechRecognizer.SetInputToAudioStream(
                    kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                //make sure the recognizer does not stop after completing     
                speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                //reduce background and ambient noise for better accuracy
                _nui.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
                _nui.AudioSource.AutomaticGainControlEnabled = false;
                this.status.Text = "Speech ready";
            }
        }

        //if speech is rejected
        private void RejectSpeech(RecognitionResult result)
        {
            if (_speechrecognition)
            status.Text = "Speech is rejected!";
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (_speechrecognition)
            RejectSpeech(e.Result);
        }

        //hypothesized result
        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (_speechrecognition)
            status.Text = "Hypothesized: " + e.Result.Text + " " + e.Result.Confidence;
        }

        private void SreSpeechStartRecogn(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text.ToUpperInvariant() == "KINECT") 
            _startspeech = true;
        }

        //Speech is recognised
        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (_speechrecognition)
            {
                //Very important! - change this value to adjust accuracy - the higher the value
                //the more accurate it will have to be, lower it if it is not recognizing you
                if (e.Result.Confidence < 0.2)
                {
                    RejectSpeech(e.Result);
                }
                string recognized_text = null;
                //and finally, here we set what we want to happen when 
                //the SRE recognizes a word
                
                switch (e.Result.Text.ToUpperInvariant())
                {
                    case "KINECT RECORD":
                        //if(!_replaying && !_learning && _playingback)
                        this.tcCaptureClick(null, null);
                        //status2.Text = "Record.";
                        recognized_text = "record in five second";
                        break;
                    case "KINECT STORE RECORD":
                        if(_capturing)
                        this.tcStoreClick(null, null);
                        //status2.Text = "Store.";
                        recognized_text = "Store";
                        break;
                    case "REPLAY":
                        if (!_capturing && !_learning && !_playingback)
                        this.tcReplayClick(null, null);
                        //status2.Text = "Replay.";
                        recognized_text = "Replay";
                        break;
                    case "KINECT STOP REPLAY":
                        if(_replaying)
                        this.tcStopReplayClick(null, null);
                        //status2.Text = "Stop.";
                        recognized_text = "Stop replay";
                        break;
                    case "KINECT LEARN":
                        //if (!_replaying && !_capturing && _playingback)
                        this.tcStartLearningClick(null, null);
                        //status2.Text = "Learn.";
                        recognized_text = "Start learning in five second";
                        break;
                    case "KINECT FINISH":
                        if(_learning)
                        this.tcStopLearningClick(null, null);
                        //status2.Text = "finish.";
                        recognized_text = "Start learning in five second";
                        break;
                    case "PLAYBACK":
                        if (_learning)
                            this.tcStopLearningClick(null, null);
                        //status2.Text = "finish.";
                        recognized_text = "Playing back " + gestureList.Text;
                        break;
                    case "STOP PLAYBACK":
                        if (_learning)
                            this.tcStopLearningClick(null, null);
                        //status2.Text = "finish.";
                        recognized_text = "Stop playing back " + gestureList.Text;
                        break;
                    default:
                        break;
                }
                _startspeech = false;
                //synthesizer.Speak(recognized_text);
            }
        }

        private void traning_Checked(object sender, RoutedEventArgs e)
        {
            _training = true;
        }

        private void challenge_Checked(object sender, RoutedEventArgs e)
        {
            _training = false;
        }

        private void SpeechRecogn_Checked(object sender, RoutedEventArgs e)
        {
            _speechrecognition = true;
        }

        private void SpeechRecogn_Unchecked(object sender, RoutedEventArgs e)
        {
            _speechrecognition = false;
        }

        private void easy_Checked(object sender, RoutedEventArgs e)
        {
            threshold = 80;
        }

        private void medium_Checked(object sender, RoutedEventArgs e)
        {
            threshold = 50;
        }

        private void hard_Checked(object sender, RoutedEventArgs e)
        {
            threshold = 30;
        }

        /// <summary>
        /// Read the selected dtw path, output an Point array 
        /// </summary>
        /// <param name="path">the files' location</param>
        public Point[] DtwReadSelectedFrames(string path)
        {
            Point[] dtwselected;
            using (FileStream fs_ma = File.Create(@path + "MasterSelected"))
            {
                using (FileStream fs_le = File.Create(@path + "LearnerSelected"))
                {
                    using (BinaryReader reader_ma = new BinaryReader(fs_ma))
                    {
                        int length = reader_ma.ReadInt32();
                        dtwselected = new Point[length];
                        using (BinaryReader reader_le = new BinaryReader(fs_le))
                        {
                            for (int i = 0; i < length; i++)
                            {
                                dtwselected[i].X = reader_ma.ReadInt32();
                                dtwselected[i].Y = reader_le.ReadInt32();
                            }
                        }
                    }
                }
            }
            return dtwselected;
        }

        private void readLastFrame(string gesture)
        {
            String path = ".\\Records\\" + gesture + "\\";
            if (Directory.Exists(path))
            {
                using (FileStream fs = File.OpenRead(@path + "frame_number"))
                {
                    BinaryReader reader = new BinaryReader(fs);
                    _finalframeno = reader.ReadInt32();
                }
            }
        }

        /// <summary>
        /// make sure to delete the file successfully
        /// </summary>
        /// <param name="filename">the targeted file</param>
        /// <returns></returns>
        public static bool FileDelete(string filename)
        {
            try
            {
                File.Delete(@filename);
                while (File.Exists(@filename))
                {
                    Thread.Sleep(2000);
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        
    }
}
