//-----------------------------------------------------------------------
// <copyright file="DtwGestureRecognizer.cs" company="Rhemyst and Rymix">
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

using System.Diagnostics;

namespace TaiChiLearning.DTW
{
    using System;
    using System.Collections;
    using System.Drawing;
    using System.IO;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Dynamic Time Warping nearest neighbour sequence comparison class.
    /// Called 'Gesture Recognizer' but really it can work with any vectors
    /// </summary>
    public class DtwForTaiChiLearning
    {
        private ArrayList _path;
        private Point[] _matchFrame;
        /// <summary>
        /// Size of obeservations vectors.
        /// </summary>
        private readonly int _dimension;
        
        /// <summary>
        /// Maximum distance between the last observations of each sequence.
        /// </summary>
        private readonly double _firstThreshold;

        /// <summary>
        /// Minimum length of a gesture before it can be recognised
        /// </summary>
        private readonly double _minimumLength;

        /// <summary>
        /// Maximum DTW distance between an example and a sequence being classified.
        /// </summary>
        private readonly double _globalThreshold;

        /// <summary>
        /// The gesture names. Index matches that of the sequences array in _sequences
        /// </summary>
        private readonly ArrayList _labels;

        /// <summary>
        /// Maximum vertical or horizontal steps in a row.
        /// </summary>
        private readonly int _maxSlope;

        /// <summary>
        /// The recorded gesture sequences
        /// </summary>
        private readonly ArrayList _sequences;


        private readonly double judgement = 120;

        /// <summary>
        /// Initializes a new instance of the DtwGestureRecognizer class
        /// First DTW constructor
        /// </summary>
        /// <param name="dim">Vector size</param>
        /// <param name="threshold">Maximum distance between the last observations of each sequence</param>
        /// <param name="firstThreshold">Minimum threshold</param>
        public DtwForTaiChiLearning(int dim, double threshold, double firstThreshold, double minLen)
        {
            _dimension = dim;
            _sequences = new ArrayList();
            _labels = new ArrayList();
            _globalThreshold = threshold;
            _firstThreshold = firstThreshold;
            _maxSlope = int.MaxValue;
            _minimumLength = minLen;
        }

        /// <summary>
        /// Initializes a new instance of the DtwGestureRecognizer class
        /// Second DTW constructor
        /// </summary>
        /// <param name="dim">Vector size</param>
        /// <param name="threshold">Maximum distance between the last observations of each sequence</param>
        /// <param name="firstThreshold">Minimum threshold</param>
        /// <param name="ms">Maximum vertical or horizontal steps in a row</param>
        public DtwForTaiChiLearning(int dim, double threshold, double firstThreshold, int ms, double minLen)
        {
            _dimension = dim;
            _sequences = new ArrayList();
            _labels = new ArrayList();
            _globalThreshold = threshold;
            _firstThreshold = firstThreshold;
            _maxSlope = ms;
            _minimumLength = minLen;
        }

