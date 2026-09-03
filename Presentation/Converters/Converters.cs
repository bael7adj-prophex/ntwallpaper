namespace NTWallpaper.Presentation.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NTWallpaper.Domain.Enums;

/// <summary>True → Visible, False → Collapsed.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>Inverse: True → Collapsed, False → Visible.</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v != Visibility.Visible;
}

/// <summary>Null → Collapsed, non-null → Visible.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Formats a byte count as a human-readable size.</summary>
public class ByteSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        var bytes = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (bytes < 1024) return $"{bytes:0} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:0.0} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024 * 1024):0.0} MB";
        return $"{bytes / (1024d * 1024 * 1024):0.00} GB";
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Converts an AppState enum to a colour for status pills.</summary>
public class AppStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            AppState.Searching or AppState.Downloading or AppState.Applying or AppState.Validating => "AccentBrush",
            AppState.UsingCache => "AccentHoverBrush",
            AppState.Applied => "SuccessBrush",
            AppState.Failed => "DangerBrush",
            AppState.Paused => "ForegroundMutedBrush",
            _ => "ForegroundMutedBrush"
        };
        return Application.Current.TryFindResource(key) ?? System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
