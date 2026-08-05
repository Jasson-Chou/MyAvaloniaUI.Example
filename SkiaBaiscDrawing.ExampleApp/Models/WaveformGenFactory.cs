using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models
{
    public enum WaveformType
    {
        Sine,
        //Square,
        //Triangle,
        //Sawtooth
    }
    public interface IWaveformGenFactory
    {
        IWaveformSimulator Create(WaveformType type, double frequency, double sampleRate, double amplitude);
    }

    public class WaveformGenFactory : IWaveformGenFactory
    {
        public IWaveformSimulator Create(WaveformType type, double frequency, double sampleRate, double amplitude)
        {
            switch(type)
            {
                case WaveformType.Sine:
                    return new SineGenerator(frequency, sampleRate, amplitude);
                //case WaveformType.Square:
                //    return new SquareGenerator(frequency, sampleRate, amplitude);
                //case WaveformType.Triangle:
                //    return new TriangleGenerator(frequency, sampleRate, amplitude);
                //case WaveformType.Sawtooth:
                //    return new SawtoothGenerator(frequency, sampleRate, amplitude);
                default:
                    throw new NotImplementedException($"Waveform type {type} is not implemented.");
            }
        }
    }
}
