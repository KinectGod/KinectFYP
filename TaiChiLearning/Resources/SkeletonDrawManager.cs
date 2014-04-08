using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace TaiChiLearning
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
            /*
            float depthX, depthY;
            var pos = nui.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution320x240Fps30);

            depthX = pos.X;
            depthY = pos.Y;
            */
            int colorX, colorY;

            // Only ImageResolution.Resolution640x480 is supported at this point
            var pos2 = nui.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
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
            brushes[5] = new SolidColorBrush(Color.FromRgb(192, 192, 192));

            rootCanvas.Children.Clear();
            foreach (var data in skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // Draw bones
                    //REMARK. change bone color here
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brushes[5], JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));


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
        public void DrawCorrection(Skeleton data, int indicator, double c_angles, int i)
        {

            var brush_warning = new SolidColorBrush(Color.FromRgb(255,  0, 0)); // red, when detect the realtime error
            var corr_color = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // orange, when draw the correction line
            int XZ = indicator / 100;
            int PorN = 1; //indicate the correction line should be left or right

            switch (XZ)
            {
                case 2:
                    // down
                    break;
                case 3:
                    // up
                    break;
                case 4:
                    // forward
                    corr_color = new SolidColorBrush(Color.FromRgb(128, 0, 128)); // purple line when need forward
                    break;
                case 5:
                    // backward
                    corr_color = new SolidColorBrush(Color.FromRgb(0, 128, 0)); // green line when backward
                    break;
            }
            int YZ = indicator % 100;

            switch (YZ)
            {
                case 20:
                    // right
                    PorN = 1;
                    break;
                case 40:
                    // left
                    PorN = -1;
                    break;
            }

            switch (i)
            {
                //left hand
                case 0:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.ShoulderCenter, JointType.ShoulderLeft));
                    //DrawCorrection(data.Joints[JointType.ShoulderCenter], data.Joints[JointType.ShoulderLeft], PorN, c_angles, corr_color);
                    break;
                case 1:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.ShoulderLeft, JointType.ElbowLeft));
                    //DrawCorrection(data.Joints[JointType.ShoulderLeft], data.Joints[JointType.ElbowLeft], PorN, c_angles, corr_color);
                    break;
                case 2:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.WristLeft, JointType.ElbowLeft));
                    //DrawCorrection(data.Joints[JointType.ElbowLeft], data.Joints[JointType.WristLeft], PorN, c_angles, corr_color);
                    break;
                case 3:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.WristLeft, JointType.HandLeft));
                    //DrawCorrection(data.Joints[JointType.WristLeft], data.Joints[JointType.HandLeft], PorN, c_angles, corr_color);
                    break;

                // right hand
                case 4:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.ShoulderCenter, JointType.ShoulderRight));
                    //DrawCorrection(data.Joints[JointType.ShoulderCenter], data.Joints[JointType.ShoulderRight], PorN, c_angles, corr_color);
                    break;
                case 5:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.ShoulderRight, JointType.ElbowRight));
                    //DrawCorrection(data.Joints[JointType.ShoulderRight], data.Joints[JointType.ElbowRight], PorN, c_angles, corr_color);
                    break;
                case 6:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.ElbowRight, JointType.WristRight));
                    //DrawCorrection(data.Joints[JointType.ElbowRight], data.Joints[JointType.WristRight], PorN, c_angles, corr_color);
                    break;
                case 7:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.WristRight, JointType.HandRight));
                    //DrawCorrection(data.Joints[JointType.WristRight], data.Joints[JointType.HandRight], PorN, c_angles, corr_color);
                    break;

                // left leg
                case 8:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.HipCenter, JointType.HipLeft));
                    //DrawCorrection(data.Joints[JointType.HipCenter], data.Joints[JointType.HipLeft], PorN, c_angles, corr_color);
                    break;
                case 9:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.HipLeft, JointType.KneeLeft));
                    //DrawCorrection(data.Joints[JointType.HipLeft], data.Joints[JointType.KneeLeft], PorN, c_angles, corr_color);
                    break;
                case 10:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.KneeLeft, JointType.AnkleLeft));
                    //DrawCorrection(data.Joints[JointType.KneeLeft], data.Joints[JointType.AnkleLeft], PorN, c_angles, corr_color);
                    break;
                case 11:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.AnkleLeft, JointType.FootLeft));
                    //DrawCorrection(data.Joints[JointType.AnkleLeft], data.Joints[JointType.FootLeft], PorN, c_angles, corr_color);
                    break;

                // right leg
                case 13:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.HipCenter, JointType.HipRight));
                    //DrawCorrection(data.Joints[JointType.HipCenter], data.Joints[JointType.HipRight], PorN, c_angles, corr_color);
                    break;
                case 14:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.HipRight, JointType.KneeRight));
                    //DrawCorrection(data.Joints[JointType.HipRight], data.Joints[JointType.KneeRight], PorN, c_angles, corr_color);
                    break;
                case 15:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.KneeRight, JointType.AnkleRight));
                    //DrawCorrection(data.Joints[JointType.KneeRight], data.Joints[JointType.AnkleRight], PorN, c_angles, corr_color);
                    break;
                case 16:
                    rootCanvas.Children.Add(GetBodySegment(data.Joints, brush_warning, JointType.AnkleRight, JointType.FootRight));
                    //DrawCorrection(data.Joints[JointType.AnkleRight], data.Joints[JointType.FootRight], PorN, c_angles, corr_color);
                    break;
                default :
                    break;
            }

        }

        /*
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
        */

        private void DrawCorrection(Joint joint1, Joint joint2, int PorN, double angle, SolidColorBrush brush)
        {
            Point StartPoint = GetDisplayPosition(joint1);
            Point endpt = GetDisplayPosition(joint2);
            if (StartPoint.X == endpt.X && StartPoint.Y == endpt.Y) return;
            Point EndPoint = getCorrectCoord(StartPoint, endpt, angle, PorN);
            if (EndPoint.X <= rootCanvas.Width && EndPoint.Y <= rootCanvas.Height) DrawLine(StartPoint, EndPoint, brush);
        }


        private void DrawLine(Point StartPoint, Point EndPoint, SolidColorBrush brush)
        {
            System.Windows.Shapes.Line _Line = new System.Windows.Shapes.Line();

            _Line.X1 = StartPoint.X;
            _Line.Y1 = StartPoint.Y;
            _Line.X2 = EndPoint.X;
            _Line.Y2 = EndPoint.Y;
            _Line.StrokeThickness = 5;
            _Line.Stroke = brush;
            rootCanvas.Children.Add(_Line);
        }

        /// <summary>
        /// Get the correct coordinate (master)
        /// </summary>
        /// <param name="startpt"></param>
        /// <param name="endpt"></param>
        /// <param name="length"></param>
        /// <param name="angle"></param>
        /// <param name="indicator1"></param>
        /// <param name="indicator2"></param>
        /// <returns></returns>
        private Point getCorrectCoord(Point startpt, Point endpt, double angle, int PorN)
        {
            Point EndPoint = new Point(double.MaxValue, double.MaxValue);
            double length = Math.Sqrt(Math.Pow(startpt.X - endpt.X, 2) + Math.Pow(startpt.Y - endpt.Y, 2));
            //the slope of the known line
            double slope1 = (startpt.Y - endpt.Y) / (startpt.X - endpt.X);
            /// use the angle and slope of line 1 to calculate the slope if line 2
            double slope2 = (slope1 / Math.Tan(angle * Math.PI / 180) - 1) * (1 / Math.Tan(angle * Math.PI / 180) - slope1);

            /*
             * Y=mx+c is the equation of the line you have. (x1,y1) is the point and D is the distance. (x,y) is the point you don't know.
             * D= sqrt((x1-x)^2 +(y1-y)^2)
             * sub in for y
             * D= sqrt((x1-x)^2 +(y1-(mx+c))^2)
             * then solve for the only unknown, x. this is your x co-ord (2 values). then y=mx+c gives the y.
             */
            double c = startpt.Y - slope2 * startpt.X;
            double equa_a = 1 + slope2 * slope2;
            double equa_b = 2 * slope2 * ((c - startpt.Y) - 2 * startpt.X);
            double equa_c = startpt.X * startpt.X + Math.Pow(c - startpt.Y, 2) - length * length;
            double x1 = (- equa_b + Math.Sqrt(equa_b * equa_b - 4 * equa_a * equa_c)) / 2 / equa_a;
            double x2 = (- equa_b - Math.Sqrt(equa_b * equa_b - 4 * equa_a * equa_c)) / 2 / equa_a;

            /// when located in left
            if (PorN == -1 && equa_a > 0 || PorN == 1 && equa_a < 0)
            {
                if (x1 <= startpt.X)
                    EndPoint.X = x1;
                else
                    EndPoint.X = x2;
                EndPoint.Y = slope2 * EndPoint.X + c;
            }
            //when located in right
            else if (PorN == -1 && equa_a < 0 || PorN == 1 && equa_a >0)
            {
                if (x1 >= startpt.X)
                    EndPoint.X = x1;
                else
                    EndPoint.X = x2;
                EndPoint.Y = slope2 * EndPoint.X + c;
            }
            
            return EndPoint;
        }

        public void MasterMatchLearner (double[] ml, double[] ll, Skeleton data, Joint ini)
        {
            var brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));

            if (SkeletonTrackingState.Tracked == data.TrackingState)
            {
                /// Draw bones
                // create temporary joint to store the original learner joints
                Joint temp1 = new Joint();
                Joint temp2 = new Joint();
                Joint shouldercentre = new Joint();
                Joint hipcentre = new Joint();

                /// Right leg
                temp1 = data.Joints[JointType.AnkleRight];
                data.Joints[JointType.AnkleRight] = ProcessCoord (data.Joints[JointType.FootRight], data.Joints[JointType.AnkleRight], ini.Position.ToVector3(), ml[16]/ll[16]);
                temp2 = data.Joints[JointType.KneeRight];
                data.Joints[JointType.KneeRight] = ProcessCoord(temp1, temp2, data.Joints[JointType.AnkleRight].Position.ToVector3(), ml[15] / ll[15]);
                temp1 = data.Joints[JointType.HipRight];
                data.Joints[JointType.HipRight] = ProcessCoord(temp2, temp1, data.Joints[JointType.KneeRight].Position.ToVector3(), ml[14] / ll[14]);
                hipcentre = data.Joints[JointType.HipCenter];
                data.Joints[JointType.HipCenter] = ProcessCoord(temp1, hipcentre, data.Joints[JointType.HipRight].Position.ToVector3(), ml[13] / ll[13]);
                rootCanvas.Children.Add(GetBodySegment(data.Joints, brush, JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));

                /// Draw line between hip centre and shoulder centre
                shouldercentre = data.Joints[JointType.ShoulderCenter];
                data.Joints[JointType.ShoulderCenter] = ProcessCoord(hipcentre, shouldercentre, data.Joints[JointType.HipCenter].Position.ToVector3(), ml[17]/ll[17]);

                /// Left leg
                temp1 = data.Joints[JointType.HipLeft];
                data.Joints[JointType.HipLeft] = ProcessCoord(hipcentre, temp1, data.Joints[JointType.HipCenter].Position.ToVector3(), ml[8] / ll[8]);
                temp2 = data.Joints[JointType.KneeLeft];
                data.Joints[JointType.KneeLeft] = ProcessCoord(temp1, temp2, data.Joints[JointType.HipLeft].Position.ToVector3(), ml[9] / ll[9]);
                temp1 = data.Joints[JointType.AnkleLeft];
                data.Joints[JointType.AnkleLeft] = ProcessCoord(temp2, temp1, data.Joints[JointType.KneeLeft].Position.ToVector3(), ml[10] / ll[10]);
                temp2 = data.Joints[JointType.FootLeft];
                data.Joints[JointType.FootLeft] = ProcessCoord(temp1, temp2, data.Joints[JointType.AnkleLeft].Position.ToVector3(), ml[11] / ll[11]);

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
        }

        private Joint ProcessCoord (Joint sjoint, Joint ejoint, Vector3 newspt, double ratio)
        {
            /// calculate the new endpoiny, using the 3d vector calculation
            Vector3 ep = newspt - (float)ratio * (ejoint.Position.ToVector3() - sjoint.Position.ToVector3());
            /// assign the new position to the original endpoint
            SkeletonPoint pos = new SkeletonPoint();

            pos.X = ep.X;
            pos.Y = ep.Y;
            pos.Z = ep.Z;
            ejoint.Position = pos;

            return ejoint;
        }
    }
}
