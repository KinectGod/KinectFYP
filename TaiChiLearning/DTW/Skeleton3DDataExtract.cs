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

namespace TaiChiLearning.DTW
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
            var p = new Vector3D[19];
            // Record the angles of the joints,  a.x = xy-plane  a.y = yz-plane
            var a = new Point[19];

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
                    case JointType.Spine:
                        p[18] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
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
                a[i] = AngleDetection(joint0to1);
            }

            a[17] = AngleDetection(Vector3D.Subtract(p[4], p[18]));
            a[18] = AngleDetection(Vector3D.Subtract(p[18], p[13]));

            return a;
        }

       
        private static Point AngleDetection(Vector3D vector)
        {
            Point AnglePair = new Point();
            vector.Normalize();
            Vector3D ProjectToXZ = new Vector3D(vector.X, 0, vector.Z);
            Vector3D ProjectToZY = new Vector3D(0, vector.Y, vector.Z);
            ProjectToXZ.Normalize();
            ProjectToZY.Normalize();
            double angleXZ = Vector3D.AngleBetween(vector, ProjectToXZ);
            double angleYZ = Vector3D.AngleBetween(vector, ProjectToZY);

            if (vector.X >= 0 && vector.Y >= 0)
            {
                AnglePair.X = angleXZ;
            }
            else if (vector.X >= 0 && vector.Y <= 0)
            {
                AnglePair.X = 360 - angleXZ;
            }
            else if (vector.X <= 0 && vector.Y >= 0)
            {
                AnglePair.X = 180 - angleXZ;
            }
            else if (vector.X <= 0 && vector.Y <= 0)
            {
                AnglePair.X = 180 + angleXZ;
            }

            if (vector.Z >= 0 && vector.X >= 0)
            {
                AnglePair.Y = angleYZ;
            }
            else if (vector.Z >= 0 && vector.X <= 0)
            {
                AnglePair.Y = 360 - angleYZ;
            }
            else if (vector.Z <= 0 && vector.X >= 0)
            {
                AnglePair.Y = 180 - angleYZ;
            }
            else if (vector.Z <= 0 && vector.X <= 0)
            {
                AnglePair.Y = 180 + angleYZ;
            }

            return AnglePair;
        }

        public static double[] LengthGeneration(Skeleton data)
        {
            // Extract the coordinates of the points.
            var p = new Vector3D[19];
            // Record the angles of the joints,  a.x = xy-plane  a.y = yz-plane
            var a = new double[19];

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
                    case JointType.Spine:
                        p[18] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                }
            }

            //left hand
            for (int i = 4; i > 0; i--)
            {
                // calculate vector joining two points
                a[4 - i] = Vector3D.Subtract(p[i - 1], p[i]).Length;
            }

            for (int i = 13; i > 9; i--)
            {
                // calculate vector joining two points
                a[21 - i] = Vector3D.Subtract(p[i - 1], p[i]).Length;
            }

            //right hand
            for (int i = 4; i < 8; i++)
            {
                // calculate vector joining two points
                a[i] = Vector3D.Subtract(p[i + 1], p[i]).Length;
            }
            for (int i = 13; i < 17; i++)
            {
                // calculate vector joining two points
                a[i] = Vector3D.Subtract(p[i + 1], p[i]).Length;
            }

            a[12] = Vector3D.Subtract(p[4], p[18]).Length; // length of shoulder center and spine
            a[18] = Vector3D.Subtract(p[18], p[13]).Length;
            return a;
        }
    }
}