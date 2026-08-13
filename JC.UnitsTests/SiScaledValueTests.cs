using Microsoft.VisualStudio.TestTools.UnitTesting;
using JC.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JC.Units.Tests
{

    [TestClass]
    public class SiScaledValueTests
    {
        private const double Tolerance = 1e-12;

        // ---------- 1. 手動模式換算 ----------

        [TestMethod]
        public void SwitchTo_ChangesPrefix_BaseValueUnchanged()
        {
            var v = new SiScaledValue(0.0015);

            v.SwitchTo(SiPrefix.Milli);
            v.SwitchTo(SiPrefix.Micro);

            Assert.AreEqual(0.0015, v.BaseValue, Tolerance);
            Assert.AreEqual(SiPrefix.Micro, v.EffectivePrefix);
        }

        [DataTestMethod]
        [DataRow(0.0015, SiPrefix.Milli, 1.5)]
        [DataRow(0.0015, SiPrefix.Micro, 1500.0)]
        [DataRow(0.0015, SiPrefix.None, 0.0015)]
        [DataRow(4700.0, SiPrefix.Kilo, 4.7)]
        [DataRow(2.5e9, SiPrefix.Giga, 2.5)]
        [DataRow(3.3e-14, SiPrefix.Femto, 33.0)]
        public void DisplayValue_ConvertsByPrefix(
            double baseValue, SiPrefix prefix, double expected)
        {
            var v = new SiScaledValue(baseValue);
            v.SwitchTo(prefix);

            Assert.AreEqual(expected, v.DisplayValue, Math.Abs(expected) * 1e-12);
        }

        [TestMethod]
        public void SwitchTo_RoundTrip_NoAccumulatedError()
        {
            var v = new SiScaledValue(1.23456789e-7);

            // 來回切換多次，真值不得漂移
            for (int i = 0; i < 1000; i++)
            {
                v.SwitchTo(SiPrefix.Nano);
                v.SwitchTo(SiPrefix.Tera);
                v.SwitchTo(SiPrefix.Femto);
            }

            Assert.AreEqual(1.23456789e-7, v.BaseValue, Tolerance);
        }

        // ---------- 2. 輸入換算 ----------

        [DataTestMethod]
        [DataRow(SiPrefix.Milli, 1.5, 0.0015)]
        [DataRow(SiPrefix.Kilo, 4.7, 4700.0)]
        [DataRow(SiPrefix.None, 2.0, 2.0)]
        [DataRow(SiPrefix.Pico, 250.0, 2.5e-10)]
        public void SetDisplayValue_WritesBackAsBaseValue(
            SiPrefix prefix, double input, double expectedBase)
        {
            var v = new SiScaledValue();
            v.SwitchTo(prefix);

            v.SetDisplayValue(input);

            Assert.AreEqual(expectedBase, v.BaseValue, Math.Abs(expectedBase) * 1e-12);
        }

        [TestMethod]
        public void SetDisplayValue_ThenDisplayValue_IsInverse()
        {
            var v = new SiScaledValue();
            v.SwitchTo(SiPrefix.Micro);

            v.SetDisplayValue(123.456);

            Assert.AreEqual(123.456, v.DisplayValue, 1e-9);
        }

        // ---------- 3. Auto 前綴選擇 ----------

        [DataTestMethod]
        [DataRow(4.7e-15, SiPrefix.Femto)]
        [DataRow(2.2e-10, SiPrefix.Pico)]   // 220 p
        [DataRow(4.7e-6, SiPrefix.Micro)]
        [DataRow(0.0015, SiPrefix.Milli)]
        [DataRow(2.0, SiPrefix.None)]
        [DataRow(999.0, SiPrefix.None)]
        [DataRow(1000.0, SiPrefix.Kilo)]
        [DataRow(4.7e6, SiPrefix.Mega)]
        [DataRow(2.5e9, SiPrefix.Giga)]
        [DataRow(1.0e12, SiPrefix.Tera)]
        public void AutoMode_SelectsExpectedPrefix(double baseValue, SiPrefix expected)
        {
            var v = new SiScaledValue(baseValue);
            v.SwitchToAuto();

            Assert.AreEqual(expected, v.EffectivePrefix);
        }

        [TestMethod]
        public void AutoMode_NegativeValue_UsesAbsoluteMagnitude()
        {
            var v = new SiScaledValue(-0.0047);
            v.SwitchToAuto();

            Assert.AreEqual(SiPrefix.Milli, v.EffectivePrefix);
            Assert.AreEqual(-4.7, v.DisplayValue, Tolerance);
        }

        [TestMethod]
        public void AutoMode_Zero_ReturnsNonePrefix()
        {
            var v = new SiScaledValue(0.0);
            v.SwitchToAuto();

            Assert.AreEqual(SiPrefix.None, v.EffectivePrefix);
            Assert.AreEqual(0.0, v.DisplayValue, Tolerance);
        }

        // ---------- 4. 邊界條件 ----------

        [TestMethod]
        public void AutoMode_RoundingBoundary_CarriesToNextPrefix()
        {
            // 999.96 有效位數 4 → 四捨五入為 1000 → 應進位到 None 顯示 1
            var v = new SiScaledValue(0.99996) { SignificantDigits = 4 };
            v.SwitchToAuto();

            Assert.AreEqual(SiPrefix.None, v.EffectivePrefix);
            Assert.AreEqual(0.99996, v.DisplayValue, Tolerance);
        }

        [TestMethod]
        public void AutoMode_RoundingBoundary_NoCarryWhenDigitsSufficient()
        {
            // 有效位數 5 時 999.96 不進位，維持 Milli
            var v = new SiScaledValue(0.99996) { SignificantDigits = 5 };
            v.SwitchToAuto();

            Assert.AreEqual(SiPrefix.Milli, v.EffectivePrefix);
        }

        [DataTestMethod]
        [DataRow(1.0e-18, SiPrefix.Femto)] // 低於 f，鉗制在 Femto
        [DataRow(1.0e15, SiPrefix.Tera)]   // 高於 T，鉗制在 Tera
        public void AutoMode_OutOfRange_ClampsToBoundary(
            double baseValue, SiPrefix expected)
        {
            var v = new SiScaledValue(baseValue);
            v.SwitchToAuto();

            Assert.AreEqual(expected, v.EffectivePrefix);
        }

        [DataTestMethod]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(double.NegativeInfinity)]
        public void AutoMode_NonFiniteValue_FallsBackToNone(double baseValue)
        {
            var v = new SiScaledValue(baseValue);
            v.SwitchToAuto();
            
            Assert.AreEqual(SiPrefix.None, v.EffectivePrefix);
        }

        // ---------- 5. 參數驗證 ----------

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(16)]
        [DataRow(-1)]
        public void SignificantDigits_OutOfRange_Throws(int digits)
        {
            var v = new SiScaledValue();

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => v.SignificantDigits = digits);
        }

        // ---------- 6. 格式化輸出 ----------

        [DataTestMethod]
        [DataRow(0.0047, "4.7 m")]
        [DataRow(4700.0, "4.7 k")]
        [DataRow(2.5e9, "2.5 G")]
        [DataRow(2.0, "2")]          // None 前綴無符號、無尾空白
        public void ToString_FormatsWithSymbol(double baseValue, string expected)
        {
            var v = new SiScaledValue(baseValue);
            v.SwitchToAuto();

            Assert.AreEqual(expected, v.ToString());
        }

        [DataTestMethod]
        [DataRow(0.00123456, "1.23 m")]
        public void ToString_RespectsSignificantDigits(double baseValue, string expected)
        {
            var v = new SiScaledValue(baseValue) { SignificantDigits = 3 };
            v.SwitchToAuto();

            Assert.AreEqual(expected, v.ToString());
        }
    }
}