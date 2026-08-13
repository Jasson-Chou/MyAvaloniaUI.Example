namespace JC.Units
{
    public enum SiPrefix
    {
        Femto = -15, // f
        Pico = -12, // p
        Nano = -9,  // n
        Micro = -6,  // u
        Milli = -3,  // m
        None = 0,
        Kilo = 3,   // k
        Mega = 6,   // M
        Giga = 9,   // G
        Tera = 12,  // T
    }

    /// <summary>
    /// 以基準值儲存、支援手動選擇前綴與 Auto 自動換算的數值類別。
    /// </summary>
    public sealed class SiScaledValue
    {
        private const int MinExp = (int)SiPrefix.Femto;
        private const int MaxExp = (int)SiPrefix.Tera;

        private int _significantDigits = 4;

        /// <summary>基準值（無前綴），唯一的真值來源。</summary>
        public double BaseValue { get; private set; }

        /// <summary>是否為 Auto 模式。</summary>
        public bool IsAuto { get; private set; }

        /// <summary>手動模式下使用者選定的前綴。</summary>
        public SiPrefix ManualPrefix { get; private set; } = SiPrefix.None;

        /// <summary>Auto 模式的有效位數（1~15）。</summary>
        public int SignificantDigits
        {
            get => _significantDigits;
            set
            {
                if (value is < 1 or > 15)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _significantDigits = value;
            }
        }

        public SiScaledValue(double baseValue = 0.0, SiPrefix prefix = SiPrefix.None)
        {
            BaseValue = baseValue;
            ManualPrefix = prefix;
        }

        /// <summary>目前實際生效的前綴（Auto 模式下即時計算）。</summary>
        public SiPrefix EffectivePrefix => IsAuto ? ResolveAutoPrefix(BaseValue) : ManualPrefix;

        /// <summary>依目前前綴換算後的顯示數值。</summary>
        public double DisplayValue => BaseValue / Math.Pow(10, (int)EffectivePrefix);

        /// <summary>切換到指定前綴（手動模式）。真值不變，僅改變顯示換算。</summary>
        public void SwitchTo(SiPrefix prefix)
        {
            IsAuto = false;
            ManualPrefix = prefix;
        }

        /// <summary>切換到 Auto 模式。</summary>
        public void SwitchToAuto() => IsAuto = true;

        /// <summary>以「目前單位」寫入數值，內部換算回基準值。</summary>
        public void SetDisplayValue(double displayValue)
        {
            BaseValue = displayValue * Math.Pow(10, (int)EffectivePrefix);
        }

        /// <summary>直接寫入基準值。</summary>
        public void SetBaseValue(double baseValue) => BaseValue = baseValue;

        /// <summary>依有效位數選出最適前綴（含進位邊界處理）。</summary>
        private SiPrefix ResolveAutoPrefix(double value)
        {
            if (value == 0 || double.IsNaN(value) || double.IsInfinity(value))
                return SiPrefix.None;

            double abs = Math.Abs(value);

            // 工程記號：取 3 的倍數指數，並限制在 f~T 範圍內。
            int exp = (int)Math.Floor(Math.Log10(abs) / 3.0) * 3;
            exp = Math.Clamp(exp, MinExp, MaxExp);

            // 進位邊界：依有效位數四捨五入後，尾數可能達到 1000（如 999.96 → 1000）。
            double mantissa = abs / Math.Pow(10, exp);
            double rounded = RoundToSignificant(mantissa, SignificantDigits);
            if (rounded >= 1000.0 && exp < MaxExp)
                exp += 3;

            return (SiPrefix)exp;
        }

        private static double RoundToSignificant(double value, int digits)
        {
            if (value == 0) return 0;
            int magnitude = (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;
            double scale = Math.Pow(10, digits - magnitude);
            return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
        }

        private static string GetSymbol(SiPrefix prefix) => prefix switch
        {
            SiPrefix.Femto => "f",
            SiPrefix.Pico => "p",
            SiPrefix.Nano => "n",
            SiPrefix.Micro => "u",
            SiPrefix.Milli => "m",
            SiPrefix.None => "",
            SiPrefix.Kilo => "k",
            SiPrefix.Mega => "M",
            SiPrefix.Giga => "G",
            SiPrefix.Tera => "T",
            _ => throw new ArgumentOutOfRangeException(nameof(prefix)),
        };

        /// <summary>格式化輸出，如 "12.34 m"。</summary>
        public override string ToString()
        {
            string number = DisplayValue.ToString("G" + SignificantDigits);
            string symbol = GetSymbol(EffectivePrefix);
            return symbol.Length > 0 ? $"{number} {symbol}" : number;
        }
    }
}
