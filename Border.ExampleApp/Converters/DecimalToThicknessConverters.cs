using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Converters
{
    public class DecimalToThicknessConverters : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is decimal d ? new Avalonia.Thickness((double)d) : Avalonia.Thickness.Parse(value?.ToString() ?? "0");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Avalonia.Thickness t ? (decimal)t.Left : decimal.Parse(value?.ToString() ?? "0");
        }
    }
}
