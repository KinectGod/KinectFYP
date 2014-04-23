using System.Diagnostics;

namespace TaiChiLearning.DTW
{
    using Microsoft.Kinect;
    using System;
    using System.Collections;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using System.Windows.Media.Media3D;
    using TaiChiLearning.Recorder;
    using TaiChiLearning.Replay;

    /// <summary>
    /// Dynamic Time Warping nearest neighbour sequence comparison class.
    /// Called 'Gesture Recognizer' but really it can work with any vectors
    /// </summary>
    public class DtwForTaiChiLearning
    {
        private ArrayList _path;
        /// <summary>
        /// Size of obeservations vectors.
        /// </summary>
        private readonly int _dimension;

        private static KinectRecorder _lrecorder;
        private static Stream _learnerskeletonstream;

        /// <summary>
        /// Initializes a new instance of the DtwGestureRecognizer class
        /// Second DTW constructor
        /// </summary>
        /// <param name="dim">Vector size</param>
        /// <param name="threshold">Maximum distance between the last observations of each sequence</param>
        /// <param name="firstThreshold">Minimum threshold</param>
        /// <param name="ms">Maximum vertical or horizontal steps in a row</param>
        public DtwForTaiChiLearning(int dim)
        {
            _dimension = dim;
            _path = new ArrayList();
        }


        /// <summary>
        /// Compute the min DTW distance between seq2 and all possible endings of seq1.
        /// </summary>
        /// <param name="seq1">The master array of sequences to compare</param>
        /// <param name="seq2">The learner array of sequences to compare</param>
        /// <returns>The best match</returns>
        public double DtwComputation(ArrayList seq1, ArrayList seq2, ArrayList learnerframe, string path, double anglethreshold)
        {
            Console.WriteLine(learnerframe.Count);
            // Init
            var seq1R = new ArrayList(seq1);
            //seq1R.Reverse();
            var seq2R = new ArrayList(seq2);

            var learnerf = new ArrayList();
            //seq2R.Reverse();

            var tab = new double[seq1R.Count, seq2R.Count];

            for (int i = 0; i < seq1R.Count; i++)
            {
                for (int j = 0; j < seq2R.Count; j++)
                {
                    tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold);
                }
            }
            tab[0, 0] = 0;

            if (_learnerskeletonstream != null)
                _learnerskeletonstream.Close();
            while (FileDelete(@path + "LearnerSelected")) ;
            _learnerskeletonstream = File.Create(@path + "LearnerSelected");
            _lrecorder = new KinectRecorder(KinectRecordOptions.Skeletons, _learnerskeletonstream);

