using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Veriflow.Desktop.ViewModels
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return Visibility.Visible;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZeroToVisibleConverter : IValueConverter
    {
         public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && i == 0) return Visibility.Visible;
            // Also handle double or long if needed, but Count is int
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public static class Converters
    {
        public static BooleanToVisibilityConverter BooleanToVisibilityConverter { get; } = new();
        public static ZeroToVisibleConverter ZeroToVisibleConverter { get; } = new();
    }
}
