using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaiChiLearning.Recorder
{
    [FlagsAttribute]
    public enum KinectRecordOptions
    {
        Color = 1,
        Depth = 2,
        ReplayFrame =3,
        Skeletons = 4
        
    }
}
