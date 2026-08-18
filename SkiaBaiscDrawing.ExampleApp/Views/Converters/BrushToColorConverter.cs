using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Views.Converters
{
    public class BrushToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is ISolidColorBrush brush ? brush.Color : AvaloniaProperty.UnsetValue;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Color color ? new SolidColorBrush(color) : AvaloniaProperty.UnsetValue;
    }
}