            // Dynamic computation of the DTW matrix.
            for (int i = 1; i < seq1R.Count; i++)
            {
                for (int j = 1; j < seq2R.Count; j++)
                {
                    switch (min(tab[i, j - 1], tab[i - 1, j - 1], tab[i - 1, j]))
                    {
                        case 1:
                            tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i, j - 1];
                            break;
                        case 2:
                            tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i - 1, j - 1];
                            break;
                        case 3:
                            tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i - 1, j];
                            break;

                    }
                }
            }

            int totalframe = 0;
            int correctfream = 0;
            bool chosen = false;
            //Reconstruct the best matched path  //int currentI = bestMatchI;
            //int currentJ = bestMatchI;
            int currentI = seq1R.Count - 1;
            int currentJ = seq2R.Count - 1;
            while (currentI != 0 && currentJ != 0)
            {
                //Console.WriteLine(target.I + " " + target.J);
                switch (min(tab[currentI, currentJ - 1], tab[currentI - 1, currentJ - 1], tab[currentI - 1, currentJ]))
                {
                    case 1:
                        //if(currentJ != 0)
                        currentJ--;
                        Console.Write("3");
                        chosen = false;
                        break;
                    case 2:
                        //if(currentI !=0 )
                        currentI--;
                        currentJ--;
                        learnerf.Add((SkeletonFrame)learnerframe[currentJ]);
                        correctfream++;
                        chosen = false;
                        Console.Write("1");
                        break;
                    case 3:
                        //if(currentI!=0)
                        currentI--;
                        learnerf.Add((SkeletonFrame)learnerframe[currentJ]);
                        if(!chosen)
                        correctfream++;
                        chosen = true;
                        Console.Write("2");
                        break;
                }
                totalframe++;
            }
            //DtwRecordSelectedFrames(path);
            while (learnerf.Count != 0)
            {
                _lrecorder.Record((SkeletonFrame)learnerf[learnerf.Count - 1]);
                learnerf.RemoveAt(learnerf.Count - 1);
            }
            _learnerskeletonstream.Close();
            _lrecorder.Stop();
            Console.WriteLine("mark " + (Double)correctfream / (Double)totalframe);
            return (1 - (tab[seq1R.Count - 1, seq2R.Count - 1] / totalframe));
        }


        private int min(double a, double b, double c)
        {
            if (Math.Min(Math.Min(a, b), c) == a)
                return 1;
            else if (Math.Min(Math.Min(a, b), c) == b)
                return 2;
            else 
                return 3;
        }

        /// <summary>
        /// Computes a 2-distance between two observations. (aka Euclidian distance).
        /// </summary>
        /// <param name="a">Point a (double)</param>
        /// <param name="b">Point b (double)</param>
        /// <returns>Euclidian distance between the two points</returns>

        private double Marker(System.Windows.Point[] a, System.Windows.Point[] b, double anglethreshold)
        {
            /*
            double mark = 0.0;
            //error checking
            if (a.Length != b.Length) return 1; //would it affect the result?
            //aggregate the angle differences
            //a.3,7,11,16
            int count = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double d = Math.Sqrt(Math.Pow(Math.Abs(a[i].X - b[i].X), 2) + Math.Pow(Math.Abs(a[i].Y - b[i].Y), 2));
                
                if (d > anglethreshold && i != 3 && i != 7 && i != 11 && i != 16 )
                {
                    mark += 0.2;
                    count += 1;
                }
                    
                else if (d > (anglethreshold * 4 / 5))
                {
                    mark += 0.16;
                    count += 1;
                }
                else if (d > (anglethreshold * 3 / 5))
                {
                    mark += 0.12;
                    //count += 1;
                }
            }
            
            //return count / a.Length;
            if (count > 5 || mark > 1)
               return 1;
            else 
                return mark;
           */
            double d = 0.0;
            double mark = 0.0;

            for (int i = 0; i < 8; i++)
            {
                d += Math.Abs(a[i].X - b[i].X) + Math.Abs(a[i].Y - b[i].Y);
            }
            //d = Math.Sqrt(d);
            //d2 = Math.Sqrt(d2);
            mark = d;
            //error checking
            /*
            if (double.IsNaN(d) || double.IsNaN(d2)) return -1;

            if (d > 30 * _dimension / 2)       
            {
                mark += 0.5;
            }
            else if (d > 24 * _dimension / 2)
            {
                mark += 0.4;
            }
            else if (d > 18 * _dimension / 2)
            {
                mark += 0.3;
            }
            else if (d > 12 * _dimension / 2)
            {
                mark += 0.2;
            }


            if (d2 > 30 * _dimension / 2)
            {
                mark += 0.5;
            }
            else if (d2 > 24 * _dimension / 2)
            {
                mark += 0.4;
            }
            else if (d2 > 18 * _dimension / 2)
            {
                mark += 0.3;
            }
            else if (d2 > 12 * _dimension / 2)
            {
                mark += 0.2;
            }
             * */
            //return d + d2;
            return mark;
            
             
        }
        /// <summary>
        /// Calculate the result
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        private double RealtimeMarker(Point[] a1, Point[] a2) 
        { 
            double score = 0.0;
            return score;
        }

        /*
        /// <summary>
        /// record the selected dtw path in files
        /// </summary>
        /// <param name="path">the correspoding path</param>
        public void DtwRecordSelectedFrames(string path)
        {
            using (FileStream fs_ma = File.Create(@path + "MasterSelected"))
            {
                using (FileStream fs_le = File.Create(@path + "LearnerSelected"))
                {
                    using (BinaryWriter writer_ma = new BinaryWriter(fs_ma))
                    {
                        writer_ma.Write(_path.Count);
                        using (BinaryWriter writer_le = new BinaryWriter(fs_le))
                        {
                            foreach (DtwPathNode data in _path)
                            {
                                writer_ma.Write(data.I);
                                writer_le.Write(data.J);
                            }
                        }
                    }
                }
            }
        }
         * */

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

    }
}