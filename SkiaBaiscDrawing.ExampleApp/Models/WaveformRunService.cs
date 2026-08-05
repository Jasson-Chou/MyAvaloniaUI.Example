using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models
{
    public interface IWaveformRunService
    {
        int BufferSize { get; }
        void SetBufferSize(int newSize);
        void ResetSimulator(IWaveformSimulator newSimulator);
        void Start();
        void Pull();
        void Stop();
        bool GetMinMax(out float min, out float max);
        float[] GetValues();
    }


    public class WaveformRunService : IWaveformRunService
    {
        public WaveformRunService() : this(new SineGenerator(1, 1000, 1)) { }

        public WaveformRunService(IWaveformSimulator simulator)
        {
            _simulator = simulator;
            _queue = new DropOldestQueue<float>();
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly DropOldestQueue<float> _queue;
        private TimeSpan _lastTime = TimeSpan.Zero;
        private IWaveformSimulator _simulator;

        public int BufferSize => _queue.Capacity;

        public void SetBufferSize(int newSize)
        {
            _queue.SetCapacity(newSize);
        }

        public void ResetSimulator(IWaveformSimulator newSimulator)
        {
            _simulator = newSimulator;
        }

        public void Start()
        {
            _lastTime = TimeSpan.Zero;
            _stopwatch.Restart();
            _queue.Clear();
        }

        public void Pull()
        {
            if(_stopwatch.Elapsed == _lastTime)
                return;
            if(_simulator is null) 
                throw new InvalidOperationException("Simulator is not set.");
            var currentTime = _stopwatch.Elapsed;
            var duration = currentTime - _lastTime;
            var newSamples = _simulator.GenerateF(_lastTime, duration);
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

        public float[] GetValues()
        {
            return _queue.ToArray();
        }

        


    }
}
