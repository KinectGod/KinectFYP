using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrabSkeletonData.Recorder
{
    [FlagsAttribute]
    public enum KinectRecordOptions
    {
        Color = 1,
        Depth = 2,
        Skeletons = 4
    }
}
