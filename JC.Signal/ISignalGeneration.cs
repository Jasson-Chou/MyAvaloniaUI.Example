namespace JC.Signal
{
    public interface ISignalGeneration
    {
        double StartPhase { get; }
        double Frequency { get; }
        double SampleRate { get; }
        double Amplitude { get; }

        double NextSample();
        void Fill(Span<double> buffer);
        void Fill(Span<float> buffer);
        double[] Generate(int count);
        float[] GenerateF(int count);
        void Reset();

        double GetValueAtTime(double timeSeconds);
        double GetValueAtTime(TimeSpan time);
        double[] Generate(double startTimeSeconds, int count);


        double[] Generate(TimeSpan startTime, TimeSpan duration);
        float[] GenerateF(TimeSpan startTime, TimeSpan duration);
    }
}
