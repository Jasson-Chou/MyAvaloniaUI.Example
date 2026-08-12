using JC.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JC.Signal
{
    public interface ISignalRunService
    {
        int BufferSize { get; }
        ulong CumulativePoints { get; }
        void SetBufferSize(int newSize);
        void ResetGeneration(ISignalGeneration generation);
        void Start();
        void Pull();
        void Stop();
        bool GetMinMax(out float min, out float max);
        ulong GetCumulativePoints();
        float[] GetValues();
    }


    public class SignalRunService : ISignalRunService
    {
        public SignalRunService() : this(new SineGenerator(1, 1000, 1)) { }

        public SignalRunService(ISignalGeneration signalGenerator)
        {
            _signalGenerator = signalGenerator;
            _queue = new DropOldestQueue<float>();
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly DropOldestQueue<float> _queue;
        private TimeSpan _lastTime = TimeSpan.Zero;
        private ISignalGeneration _signalGenerator;

        public int BufferSize => _queue.Capacity;
        public ulong CumulativePoints { get; private set; } = 0;

        public void SetBufferSize(int newSize)
        {
            _queue.SetCapacity(newSize);
        }

        public void ResetGeneration(ISignalGeneration generation)
        {
            _signalGenerator = generation;
        }

        public void Start()
        {
            _lastTime = TimeSpan.Zero;
            CumulativePoints = 0;
            _stopwatch.Restart();
            _queue.Clear();
        }

        public void Pull()
        {
            if (_stopwatch.Elapsed == _lastTime)
                return;
            if (_signalGenerator is null)
                throw new InvalidOperationException("Signal generator is not set.");
            var currentTime = _stopwatch.Elapsed;
            var duration = currentTime - _lastTime;
            var newSamples = _signalGenerator.GenerateF(_lastTime, duration);
            CumulativePoints += (ulong)newSamples.LongLength;
            _queue.EnqueueRange(newSamples);
            _lastTime = currentTime;
        }

        public void Stop()
        {
            _stopwatch.Stop();
            Pull();
        }

        public bool GetMinMax(out float min, out float max)
        {
            return _queue.GetMinMax(out min, out max);
        }

        public ulong GetCumulativePoints() => CumulativePoints;

        public float[] GetValues()
        {
            return _queue.ToArray();
        }




    }
}
