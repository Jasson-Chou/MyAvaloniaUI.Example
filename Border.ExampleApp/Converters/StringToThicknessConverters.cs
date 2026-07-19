using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Converters
{
    public class StringToThicknessConverters : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is not string text)
            {
                return BindingOperations.DoNothing;
            }

            try
            {
                return Avalonia.Thickness.Parse(text);
            }
            catch
            {
                return BindingOperations.DoNothing;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Avalonia.Thickness thickness ? thickness.ToString() : BindingOperations.DoNothing;
        }
    }
}
