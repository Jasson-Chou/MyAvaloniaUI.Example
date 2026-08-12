namespace JC.Waveform.Skia.Core
{

    public readonly record struct WaveformTransform(
    float XOffset = 0f,
    float YOffset = 0f,
    float XScale = 1f,
    float YScale = 1f)
    {
        public static WaveformTransform Identity => new();
    }

    public readonly record struct ValueRange(float Min, float Max)
    {
        public float Span => Max - Min;
    }

    public readonly record struct WaveformBuildResult<T>(
    T[] Points,
    int[] ActualIndexes,
    float XStep,
    bool IsDownSampled)
    {
        public static WaveformBuildResult<T> Empty =>
            new(Array.Empty<T>(), Array.Empty<int>(), float.NaN, false);
    }

    public static class Class1
    {

    }
}
