using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using TaiChiLearning.Recorder;
using TaiChiLearning.Replay;

namespace GrabSkeletonData.Recorder
{
    class ReplayFrameRecorder
    {
        DateTime recordingTime;
        readonly BinaryWriter writer;

        internal ReplayFrameRecorder(BinaryWriter writer) 
        {
            this.writer = writer;
            recordingTime = DateTime.Now;
        }

        public void Record(ReplaySkeletonFrame frame) 
        {

            // Header
            writer.Write((int)KinectRecordOptions.ReplayFrame);

            // Data
            TimeSpan timeSpan = DateTime.Now.Subtract(recordingTime);
            recordingTime = DateTime.Now;
            writer.Write((long)timeSpan.TotalMilliseconds);
            writer.Write((int)frame.TrackingMode);
            writer.Write(frame.FloorClipPlane.Item1);
            writer.Write(frame.FloorClipPlane.Item2);
            writer.Write(frame.FloorClipPlane.Item3);
            writer.Write(frame.FloorClipPlane.Item4);

            writer.Write(frame.FrameNumber);

            // Skeletons
            Skeleton[] skeletons = frame.Skeletons;

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(writer.BaseStream, skeletons);
        }
    }
}
