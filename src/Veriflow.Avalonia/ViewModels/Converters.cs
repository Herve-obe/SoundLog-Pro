using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia;

namespace Veriflow.Avalonia.ViewModels
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && b) return true; // Binding to IsVisible (bool) in Avalonia, not Visibility enum usually
            // However, legacy WPF might rely on Visibility. 
            // In Avalonia, IsVisible is boolean. But if we bind to 'IsVisible', we return bool.
            // If the XAML expects Visibility, we need to return bool? No, Avalonia controls have IsVisible which is bool.
            // But wait, WPF had Visibility.Visible/Collapsed.
            // If we are migrating XAML, we probably need to check if we are binding to IsVisible or something else.
            // Assuming standard IsVisible binding:
            return value is bool x && x;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    
    // Actually, Avalonia 11 uses IsVisible (bool). 
    // If we want to support "Visibility" for XAML compatibility (maybe XAML uses Visibility property?), we might need a custom converter that returns bool?
    // Let's assume we update XAML to use IsVisible.
    // BUT if the XAML is still standard WPF style `Visibility="{Binding ...}"`, Avalonia XAML compiler might complain if property is IsVisible (bool).
    // I haven't migrated XAML yet. I am building ViewModels.
    // The ViewModels don't use Converters, the Views do. 
    // But `Converters.cs` is here. 

    // Let's implement standard BooleanToVisibility for Avalonia which returns bool for IsVisible
    public class BoolToIsVisibleConverter : IValueConverter
    {
         public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && b;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZeroToVisibleConverter : IValueConverter
    {
         public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Returns TRUE if 0, else FALSE
            if (value is int i && i == 0) return true;
            return false;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    
    // We keep class names compatible if used in VM code, but typically Converters are Resources.
    public static class Converters
    {
        // Expose instances if VM uses them directly (rare)
        public static BooleanToVisibilityConverter BooleanToVisibilityConverter { get; } = new();
        public static ZeroToVisibleConverter ZeroToVisibleConverter { get; } = new();
    }
}
