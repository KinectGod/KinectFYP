using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Kinect;
using TaiChiLearning.Replay;
using TaiChiLearning.Recorder;

namespace TaiChiLearning.Recorder
{
    class KinectRecorder
    {
        public Stream recordStream;
        readonly BinaryWriter writer;
        // Recorders
        //readonly ColorRecorder colorRecoder;
        readonly SkeletonRecorder skeletonRecorder;
        readonly ColorRecorder colorRecoder;
        readonly ReplayFrameRecorder replayrecorder;

        //
        public KinectRecordOptions Options { get; set; }

        // Constructor
        public KinectRecorder(KinectRecordOptions options, Stream stream)
        {
                Options = options;
                recordStream = stream;
                writer = new BinaryWriter(recordStream);
                writer.Write((int)Options);

                
                if ((Options & KinectRecordOptions.Color) != 0)
                {
                    colorRecoder = new ColorRecorder(writer);
                }

                if ((Options & KinectRecordOptions.Skeletons) != 0)
                {
                    skeletonRecorder = new SkeletonRecorder(writer);
                }

                if ((Options & KinectRecordOptions.ReplayFrame) != 0)
                {
                    replayrecorder = new ReplayFrameRecorder(writer);
                }
            
        }

        // three methods to record specific frames (color, depth, and skeleton):
        public void Record(SkeletonFrame frame)
        {

            if (writer == null)
                throw new Exception("This recorder is stopped");

            if (skeletonRecorder == null)
                throw new Exception("Skeleton recording is not actived on this KinectRecorder");

            skeletonRecorder.Record(frame);
           
        }

        public void Record(ReplaySkeletonFrame frame)
        {

            if (writer == null)
                throw new Exception("This recorder is stopped");

            if (replayrecorder == null)
                throw new Exception("Skeleton recording is not actived on this KinectRecorder");

            replayrecorder.Record(frame);

        }
        
        public void Record(ColorImageFrame frame)
        {
            if (writer == null)
                throw new Exception("This recorder is stopped");

            
            if (colorRecoder == null)
                throw new Exception("Color recording is not actived on this KinectRecorder");

            colorRecoder.Record(frame);
           
        }
        


        public void Stop()
        {
            if (writer == null)
                throw new Exception("This recorder is already stopped");

            writer.Close();
            writer.Dispose();

            recordStream.Dispose();
            recordStream = null;
        }
    }
}
