﻿namespace TaiChiLearning
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
    using System.Windows.Media;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.ComponentModel;
    //using Microsoft.Speech.Synthesis;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        /// <summary>
        /// Handle the color stream
        /// </summary>
        readonly ColorStreamManager RealTimeColorManager = new ColorStreamManager();
        readonly ColorStreamManager ReplayColorManager = new ColorStreamManager();
        readonly ColorStreamManager PlayBackColorManager = new ColorStreamManager();
        //SkeletonDrawManager LearningSkeleton;
        SkeletonDrawManager RealTimeSkeleton;
        SkeletonDrawManager ReplaySkeleton;
        SkeletonDrawManager LearnerSkeleton;

        /// <summary>
        /// The minumum number of frames in the _video buffer before we attempt to start matching gestures
        /// </summary>
        private const int CaptureCountdownSeconds = 5;

        /// <summary>
        /// Dictionary of all the joints Kinect SDK is capable of tracking. You might not want always to use them all but they are included here for thouroughness.
       
        /// number of joints that we need
        private const int dimension = 19;

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

        /// <summary>
        /// flag to show whether in playback lerning mode
        /// </summary>
        private static bool _playingbackmaster = false;

        /// <summary>
        /// used to store the masater skeleton data, ;ater support for 6 people
        /// </summary>
        private static Skeleton _masterskeleton;

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

        /// <summary>
        /// ArrayList of master's and learner motion
        /// </summary>
        private ArrayList _masterseq = new ArrayList();
        private ArrayList _learnerseq = new ArrayList();
        private ArrayList _learnerseqFrame = new ArrayList();
        private ArrayList _learnercolorFrame = new ArrayList();

        // Kinect recorder
        private static KinectRecorder _recorder;
        private static KinectRecorder _colorrecorder;

        /// <summary>
        /// stream for File I/O operation
        /// </summary>
        private static Stream _recordskeletonstream;
        private static Stream _recordcolorstream;
        private static Stream _learnerskeletonstream;
        private static Stream _learnercolorstream;

        // kinect replay
        private KinectReplay _replay;
        private KinectReplay _colorreplay;
        private KinectReplay _playback;
        private KinectReplay _colorplayback;
        private KinectReplay _playbackmaster;
        private KinectReplay _colorplaybackmaster;

        private string _temppath = ".\\";

        /// <summary>
        /// used as parameter for count down function
        /// </summary>
        private DateTime _captureCountdown = DateTime.Now;


        /// 
        private System.Windows.Forms.Timer _captureCountdownTimer;
        
        /// <summary>
        /// used to store the master and learner instant angles
        /// </summary>
        private static Point[] _MasterAngle;
        private static Point[] _LearnerAngle;

        /// <summary>
        /// flag to indicate whether the body segment is too wrong
        /// </summary>
        public static int[] detection = new int [dimension];

        /// <summary>
        /// used to store the angles differences
        /// </summary>
        private static double[] _anglesdiff = new double [dimension];

        /// <summary>
        /// used to store the body segment length of master
        /// </summary>
        private static double[] _master_length = new double[dimension];

        /// <summary>
        /// used to store the final frame number of a replaying video, to check whether it is the end of video
        /// </summary>
        private static int _finalframeno;
        private static int _finalframeno2;

        ///Difficulty
        private static double threshold = 60.0;

        /// <summary>
        /// Property change event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The replay rate 
        /// </summary>
        private int selectedFPS = 30;
        
        /// <summary>
        /// detect the slide bar value of the replay fps
        /// </summary>
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
            LearnerSkeleton = new SkeletonDrawManager(MasterSkeletonCanvas, _nui);
            //PlayBackSkeleton = new SkeletonDrawManager(PlayBackSkeletonCanvas, _nui);
            //LearningSkeleton = new SkeletonDrawManager(LearningSkeletonCanvas, _nui);

            _dtw = new DtwForTaiChiLearning(dimension);
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

            /*
            
             * */
            if (Directory.Exists(@_temppath + ".\\Records\\"))
            {
                string[] directories = Directory.GetDirectories(@_temppath + ".\\Records\\");
                for (int i = 0; i < directories.Length; i++)
                {
                    directories[i] = Path.GetFileName(directories[i]);
                    gestureList.Items.Add(directories[i]);
                    gestureList.SelectedItem = directories[0];
                    string path = ".\\Records\\" + directories[0] + "\\";
                    if (File.Exists(@path + "frame_number"))
                    {
                        using (FileStream fs = File.OpenRead(@path + "frame_number"))
                        {
                            BinaryReader reader = new BinaryReader(fs);
                            _finalframeno = reader.ReadInt32();
                        }
                    }
                }
            }
                _nui.Start();
            CreateSpeechRecognizer();
            _masterseq.Clear();
            _learnerseq.Clear();
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
            if (!_playingback && !_playingbackmaster)
            {
                int length;
                Point[] temppt = new Point[dimension];
                double[] templength = new double[dimension];
                Skeleton temp = null;
                SkeletonFrame _learner = e.OpenSkeletonFrame();

                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame == null) return;
                    var skeletons = new Skeleton[frame.SkeletonArrayLength];
                    length = frame.SkeletonArrayLength;
                    frame.CopySkeletonDataTo(skeletons);
                    RealTimeSkeleton.DrawSkeleton(skeletons);
                    //_learnerseqNum.Add(frame);

                    if (_learning && _capturing)
                    {
                        //RealTimeSkeleton.DrawSkeleton(skeletons);
                        var brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                        int[] DetectionTemp = new int[dimension];
                        DetectionTemp = detection;

                        foreach (var data in skeletons)
                        {
                            if (SkeletonTrackingState.Tracked == data.TrackingState)
                            {
                                temppt = Skeleton3DDataExtract.ProcessData(data);
                                templength = Skeleton3DDataExtract.LengthGeneration(data);
                                /*
                                if(_learnerini == null && _masterini != null)
                                {
                                    //_learnerini = data.Joints[JointType.ShoulderCenter];
                                     //inidiff = _masterini.Position.ToVector3() - _learnerini.Position.ToVector3();
                                }
                                */
                                if (_masterskeleton != null)
                                    temp = LearnerSkeleton.MasterMatchLearner(_master_length, templength, data, _masterskeleton);
                                _LearnerAngle = temppt;
                                if (_LearnerAngle != null)
                                {
                                    if (_training)
                                    {
                                        for (int i = 0; i < dimension; i++)
                                        {
                                            if (DetectionTemp[i] > 0)
                                            {
                                                RealTimeSkeleton.DrawCorrection(data, DetectionTemp[i], _anglesdiff[i], i);
                                                if (temp != null)
                                                {
                                                    ReplaySkeleton.DrawCorrection(temp, DetectionTemp[i], _anglesdiff[i], i);
                                                }
                                            }
                                        }
                                    }
                                    _learnerseq.Add(temppt);
                                    _learnerseqFrame.Add(_learner);
                                }
                            }
                        }
                        
                    }
                    
                    if (_capturing)
                    {
                        if (_recorder == null) return;
                        _recorder.Record(frame);
                        if (!_learning) _finalframeno = frame.FrameNumber;
                        else _finalframeno2 = frame.FrameNumber;
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
            var image2 = e.OpenColorImageFrame();
            using (var image = e.OpenColorImageFrame())
            {
                if (image == null) return; // sometimes frame image comes null, so skip it.

                RealTimeColorManager.Update(image);
                if (_capturing)
                {
                    if (_colorrecorder != null)
                        _colorrecorder.Record(image);
                    if (_learning)
                    {
                        _learnercolorFrame.Add(image2.FrameNumber);
                    }
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
                else if (_playingbackmaster) 
                {
                    this.tcStopPlaybackMasterClick(null, null);
                }
                else
                {
                    this.tcStopReplayClick(null, null);
                }
            }
            if (frame == null) return;

            skeletons = new Skeleton[frame.ArrayLength];
            skeletons = frame.Skeletons;
            Point[] temppt = new Point[dimension];
            double[] templength = new double[dimension];

            //DrawSkeleton(skeletons, MasterSkeletonCanvas);

            /// get the joint angle data of master
            /// then make comparison
            if (_learning)
            {
                foreach (var data in skeletons)
                {
                    if (SkeletonTrackingState.Tracked == data.TrackingState)
                    {

                        temppt = Skeleton3DDataExtract.ProcessData(data);

                        //if (temppt[4].X >= 0)
                        _MasterAngle = temppt;
                        //Console.WriteLine(_MasterAngle[4].X);
                        if (_LearnerAngle != null && _MasterAngle != null)
                        {
                            _masterskeleton = data;
                            _anglesdiff = MotionDetection.Detect(_LearnerAngle, _MasterAngle, dimension, threshold, detection);
                            _master_length = Skeleton3DDataExtract.LengthGeneration(data);
                            
                            _masterseq.Add(temppt);
                        }
                    }
                }
                //_masterseqNum.Add(frame);
            }
            else if(_replaying || _playingback || _learning || _playingbackmaster) 
            {
                ReplaySkeleton.DrawSkeleton(skeletons);
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
            if (frame == null ) return; // make sure it is replaying the dtw selected path
            skeletons = new Skeleton[frame.ArrayLength];
            skeletons = frame.Skeletons;
            Point[] temppt = new Point[dimension];

            //DrawSkeleton(skeletons, MasterSkeletonCanvas);
            RealTimeSkeleton.DrawSkeleton(skeletons);

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
                    paragraph.Inlines.Clear();
                    paragraph.Inlines.Add( "Wait " + ((_captureCountdown - DateTime.Now).Seconds + 1) + " seconds");
                }
                else
                {
                    _captureCountdownTimer.Stop();
                    paragraph.Inlines.Clear();
                    if (_learning)
                    {
                        status.Text = "Learning";
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

            paragraph.Inlines.Clear();
            paragraph.Inlines.Add("Start ...");
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
            var inputbox = Microsoft.VisualBasic.Interaction.InputBox("Your recording name", "HAPPY TAI CHI", "Default Text");
            if (inputbox == "") return;
            gestureList.Items.Add(inputbox);
            gestureList.SelectedItem = (inputbox);
            _learning = false;
            //dtwRead.IsEnabled = false;
            tcPlaybackMaster.IsEnabled = false;
            tcCapture.IsEnabled = false;
            tcStore.IsEnabled = false;
            tcReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = false;
            tcPlayBack.IsEnabled = false;

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
            tcCapture.IsEnabled = true;
            tcStore.IsEnabled = false;
            tcReplay.IsEnabled = true;
            tcStartLearning.IsEnabled = true;
            tcPlayBack.IsEnabled = true;
            tcPlaybackMaster.IsEnabled = true;
            // Set the capturing? flag
            _learning = false;
            _capturing = false;
            _recorder = null;
            _colorrecorder = null;

            paragraph.Inlines.Clear();

            /*
            const string message = "Are you sure that you would like to store the TaiChi motion?";
            const string caption = "Confirmation";
            var result = System.Windows.Forms.MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);

            // If the no button was pressed ... 
            if (result == System.Windows.Forms.DialogResult.Yes)
            {*/
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
 
                File.Copy(@_temppath + "skeleton", @path + "skeleton");
                File.Copy(@_temppath + "colorStream", @path + "colorStream");

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
           // }
           // else
           // {
            //    _recordskeletonstream.Close();
           //     _recordcolorstream.Close();
            //    return;
           // }
        }

        //Replay the saved skeleton
        private void tcReplayClick (object sender, RoutedEventArgs e) 
        {
            if (gestureList.SelectedItem != null)
            {
                if(TempReplayImage.Source != null)
                ReplayImage.Source = TempReplayImage.Source;
                _replaying = true;
                _learning = false;
                tcCapture.IsEnabled = false;
                tcStartLearning.IsEnabled = false;
                tcReplay.IsEnabled = false;
                tcPlayBack.IsEnabled = false;
                tcPlaybackMaster.IsEnabled = false;
                status.Text = "Replaying master motion " + gestureList.Text;
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

                tcStopReplay.IsEnabled = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a motion.");
            }
        }

        private void tcStopReplayClick(object sender, RoutedEventArgs e)
        {
            _replaying = false;
            status.Text = "Stopped replay";
            tcCapture.IsEnabled = true;
            tcStopReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = true;
            tcReplay.IsEnabled = true;
            tcPlayBack.IsEnabled = true;
            tcPlaybackMaster.IsEnabled = true;
            _replay.Stop();
            _colorreplay.Stop();

            MasterSkeletonCanvas.Children.Clear();
            TempReplayImage.Source = ReplayImage.Source;
            ReplayImage.Source = null;
        }

        private void tcStartLearningClick(object sender, RoutedEventArgs e)
        {
            tcCapture.IsEnabled = false;
            tcStartLearning.IsEnabled = false;
            tcReplay.IsEnabled = false;
            tcStopLearning.IsEnabled = true;
            tcPlayBack.IsEnabled = false;
            tcPlaybackMaster.IsEnabled = false;

            if (gestureList.SelectedItem != null)
            {
                if (TempReplayImage.Source != null)
                    ReplayImage.Source = TempReplayImage.Source;
                paragraph.Inlines.Clear();
                _learning = true;
                _capturing = false;
                _captureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);
                _learnerseqFrame.Clear();
                _masterseq.Clear();
                _learnerseq.Clear();
                _learnercolorFrame.Clear();

                _captureCountdownTimer = new System.Windows.Forms.Timer();
                _captureCountdownTimer.Interval = 50;
                _captureCountdownTimer.Start();
                _captureCountdownTimer.Tick += CaptureCountdown;
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a motion.");
            }
        }
        
        /// <summary>
        /// Event handler for the stop learning click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tcStopLearningClick(object sender, RoutedEventArgs e)
        {
            status.Text = "Stopped learning, waiting for process complete.";
            tcCapture.IsEnabled = true;
            tcStopReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = true;
            tcStopLearning.IsEnabled = false;
            tcReplay.IsEnabled = true;
            tcPlayBack.IsEnabled = true;
            tcPlaybackMaster.IsEnabled = true;
            _learning = false;
            _capturing = false;

            _replay.Stop();
            _colorreplay.Stop();
            MasterSkeletonCanvas.Children.Clear();
            RealTimeSkeletonCanvas.Children.Clear();
            TempReplayImage.Source = ReplayImage.Source;
            ReplayImage.Source = null;
            _recorder = null;
            _colorrecorder = null;
            
            /*
            const string message = "Are you satisfied with your performance this time, save or not?";
            const string caption = "Confirmation";
            var result = System.Windows.Forms.MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);
            // If the no button was pressed ... 
            if (result == System.Windows.Forms.DialogResult.Yes)
             
            {*/
            paragraph.Inlines.Clear();
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

                using (FileStream fs = File.Create(@path + "frame_number")) 
                {
                    using (BinaryWriter sw = new BinaryWriter(fs))
                    {
                        sw.Write(_finalframeno2);
                    }
                }
                

                if (_learnerseqFrame.Count > 0)
                {
                    _dTWresult = _dtw.DtwComputation(_masterseq, _learnerseq, _learnerseqFrame, _learnercolorFrame, path, threshold);

                    if (_dTWresult > 80.0)
                    {
                        paragraph.Inlines.Add("Excellent, your learning score is " + (int)_dTWresult);
                    }
                    else if (_dTWresult > 60.0)
                    {
                        paragraph.Inlines.Add("Good, your learning score is " + (int)_dTWresult);
                    }
                    else if (_dTWresult > 40.0)
                    {
                        paragraph.Inlines.Add("Please work harder, your learning score is " + (int)_dTWresult);
                    }
                    else
                    {
                        paragraph.Inlines.Add("Too bad, your learning score is " + (int)_dTWresult);
                    }
                }
                
                File.Copy(@_temppath + "skeleton", @path + "skeleton");
                File.Copy(@_temppath + "colorStream", @path + "colorStream");

                status.Text = gestureList.Text + " added";
           // }
            //else
           // {
            //    _learnerskeletonstream.Close();
           //     _learnercolorstream.Close();
           // }
        }

        private void tcPlayBack_Click(object sender, RoutedEventArgs e)
        {
            if (gestureList.SelectedItem != null)
            {
                string path = ".\\Learnings\\" + gestureList.Text + "\\";
                if (!File.Exists(@path+"skeleton"))
                {
                    System.Windows.MessageBox.Show("You have not recorded your learning of " + gestureList.SelectedItem);
                    return;
                }

                if (TempReplayImage.Source != null)
                    ReplayImage.Source = TempReplayImage.Source;
                _learning = false;
                _capturing = false;
                _playingback = true;
                _playingbackmaster = false;

                tcCapture.IsEnabled = false;
                tcStartLearning.IsEnabled = false;
                tcReplay.IsEnabled = false;
                tcStopLearning.IsEnabled = false;
                tcStopPlayBack.IsEnabled = true;
                tcPlayBack.IsEnabled = false;
                tcPlaybackMaster.IsEnabled = false;

                RealTimeImage.DataContext = PlayBackColorManager;
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

                if (_learnerskeletonstream != null)
                    _learnerskeletonstream.Close();
                _learnerskeletonstream = File.OpenRead(@path + "LearnerSelected");
                _playback = new KinectReplay(_learnerskeletonstream);
                _playback.SkeletonFrameReady += playback_SkeletonFrameReady;

                if (_learnercolorstream != null)
                    _learnercolorstream.Close();
                _learnercolorstream = File.OpenRead(@path + "LearnerSelectedColor");
                _colorplayback = new KinectReplay(_learnercolorstream);
                _colorplayback.ColorImageFrameReady += playback_ColorImageFrameReady;

                _playback.Start(1000.0 / this.SelectedFPS);
                _colorplayback.Start(1000.0 / this.SelectedFPS);
                _colorreplay.Start(1000.0 / this.SelectedFPS);
                _replay.Start(1000.0 / this.SelectedFPS);

                status.Text = "Playing back " + gestureList.Text;
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a motion.");
            }
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
            _playingbackmaster = false;
            tcPlaybackMaster.IsEnabled = true;
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
            TempReplayImage.Source = ReplayImage.Source;
            ReplayImage.Source = null;
        }

        private void tcPlaybackMasterClick(object sender, RoutedEventArgs e)
        {
            if (gestureList.SelectedItem != null)
            {
                string path = ".\\Learnings\\" + gestureList.Text + "\\";
                string path2 = ".\\Records\\" + gestureList.Text + "\\";

                if (!File.Exists(@path + "skeleton"))
                {
                    System.Windows.MessageBox.Show("You have not recorded your learning of " + gestureList.SelectedItem);
                    return;
                }

                if (TempReplayImage.Source != null)
                    ReplayImage.Source = TempReplayImage.Source;
                _learning = false;
                _capturing = false;
                _playingback = false;
                _playingbackmaster = true;

                tcCapture.IsEnabled = false;
                tcStartLearning.IsEnabled = false;
                tcReplay.IsEnabled = false;
                tcStopLearning.IsEnabled = false;
                tcPlayBack.IsEnabled = false;
                tcPlaybackMaster.IsEnabled = false;
                tcStopPlaybackMaster.IsEnabled = true;

                RealTimeImage.DataContext = PlayBackColorManager;
                readLastFrame(path2);

                if (_recordskeletonstream != null)
                    _recordskeletonstream.Close();
                _recordskeletonstream = File.OpenRead(@path2 + "skeleton");
                _replay = new KinectReplay(_recordskeletonstream);
                _replay.SkeletonFrameReady += replay_SkeletonFrameReady;

                if (_recordcolorstream != null)
                    _recordcolorstream.Close();
                _recordcolorstream = File.OpenRead(@path2 + "colorStream");
                _colorreplay = new KinectReplay(_recordcolorstream);
                _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;

                if (_learnerskeletonstream != null)
                    _learnerskeletonstream.Close();
                _learnerskeletonstream = File.OpenRead(@path + "LearnerSelected");
                _playbackmaster = new KinectReplay(_learnerskeletonstream);
                _playbackmaster.SkeletonFrameReady += playback_SkeletonFrameReady;

                if (_learnercolorstream != null)
                    _learnercolorstream.Close();
                _learnercolorstream = File.OpenRead(@path + "LearnerSelectedColor");
                _colorplaybackmaster = new KinectReplay(_learnercolorstream);
                _colorplaybackmaster.ColorImageFrameReady += playback_ColorImageFrameReady;

                _playbackmaster.Start(1000.0 / this.SelectedFPS);
                _colorplaybackmaster.Start(1000.0 / this.SelectedFPS);
                _colorreplay.Start(1000.0 / this.SelectedFPS);
                _replay.Start(1000.0 / this.SelectedFPS);

                status.Text = "Playing back " + gestureList.Text;
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a motion.");
            }
 
        }

        private void tcStopPlaybackMasterClick(object sender, RoutedEventArgs e)
        {
            _learning = false;
            _capturing = false;
            _playingback = false;
            _playingbackmaster = false;

            RealTimeImage.DataContext = RealTimeColorManager;

            status.Text = "Stopped playing back";
            tcCapture.IsEnabled = true;
            tcStopReplay.IsEnabled = false;
            tcStartLearning.IsEnabled = true;
            tcReplay.IsEnabled = true;
            tcPlayBack.IsEnabled = true;
            tcPlaybackMaster.IsEnabled = true;
            tcStopPlaybackMaster.IsEnabled = false;
            _replay.Stop();
            _colorreplay.Stop();
            _playbackmaster.Stop();
            _colorplaybackmaster.Stop();

            MasterSkeletonCanvas.Children.Clear();
            RealTimeSkeletonCanvas.Children.Clear();
            TempReplayImage.Source = ReplayImage.Source;
            ReplayImage.Source = null;
        }

        private void CreateSpeechRecognizer()
        {
            //set recognizer info
            RecognizerInfo ri = GetKinectRecognizer();
            //create instance of SRE
            if (null != ri)
            {
                //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); 
                //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

               //SpeechRecognitionEngine speechRecognizer;
                speechRecognizer = new SpeechRecognitionEngine(ri.Id);
                                
                //Now we need to add the words we want our program to recognise
                //  Create lists of alternative choices.
                Choices speechaction = new Choices(new string[] { "RECORD MOTION", "STOP RECORD", "REPLAY",
                    "STOP REPLAY", "START LEARNING", "FINISH LEARNING", "PLAYBACK IMPROVED MOTION", "STOP PLAYBACK MOTION", "PLAYBACK LEARNING", "STOP PLAYBACK LEARNING"  });

                // Create a GrammarBuilder object and assemble the grammar components.
                GrammarBuilder actionMenu = new GrammarBuilder("KINECT");
                //actionMenu.Append("KINECT");
                actionMenu.Append(speechaction);
                //actionMenu.Append("KINECT");
                actionMenu.Culture = ri.Culture;

                // Build a Grammar object from the GrammarBuilder.
                Grammar grammar = new Grammar(actionMenu);
                grammar.Name = "button";
                speechRecognizer.LoadGrammar(grammar);
                
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
            }
        }

        //if speech is rejected
        private void RejectSpeech(RecognitionResult result)
        {
            if (_speechrecognition)
            status.Text = "Speech " + result.Text + "is rejected!";
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

        /*
        private void SreSpeechStartRecogn(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text.ToUpperInvariant() == "KINECT") 
            _startspeech = true;
        }
         */

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
                //string recognized_text = null;
                //and finally, here we set what we want to happen when 
                //the SRE recognizes 

                //System.Media.SoundPlayer player = new System.Media.SoundPlayer(@_temppath + "countdown.mp3");
                //player.Play();
                switch (e.Result.Text.ToUpperInvariant())
                {
                    case "KINECT RECORD MOTION":
                        //if (!_replaying && !_learning && !_playingback && !_counting && !_capturing && !_playingbackmaster)
                        if(this.tcCapture.IsEnabled == true)
                        this.tcCaptureClick(null, null);
                        status.Text = "Recognized as Record Motion";
                        break;
                    case "KINECT STOP RECORD":
                        if(_capturing)
                        this.tcStoreClick(null, null);
                        break;
                    case "KINECT REPLAY":
                        //if (!_replaying && !_learning && !_playingback && !_counting && !_capturing && !_playingbackmaster)
                        if(tcReplay.IsEnabled == true)
                        this.tcReplayClick(null, null);
                        break;
                    case "KINECT STOP REPLAY":
                        if(_replaying)
                        this.tcStopReplayClick(null, null);
                        break;
                    case "KINECT START LEARNING":
                        //if (!_replaying && !_learning && !_playingback && !_counting && !_capturing && !_playingbackmaster)
                        if(tcStartLearning.IsEnabled == true)
                        this.tcStartLearningClick(null, null);
                        status.Text = "Recognized as Start Learning";
                        break;
                    case "KINECT FINISH LEARNING":
                        if(_learning)
                        this.tcStopLearningClick(null, null);
                        break;
                    case "KINECT PLAYBACK IMPROVED MOTION":
                        //if (!_replaying && !_learning && !_playingback && !_counting && !_capturing && !_playingbackmaster)
                        if(tcPlayBack.IsEnabled == true)
                        this.tcPlayBack_Click(null, null);
                        break;
                    case "KINECT STOP PLAYBACK MOTION":
                        if (_playingback)
                        this.tcStopPlayBackClick(null, null);
                        break;
                    case "KINECT PLAYBACK LEARNING":
                        //if (!_replaying && !_learning && !_playingback && !_counting && !_capturing && !_capturing)
                        if(tcPlaybackMaster.IsEnabled == true)
                            this.tcPlaybackMasterClick(null, null);
                        break;
                    case "KINECT STOP PLAYBACK LEARNING":
                        if (_playingbackmaster)
                            this.tcStopPlaybackMasterClick(null, null);
                        break;
                    default:
                        break;
                }
                
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
            threshold = 60;
        }

        private void medium_Checked(object sender, RoutedEventArgs e)
        {
            threshold = 45;
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
            using (FileStream fs_ma = File.OpenRead(@path + "MasterSelected"))
            {
                using (FileStream fs_le = File.OpenRead(@path + "LearnerSelected"))
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

        private void readLastFrame(string path)
        {
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
                    Thread.Sleep(500);
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        private void gestureList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string path = ".\\Records\\" + gestureList.SelectedItem + "\\";
            if (File.Exists(@path + "frame_number"))
            {
                using (FileStream fs = File.OpenRead(@path + "frame_number"))
                {
                    BinaryReader reader = new BinaryReader(fs);
                    _finalframeno = reader.ReadInt32();
                }
            }
        }

        
    }
}
