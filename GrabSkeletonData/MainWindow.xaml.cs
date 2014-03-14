namespace GrabSkeletonData
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Linq;
    using Microsoft.Kinect;
    using GrabSkeletonData.DTW;
    using GrabSkeletonData.Recorder;
    using GrabSkeletonData.Replay;
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // We want to control how depth data gets converted into false-color data
        // for more intuitive visualization, so we keep 32-bit color frame buffer versions of
        // these, to be updated whenever we receive and process a 16-bit frame.

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

        private static int counttime = 0;

        private readonly Dictionary<JointType, Brush> _jointColors = new Dictionary<JointType, Brush>
        { 
            {JointType.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointType.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointType.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointType.Head, new SolidColorBrush(Color.FromRgb(200, 0, 0))},
            {JointType.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79, 84, 33))},
            {JointType.ElbowLeft, new SolidColorBrush(Color.FromRgb(84, 33, 42))},
            {JointType.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointType.HandLeft, new SolidColorBrush(Color.FromRgb(215, 86, 0))},
            {JointType.ShoulderRight, new SolidColorBrush(Color.FromRgb(33, 79,  84))},
            {JointType.ElbowRight, new SolidColorBrush(Color.FromRgb(33, 33, 84))},
            {JointType.WristRight, new SolidColorBrush(Color.FromRgb(77, 109, 243))},
            {JointType.HandRight, new SolidColorBrush(Color.FromRgb(37,  69, 243))},
            {JointType.HipLeft, new SolidColorBrush(Color.FromRgb(77, 109, 243))},
            {JointType.KneeLeft, new SolidColorBrush(Color.FromRgb(69, 33, 84))},
            {JointType.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointType.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointType.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointType.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222, 76))},
            {JointType.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointType.FootRight, new SolidColorBrush(Color.FromRgb(77, 109, 243))}
        };

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
        /// Flag to show whether or not the the system is in Learning Mode
        /// </summary>
        private bool _learning = false;

        /// <summary>
        /// Dynamic Time Warping object
        /// </summary>
        private DtwGestureRecognizer _dtw;

        /// <summary>
        /// How many frames occurred 'last time'. Used for calculating frames per second
        /// </summary>
        private int _lastFrames;

        /// <summary>
        /// The 'last time' DateTime. Used for calculating frames per second
        /// </summary>
        private DateTime _lastTime = DateTime.MaxValue;

        /// <summary>
        /// The Natural User Interface runtime
        /// </summary>
        private KinectSensor  _nui;

        /// <summary>
        /// Total number of framed that have occurred. Used for calculating frames per second
        /// </summary>
        private int _totalFrames;

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

        private static Stream _recordstream;
        private static Stream _recordcolorstream;

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

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
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
        /// Gets the display position (i.e. where in the display image) of a Joint
        /// </summary>
        /// <param name="joint">Kinect NUI Joint</param>
        /// <returns>Point mapped location of sent joint</returns>
        private Point GetDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            var pos = _nui.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution320x240Fps30);

            depthX = pos.X;
            depthY = pos.Y;

            int colorX, colorY;

            // Only ImageResolution.Resolution640x480 is supported at this point
            var pos2 = _nui.MapSkeletonPointToColor(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
            colorX = pos2.X;
            colorY = pos2.Y;

            // Map back to skeleton.Width & skeleton.Height
            return new Point((int)(LearnerSkeletonCanvas.Width * colorX / 640.0), (int)(LearnerSkeletonCanvas.Height * colorY / 480));
        }

        /// <summary>
        /// Works out how to draw a line ('bone') for sent Joints
        /// </summary>
        /// <param name="joints">Kinect NUI Joints</param>
        /// <param name="brush">The brush we'll use to colour the joints</param>
        /// <param name="ids">The JointsIDs we're interested in</param>
        /// <returns>A line or lines</returns>
        private Polyline GetBodySegment(JointCollection joints, Brush brush, params JointType[] ids)
        {
            var points = new PointCollection(ids.Length);
            foreach (JointType t in ids)
            {
                points.Add(GetDisplayPosition(joints[t]));
            }

            var polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 5;
            return polyline;
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

            DrawSkeleton(skeletons, LearnerSkeletonCanvas);

            if (_learning == true)
            {
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

                                    switch (i)
                                    {
                                        case 0:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderCenter, JointType.ShoulderLeft));
                                            temp += "Left Shoulder ";
                                            //feedback.Text += "Left Shoulder ";
                                            
                                            break;
                                        case 1:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderLeft, JointType.ElbowLeft));
                                            temp += "Left Elbow ";
                                            break;
                                        case 2:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.WristLeft, JointType.ElbowLeft));
                                            temp += "Left Wrist ";
                                            break;
                                        case 3:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.WristLeft, JointType.HandLeft));
                                            temp += "Left Hand ";
                                            break;
                                        case 4:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderCenter, JointType.ShoulderRight));
                                            temp += "Right Shoulder ";
                                            break;
                                        case 5:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderRight, JointType.ElbowRight));
                                            temp += "Right Elbow ";
                                            break;
                                        case 6:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ElbowRight, JointType.WristRight));
                                            temp += "Right Wrist ";
                                            break;
                                        case 7:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.WristRight, JointType.HandRight));
                                            temp += "Right Hand ";
                                            break;
                                        case 8:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipCenter, JointType.HipLeft));
                                            temp += "Left Hip";
                                            break;
                                        case 9:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipLeft, JointType.KneeLeft));
                                            temp += "Left Knee ";
                                            break;
                                        case 10:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.KneeLeft, JointType.AnkleLeft));
                                            temp += "Left Ankle ";
                                            break;
                                        case 11:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.AnkleLeft, JointType.FootLeft));
                                            temp += "Left Foot ";
                                            break;
                                        case 12:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipCenter, JointType.HipRight));
                                            temp += "Right Hip";
                                            break;
                                        case 13:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipRight, JointType.KneeRight));
                                            temp += "Right Knee ";
                                            break;
                                        case 14:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.KneeRight, JointType.AnkleRight));
                                            temp += "Right Ankle ";
                                            break;
                                        case 15:
                                            LearnerSkeletonCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.AnkleRight, JointType.FootRight));
                                            temp += "Right Foot ";
                                            break;
                                    }
                                    temp += instructionX + instructionY +"\r\n";
                                }
                            }

                            if (counttime % 180 == 0)
                                feedback.Text += temp;
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

                videoImage.Source = image.ToBitmapSource();
            }

            if (_capturing == true || _learning == true)
            {
                using (var scolorImage = e.OpenColorImageFrame())
                {
                    if (scolorImage == null)
                        return;
                    //_colorrecorder.Record(scolorImage);
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
            _nui = (from i in KinectSensor.KinectSensors
                    where i.Status == KinectStatus.Connected
                    select i).FirstOrDefault();

            if (_nui == null)
                throw new NotSupportedException("No kinectes connected!");

            try
            {
                
                _nui.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                
                _nui.SkeletonStream.Enable();
                _nui.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                _nui.Start();
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }

            _lastTime = DateTime.Now;

            _dtw = new DtwGestureRecognizer( dimension * 3, 0.6, 2, 2, 10);
            _video = new ArrayList();

            // If you want to see the depth image and frames per second then include this
            // I'mma turn this off 'cos my 'puter is proper slow
            //_nui.DepthFrameReady += NuiDepthFrameReady;

            _nui.SkeletonFrameReady += NuiSkeletonFrameReady;
            _nui.SkeletonFrameReady += SkeletonExtractSkeletonFrameReady;

            // If you want to see the RGB stream then include this
            _nui.ColorFrameReady += NuiColorFrameReady;

            Skeleton3DDataExtract.Skeleton3DdataCoordReady += NuiSkeleton3DdataCoordReady;

            // Update the debug window with Sequences information
            dtwTextOutput.Text = _dtw.RetrieveText();

            Debug.WriteLine("Finished Window Loading");
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

            Stream recordStream = File.OpenRead(@path + "skeleton");
            _replay = new KinectReplay(recordStream);
            recordStream.Close();
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            _replay.Start();

            Stream recordColorStream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(recordColorStream);
            recordColorStream.Close();
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            _colorreplay.Start();
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
            }
            else
            {
                path = ".\\Records\\" + gestureList.Text + "\\";
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            _MasterMovesSaveFileLocation = path;
            _recordstream = File.Create(@path + "skeleton");
            _recordcolorstream = File.Create(@path + "colorStream");
            _recorder = new KinectRecorder(KinectRecordOptions.Skeletons, _recordstream);
            _colorrecorder = new KinectRecorder(KinectRecordOptions.Color, _recordcolorstream);
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

            // Set the capturing? flag
            _learning = false;
            _capturing = false;

            
            // Add the current video buffer to the dtw sequences list
            _dtw.AddOrUpdate(_video, gestureList.Text);

            string fileName = "AnglesData.txt";
            System.IO.File.WriteAllText(@_MasterMovesSaveFileLocation + fileName, _dtw.RetrieveText());
            status.Text = "Remembering " + gestureList.Text;

            results.Text = gestureList.Text + " added";
            status.Text = "";
            _recordstream.Close();
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
            string path = ".\\Records\\" + gestureList.Text + "\\";

            Stream recordStream = File.OpenRead(@path+"skeleton");
            _replay = new KinectReplay(recordStream);
            recordStream.Close();
            _replay.SkeletonFrameReady += replay_SkeletonFrameReady;
            _replay.Start();

            Stream recordColorStream = File.OpenRead(@path + "colorStream");
            _colorreplay = new KinectReplay(recordColorStream);
            recordColorStream.Close();
            _colorreplay.ColorImageFrameReady += replay_ColorImageFrameReady;
            _colorreplay.Start();

            dtwStopReplay.IsEnabled = true;
        }

        void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            var image = e.ColorImageFrame;
            if (image == null) return; // sometimes frame image comes null, so skip it.
            //videoImage2.Source = GetImage(, System.Windows.Media.PixelFormats.Rgb24);
        }

        void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons;
            var frame = e.SkeletonFrame;
            if (frame == null) return;
            skeletons = new Skeleton[frame.ArrayLength]; 
            skeletons = frame.Skeletons;
            Point[] temppt = new Point[dimension];

            DrawSkeleton(skeletons, MasterSkeletonCanvas);

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

                        MotionDetection.Detect(_LearnerAngle, _MasterAngle, dimension, 100, detection);
                    }
                }
            }
        }

        private void DtwStopReplayClick(object sender, RoutedEventArgs e)
        {
            dtwCapture.IsEnabled = true;
            dtwStopReplay.IsEnabled = false;
            dtwStartRegcon.IsEnabled = true;
            _replay.Stop();
            _colorreplay.Stop();

            MasterSkeletonCanvas.Children.Clear();
        }

        private void DtwStartRecogn(object sender, RoutedEventArgs e)
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
            dtwCapture.IsEnabled = true;
            dtwStopReplay.IsEnabled = false;
            dtwStartRegcon.IsEnabled = true;
            dtwStopRegcon.IsEnabled = false;
            _learning = false;
            _capturing = false;
            _replay.Stop();
            _colorreplay.Stop();
            _recordstream.Close();
            _recordcolorstream.Close();

            MasterSkeletonCanvas.Children.Clear();
            LearnerSkeletonCanvas.Children.Clear();
        }

        private void DrawSkeleton(Skeleton[] skeletons, System.Windows.Controls.Canvas skeletonCanvas) 
        {
            int iSkeleton = 0;
            var brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

            skeletonCanvas.Children.Clear();
            foreach (var data in skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // Draw bones
                    //REMARK. change bone color here
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head));
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft));
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    skeletonCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));
                    

                    // Draw joints
                    foreach (Joint joint in data.Joints)
                    {
                        Point jointPos = GetDisplayPosition(joint);
                        var jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = _jointColors[joint.JointType];
                        jointLine.StrokeThickness = 6;
                        skeletonCanvas.Children.Add(jointLine);
                    }
                }
                iSkeleton++;
            } // for each skeleton

        }

        

       

        private void PlayBack(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Opens the sent text file and creates a _dtw recorded gesture sequence
        /// Currently not very flexible and totally intolerant of errors.
        /// </summary>
        /// <param name="fileLocation">Full path to the gesture file</param>
        /*
        public void LoadGesturesFromFile(string fileLocation, int dimension, DtwGestureRecognizer _dtw)
        {
            int itemCount = 0;
            string line;
            string gestureName = String.Empty;

            // TODO I'm defaulting this to 12 here for now as it meets my current need but I need to cater for variable lengths in the future
            ArrayList frames = new ArrayList();
            double[] items = new double[dimension * 3];

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(fileLocation);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("@"))
                {
                    gestureName = line;
                    continue;
                }

                if (line.StartsWith("~"))
                {
                    frames.Add(items);
                    itemCount = 0;
                    items = new double[dimension * 3];
                    continue;
                }

                if (!line.StartsWith("----"))
                {
                    items[itemCount] = Double.Parse(line);
                }

                itemCount++;

                if (line.StartsWith("----"))
                {
                    _dtw.AddOrUpdate(frames, gestureName);
                    frames = new ArrayList();
                    gestureName = String.Empty;
                    itemCount = 0;
                }
            }

            file.Close();
        }
        */
        /// <summary>
        /// Stores our gesture to the DTW sequences list
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        /*
        private void DtwSaveToFile(object sender, RoutedEventArgs e)
        {
            string fileName = GestureSaveFileNamePrefix + ".txt";
            System.IO.File.WriteAllText(MasterMovesSaveFileLocation + fileName, _dtw.RetrieveText());
            status.Text = "Saved to " + fileName;
        }
        */
        /// <summary>
        /// Loads the user's selected gesture file
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        /*
        private void DtwLoadFile(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";

            dlg.InitialDirectory = MasterMovesSaveFileLocation;

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                LoadGesturesFromFile(dlg.FileName);
                dtwTextOutput.Text = _dtw.RetrieveText();
                status.Text = "Gestures loaded!";
            } 
        }
         * */

        /// <summary>
        /// Stores our gesture to the DTW sequences list
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
    }
}
