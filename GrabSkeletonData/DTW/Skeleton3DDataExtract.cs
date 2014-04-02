//-----------------------------------------------------------------------
// <copyright file="Skeleton2DDataExtract.cs" company="Rhemyst and Rymix">
//     Open Source. Do with this as you will. Include this statement or 
//     don't - whatever you like.
//
//     No warranty or support given. No guarantees this will work or meet
//     your needs. Some elements of this project have been tailored to
//     the authors' needs and therefore don't necessarily follow best
//     practice. Subsequent releases of this project will (probably) not
//     be compatible with different versions, so whatever you do, don't
//     overwrite your implementation with any new releases of this
//     project!
//
//     Enjoy working with Kinect!
// </copyright>
//-----------------------------------------------------------------------

namespace GrabSkeletonData.DTW
{
    using System;
    using System.Windows;
    using Microsoft.Kinect;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// This class is used to transform the data of the skeleton
    /// </summary>
    internal class Skeleton3DDataExtract
    {
        /*
        /// <summary>
        /// Skeleton2DdataCoordEventHandler delegate
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 2Ddata Coord Event Args</param>
        public delegate void Skeleton3DdataCoordEventHandler(object sender, Skeleton3DdataCoordEventArgs a);
        

        /// <summary>
        /// The Skeleton 2Ddata Coord Ready event
        /// </summary>
        public static event Skeleton3DdataCoordEventHandler Skeleton3DdataCoordReady; * */

        public static Point[] OutputData;
        /// <summary>
        /// Crunches Kinect SDK's Skeleton Data and spits out a format more useful for DTW
        /// </summary>
        /// <param name="data">Kinect SDK's Skeleton Data</param>
        public static Point[] ProcessData(Skeleton data)
        {
            // Extract the coordinates of the points.
            var p = new Vector3D[18];
            // Record the angles of the joints,  a.x = xy-plane  a.y = yz-plane
            var a = new Point[17];

            foreach (Joint j in data.Joints)
            {
                switch (j.JointType)
                {
                    case JointType.HandLeft:
                        p[0] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristLeft:
                        p[1] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowLeft:
                        p[2] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderLeft:
                        p[3] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderCenter:
                        p[4] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderRight:
                        p[5] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowRight:
                        p[6] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristRight:
                        p[7] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HandRight:
                        p[8] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.FootLeft:
                        p[9] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.AnkleLeft:
                        p[10] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.KneeLeft:
                        p[11] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HipLeft:
                        p[12] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HipCenter:
                        p[13] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HipRight:
                        p[14] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.KneeRight:
                        p[15] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.AnkleRight:
                        p[16] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.FootRight:
                        p[17] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                }
            }

            Vector3D joint0to1 = new Vector3D();
            
            //left hand
            for (int i = 4; i > 0; i--)
            {
                // calculate vector joining two points
                joint0to1 = Vector3D.Subtract(p[i - 1], p[i]);
                a[4 - i] = AngleDetection(joint0to1);
            }

            for (int i = 13; i > 9; i--)
            {
                // calculate vector joining two points
                joint0to1 = Vector3D.Subtract(p[i - 1], p[i]);
                // calculate angle between the vector and the plane
                a[21 - i] = AngleDetection(joint0to1);
            }

            //right hand
            for (int i = 4; i < 8; i++)
            {
                // calculate vector joining two points
                joint0to1 = Vector3D.Subtract(p[i + 1], p[i]);
                // calculate angle between the vector and the plane
                a[i] = AngleDetection(joint0to1);
            } 
            for (int i = 13; i < 17; i++)
            {
                // calculate vector joining two points
                joint0to1 = Vector3D.Subtract(p[i + 1], p[i]);
                // calculate angle between the vector and the plane
                a[i - 1] = AngleDetection(joint0to1);
            }

            return a;
        }

       
        private static Point AngleDetection(Vector3D angle)
        {
            Point temp = new Point();
            Vector3D ProjectToXZ = new Vector3D(angle.X, 0, angle.Z);
            Vector3D ProjectToZY = new Vector3D(0, angle.Y, angle.Z);
            double tempX = Vector3D.AngleBetween(angle, ProjectToXZ);
            double tempY = Vector3D.AngleBetween(angle, ProjectToZY);

            if (angle.X >= 0 && angle.Y >= 0)
            {
                temp.X = tempX;
            }
            else if (angle.X >= 0 && angle.Y <= 0)
            {
                temp.X = 360 - tempX;
            }
            else if (angle.X <= 0 && angle.Y >= 0)
            {
                temp.X = 180 - tempX;
            }
            else if (angle.X <= 0 && angle.Y <= 0)
            {
                temp.X = 180 + tempX;
            }

            if (angle.Z >= 0 && angle.X >= 0)
            {
                temp.Y = tempY;
            }
            else if (angle.Z >= 0 && angle.X <= 0)
            {
                temp.Y = 360 - tempY;
            }
            else if (angle.Z <= 0 && angle.X >= 0)
            {
                temp.Y = 180 - tempY;
            }
            else if (angle.Z <= 0 && angle.X <= 0)
            {
                temp.Y = 180 + tempY;
            }

            return temp;
        }

       
    }
}