﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TaiChiLearning.Replay
{
    class ReplaySystem<T>  where T:ReplayFrame, new()
    {
        internal event Action<T> FrameReady;
        readonly List<T> frames = new List<T>();

        CancellationTokenSource cancellationTokenSource;

        public bool IsFinished
        {
            get;
            private set;
        }

        internal void AddFrame(BinaryReader reader)
        {
            T frame = new T();

            frame.CreateFromReader(reader);

            frames.Add(frame);
        }

        public void Start(double rateinmsec)
        {
            Stop();

            IsFinished = false;

            cancellationTokenSource = new CancellationTokenSource();

            CancellationToken token = cancellationTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                foreach (T frame in frames)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(rateinmsec));

                    if (token.IsCancellationRequested)
                        break;

                    if (FrameReady != null)
                        FrameReady(frame);
                }
                IsFinished = true;
            }, token);
        }

        public void Stop()
        {
            if (cancellationTokenSource == null)
                return;

            cancellationTokenSource.Cancel();
        }
    }
}
