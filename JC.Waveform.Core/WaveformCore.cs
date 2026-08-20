using System.Drawing;
using System.Runtime.InteropServices;

namespace JC.Waveform.Core
{
    public readonly record struct WaveformTransform(
    float XOffset = 0f,
    float YOffset = 0f,
    float XScale = 1f,
    float YScale = 1f)
    {
        public static WaveformTransform Identity => new();

        public bool Equals(float xOffset, float yOffset, float xScale, float yScale) =>
            XOffset == xOffset && YOffset == yOffset && XScale == xScale && YScale == yScale;
    }
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct WaveformPoint(float X, float Y)
    {
        public static WaveformPoint Zero => new(0f, 0f);
    }

    public readonly record struct ValueRange(float Min, float Max)
    {
        public static ValueRange Zero => new(0f, 0f);
        public float Span => Max - Min;

        public bool Contains(float value) => value >= Min && value <= Max;

        public bool Equals(float min, float max) => Min == min && Max == max;
    }

    /// <summary>
    /// 根據WaveformCore.Build方法的結果，包含了生成的波形點陣列(Points)、實際索引(ActualIndexes)、X軸步長(XStep)以及是否進行了下採樣的資訊(IsDownSampled)。
    /// 建議使用此結構來存儲和傳遞波形生成的結果，以便在後續的繪製或分析中使用。
    /// </summary>
    /// <param name="Points">實際繪製的波形點陣列</param>
    /// <param name="ActualIndexes">實際索引陣列</param>
    /// <param name="XStep">X軸步長</param>
    /// <param name="IsDownSampled">是否進行了下採樣</param>
    public readonly record struct WaveformBuildResult(
    WaveformPoint[] Points,
    int[] ActualIndexes,
    float XStep,
    bool IsDownSampled)
    {
        public static WaveformBuildResult Empty =>
            new(Array.Empty<WaveformPoint>(), Array.Empty<int>(), float.NaN, false);
    }
    public static class WaveformCore
    {
        /// <summary>
        /// 根據參數建置波形點陣列，並返回包含生成結果的WaveformBuildResult結構。
        /// </summary>
        /// <param name="values">輸入實際值陣列</param>
        /// <param name="rect">繪製區域的矩形</param>
        /// <param name="valueRange">預期實際繪製值的範圍</param>
        /// <param name="transform">波形變換</param>
        /// <param name="fixedPointCount">固定點數，如不指定則根據輸入值自動計算</param>
        /// <returns>包含生成結果的WaveformBuildResult結構</returns>
        public static WaveformBuildResult Build(ReadOnlySpan<float> values,
            in RectangleF rect,
            in ValueRange valueRange,
            in WaveformTransform transform,
            int? fixedPointCount = null)
        {
            if(values.Length == 0)
                return WaveformBuildResult.Empty;
            WaveformPoint[] waveformPoints = Array.Empty<WaveformPoint>();
            int[] actualIndexes = Array.Empty<int>();
            float xStep = 0f;
            bool isDownSampled = false;

            float left = rect.Left;
            float top = rect.Top;
            float width = rect.Width;
            float height = rect.Height;

            int canShowColumns = (int)MathF.Ceiling(width);

            float maxValue = valueRange.Max;
            float yOffset = transform.YOffset;

            float yRange = valueRange.Span;
            float yScaleFactor = (yRange != 0) ? (height * transform.YScale / yRange) : 1.0f; // 計算 Y 軸縮放因子
            float getYValue(float value) => top + (maxValue - value) * yScaleFactor - yOffset; // 計算 Y 軸座標

            if (fixedPointCount is not null && fixedPointCount > 0)
            {
                xStep = (float)(width * transform.XScale / (fixedPointCount - 1)); // 計算寬度，當固定點數時，使用 pointCount 來計算 xStep
            }
            else
            {
                xStep = (float)(width * transform.XScale / (values.Length - 1)); // 計算寬度，適合動態點數，使用 n 來計算 xStep
            }

            int startIndex = transform.XOffset > 0 ? (int)(transform.XOffset / xStep) : 0;
            int endIndex = Math.Min(values.Length - 1, startIndex + (int)Math.Ceiling(width / xStep));
            int destCount = endIndex - startIndex + 1;

            if (destCount <= canShowColumns * 2)
            {
                //destCount = Math.Min(destCount, n - startIndex);
                waveformPoints = new WaveformPoint[destCount];
                actualIndexes = new int[destCount];

                for (int i = 0; i < destCount && startIndex + i < values.Length; i++)
                {
                    int valueIndex = startIndex + i;
                    float actualX = valueIndex * xStep - transform.XOffset + left;
                    float y = getYValue(values[valueIndex]);
                    waveformPoints[i] = new WaveformPoint(actualX, y);
                    actualIndexes[i] = valueIndex;
                }
                isDownSampled = false;
            }
            else
            {
                // 採樣顯示，避免過多的點數 (min-max downsampling)
                int sampleRate = (int)Math.Ceiling((double)destCount / canShowColumns);
                waveformPoints = new WaveformPoint[canShowColumns * 2];
                actualIndexes = new int[canShowColumns * 2];
                int sKPointIdx = 0;
                for (int i = 0; i < canShowColumns; i++)
                {
                    int cLIdx = Math.Min(startIndex + (i * sampleRate), values.Length - 1);
                    int cHIdx = Math.Min(cLIdx + sampleRate - 1, values.Length - 1);

                    float cLValue = values[cLIdx];
                    float cHValue = values[cHIdx];

                    for (int j = cLIdx; j <= cHIdx; j++)
                    {
                        if (cLValue > values[j])
                        {
                            cLValue = values[j];
                            cLIdx = j;
                        }

                        if (cHValue < values[j])
                        {
                            cHValue = values[j];
                            cHIdx = j;
                        }
                    }

                    float cLPosX = left + ((cLIdx - startIndex) * xStep);
                    float cHPosX = left + ((cHIdx - startIndex) * xStep);

                    if (cLIdx < cHIdx)
                    {
                        actualIndexes[sKPointIdx] = cLIdx;
                        waveformPoints[sKPointIdx++] = new WaveformPoint(cLPosX, getYValue(cLValue));
                        actualIndexes[sKPointIdx] = cHIdx;
                        waveformPoints[sKPointIdx++] = new WaveformPoint(cHPosX, getYValue(cHValue));
                    }
                    else
                    {
                        actualIndexes[sKPointIdx] = cHIdx;
                        waveformPoints[sKPointIdx++] = new WaveformPoint(cHPosX, getYValue(cHValue));
                        actualIndexes[sKPointIdx] = cLIdx;
                        waveformPoints[sKPointIdx++] = new WaveformPoint(cLPosX, getYValue(cLValue));
                    }

                }
                isDownSampled = true;
            }
            return new WaveformBuildResult(waveformPoints, actualIndexes, xStep, isDownSampled);
        }
    }

    public static class WaveformCoreExtensions
    {
        /// <summary>
        /// 高效地將WaveformPoint陣列轉換為ReadOnlySpan<T>，避免不必要的資料複製。
        /// 須注意AsPoints指向的還是原始的WaveformPoint陣列，若原始陣列被釋放或修改，AsPoints將會失效。
        /// </summary>
        /// <typeparam name="T">目標結構類型</typeparam>
        /// <param name="pts">WaveformPoint陣列</param>
        /// <returns>轉換後的ReadOnlySpan<T></returns>
        public static ReadOnlySpan<T> AsPoints<T>(this WaveformPoint[] pts) where T : struct
        {
            return MemoryMarshal.Cast<WaveformPoint, T>(pts);
        }
    }
}
