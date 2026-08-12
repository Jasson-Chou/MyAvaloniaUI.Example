using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JC.Signal
{
    public class SineGenerator : ISignalGeneration
    {
        private readonly double _phaseIncrement; // 每個樣本的相位增量 (弧度)
        private double _phase;                   // 目前相位 (弧度)

        public double Frequency { get; }   // 頻率 (Hz)
        public double SampleRate { get; }  // 取樣率 (samples/sec)
        public double Amplitude { get; }   // 振幅

        public SineGenerator(double frequency, double sampleRate = 44100, double amplitude = 1.0)
        {

            Frequency = frequency;
            SampleRate = sampleRate;
            Amplitude = amplitude;
            _phaseIncrement = Math.Tau * frequency / sampleRate; // 2π * f / fs => 2π * (f/fs) = 2π * (cycles per sample)
        }

        // ---------------------------------------------------------------
        // 有狀態產生 (會推進內部相位，適合連續串流)
        // ---------------------------------------------------------------

        /// <summary>取得下一個樣本並推進相位。</summary>
        public double NextSample()
        {
            double sample = Amplitude * Math.Sin(_phase);

            _phase += _phaseIncrement;
            if (_phase >= Math.Tau)  // 相位繞回，避免無限累積造成精度流失
                _phase -= Math.Tau;

            return sample;
        }

        /// <summary>將樣本填入既有的 buffer (零額外配置)。</summary>
        public void Fill(Span<double> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextSample();
        }

        public void Fill(Span<float> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (float)NextSample();
        }

        /// <summary>產生指定數量的樣本並回傳新陣列。</summary>
        public double[] Generate(int count)
        {
            var samples = new double[count];
            Fill(samples);
            return samples;
        }

        public float[] GenerateF(int count)
        {
            var samples = new float[count];
            Fill(samples);
            return samples;
        }

        /// <summary>將相位重置歸零。</summary>
        public void Reset() => _phase = 0;

        // ---------------------------------------------------------------
        // 無狀態查詢 (依時間直接計算，不影響內部相位)
        // ---------------------------------------------------------------

        /// <summary>
        /// 計算指定時間點的樣本值。
        /// 公式: value(t) = Amplitude * sin(2π * Frequency * t)
        /// </summary>
        /// <param name="timeSeconds">時間 (秒)</param>
        public double GetValueAtTime(double timeSeconds)
        {
            // 只保留週期的小數部分，避免大時間值造成精度問題
            double cycles = Frequency * timeSeconds;
            double phase = Math.Tau * (cycles - Math.Floor(cycles));
            return Amplitude * Math.Sin(phase);
        }

        /// <summary>計算指定時間點的樣本值 (TimeSpan 多載)。</summary>
        public double GetValueAtTime(TimeSpan time) => GetValueAtTime(time.TotalSeconds);

        /// <summary>
        /// 從指定起始時間產生 count 個樣本 (依取樣率遞增)。
        /// 第 i 個樣本對應時間 t = startTimeSeconds + i / SampleRate。
        /// </summary>
        public double[] Generate(double startTimeSeconds, int count)
        {
            var samples = new double[count];
            for (int i = 0; i < count; i++)
                samples[i] = GetValueAtTime(startTimeSeconds + i / SampleRate);
            return samples;
        }

        public double[] Generate(TimeSpan startTime, TimeSpan duration)
        {
            int count = (int)(duration.TotalSeconds * SampleRate);
            return Generate(startTime.TotalSeconds, count);
        }

        public float[] GenerateF(TimeSpan startTime, TimeSpan duration)
        {
            int count = (int)(duration.TotalSeconds * SampleRate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = (float)GetValueAtTime(startTime.TotalSeconds + i / SampleRate);
            return samples;
        }
    }
}
