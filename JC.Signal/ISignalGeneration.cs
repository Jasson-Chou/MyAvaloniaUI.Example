namespace JC.Signal
{
    public interface ISignalGeneration
    {
        float[] GenerateF(TimeSpan startTime, TimeSpan duration);

        float[] GenerateF(int count);
    }
}
