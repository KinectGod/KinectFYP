namespace GrabSkeletonData
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Forms;
    using System.Linq;
    using Microsoft.Kinect;
    using GrabSkeletonData.DTW;
    using GrabSkeletonData.Recorder;
    using GrabSkeletonData.Replay;
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;
    using System.Windows.Media;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Speech.Synthesis;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // We want to control how depth data gets converted into false-color data
        // for more intuitive visualization, so we keep 32-bit color frame buffer versions of
        // these, to be updated whenever we receive and process a 16-bit frame.

        /// <summary>
        /// Handle the color stream
        /// </summary>
        readonly ColorStreamManager RealTimeColorManager = new ColorStreamManager();
        readonly ColorStreamManager ReplayColorManager = new ColorStreamManager();
        SkeletonDrawManager LearningSkeleton;
        SkeletonDrawManager RealTimeSkeleton;
        SkeletonDrawManager ReplaySkeleton;

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
        /// Where we will save our gestures to. The app will append a data/time and .txt to this string
        /// </summary>

        private static string _MasterMovesSaveFileLocation = "";

        /// <summary>
        /// Dictionary of all the joints Kinect SDK is capable of tracking. You might not want always to use them all but they are included here for thouroughness.
       
        /// number of joints that we need
        private const int dimension = 16;

        /// <summary>
        /// To avoid feedback too much
        /// </summary>
        private static int counttime = 0;

        /* DEPTH
        /// <summary>
        /// The depth frame byte array. Only supports 320 * 240 at this time
        /// </summary>
        private readonly short[] _depthFrame32 = new short[320 * 240 * 4];
         * */

        /// <summary>
        /// Flag to show whether or not the Tai Chi learning system is capturing a new pose
        /// </summary>
        private bool _capturing = false;

        /// <summary>
        /// Flag to show whether the mode is training mode or challenge mode
        /// </summary>
        private static bool _training = true;

        /// <summary>
        /// flag to show whether it is playing back or not
        /// </summary>
        private static bool _playback = false;

        /// <summary>
        /// Flag to show whether or not the the system is in Learning Mode
        /// </summary>
        private bool _learning = false;

        /// <summary>
        /// Dynamic Time Warping object
        /// </summary>
        private DtwGestureRecognizer _dtw;

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
        /// Switch used to ignore certain skeleton frames
        /// </summary>
        private int _flipFlop;

        /// <summary>
        /// ArrayList of coordinates which are recorded in sequence to define one gesture
        /// </summary>
        private ArrayList _video;

        // Kinect recorder
        private static KinectRecorder _recorder;
        private static KinectRecorder _colorrecorder;

        private static Stream _recordskeletonstream;
        private static Stream _recordcolorstream;
        private static Stream _learningskeletonstream;
        private static Stream _learningcolorstream;

        private KinectReplay _replay;
        private KinectReplay _colorreplay;

        /// 
        private DateTime _captureCountdown = DateTime.Now;

        /// 
        private System.Windows.Forms.Timer _captureCountdownTimer;

        ///REMARK
        private static Skeleton[] _RecogSkeletons;

        private static Point[] _MasterAngle;

        private static Point[] _LearnerAngle;

        public static int[] detection = new int [dimension];

        private static double[] angles = new double [dimension];

        ///Difficulty
        private static double threshold = 40.0;

        /// <summary>
        /// The replay rate 
        /// </summary>
        private static int SelectedFPS = 30;
        private static double rateinmsec = 1000.0/SelectedFPS;


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
            /* voice control
            audioManager = new AudioStreamManager(kinectSensor.AudioSource);
            audioBeamAngle.DataContext = audioManager;
             * */
            _lastTime = DateTime.Now;

            _video = new ArrayList();

            RealTimeSkeleton = new SkeletonDrawManager(RealTimeSkeletonCanvas, _nui);
            ReplaySkeleton = new SkeletonDrawManager(MasterSkeletonCanvas, _nui);
            LearningSkeleton = new SkeletonDrawManager(LearningSkeletonCanvas, _nui);

            _dtw = new DtwGestureRecognizer(dimension *2, 0.6, 2, 2, 10);
            // If you want to see the RGB stream then include this
            _nui.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            _nui.ColorFrameReady += NuiColorFrameReady;

            Skeleton3DDataExtract.Skeleton3DdataCoordReady += NuiSkeleton3DdataCoordReady;
            _nui.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            });
            _nui.SkeletonFrameReady += NuiSkeletonFrameReady;
            _nui.SkeletonFrameReady += SkeletonExtractSkeletonFrameReady;
            /* Voice Control
            voiceCommander = new VoiceCommander("record", "stop");
            voiceCommander.OrderDetected += voiceCommander_OrderDetected;

            StartVoiceCommander();
            */

            RealTimeImage.DataContext = RealTimeColorManager;
            ReplayImage.DataContext = ReplayColorManager;

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

        private static void SkeletonExtractSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null) return; // sometimes frame image comes null, so skip it.
                var skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo(skeletons);

                foreach (Skeleton data in skeletons)
                {
                    Skeleton3DDataExtract.ProcessData(data, false);
                }

                //maker for record
            }
        }


        /// <summary>
        /// Runds every time a skeleton frame is ready. Updates the skeleton canvas with new joint and polyline locations.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Skeleton Frame Event Args</param>
        private void NuiSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons;
            int length;
            Point[] temppt = new Point[dimension];
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null) return;
                skeletons = new Skeleton[frame.SkeletonArrayLength];
                length = frame.SkeletonArrayLength;
                frame.CopySkeletonDataTo(skeletons);
            }

            //DrawSkeleton(skeletons, LearnerSkeletonCanvas);
            RealTimeSkeleton.DrawSkeleton(skeletons);

            if (_learning && _training || _playback)
            {
                LearningSkeleton.DrawSkeleton(skeletons);
                var brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                int[] DetectionTemp = new int[dimension];
                DetectionTemp = detection;
                string temp = "";

                foreach (var data in skeletons)
                {
                    temppt = Skeleton3DDataExtract.ProcessDataTEST(data);
                    if (temppt[4].X >= 0)
                        _LearnerAngle = temppt;
                    if (_LearnerAngle != null)
                    {
                        counttime++;
                            for (int i = 0; i < dimension; i++)
                            {
                                if (DetectionTemp[i] > 0)
                                {
                                    /// REMARK : 16 cases
                                    //Console.WriteLine("k");
                                    //generate the instruction
                                    /// The hundreds place and a.X represent ProjectToXZ;
                                    /// 200=down ;300=up; 400=forward; 500=backward.
                                    /// The tens place and a.Y represent ProjectToZY.
                                    /// 20=right  40=left
                                    
                                    /*
                                    int XZ =  DetectionTemp[i]/100;
                                    string instructionX = "";
                                    switch(XZ)
                                    {
                                        case 2:
                                            instructionX = "down ";
                                            break;
                                        case 3:
                                            instructionX = "up ";
                                            break;
                                        case 4:
                                            instructionX = "forward ";
                                            break;
                                        case 5:
                                            instructionX = "backward ";
                                            break;
                                    }
                                    int YZ = DetectionTemp[i] % 100;
                                    string instructionY = "";
                                    switch (YZ)
                                    {
                                        case 20:
                                            instructionY = "right ";
                                            break;
                                        case 40:
                                            instructionY = "left ";
                                            break;
                                    }

                                    DateTime now = DateTime.Now;
                                    temp  = "[" + now + "] ";
                                     * temp += instructionX + instructionY +"\r\n";
                                    */
                                    LearningSkeleton.DrawCorrection(data, DetectionTemp[i], angles[i], i);
                                    
                                    
                                }
                            }
                        }
                }
            }

            if (_capturing == true)
            {
                using (var sframe = e.OpenSkeletonFrame())
                {
                    if (sframe == null)
                        return;
                    _recorder.Record(sframe);

                    String path = ".\\Records\\" + gestureList.Text + "\\";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    FileStream fs = File.Create(@path + "frame_number");
                    BinaryWriter sw = new BinaryWriter(fs);
                    sw.Write(sframe.FrameNumber);
                    sw.Close();
                    fs.Close();
                    //REMARK
                }
            }
            
            

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
            }

            if (_capturing == true || _learning == true)
            {
                using (var scolorImage = e.OpenColorImageFrame())
                {
                    if (scolorImage == null)
                        return;
                    if(!_learning)
                    _colorrecorder.Record(scolorImage);
                }
            }
        }

        void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            var image = e.ColorImageFrame;

            if (image == null) return; // sometimes frame image comes null, so skip it.
            ReplayColorManager.Update(image);
        }

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
            if (_learning)
            {
                foreach (var data in skeletons)
                {
                    temppt = Skeleton3DDataExtract.ProcessDataTEST(data);
                    if (temppt[4].X >= 0)
                        _MasterAngle = temppt;
                    //Console.WriteLine(_MasterAngle[4].X);
                    if (_LearnerAngle != null && _MasterAngle != null)
                    {
                        angles = MotionDetection.Detect(_LearnerAngle, _MasterAngle, dimension, threshold, detection);
                    }
                }
            }

            String path = ".\\Records\\" + gestureList.Text + "\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            FileStream fs = File.OpenRead(path + "frame_number");
            
            BinaryReader reader = new BinaryReader(fs);

            int intVal = reader.ReadInt32();

            if (intVal <= frame.FrameNumber)
            {
                MasterSkeletonCanvas.Children.Clear();
                LearningSkeletonCanvas.Children.Clear();
                ReplayImage.Source = null;
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
            Debug.WriteLine("Stopping NUI");
            _nui.Stop();
            Debug.WriteLine("NUI stopped");
            Environment.Exit(0);
        }

        /// <summary>
        /// Runs every time our 2D coordinates are ready.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 2Ddata Coord Event Args</param>
        // DEBUG : whether this function relates to the limitation of recording frames
        
        private void NuiSkeleton3DdataCoordReady(object sender, Skeleton3DdataCoordEventArgs a)
        {
            /// display the current frame number
            //currentBufferFrame.Text = _video.Count.ToString();

            // Decide which skeleton frames to capture. Only do so if the frames actually returned a number. 
            // For some reason my Kinect/PC setup didn't always return a double in range (i.e. infinity) even when standing completely within the frame.
            // TODO Weird. Need to investigate this
            // REMARK. INFINIY PROBLEM
            if (!double.IsNaN(a.GetPoint(0).X))
            {
                // Optionally register only 1 frame out of every n
                _flipFlop = (_flipFlop + 1) % Ignore;
                if (_flipFlop == 0)
                {
                    _video.Add(a.GetAngle());
                    //marker
                    
                }
            }

            // Update the debug window with Sequences information
            //dtwTextOutput.Text = _dtw.RetrieveText();
        }
        /// <summary>
        /// Starts a countdown timer to enable the player to get in position to record gestures
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwCaptureClick(object sender, RoutedEventArgs e)
        {
            _learning = false;
            //dtwRead.IsEnabled = false;
            //dtwCapture.IsEnabled = false;
            dtwStore.IsEnabled = true;
            dtwReplay.IsEnabled = false;
            dtwStartRegcon.IsEnabled = false;
            _captureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);

            _captureCountdownTimer = new System.Windows.Forms.Timer();
            _captureCountdownTimer.Interval = 50;
            _captureCountdownTimer.Start();
            _captureCountdownTimer.Tick += CaptureCountdown;
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
                        DtwStartRecogn();
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

        private void DtwStartRecogn()
        {
            _learning = true;
            _capturing = false;

            dtwCapture.IsEnabled = false;
            dtwStartRegcon.IsEnabled = false;
            dtwStopRegcon.IsEnabled = true;
            string path = ".\\Records\\" + gestureList.Text + "\\";

            if (_recordskeletonstream != null)
                _recordskeletonstream.Close();
            _recordskeletonstream = File.OpenRead(@path + "skeleton");
            _replay = new KinectReplay(_recordskeletonstream);
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            _replay.Start(rateinmsec);

            if (_recordcolorstream != null)
                _recordcolorstream.Close();
            _recordcolorstream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(_recordcolorstream);
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            _colorreplay.Start(rateinmsec);

            _captureCountdownTimer.Dispose();

            status.Text = "Learning " + gestureList.Text;

            /*
            // Clear the _video buffer and start from the beginning
            _video = new ArrayList();
            path = ".\\Learning\\" + gestureList.Text + "\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            _recordstream = File.Create(@path + "skeleton");
            _recorder = new KinectRecorder(KinectRecordOptions.Skeletons, _recordstream);
            //throw new NotImplementedException();
             * */
        } 

        /// <summary>
        /// Capture mode. Sets our control variables and button enabled states
        /// </summary>
        private void StartCapture()
        {
            // Set the buttons enabled state
            //dtwRead.IsEnabled = false;
            //dtwCapture.IsEnabled = false;
            dtwStore.IsEnabled = true;

            // Set the capturing? flag
            _capturing = true;
            ////_captureCountdownTimer.Dispose();
            status.Text = "Recording motion " + gestureList.Text;

            // Clear the _video buffer and start from the beginning
            _video = new ArrayList();
            string path;
            if (_learning)
            {
                path = ".\\Learning\\" + gestureList.Text + "\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (_learningcolorstream != null)
                    _learningcolorstream.Close();
                if (_learningskeletonstream != null)
                    _learningskeletonstream.Close();
                _MasterMovesSaveFileLocation = path;
                _learningskeletonstream = File.Create(@path + "skeleton");
                _learningcolorstream = File.Create(@path + "colorStream");
                _recorder = new KinectRecorder(KinectRecordOptions.Skeletons, _learningskeletonstream);
                _colorrecorder = new KinectRecorder(KinectRecordOptions.Color, _learningcolorstream);
            }
            else
            {
                path = ".\\Records\\" + gestureList.Text + "\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (_recordcolorstream != null)
                    _recordcolorstream.Close();
                if (_recordskeletonstream != null)
                    _recordskeletonstream.Close();
                _MasterMovesSaveFileLocation = path;
                _recordskeletonstream = File.Create(@path + "skeleton");
                _recordcolorstream = File.Create(@path + "colorStream");
                _recorder = new KinectRecorder(KinectRecordOptions.Skeletons, _recordskeletonstream);
                _colorrecorder = new KinectRecorder(KinectRecordOptions.Color, _recordcolorstream);
                
            }

            
        }
       


        /// <summary>
        /// Stores our gesture to the DTW sequences list
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwStoreClick(object sender, RoutedEventArgs e)
        {
            // Set the buttons enabled state
            //dtwRead.IsEnabled = false;
            //dtwCapture.IsEnabled = true;
            dtwStore.IsEnabled = false;
            dtwReplay.IsEnabled = true;
            dtwStartRegcon.IsEnabled = true;
            // Set the capturing? flag
            _learning = false;
            _capturing = false;


            // Add the current video buffer to the dtw sequences list
            _dtw.AddOrUpdate(_video, gestureList.Text);

            string fileName = "AnglesData.txt";
            System.IO.File.WriteAllText(@_MasterMovesSaveFileLocation + fileName, _dtw.RetrieveText());
            status.Text = "Remembering " + gestureList.Text;

            status.Text = gestureList.Text + " added";
            status.Text = "";
            _recordskeletonstream.Close();
            _recordcolorstream.Close();
            // Scratch the _video buffer
            _video = new ArrayList();

            // Switch back to Read mode
            // DtwReadClick(null, null);
        }

        //Replay the saved skeleton
        private void DtwReplayClick (object sender, RoutedEventArgs e) 
        {
            _learning = false;
            dtwCapture.IsEnabled = false;
            dtwStartRegcon.IsEnabled = false;
            dtwReplay.IsEnabled = false;
            status.Text = "Replaying master motion " + gestureList.Text;
            string path = ".\\Records\\" + gestureList.Text + "\\";

            if (_recordskeletonstream != null)
                _recordskeletonstream.Close();
            _recordskeletonstream = File.OpenRead(@path + "skeleton");
            _replay = new KinectReplay(_recordskeletonstream);
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            _replay.Start(rateinmsec);

            if (_recordcolorstream != null)
                _recordcolorstream.Close();
            _recordcolorstream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(_recordcolorstream);
            //recordColorStream.Close();
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            _colorreplay.Start(rateinmsec);

            dtwStopReplay.IsEnabled = true;
        }

        private void DtwStopReplayClick(object sender, RoutedEventArgs e)
        {
            status.Text = "Stopped replay";
            dtwCapture.IsEnabled = true;
            dtwStopReplay.IsEnabled = false;
            dtwStartRegcon.IsEnabled = true;
            _replay.Stop();
            _colorreplay.Stop();

            MasterSkeletonCanvas.Children.Clear();
        }

        private void DtwStartRecogn(object sender, RoutedEventArgs e)
        {
            DtwStartRecognClick();
        }
        private void DtwStartRecognClick()
        {
            _learning = true;
            _capturing = false;
            _captureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);

            _captureCountdownTimer = new System.Windows.Forms.Timer();
            _captureCountdownTimer.Interval = 50;
            _captureCountdownTimer.Start();
            _captureCountdownTimer.Tick += CaptureCountdown;
        }

        private void DtwStopRecogn(object sender, RoutedEventArgs e)
        {
            DtwStopRecogn();
        }

        private void DtwStopRecogn()
        {
            status.Text = "Stopped learaning";
            dtwCapture.IsEnabled = true;
            dtwStopReplay.IsEnabled = false;
            dtwStartRegcon.IsEnabled = true;
            dtwStopRegcon.IsEnabled = false;
            _learning = false;
            _capturing = false;
            _replay.Stop();
            _colorreplay.Stop();
            _recordskeletonstream.Close();
            _recordcolorstream.Close();

            MasterSkeletonCanvas.Children.Clear();
            LearningSkeletonCanvas.Children.Clear();
            ReplayImage.Source = null;
        }

        private void CreateSpeechRecognizer()
        {
            //set recognizer info
            RecognizerInfo ri = GetKinectRecognizer();
            //create instance of SRE
            if (null != ri)
            {
                //SpeechRecognitionEngine speechRecognizer;
                speechRecognizer = new SpeechRecognitionEngine(ri.Id);
                
                //Now we need to add the words we want our program to recognise
                var grammar = new Choices("Kinect");
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

                //set up the grammar builder
                var g = new Grammar(gb);
                speechRecognizer.LoadGrammar(g);

                //Set events for recognizing, hypothesising and rejecting speech
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
            status.Text = "Speech is rejected!";
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            RejectSpeech(e.Result);
        }

        //hypothesized result
        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            status.Text = "Hypothesized: " + e.Result.Text + " " + e.Result.Confidence;
        }

        //Speech is recognised
        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Very important! - change this value to adjust accuracy - the higher the value
            //the more accurate it will have to be, lower it if it is not recognizing you
            if (e.Result.Confidence < 0.7f)
            {
                RejectSpeech(e.Result);
            }
            string recognized_text = null;
            //and finally, here we set what we want to happen when 
            //the SRE recognizes a word
            /*
            switch (e.Result.Text.ToUpperInvariant())
            {
                case "RECORD":
                    DtwCaptureClick();
                    status2.Text = "Record.";
                    recognized_text = "record in five second";
                    break;
                case "STORE":
                    DtwStoreClick();
                    status2.Text = "Store.";
                    recognized_text = "Store";
                    break;
                case "REPLAY":
                    DtwReplayClick();
                    status2.Text = "Replay.";
                    recognized_text = "Replay";
                    break;
                case "STOP":
                    DtwStopReplayClick();
                    status2.Text = "Stop.";
                    recognized_text = "Stop replay";
                    break;
                case "LEARN":
                    DtwStartRecognClick();
                    status2.Text = "Learn.";
                    recognized_text = "Start learning in five second";
                    break;
                case "FINISH":
                    DtwStopRecogn();
                    status2.Text = "finish.";
                    recognized_text = "Finish learning"
                    break;
                default:
                    break;
            }
            */
            synthesizer.Speak(recognized_text);
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
            CreateSpeechRecognizer();
        }

        private void SpeechRecogn_Checked(object sender, RoutedEventArgs e)
        {
            speechRecognizer.RecognizeAsyncStop();
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



        /*
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void dtwSpeedSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
         * */
    }
}
