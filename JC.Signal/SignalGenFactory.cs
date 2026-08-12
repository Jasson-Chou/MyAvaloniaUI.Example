using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JC.Signal
{
    public enum ESignalType
    {
        Sine,
        //Square,
        //Triangle,
        //Sawtooth
    }
    public interface ISignalGenFactory
    {
        ISignalGeneration Create(ESignalType type, double frequency, double sampleRate, double amplitude);
    }

    public class SignalGenFactory : ISignalGenFactory
    {
        public ISignalGeneration Create(ESignalType type, double frequency, double sampleRate, double amplitude)
        {
            switch (type)
            {
                case ESignalType.Sine:
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
