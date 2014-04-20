using System;
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

        /*
        public void StartDTWSelected(double rateinmsec, Point[] dtwselected, string whose)
        {
            Stop();

            IsFinished = false;

            cancellationTokenSource = new CancellationTokenSource();

            CancellationToken token = cancellationTokenSource.Token;

            int dtwskeleton = 0;

            Task.Factory.StartNew(() =>
            {
                foreach (T frame in frames)
                {
                    if (whose == "Learner")
                    {
                        if (dtwselected.Length / 2 > dtwskeleton)
                        {
                            while (dtwselected[dtwskeleton + 1].Y == dtwselected[dtwskeleton].Y)
                            {
                                if (dtwselected.Length / 2 == dtwskeleton - 1 ) break;
                                Thread.Sleep(TimeSpan.FromMilliseconds(rateinmsec));
                                dtwskeleton++;
                            }
                        }
                    }
                    else
                    {
                        if (dtwselected.Length / 2 > dtwskeleton)
                        {
                            while (dtwselected[dtwskeleton + 1].X == dtwselected[dtwskeleton].X)
                            {
                                if (dtwselected.Length / 2 == dtwskeleton - 1) break;
                                Thread.Sleep(TimeSpan.FromMilliseconds(rateinmsec));
                                dtwskeleton++;
                            }
                        }
                    }
                    
                    Thread.Sleep(TimeSpan.FromMilliseconds(rateinmsec));

                    if (token.IsCancellationRequested)
                        break;

                    if (FrameReady != null)
                        FrameReady(frame);

                    dtwskeleton++;
                }

                IsFinished = true;
            }, token);
        }
        */
        public void Stop()
        {
            if (cancellationTokenSource == null)
                return;

            cancellationTokenSource.Cancel();
        }
    }
}
