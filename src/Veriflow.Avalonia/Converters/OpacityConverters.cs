using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Veriflow.Avalonia.ViewModels
{
    /// <summary>
    /// Converts bool to opacity: true=1.0, false=0.2
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.2;
            }
            return 0.2;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts bool to opacity (inverse): true=0.2, false=1.0
    /// </summary>
    public class InverseBoolToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 0.2 : 1.0;
            }
            return 1.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
