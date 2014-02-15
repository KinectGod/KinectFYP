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
        /// <summary>
        /// Skeleton2DdataCoordEventHandler delegate
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 2Ddata Coord Event Args</param>
        public delegate void Skeleton3DdataCoordEventHandler(object sender, Skeleton3DdataCoordEventArgs a);

        /// <summary>
        /// The Skeleton 2Ddata Coord Ready event
        /// </summary>
        public static event Skeleton3DdataCoordEventHandler Skeleton3DdataCoordReady;

        /// <summary>
        /// Crunches Kinect SDK's Skeleton Data and spits out a format more useful for DTW
        /// </summary>
        /// <param name="data">Kinect SDK's Skeleton Data</param>
        public static void ProcessData(Skeleton data)
        {
            // Extract the coordinates of the points.
            var p = new Vector3D[8];
            var a = new Point[7];

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
                    case JointType.ElbowRight:
                        p[3] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristRight:
                        p[4] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HandRight:
                        p[5] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderLeft:
                        p[6] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderRight:
                        p[7] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                }
            }

            Vector3D joint0to1;
            // used to store the vector project to the planes
            Vector3D ProjectToXY = new Vector3D();
            Vector3D ProjectToZY = new Vector3D();

            for (int i = 0; i < 7; i++)
            {
                // calculate vector joining two points
                joint0to1 = Vector3D.Subtract(p[i], p[i + 1]);
                ProjectToXY = new Vector3D(Math.Abs(joint0to1.X), Math.Abs(joint0to1.Y), 0);
                ProjectToZY = new Vector3D(0, Math.Abs(joint0to1.Y), Math.Abs(joint0to1.Z));

                // calculate angle between the vector and the plane
                a[i].X = Vector3D.AngleBetween(joint0to1, ProjectToXY);
                a[i].Y = Vector3D.AngleBetween(joint0to1, ProjectToZY);
            }

            // Launch the event!
            Skeleton3DdataCoordReady(null, new Skeleton3DdataCoordEventArgs(p));
        }

        /// <summary>
        /// Crunches Kinect SDK's Skeleton Data and spits out a format more useful for DTW
        /// </summary>
        /// <param name="data">Kinect SDK's Skeleton Data</param>
        public static Point[] OutputData(Skeleton data)
        {
            // Extract the coordinates of the points.
            var p = new Vector3D[8];
            var a = new Point[7];

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
                    case JointType.ElbowRight:
                        p[3] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristRight:
                        p[4] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HandRight:
                        p[5] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderLeft:
                        p[6] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderRight:
                        p[7] = new Vector3D(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                }
            }

            Vector3D joint0to1;
            // used to store the vector project to the planes
            Vector3D ProjectToXY = new Vector3D();
            Vector3D ProjectToZY = new Vector3D();

            for (int i = 0; i < 7; i++)
            {
                // calculate vector joining two points
                joint0to1 = Vector3D.Subtract(p[i], p[i + 1]);
                ProjectToXY = new Vector3D(Math.Abs(joint0to1.X), Math.Abs(joint0to1.Y), 0);
                ProjectToZY = new Vector3D(0, Math.Abs(joint0to1.Y), Math.Abs(joint0to1.Z));

                // calculate angle between the vector and the plane
                a[i].X = Vector3D.AngleBetween(joint0to1, ProjectToXY);
                a[i].Y = Vector3D.AngleBetween(joint0to1, ProjectToZY);
            }

            return a;
        }
    }
}