        /// <summary>
        /// Recognize gesture in the given sequence.
        /// It will always assume that the gesture ends on the last observation of that sequence.
        /// If the distance between the last observations of each sequence is too great, or if the overall DTW distance between the two sequence is too great, no gesture will be recognized.
        /// </summary>
        /// <param name="seq">The sequence to recognise</param>
        /// <returns>The recognised gesture name</returns>
        //public string Recognize(ArrayList seq)
        /*
        public double Recognize(ArrayList seq)
        {
            double minDist = double.PositiveInfinity;

            for (int i = 0; i < _sequences.Count; i++)

            {
                var example = (ArrayList)_sequences[i];
                ////Debug.WriteLine(Marker((double[]) seq[seq.Count - 1], (double[]) example[example.Count - 1]));
                if (Marker((double[])seq[seq.Count - 1], (double[])example[example.Count - 1]) < _firstThreshold)
                {
                double d = DtwCompution(seq, example) / seq.Count;
                if (d < minDist) minDist = d;
            }

            return 1 - minDist +minDist.ToString();
        }

        
        /// <summary>
        /// Retrieves a text represeantation of the _label and its associated _sequence
        /// For use in dispaying debug information and for saving to file
        /// </summary>
        /// <returns>A string containing all recorded gestures and their names</returns>
        public string RetrieveText()
        {
            string retStr = String.Empty;

            if (_sequences != null)
            {
                // Iterate through each gesture
                for (int gestureNum = 0; gestureNum < _sequences.Count; gestureNum++)
                {
                    // Echo the label
                    retStr += _labels[gestureNum] + "\r\n";

                    int frameNum = 0;

                    //Iterate through each frame of this gesture
                    foreach (double[] frame in ((ArrayList)_sequences[gestureNum]))
                    {
                        // Extract each double
                        foreach (double dub in (double[])frame)
                        {
                            retStr += dub + "\r\n";
                        }

                        // Signifies end of this double
                        retStr += "~\r\n";

                        frameNum++;
                    }

                    // Signifies end of this gesture
                    retStr += "----";
                    if (gestureNum < _sequences.Count - 1)
                    {
                        retStr += "\r\n";
                    }
                }
            }

            return retStr;
        }

        public string ReadText()
        {
            string retStr = String.Empty;

            if (_sequences != null)
            {
                // Iterate through each gesture
                for (int gestureNum = 0; gestureNum < _sequences.Count; gestureNum++)
                {
                    // Echo the label
                    retStr += _labels[gestureNum] + "\r\n";

                    int frameNum = 0;

                    //Iterate through each frame of this gesture
                    foreach (double[] frame in ((ArrayList)_sequences[gestureNum]))
                    {
                        // Extract each double
                        foreach (double dub in (double[])frame)
                        {
                            retStr += dub + "\r\n";
                        }

                        // Signifies end of this double
                        retStr += "~\r\n";

                        frameNum++;
                    }

                    // Signifies end of this gesture
                    retStr += "----";
                    if (gestureNum < _sequences.Count - 1)
                    {
                        retStr += "\r\n";
                    }
                }
            }

            return retStr;
        }
         /// <summary>
        /// Add a seqence with a label to the known sequences library.
        /// The gesture MUST start on the first observation of the sequence and end on the last one.
        /// Sequences may have different lengths.
        /// </summary>
        /// <param name="seq">The sequence</param>
        /// <param name="lab">Sequence name</param>
        public void AddOrUpdate(ArrayList seq, string lab)
        {
            // First we check whether there is already a recording for this label. If so overwrite it, otherwise add a new entry
            int existingIndex = -1;

            for (int i = 0; i < _labels.Count; i++)
            {
                if ((string)_labels[i] == lab)
                {
                    existingIndex = i;
                }
            }

            // If we have a match then remove the entries at the existing index to avoid duplicates. We will add the new entries later anyway
            if (existingIndex >= 0)
            {
                _sequences.RemoveAt(existingIndex);
                _labels.RemoveAt(existingIndex);
            }

            // Add the new entries
            _sequences.Add(seq);
            _labels.Add(lab);
        }
         */


