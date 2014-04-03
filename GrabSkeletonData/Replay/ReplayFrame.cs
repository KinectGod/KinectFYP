using System.IO;

namespace TaiChiLearning.Replay
{
    public abstract class ReplayFrame
    {
        public int FrameNumber { get; protected set; }
        public long TimeStamp { get; protected set; }

        internal abstract void CreateFromReader(BinaryReader reader);
    }
}
