using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GrabSkeletonData
{
    public class SkeletonDrawManager
    {
        readonly Canvas rootCanvas;
        readonly KinectSensor nui;
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

        public SkeletonDrawManager(Canvas root, KinectSensor sensor)
        {
            rootCanvas = root;
            nui = sensor;
        }

        /// <summary>
        /// Gets the display position (i.e. where in the display image) of a Joint
        /// </summary>
        /// <param name="joint">Kinect NUI Joint</param>
        /// <returns>Point mapped location of sent joint</returns>
        private Point GetDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            var pos = nui.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution320x240Fps30);

            depthX = pos.X;
            depthY = pos.Y;

            int colorX, colorY;

            // Only ImageResolution.Resolution640x480 is supported at this point
            var pos2 = nui.MapSkeletonPointToColor(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
            colorX = pos2.X;
            colorY = pos2.Y;

            // Map back to skeleton.Width & skeleton.Height
            return new Point((int)(rootCanvas.Width * colorX / 640.0), (int)(rootCanvas.Height * colorY / 480));
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
        /// Draw skelton on the canvas
        /// </summary>
        /// <param name="skeletons"></param>
        public void DrawSkeleton(Skeleton[] skeletons)
        {
            int iSkeleton = 0;
            var brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

            rootCanvas.Children.Clear();
            foreach (var data in skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // Draw bones
                    //REMARK. change bone color here
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[1], JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));


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
                        rootCanvas.Children.Add(jointLine);
                    }
                }
                iSkeleton++;
            } // for each skeleton

        }

        /// <summary>
        /// When there is error, this will change the color of the skeleton stroke
        /// </summary>
        /// <param name="data"></param>
        /// <param name="indicator"></param>
        /// <param name="i"></param>
        public void DrawCorrection(Skeleton data,int indicator, int i)
        {
            var brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            /*
            int XZ = indicator / 100;
            string instructionX = "";

            switch (XZ)
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
            int YZ = indicator % 100;
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
            */
            switch (i)
            {
                case 0:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderCenter, JointType.ShoulderLeft));
                    DrawFeedbackArrow(data.Joints[JointType.ShoulderLeft], indicator, brush);
                    break;
                case 1:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderLeft, JointType.ElbowLeft));
                    DrawFeedbackArrow(data.Joints[JointType.ElbowLeft], indicator, brush);
                    break;
                case 2:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.WristLeft, JointType.ElbowLeft));
                    DrawFeedbackArrow(data.Joints[JointType.WristLeft], indicator, brush);
                    break;
                case 3:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.WristLeft, JointType.HandLeft));
                    DrawFeedbackArrow(data.Joints[JointType.HandLeft], indicator, brush);
                    break;
                case 4:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderCenter, JointType.ShoulderRight));
                    DrawFeedbackArrow(data.Joints[JointType.ShoulderRight], indicator, brush);
                    break;
                case 5:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ShoulderRight, JointType.ElbowRight));
                    DrawFeedbackArrow(data.Joints[JointType.ElbowRight], indicator, brush);
                    break;
                case 6:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.ElbowRight, JointType.WristRight));
                    DrawFeedbackArrow(data.Joints[JointType.WristRight], indicator, brush);
                    break;
                case 7:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.WristRight, JointType.HandRight));
                    DrawFeedbackArrow(data.Joints[JointType.HandRight], indicator, brush);
                    break;
                case 8:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipCenter, JointType.HipLeft));
                    DrawFeedbackArrow(data.Joints[JointType.HipLeft], indicator, brush);
                    break;
                case 9:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipLeft, JointType.KneeLeft));
                    DrawFeedbackArrow(data.Joints[JointType.KneeLeft], indicator, brush);
                    break;
                case 10:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.KneeLeft, JointType.AnkleLeft));
                    DrawFeedbackArrow(data.Joints[JointType.AnkleLeft], indicator, brush);
                    break;
                case 11:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.AnkleLeft, JointType.FootLeft));
                    DrawFeedbackArrow(data.Joints[JointType.FootLeft], indicator, brush);
                    break;
                case 12:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipCenter, JointType.HipRight));
                    DrawFeedbackArrow(data.Joints[JointType.HipRight], indicator, brush);
                    break;
                case 13:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipRight, JointType.KneeRight));
                    DrawFeedbackArrow(data.Joints[JointType.KneeRight], indicator, brush);
                    break;
                case 14:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.KneeRight, JointType.AnkleRight));
                    DrawFeedbackArrow(data.Joints[JointType.AnkleRight], indicator, brush);
                    break;
                case 15:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.AnkleRight, JointType.FootRight));
                    DrawFeedbackArrow(data.Joints[JointType.FootRight], indicator, brush);
                    break;
            }
 
        }

        /// <summary>
        /// Draw arrow to indicate the correction direction
        /// </summary>
        /// <param name="joint"></param>
        /// <param name="indicator"></param>
        private void DrawFeedbackArrow(Joint joint, int indicator, SolidColorBrush brush) 
        {
            Point StartPoint = GetDisplayPosition(joint);
            Point EndPoint = new Point();
            EndPoint.X = StartPoint.X;
            EndPoint.Y = StartPoint.Y + 20;
            DrawArrow(StartPoint, EndPoint, brush);
        }

        private void DrawArrow(Point StartPoint, Point EndPoint, SolidColorBrush brush)
        {
            System.Windows.Shapes.Line _Line = new System.Windows.Shapes.Line();
            System.Windows.Shapes.Line Head = new System.Windows.Shapes.Line();

            _Line.X1 = StartPoint.X;
            _Line.Y1 = StartPoint.Y;
            _Line.X2 = EndPoint.X;
            _Line.Y2 = EndPoint.Y;
            _Line.StrokeThickness = 3;
            _Line.Stroke = brush;
            /*
            int d;
            if (EndPoint.X > StartPoint.X)
            {
                Head.X1 = EndPoint.X - 1;
                Head.X2 = EndPoint.X;
                d = 1;
            }
            else
            {
                Head.X1 = EndPoint.X + 1;
                Head.X2 = EndPoint.X;
                d = -1;
            }

            Head.Y1 = getArrowYByX(d, StartPoint, EndPoint);
            Head.Y2 = EndPoint.Y;

            Head.StrokeEndLineCap = PenLineCap.Triangle;
            Head.StrokeThickness = 8;
            Head.Stroke = brush;
             * 
            rootCanvas.Children.Add(Head);
             * */
            rootCanvas.Children.Add(_Line);
            
        }

        double getArrowYByX(double d, Point pStart, Point pEnd)
        {
            return pStart.Y + (pEnd.X - pStart.X - d) * (pEnd.Y - pStart.Y) / (pEnd.X - pStart.X);
        }
    }
}
