using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models
{
    public class WaveformSimService
    {
        public WaveformSimService(IWaveformSimulator simulator, int buffSize)
        {
            _simulator = simulator;
            _queue = new DropOldestQueue<float>(buffSize);
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly IWaveformSimulator _simulator;
        private readonly DropOldestQueue<float> _queue;
        private TimeSpan _lastTime = TimeSpan.Zero;
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
