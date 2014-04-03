using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaiChiLearning.DTW
{
    internal class DtwPathNode
    {
        private readonly int TimeI; //specify which frame in seq1
        private readonly int TimeJ; //specify which frame in seq2
        public readonly double Score; //specify the score between seq1[timeI] and seq2[timeJ]
        //private DtwPathNode Next;

        public DtwPathNode(int i, int j, double score)
        {
            TimeI = i;
            TimeJ = j;
            Score = score;
            //Next = null;
        }

        /*
        public void AddNext(DtwPathNode next){
            Next = next;
        }
        */
        public int I
        {
            get
            {
                return this.TimeI;
            }
        }

        public int J
        {
            get
            {
                return this.TimeJ;
            }
        }

    
    }
}
