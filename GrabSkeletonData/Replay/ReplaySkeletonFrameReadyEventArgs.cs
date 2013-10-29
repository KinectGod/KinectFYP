using System;

namespace GrabSkeletonData.Replay
{
    public class ReplaySkeletonFrameReadyEventArgs : EventArgs
    {
        public ReplaySkeletonFrame SkeletonFrame { get; set; } // construct a new value called 'SkeletonFrame', and gettable and settable
    }
}
