using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Kinect;

namespace GrabSkeletonData
{
    class ColorRecorder
    {        
        DateTime recordingTime;
        readonly BinaryWriter writer;

        //use internel: only allow access within the same asembly
        internal ColorRecorder(BinaryWriter writer) 
        {
            this.writer = writer;
            recordingTime = DateTime.Now;
        }

        public void Record(ColorImageFrame frame)
        {
            // Header
            writer.Write((int)KinectRecordOptions.Skeletons);
            
            // ColorFrame Information   
            TimeSpan timeSpan = DateTime.Now.Subtract(recordingTime);
            recordingTime = DateTime.Now;
            writer.Write((long)timeSpan.TotalMilliseconds);
            writer.Write(frame.BytesPerPixel);
            writer.Write((int)frame.Format);
            writer.Write(frame.Width);
            writer.Write(frame.Height);
            writer.Write(frame.FrameNumber);

            // Bytes
            
            writer.Write(frame.PixelDataLength);
            byte[] bytes = new byte[frame.PixelDataLength];
            
            // Save the Frame Pixel Data
            frame.CopyPixelDataTo(bytes);
            writer.Write(bytes);
             
        }
    }
}
