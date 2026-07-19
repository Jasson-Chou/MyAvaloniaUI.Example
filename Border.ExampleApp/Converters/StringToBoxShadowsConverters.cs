using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Converters
{
    public class StringToBoxShadowsConverters : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string text ||
            string.IsNullOrWhiteSpace(text))
            {
                return default(BoxShadows);
            }

            try
            {
                return BoxShadows.Parse(text);
            }
            catch (FormatException)
            {
                return default(BoxShadows);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is BoxShadows shadows
            ? shadows.ToString()
            : BindingOperations.DoNothing;
        }
    }
}
