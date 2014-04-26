using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace TaiChiLearning.DTW
{
    class MotionDetection
    {
        public static double [] Detect(Point[] a1, Point[] a2, int dimension, double threshold, int [] _detection)
        {
            /*
            DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += new EventHandler (delegate(object s,EventArgs e){
            Console.WriteLine("a1"+a1[1]);            
            });
            dispatcherTimer.Start();
             * */
            //int[] Detect = new int[a1.Length];
            double[] angles = new double[dimension];
            for (int i = 0; i < 8; i++)
            {
                //a.x = xy-plane  a.y = yz-plane
                if ((Math.Abs(a1[i].X - a2[i].X) > threshold && Math.Abs(a1[i].X - a2[i].X) < (360 - threshold)))
                {
                    angles[i] = Math.Abs(a1[i].X - a2[i].X);
                    _detection[i] = 1;
                    if (Math.Abs(a1[i].Y - a2[i].Y) > threshold && Math.Abs(a1[i].Y - a2[i].Y) < (360 - threshold))
                    {
                        angles[i] += Math.Abs(a1[i].Y - a2[i].Y);
                        _detection[i] = 1;
                    }
                }
                else
                {
                    _detection[i] = 0;
                    angles[i] = 0;
                }
            }

            double ratio = 0.6;
            for (int i = 8; i < dimension; i++)
            {
                //a.x = xy-plane  a.y = yz-plane
                if ((Math.Abs(a1[i].X - a2[i].X) * ratio > threshold && Math.Abs(a1[i].X - a2[i].X) * ratio < (360 - threshold)))
                {
                    angles[i] = Math.Abs(a1[i].X - a2[i].X) * ratio;
                    _detection[i] = 1;
                    if (Math.Abs(a1[i].Y - a2[i].Y) * ratio > threshold && Math.Abs(a1[i].Y - a2[i].Y) * ratio < (360 - threshold))
                    {
                        angles[i] += Math.Abs(a1[i].Y - a2[i].Y) * ratio;
                        _detection[i] = 1;
                    }
                }
                else
                {
                    _detection[i] = 0;
                    angles[i] = 0;
                }
            }

            /// 
            return angles;
            // return Detect;
        }

        /// <summary>
        /// To generate the movement instrcution by interger.
        /// The hundreds place and a.X represent ProjectToXZ;
        /// 200=down ;300=up; 400=forward; 500=backward.
        /// The tens place and a.Y represent ProjectToZY.
        /// 20=right  40=left
        /// (_LearnerAngle, _MasterAngle)
        /// </summary>

        /// <summary>
        /// To generate the movement instrcution by interger.
        /// The hundreds place and a.X represent ProjectToXZ;
        /// 200=down ;300=up; 400=forward; 500=backward.
        /// (_LearnerAngle, _MasterAngle)
        /// </summary>
        private static int textInstructionX(Point a1, Point a2)
        {
            if (a1.X >= 0 && a1.X < 90)
            {
                if (a2.X >= 0 && a2.X <= 90)
                    if (a1.X - a2.X > 0)
                        return 200;
                    else
                        return 300;
                if (a2.X >= 90 && a2.X <= 270)
                    return 400;
                else
                    return 300;
            }
            else if (a1.X >= 90 && a1.X < 180)
            {
                if (a2.X >= 90 && a2.X <= 180)
                    if (a1.X - a2.X > 0)
                        return 300;
                    else
                        return 200;
                if (a2.X >= 180 && a2.X <= 270)
                    return 300;
                else
                    return 500;
            }
            else if (a1.X >= 180 && a1.X < 270)
            {
                if (a2.X >= 180 && a2.X <= 270)
                    if (a1.X - a2.X > 0)
                        return 300;
                    else
                        return 200;
                if (a2.X >= 90 && a2.X <= 180)
                    return 200;
                else
                    return 500;
            }
            else
            {
                if (a2.X >= 270 && a2.X <= 360)
                    if (a1.X - a2.X > 0)
                        return 200;
                    else
                        return 300;
                if (a2.X >= 0 && a2.X <= 90)
                    return 200;
                else
                    return 400;
            }
        }


        /// <summary>
        /// To generate the movement instrcution by interger.
        /// The tens place and a.Y represent ProjectToZY.  
        /// 20 = right 40 = left
        /// (_LearnerAngle, _MasterAngle)
        /// </summary>
        private static int textInstructionYL(Point a1, Point a2)
        {
            if (a1.X >= 0 && a1.X < 90)
            {
                if (a2.X >= 0 && a2.X <= 90)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                else
                    return 20;
            }
            else if (a1.X >= 90 && a1.X < 180)
            {
                if (a2.X >= 90 && a2.X <= 180)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                if (a2.X >= 0 && a2.X <= 90)
                    return 40;
                else
                    return 20;
            }
            else if (a1.X >= 180 && a1.X < 270)
            {
                if (a2.X >= 180 && a2.X <= 270)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                if (a2.X >= 0 && a2.X <= 180)
                    return 40;
                else
                    return 20;
            }
            else
            {
                if (a2.X >= 270 && a2.X <= 360)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                else
                    return 40;
            }
        }

        /// <summary>
        /// To generate the movement instrcution by interger.
        /// The tens place and a.Y represent ProjectToZY.  
        /// 20 = right 40 = left
        /// (_LearnerAngle, _MasterAngle)
        /// </summary>
        private static int textInstructionYR(Point a1, Point a2)
        {
            if (a1.X >= 0 && a1.X < 90)
            {
                if (a2.X >= 0 && a2.X <= 90)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                if (a2.X >= 90 && a2.X <= 180)
                    return 20;
                else
                    return 40;
            }
            else if (a1.X >= 90 && a1.X < 180)
            {
                if (a2.X >= 90 && a2.X <= 180)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                else
                    return 40;
            }
            else if (a1.X >= 180 && a1.X < 270)
            {
                if (a2.X >= 180 && a2.X <= 270)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                else
                    return 20;
            }
            else
            {
                if (a2.X >= 270 && a2.X <= 360)
                    if (a1.X - a2.X > 0)
                        return 20;
                    else
                        return 40;
                if (a2.X >= 0 && a2.X <= 180)
                    return 20;
                else
                    return 40;
            }
        }
    }
}
