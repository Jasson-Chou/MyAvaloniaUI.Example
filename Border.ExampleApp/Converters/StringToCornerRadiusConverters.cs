using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Converters
{
    public class StringToCornerRadiusConverters : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string s ? Avalonia.CornerRadius.Parse(s) : Avalonia.CornerRadius.Parse(value?.ToString() ?? "0");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Avalonia.CornerRadius c ? c.ToString() : value?.ToString() ?? "0";
        }
    }
}
