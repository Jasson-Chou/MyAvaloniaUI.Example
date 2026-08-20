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
        // 共用測試參數
        private static readonly RectangleF Rect = new(0, 0, 100, 50);
        private static readonly ValueRange Range = new(-1f, 1f);

        // ---------- 1. 邊界與防呆 ----------

        [TestMethod]
        public void Build_EmptyValues_ReturnsEmpty()
        {
            var result = WaveformCore.Build(ReadOnlySpan<float>.Empty, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(0, result.Points.Length);
            Assert.AreEqual(0, result.ActualIndexes.Length);
            Assert.IsTrue(float.IsNaN(result.XStep));
            Assert.IsFalse(result.IsDownSampled);
        }

        [TestMethod]
        public void Build_ConstantValues_DoesNotThrow_AllYSame()
        {
            // Span == 0 時 yScaleFactor 退化為 1.0，驗證不拋例外且 Y 一致
            float[] values = { 5f, 5f, 5f, 5f };
            var result = WaveformCore.Build(values, Rect, new ValueRange(5f, 5f), WaveformTransform.Identity);

            Assert.AreEqual(4, result.Points.Length);
            foreach (var p in result.Points)
                Assert.AreEqual(result.Points[0].Y, p.Y, 1e-5f);
        }

        [TestMethod]
        public void Build_SingleValue_CurrentBehavior_XStepIsInfinity()
        {
            // 記錄現況：length==1 時分母為 0，xStep 為 Infinity。
            // 日後修補此行為時，此測試即為驗收條件。
            float[] values = { 0.5f };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(1, result.Points.Length);
            Assert.IsTrue(float.IsInfinity(result.XStep));
        }

        // ---------- 2. Y 軸映射 ----------

        [DataTestMethod]
        [DataRow(1f, 0f)]    // Max -> rect.Top
        [DataRow(-1f, 50f)]  // Min -> rect.Top + height
        [DataRow(0f, 25f)]   // 中間值 -> 線性對應
        public void Build_YMapping_MatchesFormula(float value, float expectedY)
        {
            float[] values = { value, value }; // 兩點避免 length-1 特例
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.AreEqual(expectedY, result.Points[0].Y, 1e-3f);
        }

        // ---------- 3. 可視範圍裁切 ----------

        [TestMethod]
        public void Build_WithXOffset_StartsFromCorrectIndex()
        {
            // rect 寬 100、10 筆資料 → xStep = 100/9 ≈ 11.11
            // XOffset = 2 * xStep → startIndex = 2
            float[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var transform = new WaveformTransform(XOffset: 100f / 9f * 2f);

            var result = WaveformCore.Build(values, Rect, Range, transform);

            Assert.AreEqual(2, result.ActualIndexes[0]);
        }

        [TestMethod]
        public void Build_EndIndexClamped_NeverExceedsDataLength()
        {
            float[] values = { 0, 1, 2 };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            foreach (var i in result.ActualIndexes)
            {
                Assert.IsTrue(i >= 0 && i <= values.Length - 1,
                    $"索引 {i} 超出資料範圍");
            }
        }

        // ---------- 4. 模式切換 ----------

        [TestMethod]
        public void Build_FewPoints_DirectMode_SequentialIndexes()
        {
            // rect 寬 100 → canShowColumns = 100，10 點 < 200 → 直接模式
            float[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            Assert.IsFalse(result.IsDownSampled);
            Assert.AreEqual(values.Length, result.Points.Length);
            for (int i = 0; i < values.Length; i++)
                Assert.AreEqual(i, result.ActualIndexes[i]);
        }

        [TestMethod]
        public void Build_ManyPoints_DownSampled_FixedOutputCount()
        {
            // rect 寬 10 → canShowColumns = 10，1000 點 > 20 → 下採樣
            float[] values = new float[1000];
            for (int i = 0; i < values.Length; i++) values[i] = MathF.Sin(i * 0.1f);

            var result = WaveformCore.Build(values, new RectangleF(0, 0, 10, 50), Range, WaveformTransform.Identity);

            Assert.IsTrue(result.IsDownSampled);
            Assert.AreEqual(20, result.Points.Length); // canShowColumns * 2
        }

        // ---------- 5. Min-Max 下採樣正確性 ----------

        // 每筆案例：資料陣列、該欄預期最小值、預期最大值
        public static IEnumerable<object[]> DownSampleCases
        {
            get
            {
                yield return new object[] { new float[] { 0.2f, -0.9f, 0.5f, 0.8f, -0.1f }, -0.9f, 0.8f };
                yield return new object[] { new float[] { 1f, 1f, 1f }, 1f, 1f };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(DownSampleCases), DynamicDataSourceType.Property)]
        public void Build_DownSampled_PreservesMinMax(float[] values, float expectedMin, float expectedMax)
        {
            // 寬度 1 → canShowColumns = 1 → 輸出 2 點（該欄的 min 與 max）
            var result = WaveformCore.Build(values, new RectangleF(0, 0, 1, 50), Range, WaveformTransform.Identity);

            Assert.IsTrue(result.IsDownSampled);
            Assert.AreEqual(2, result.Points.Length);

            float[] actuals =
            {
                values[result.ActualIndexes[0]],
                values[result.ActualIndexes[1]],
            };
            CollectionAssert.Contains(actuals, expectedMin);
            CollectionAssert.Contains(actuals, expectedMax);
        }

        [TestMethod]
        public void Build_DownSampled_PointsOrderedByIndex_NotByValue()
        {
            // 峰值在索引 1、谷值在索引 2 → max 點應排在 min 點之前（依索引排序）
            float[] values = { 0f, 0.9f, -0.9f, 0f, 0f };

            var result = WaveformCore.Build(values, new RectangleF(0, 0, 1, 50), Range, WaveformTransform.Identity);

            Assert.AreEqual(1, result.ActualIndexes[0]); // max 先出現
            Assert.AreEqual(2, result.ActualIndexes[1]); // min 後出現
        }

        // ---------- 6. AsPoints 轉型 ----------

        // 與 WaveformPoint 相同記憶體佈局的測試用結構
        private readonly record struct TestPoint(float X, float Y);

        [TestMethod]
        public void AsPoints_Cast_PreservesCoordinates()
        {
            var pts = new[]
            {
                new WaveformPoint(1f, 2f),
                new WaveformPoint(3f, 4f),
            };

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
            // 記錄 reference 語意：修改原陣列，Span 內容跟著變
            var pts = new[] { new WaveformPoint(1f, 2f) };
            var casted = pts.AsPoints<TestPoint>();

            pts[0] = new WaveformPoint(9f, 9f);

            Assert.AreEqual(9f, casted[0].X);
        }

        // ---------- 7. 屬性型 invariant（隨機資料） ----------

        [TestMethod]
        public void Build_RandomData_AllPointsWithinRectHorizontally()
        {
            var rng = new Random(42);
            float[] values = Enumerable.Range(0, 5000)
                .Select(_ => (float)(rng.NextDouble() * 2 - 1))
                .ToArray();

            var result = WaveformCore.Build(values, Rect, Range, WaveformTransform.Identity);

            foreach (var p in result.Points)
            {
                Assert.IsTrue(p.X >= Rect.Left - 0.01f && p.X <= Rect.Left + Rect.Width + 0.01f,
                    $"X={p.X} 超出繪製區域");
                Assert.IsTrue(p.Y >= Rect.Top - 0.01f && p.Y <= Rect.Top + Rect.Height + 0.01f,
                    $"Y={p.Y} 超出繪製區域");
            }
            foreach (var i in result.ActualIndexes)
                Assert.IsTrue(i >= 0 && i <= values.Length - 1, $"索引 {i} 超出資料範圍");
        }
    }
}