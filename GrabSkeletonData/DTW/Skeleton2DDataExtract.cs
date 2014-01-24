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

    /// <summary>
    /// This class is used to transform the data of the skeleton
    /// </summary>
    internal class Skeleton2DDataExtract
    {
        /// <summary>
        /// Skeleton2DdataCoordEventHandler delegate
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 2Ddata Coord Event Args</param>
        public delegate void Skeleton2DdataCoordEventHandler(object sender, Skeleton2DdataCoordEventArgs a);

        /// <summary>
        /// The Skeleton 2Ddata Coord Ready event
        /// </summary>
        public static event Skeleton2DdataCoordEventHandler Skeleton2DdataCoordReady;

        /// <summary>
        /// Crunches Kinect SDK's Skeleton Data and spits out a format more useful for DTW
        /// </summary>
        /// <param name="data">Kinect SDK's Skeleton Data</param>
        public static void ProcessData(Skeleton data)
        {
            // Extract the coordinates of the points.
            var p = new Point[12]; //updated
            Point shoulderRight = new Point(), shoulderLeft = new Point(), lowerCenter = new Point(), HipLeft = new Point(), HipRight = new Point();

            foreach (Joint j in data.Joints)
            {
                switch (j.JointType)
                {
                    case JointType.HandLeft:
                        p[0] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.WristLeft:
                        p[1] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.ElbowLeft:
                        p[2] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.ElbowRight:
                        p[3] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.WristRight:
                        p[4] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.HandRight:
                        p[5] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.ShoulderLeft:
                        shoulderLeft = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.ShoulderRight:
                        shoulderRight = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.HipLeft:
                        HipLeft = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.KneeLeft:
                        p[6] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.AnkleLeft:
                        p[7] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.FootLeft:
                        p[8] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.HipRight:
                        HipRight = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.KneeRight:
                        p[9] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.AnkleRight:
                        p[10] = new Point(j.Position.X, j.Position.Y);
                        break;
                    case JointType.FootRight:
                        p[11] = new Point(j.Position.X, j.Position.Y);
                        break;
                }
            }

            // Centre the data of upper body
            var center = new Point((shoulderLeft.X + shoulderRight.X) / 2, (shoulderLeft.Y + shoulderRight.Y) / 2);
            for (int i = 0; i < 6; i++)
            {
                p[i].X -= center.X;
                p[i].Y -= center.Y;
            }

            //Centre the data of lower body
            var lowercenter = new Point((HipRight.X + HipLeft.X) / 2, (HipRight.Y + HipLeft.Y) / 2);
            for (int i = 6; i < 12; i++)
            {
                p[i].X -= lowerCenter.X;
                p[i].Y -= lowerCenter.Y;
            }

            // Normalization of the coordinates
            double shoulderDist =
                Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                          Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2));
            double legDist = Math.Sqrt(Math.Pow((p[10].X - p[11].X), 2) + Math.Pow((p[10].Y - p[11].Y), 2));
            double normalizeIndex = shoulderDist + legDist; // not good 
            for (int i = 0; i < 6; i++)
            {
                p[i].X /= normalizeIndex;
                p[i].Y /= normalizeIndex;
            }
            for (int i = 6; i < 14; i++)
            {
                p[i].X /= normalizeIndex;
                p[i].Y /= normalizeIndex;
            }
            
            /// Question : should we seperate upper body and lower body in order to increase pricision ?
            // Launch the event!
            Skeleton2DdataCoordReady(null, new Skeleton2DdataCoordEventArgs(p));
        }
    }
}