        /// <summary>
        /// Compute the min DTW distance between seq2 and all possible endings of seq1.
        /// </summary>
        /// <param name="seq1">The master array of sequences to compare</param>
        /// <param name="seq2">The learner array of sequences to compare</param>
        /// <returns>The best match</returns>
        /*
        public void DtwComputation(ArrayList seq1, ArrayList seq2, string path)
        {
            // Init
            var seq1R = new ArrayList(seq1);
            seq1R.Reverse();
            var seq2R = new ArrayList(seq2);
            seq2R.Reverse();
            var tab = new double[seq1R.Count + 1, seq2R.Count + 1];
            var slopeI = new int[seq1R.Count + 1, seq2R.Count + 1];
            var slopeJ = new int[seq1R.Count + 1, seq2R.Count + 1];

            for (int i = 0; i < seq1R.Count + 1; i++)
            {
                for (int j = 0; j < seq2R.Count + 1; j++)
                {
                    tab[i, j] = double.PositiveInfinity;
                    slopeI[i, j] = 0; //number of times that the shortest path is from the left consecutively
                    slopeJ[i, j] = 0; //number of times that the shortest path is from the top consecutively
                }
            }

            tab[0, 0] = 0;

            // Dynamic computation of the DTW matrix.
            for (int i = 1; i < seq1R.Count + 1; i++)
            {
                for (int j = 1; j < seq2R.Count + 1; j++)
                {
                    if (tab[i, j - 1] < tab[i - 1, j - 1] && tab[i, j - 1] < tab[i - 1, j] && //case1: tab[i,j-1] (left) have passed the shortest path so far
                        slopeI[i, j - 1] < _maxSlope)
                    {
                        tab[i, j] = Marker((double[])seq1R[i - 1], (double[])seq2R[j - 1]) + tab[i, j - 1];
                        slopeI[i, j] = slopeI[i, j - 1] + 1;
                        slopeJ[i, j] = 0;
                    }
                    else if (tab[i - 1, j] < tab[i - 1, j - 1] && tab[i - 1, j] < tab[i, j - 1] && //case2: tab[i-1,j] (top) have passed the shortest path so far
                             slopeJ[i - 1, j] < _maxSlope)
                    {
                        tab[i, j] = Marker((double[])seq1R[i - 1], (double[])seq2R[j - 1]) + tab[i - 1, j];
                        slopeI[i, j] = 0;
                        slopeJ[i, j] = slopeJ[i - 1, j] + 1;
                    }
                    else //case3: tab[i-1,j-1] (top left) have passed the shortest path so far
                    {
                        tab[i, j] = Marker((double[])seq1R[i - 1], (double[])seq2R[j - 1]) + tab[i - 1, j - 1];
                        slopeI[i, j] = 0;
                        slopeJ[i, j] = 0;
                    }
                }
            }

            // Find best between seq2 and an ending (postfix) of seq1.
            double bestMatch = double.PositiveInfinity;
            int bestMatchI = 0;
            for (int i = 1; i < (seq1R.Count + 1) - _minimumLength; i++)
            {
                if (tab[i, seq2R.Count] < bestMatch)
                {
                    bestMatch = tab[i, seq2R.Count];
                    bestMatchI = i; //trace the AugMin of bestMatch
                }
            }

            //Reconstruct the best matched path
            if (bestMatchI >= 1) //return -1; //error checking 
            {
                _path.Clear();
                _path = new ArrayList();
                int currentI = bestMatchI;
                int currentJ = seq2R.Count;

                while (currentI != 0 && currentJ != 0)
                {
                    var target = new DtwPathNode(seq1.Count - currentI, seq2.Count - currentJ, tab[currentI, currentJ]);
                    _path.Add(target);
                    if (slopeI[currentI, currentJ] > 0) //trace the left one
                    {
                        currentJ -= 1;
                    }
                    else if (slopeJ[currentI, currentJ] > 0) //trace the top one
                    {
                        currentI -= 1;
                    }
                    else //trace the top left one
                    {
                        currentI -= 1;
                        currentJ -= 1;
                    }
                }

                DtwRecordSelectedFrames(path);
            }

                //return bestMatch;
        }
        */

        /// <summary>
        /// Computes a 2-distance between two observations. (aka Euclidian distance).
        /// </summary>
        /// <param name="a">Point a (double)</param>
        /// <param name="b">Point b (double)</param>
        /// <returns>Euclidian distance between the two points</returns>
        private double Marker(Point[] a, Point[] b)
        {
            
            double d = 0;
            //error checking
            if (a.Length != b.Length) return -1;
            //aggregate the angle differences
            for (int i = 0; i < a.Length; i++)
            {
                d += Math.Abs(a[i].X - b[i].X) + Math.Abs(a[i].Y - b[i].Y);
            }
            Math.Sqrt(d);
            //error checking
            if (!double.IsNaN(d)) return -1;
            //scale the score
            d *= 100.0 / 720.0;
            return d;
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
        public DtwPathNode MostWrongNode()
        {
            int index = 0;
            int highScore = 0;
            for (int i = 0; i < _path.Count; i++)
            {
                if (((DtwPathNode)_path[i]).Score > highScore)
                    index = i;
            }
                return (DtwPathNode)_path[index];
        }
         * */

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

    }
}