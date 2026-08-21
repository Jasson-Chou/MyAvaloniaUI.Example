using JC.Waveform.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JC.Waveform.Core.Tests
{
    [TestClass]
    public class WaveformCoreTests
    {
        // 共用參數：rect(0,0,100,50)、值域(-1,1)
        // → canShowColumns = 100、yScaleFactor = 50*1/2 = 25
        // → y = top + (Max - value) * 25 - YOffset
        private static readonly RectangleF Rect = new(0, 0, 100, 50);
        private static readonly ValueRange Range = new(-1f, 1f);

        // ---------- 1. 邊界與防呆 ----------

        [TestMethod]
        public void Build_EmptyValues_ReturnsEmpty()
        {
            var result = WaveformCore.Build(ReadOnlySpan<float>.Empty, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(0, result.Points.Length);
            Assert.AreEqual(0, result.ActualIndexes.Length);
            Assert.IsTrue(float.IsNaN(result.XStep));   // Empty 定義 XStep = float.NaN
            Assert.IsFalse(result.IsDownSampled);
        }

        [TestMethod]
        public void Build_ZeroSpanRange_YScaleFactorFallsBackToOne()
        {
            // yRange == 0 → yScaleFactor = 1.0 → y = top + (5 - 5) * 1 = 0
            float[] values = { 5f, 5f, 5f, 5f };
            var result = WaveformCore.Build(values, Rect, new ValueRange(5f, 5f), WaveformTransform.Identity);

            Assert.AreEqual(4, result.Points.Length);
            foreach (var p in result.Points)
                Assert.AreEqual(0f, p.Y, 1e-5f);
        }

        [TestMethod]
        public void Build_SingleValue_CurrentBehavior_XStepInfinity_XIsNaN()
        {
            // 現況記錄：length==1 → xStep = width/(1-1) = Infinity
            // X = 0 * Infinity = NaN。日後若修補單點行為，以此測試為驗收條件。
            float[] values = { 0.5f };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(1, result.Points.Length);
            Assert.IsTrue(float.IsInfinity(result.XStep));
            Assert.IsTrue(float.IsNaN(result.Points[0].X));
        }

        // ---------- 2. Y 軸映射 getYValue ----------

        [DataTestMethod]
        [DataRow(1f, 0f)]    // Max → top = 0
        [DataRow(-1f, 50f)]  // Min → top + height = 50
        [DataRow(0f, 25f)]   // 中間值 → 25（螢幕 Y 向下遞增）
        public void Build_YMapping_MatchesFormula(float value, float expectedY)
        {
            float[] values = { value, value };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(expectedY, result.Points[0].Y, 1e-3f);
        }

        [TestMethod]
        public void Build_YOffset_ShiftsAllPointsUp()
        {
            // y = top + (Max - value) * 25 - YOffset → YOffset=10 時整體上移 10
            float[] values = { 0f, 0f };
            var result = WaveformCore.Build(values, Rect, Range, new WaveformTransform(YOffset: 10f));

            Assert.AreEqual(15f, result.Points[0].Y, 1e-3f); // 25 - 10
        }

        // ---------- 3. X 軸步長與 fixedPointCount ----------

        [TestMethod]
        public void Build_DynamicPointCount_XStepUsesValuesLength()
        {
            // xStep = 100 * 1 / (10 - 1) ≈ 11.1111
            float[] values = new float[10];
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(100f / 9f, result.XStep, 1e-3f);
        }

        [TestMethod]
        public void Build_FixedPointCount_OverridesXStep_AndLimitsVisibleRange()
        {
            // fixedPointCount=5 → xStep = 100/4 = 25
            // endIndex = min(9, ceil(100/25)=4) = 4 → destCount = 5（只顯示前 5 點）
            float[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity, fixedPointCount: 5);

            Assert.AreEqual(25f, result.XStep, 1e-3f);
            Assert.AreEqual(5, result.Points.Length);
            Assert.AreEqual(4, result.ActualIndexes[^1]);
        }

        [TestMethod]
        public void Build_XScaleZoomIn_ShowsFewerPoints()
        {
            // XScale=2 → xStep = 200/9 ≈ 22.22
            // endIndex = min(9, ceil(100/22.22)=5) = 5 → destCount = 6
            float[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var result = WaveformCore.Build(values, Rect, Range, new WaveformTransform(XScale: 2f));

            Assert.AreEqual(200f / 9f, result.XStep, 1e-3f);
            Assert.AreEqual(6, result.Points.Length);
        }

        // ---------- 4. 可視範圍裁切（XOffset 平移） ----------

        [TestMethod]
        public void Build_WithXOffset_StartsFromCorrectIndex()
        {
            // xStep ≈ 11.1111、XOffset = 2 * xStep → startIndex = (int)(XOffset/xStep) = 2
            float[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            float xStep = 100f / 9f;
            var result = WaveformCore.Build(values, Rect, Range, new WaveformTransform(XOffset: xStep * 2f));

            Assert.AreEqual(2, result.ActualIndexes[0]);
            // 第一點 X = 2*xStep - XOffset + left = 0，平移後仍從左緣開始
            Assert.AreEqual(0f, result.Points[0].X, 1e-3f);
        }

        [TestMethod]
        public void Build_EndIndexClamped_NeverExceedsDataLength()
        {
            float[] values = { 0, 1, 2 };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            foreach (var i in result.ActualIndexes)
                Assert.IsTrue(i >= 0 && i <= values.Length - 1, $"索引 {i} 超出資料範圍");
        }

        // ---------- 5. 模式切換（destCount vs canShowColumns * 2） ----------

        [TestMethod]
        public void Build_FewPoints_DirectMode_SequentialIndexes()
        {
            // destCount = 10 ≤ 100*2 → 直接模式，索引連續遞增
            float[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.IsFalse(result.IsDownSampled);
            Assert.AreEqual(10, result.Points.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, result.ActualIndexes[i]);
        }

        [TestMethod]
        public void Build_ManyPoints_DownSampled_OutputIsCanShowColumnsTimesTwo()
        {
            // rect 寬 10 → canShowColumns = 10；1000 點 > 20 → 下採樣
            // sampleRate = ceil(1000/10) = 100，輸出固定 20 點
            float[] values = new float[1000];
            for (int i = 0; i < values.Length; i++) values[i] = MathF.Sin(i * 0.1f);

            var result = WaveformCore.Build(values, new RectangleF(0, 0, 10, 50), Range, WaveformTransform.Identity);

            Assert.IsTrue(result.IsDownSampled);
            Assert.AreEqual(20, result.Points.Length);
        }

        // ---------- 6. Min-Max 下採樣正確性 ----------

        [TestMethod]
        public void Build_DownSampled_PreservesMinAndMax()
        {
            // rect 寬 1 → canShowColumns = 1，5 點 > 2 → 下採樣，單一欄涵蓋全部資料
            // min = -0.9 (idx1)、max = 0.8 (idx3)；cLIdx(1) < cHIdx(3) → 先 min 後 max
            float[] values = { 0.2f, -0.9f, 0.5f, 0.8f, -0.1f };
            var result = WaveformCore.Build(values, new RectangleF(0, 0, 1, 50), Range, WaveformTransform.Identity);

            Assert.IsTrue(result.IsDownSampled);
            Assert.AreEqual(2, result.Points.Length);
            Assert.AreEqual(1, result.ActualIndexes[0]); // min 的索引
            Assert.AreEqual(3, result.ActualIndexes[1]); // max 的索引
        }

        [TestMethod]
        public void Build_DownSampled_OrderedByIndex_MaxBeforeMin()
        {
            // max = 0.9 (idx1)、min = -0.9 (idx2)；cLIdx(2) > cHIdx(1)
            // → 走 else 分支：max 先輸出、min 後輸出（依索引先後，保留波形走向）
            float[] values = { 0f, 0.9f, -0.9f, 0f, 0f };
            var result = WaveformCore.Build(values, new RectangleF(0, 0, 2, 50), Range, WaveformTransform.Identity);

            Assert.AreEqual(1, result.ActualIndexes[0]);
            Assert.AreEqual(2, result.ActualIndexes[1]);
        }

        [TestMethod]
        public void Build_DownSampled_ConstantValues_TakesFirstAndLastIndex()
        {
            // 全部等值時內層迴圈不更新（僅嚴格大於/小於才更新）
            // → cLIdx 停在區間首(0)、cHIdx 停在區間尾(2)
            float[] values = { 1f, 1f, 1f };
            var result = WaveformCore.Build(values, new RectangleF(0, 0, 1, 50), Range, WaveformTransform.Identity);

            Assert.IsTrue(result.IsDownSampled);
            Assert.AreEqual(0, result.ActualIndexes[0]);
            Assert.AreEqual(2, result.ActualIndexes[1]);
        }

        // ---------- 7. AsPoints 零複製轉型 ----------

        private readonly record struct TestPoint(float X, float Y); // 與 WaveformPoint 同佈局

        [TestMethod]
        public void AsPoints_Cast_PreservesCoordinates()
        {
            var pts = new[] { new WaveformPoint(1f, 2f), new WaveformPoint(3f, 4f) };

            ReadOnlySpan<TestPoint> casted = pts.AsPoints<TestPoint>();

            Assert.AreEqual(2, casted.Length);
            Assert.AreEqual(1f, casted[0].X);
            Assert.AreEqual(2f, casted[0].Y);
            Assert.AreEqual(3f, casted[1].X);
            Assert.AreEqual(4f, casted[1].Y);
        }

        [TestMethod]
        public void AsPoints_ZeroCopy_ReflectsSourceMutation()
        {
            // MemoryMarshal.Cast 為 reference 語意：改原陣列，Span 內容跟著變
            var pts = new[] { new WaveformPoint(1f, 2f) };
            var casted = pts.AsPoints<TestPoint>();

            pts[0] = new WaveformPoint(9f, 9f);

            Assert.AreEqual(9f, casted[0].X);
        }

        // ---------- 8. 隨機資料 invariant ----------

        [TestMethod]
        public void Build_RandomData_PointsWithinRect_IndexesValid()
        {
            // 5000 點、寬 100 → destCount = 5000 > 200 → 下採樣路徑
            var rng = new Random(42);
            float[] values = Enumerable.Range(0, 5000)
                .Select(_ => (float)(rng.NextDouble() * 2 - 1))
                .ToArray();

            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.IsTrue(result.IsDownSampled);
            foreach (var p in result.Points)
            {
                Assert.IsTrue(p.X >= -0.01f && p.X <= 100.01f, $"X={p.X} 超出範圍");
                Assert.IsTrue(p.Y >= -0.01f && p.Y <= 50.01f, $"Y={p.Y} 超出範圍");
            }
            foreach (var i in result.ActualIndexes)
                Assert.IsTrue(i >= 0 && i < values.Length, $"索引 {i} 超出範圍");
        }
    }
}