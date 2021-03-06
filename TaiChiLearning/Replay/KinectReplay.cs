﻿using System;
using System.IO;
using System.Threading;
using System.Windows;
using TaiChiLearning;
using TaiChiLearning.Recorder;

namespace TaiChiLearning.Replay
{
    public class KinectReplay : IDisposable
    {
        BinaryReader reader;
        Stream stream;
        readonly SynchronizationContext synchronizationContext;

        // Events
        public event EventHandler<ReplaySkeletonFrameReadyEventArgs> SkeletonFrameReady;
        public event EventHandler<ReplayColorImageFrameReadyEventArgs> ColorImageFrameReady; 


        // Replay
        ReplaySystem<ReplaySkeletonFrame> skeletonReplay;
        ReplaySystem<ReplayColorImageFrame> colorReplay;

        public bool Started { get; internal set; }

        public bool IsFinished
        {
            get
            {

                if (skeletonReplay != null && !skeletonReplay.IsFinished)
                    return false;

                if (colorReplay != null && !colorReplay.IsFinished)
                    return false;

                return true;
            }
        }

        public KinectReplay(Stream stream)
        {
            this.stream = stream;
            reader = new BinaryReader(stream);

            synchronizationContext = SynchronizationContext.Current;

            KinectRecordOptions options = (KinectRecordOptions) reader.ReadInt32();

            
            if ((options & KinectRecordOptions.Color) != 0)
            {
                colorReplay = new ReplaySystem<ReplayColorImageFrame>();
            }

            /*
            if ((options & KinectRecordOptions.Depth) != 0)
            {
                depthReplay = new ReplaySystem<ReplayDepthImageFrame>();
            }
             * */
            

            if ((options & KinectRecordOptions.Skeletons) != 0)
            {
                skeletonReplay = new ReplaySystem<ReplaySkeletonFrame>();
            }

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                KinectRecordOptions header = (KinectRecordOptions)reader.ReadInt32();
                switch (header)
                {
                        
                    case KinectRecordOptions.Color:
                        colorReplay.AddFrame(reader);
                        break;

                    case KinectRecordOptions.Skeletons:
                        skeletonReplay.AddFrame(reader);
                        break;

                    case KinectRecordOptions.ReplayFrame:
                        skeletonReplay.AddFrame(reader);
                        break;
                }
            }
        }

        public void Start(double rateinmsec)
        {
            if (Started)
                throw new Exception("KinectReplay already started");

            Started = true;
           
            if (colorReplay != null)
            {
                colorReplay.Start(rateinmsec);
                colorReplay.FrameReady += frame => synchronizationContext.Send(state =>
                {
                    if (ColorImageFrameReady != null)
                        ColorImageFrameReady(this, new ReplayColorImageFrameReadyEventArgs { ColorImageFrame = frame });
                }, null);
            }

            if (skeletonReplay != null)
            {
                skeletonReplay.Start(rateinmsec);
                skeletonReplay.FrameReady += frame => synchronizationContext.Send(state =>
                {
                    if (SkeletonFrameReady != null)
                        SkeletonFrameReady(this, new ReplaySkeletonFrameReadyEventArgs { SkeletonFrame = frame });
                }, null);
            }
        }

        public void Stop()
        {
            
            if (colorReplay != null)
            {
                colorReplay.Stop();
            }

            /*
            if (depthReplay != null)
            {
                depthReplay.Stop();
            }
            */

            if (skeletonReplay != null)
            {
                skeletonReplay.Stop();
            }

            Started = false;
        }

        public void Dispose()
        {
            Stop();
            
            colorReplay = null;
            /*
            depthReplay = null;
             */

            skeletonReplay = null;

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }

            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}
