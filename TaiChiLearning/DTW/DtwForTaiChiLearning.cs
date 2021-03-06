﻿using System.Diagnostics;

namespace TaiChiLearning.DTW
{
    using Microsoft.Kinect;
    using System;
    using System.Collections;
    using System.Drawing;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using System.Windows.Media.Media3D;
    using TaiChiLearning.Recorder;
    using TaiChiLearning.Replay;

    public class DtwForTaiChiLearning
    {
        private ArrayList _path;
        /// <summary>
        /// Size of obeservations vectors.
        /// </summary>
        private readonly int _dimension;

        private static KinectRecorder _lrecorder;
        private static Stream _learnerskeletonstream;
        private static BinaryWriter _lcolorrecorder;
        private static Stream _learnercolorstream;

        DateTime recordingTime;
        Stream stream;
        long streamPosition;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BytesPerPixel { get; private set; }
        public ColorImageFormat Format { get; private set; }
        public int PixelDataLength { get; set; }
        public int FrameNumber { get; protected set; }
        public long TimeStamp { get; protected set; }

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
        public double DtwComputation(ArrayList seq1, ArrayList seq2, ArrayList learnerframe, ArrayList learnercolorframe, string path, double anglethreshold)
        {
            // Init
            var seq1R = new ArrayList(seq1);
            //seq1R.Reverse();
            var seq2R = new ArrayList(seq2);

            var learnerf = new ArrayList();

            var learnerc = new ArrayList();
            //seq2R.Reverse();

            var tab = new double[seq1R.Count, seq2R.Count];

            for (int i = 0; i < seq1R.Count; i++)
            {
                for (int j = 0; j < seq2R.Count; j++)
                {
                    //tab[i, j] = Double.PositiveInfinity;
                    tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold);
                }
            }
            tab[0, 0] = 0;

            if (_learnerskeletonstream != null)
                _learnerskeletonstream.Close();
            while (FileDelete(@path + "LearnerSelected")) ;
            _learnerskeletonstream = File.Create(@path + "LearnerSelected");
            _lrecorder = new KinectRecorder(KinectRecordOptions.Skeletons, _learnerskeletonstream);


            if (_learnercolorstream != null)
                _learnercolorstream.Close();
            while (FileDelete(@path + "LearnerSelectedColor")) ;
            _learnercolorstream = File.Create(@path + "LearnerSelectedColor");
            _lcolorrecorder = new BinaryWriter( _learnercolorstream);

            int []max_slope = new int [3];
            
