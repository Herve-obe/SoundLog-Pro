using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using Veriflow.Avalonia.ViewModels;

namespace Veriflow.Avalonia.ViewModels
{
    public class EqualToDoubleConverter : IValueConverter
    {
        public double TrueValue { get; set; } = 1.0;
        public double FalseValue { get; set; } = 0.4;
        public MediaViewMode CompareValue { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MediaViewMode mode)
            {
                return mode == CompareValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EqualToBrushConverter : IValueConverter
    {
        public IBrush? ActiveBrush { get; set; }
        public IBrush? InactiveBrush { get; set; }
        public MediaViewMode CompareValue { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MediaViewMode mode)
            {
                return mode == CompareValue ? ActiveBrush : InactiveBrush;
            }
            return InactiveBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
