using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Kinect;
using System.Runtime.Serialization.Formatters.Binary;

namespace GrabSkeletonData.Recorder
{
    class SkeletonRecorder
    {
        DateTime recordingTime;
        readonly BinaryWriter writer;

        internal SkeletonRecorder(BinaryWriter writer) 
        {
            this.writer = writer;
            recordingTime = DateTime.Now;
        }

        public void Record(SkeletonFrame frame) 
        {

            // Header
            writer.Write((int)KinectRecordOptions.Skeletons);

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
            Skeleton[] skeletons = frame.GetSkeletons();
            frame.CopySkeletonDataTo(skeletons);

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(writer.BaseStream, skeletons);
        }
    }
}