            // Dynamic computation of the DTW matrix.
            for (int i = 1; i < seq1R.Count; i++)
            {
                for (int j = 1; j < seq2R.Count; j++)
                {
                    switch (min(tab[i, j - 1], tab[i - 1, j - 1], tab[i - 1, j]))
                    {
                        case 1:
                            if(max_slope[0]++ %6 != 0) 
                             tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i, j - 1];
                            else tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i - 1, j - 1];
                            max_slope[1] = 0;
                            max_slope[2] = 0;
                            break;
                        case 2:
                            tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i - 1, j - 1];
                            max_slope[0] = 0;
                            max_slope[2] = 0;
                            break;
                        case 3:
                            if (max_slope[2]++ % 6 != 0) 
                            tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i - 1, j - 1];
                            else tab[i, j] = Marker((System.Windows.Point[])seq1R[i], (System.Windows.Point[])seq2R[j], anglethreshold) + tab[i - 1, j];
                            max_slope[0] = 0;
                            max_slope[1] = 0;
                            break;
                        default:
                            tab[i, j] = double.PositiveInfinity;
                            break;
                    }
                }
            }

            int totalframe = 0;
            int correctframe = 0;
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
                        currentJ--;
                        chosen = false;
                        break;
                    case 2:
                        currentI--;
                        currentJ--;
                        learnerf.Add((SkeletonFrame)learnerframe[currentJ]);
                        if (currentJ < learnercolorframe.Count) learnerc.Add((int)learnercolorframe[currentJ]);
                        correctframe++;
                        chosen = false;
                        break;
                    case 3:
                        currentI--;
                        learnerf.Add((SkeletonFrame)learnerframe[currentJ]);
                        if(currentJ < learnercolorframe.Count) learnerc.Add((int)learnercolorframe[currentJ]);// sometimes the color image frame number is smaller
                        if(!chosen) correctframe++;
                        chosen = true;
                        break;
                    default:
                        currentJ--;
                        chosen = false;
                        break;
                }
                totalframe++;
            }

            while (learnerf.Count != 0)
            {
                _lrecorder.Record((SkeletonFrame)learnerf[learnerf.Count - 1]);
                learnerf.RemoveAt(learnerf.Count - 1);
            }

            
            using (FileStream fs = File.OpenRead(".\\colorStream"))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int temp = reader.ReadInt32();
                    _lcolorrecorder.Write(temp);
                    CreateFromReader(reader);
                    while (learnerc.Count != 0)
                    {
                        if (FrameNumber == (int)learnerc[learnerc.Count - 1])
                        {
                            Record(_lcolorrecorder);
                            learnerc.RemoveAt(learnerc.Count - 1);
                        }
                        else
                        {
                            CreateFromReader(reader);
                        }
                    }
                }
            }

            _learnerskeletonstream.Close();
            _lrecorder.Stop();
            _learnercolorstream.Close();
            _lcolorrecorder.Close();
            return (Double)correctframe / (Double)totalframe * 100;
        }


        private int min(double a, double b, double c)
        {
            if (a == double.PositiveInfinity && b == double.PositiveInfinity && c == double.PositiveInfinity)
                return 4;
            if (Math.Min(Math.Min(a, b), c) == a)
                return 1;
            else if (Math.Min(Math.Min(a, b), c) == b)
                return 2;
            else 
                return 3;
        }

        /// <summary>
        /// Computes a 2-distance between two observations. 
        /// </summary>
        /// <param name="a">Point a (double)</param>
        /// <param name="b">Point b (double)</param>
        /// <returns>Euclidian distance between the two points</returns>

        private double Marker(System.Windows.Point[] a, System.Windows.Point[] b, double anglethreshold)
        {
            double mark = 0.0;
            double diffX = 0.0;
            double diffY = 0.0;
            int outofthreshold = 0;

            for (int i = 0; i < _dimension; i++)
            {
                diffX = Math.Abs(a[i].X - b[i].X);
                diffY = Math.Abs(a[i].Y - b[i].Y);
                if ((diffX > anglethreshold && diffX < 360 - anglethreshold) || (diffY > anglethreshold && diffY < 360 - anglethreshold))
                {
                    outofthreshold++;
                }
                if (Math.Abs(a[i].X - b[i].X) > 180)
                    mark += 360 - Math.Abs(a[i].X - b[i].X);
                else mark += Math.Abs(a[i].X - b[i].X);

                if (Math.Abs(a[i].Y - b[i].Y) > 180)
                    mark += 360 - Math.Abs(a[i].Y - b[i].Y);
                else mark += Math.Abs(a[i].Y - b[i].Y);
            }
            mark /= _dimension;
            if (outofthreshold > 3 && outofthreshold < 6)
            {
                mark *= 2;
            }
            else if (outofthreshold > 3 && outofthreshold < 12)
            {
                mark *= 3;
            }
            else if (outofthreshold > 3)
                mark = double.PositiveInfinity;
           
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

        private void CreateFromReader(BinaryReader reader)
        {
            int temp = reader.ReadInt32();
            TimeStamp = reader.ReadInt64();
            BytesPerPixel = reader.ReadInt32();
            Format = (ColorImageFormat)reader.ReadInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            FrameNumber = reader.ReadInt32();

            PixelDataLength = reader.ReadInt32();

            stream = reader.BaseStream;
            streamPosition = stream.Position;

            stream.Position += PixelDataLength;

        }

        public void Record(BinaryWriter writer)
        {
            // Header
            writer.Write((int)KinectRecordOptions.Color);

            // ColorFrame Information   
            TimeSpan timeSpan = DateTime.Now.Subtract(recordingTime);
            recordingTime = DateTime.Now;
            writer.Write((long)timeSpan.TotalMilliseconds);
            writer.Write(BytesPerPixel);
            writer.Write((int)Format);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(FrameNumber);

            // Bytes
            writer.Write(PixelDataLength);
            byte[] pixelData = new byte[PixelDataLength];

            long savedPosition = stream.Position;
            stream.Position = streamPosition;

            stream.Read(pixelData, 0, PixelDataLength);

            stream.Position = savedPosition;
            // Save the Frame Pixel Data
            writer.Write(pixelData);
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

        public static bool FileReadable(string filename) 
        {
            try 
            {
                using (FileStream fs = File.OpenRead(@filename)) ;
                Thread.Sleep(500);
                return false;
            }
            catch(IOException)
            {
                return true;
            }
        }
    }
}