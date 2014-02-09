using System;

namespace GrabSkeletonData.Replay
{
    public class ReplayColorImageFrameReadyEventArgs : EventArgs
    {
        public ReplayColorImageFrame ColorImageFrame { get; set; }
    }
}
