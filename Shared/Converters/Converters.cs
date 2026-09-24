using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinCarePro;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && !b) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

public class StatusToBrushConverter : IValueConverter
{
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? _greenBrush;
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? _amberBrush;
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? _redBrush;
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? _defaultGrayBrush;

    private static Microsoft.UI.Xaml.Media.SolidColorBrush? SafeCreateBrush(Windows.UI.Color color)
    {
        try { return new Microsoft.UI.Xaml.Media.SolidColorBrush(color); }
        catch { return null; }
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush? GreenBrush => _greenBrush ??= SafeCreateBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? AmberBrush => _amberBrush ??= SafeCreateBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? RedBrush => _redBrush ??= SafeCreateBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? DefaultGrayBrush => _defaultGrayBrush ??= SafeCreateBrush(Microsoft.UI.Colors.Gray);

    public static string GetStatusCategory(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "Default";

        string lower = status.ToLower();
        if (lower.Contains("success") || lower.Contains("done") || lower.Contains("healthy") || lower.Contains("completed") || lower.Contains("optimized") || lower.Contains("thành công") || lower.Contains("hoàn tất") || lower.Contains("tối ưu") || lower.Contains("tốt"))
            return "Green";
        if (lower.Contains("warn") || lower.Contains("warning") || lower.Contains("cảnh báo") || lower.Contains("lưu ý"))
            return "Amber";
        if (lower.Contains("fail") || lower.Contains("error") || lower.Contains("critical") || lower.Contains("thất bại") || lower.Contains("lỗi") || lower.Contains("nghiêm trọng"))
            return "Red";
        return "Default";
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        string category = GetStatusCategory(value as string);
        return category switch
        {
            "Green" => GreenBrush,
            "Amber" => AmberBrush,
            "Red" => RedBrush,
            _ => (Application.Current?.Resources["SystemControlPageTextBaseMediumBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? DefaultGrayBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is string s && !string.IsNullOrWhiteSpace(s)) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class HexToBrushConverter : IValueConverter
{
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? _transparentBrush;
    private static Microsoft.UI.Xaml.Media.SolidColorBrush? TransparentBrush => _transparentBrush ??= SafeCreateBrush(Microsoft.UI.Colors.Transparent);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Microsoft.UI.Xaml.Media.SolidColorBrush> BrushCache = new();

    private static Microsoft.UI.Xaml.Media.SolidColorBrush? SafeCreateBrush(Windows.UI.Color color)
    {
        try { return new Microsoft.UI.Xaml.Media.SolidColorBrush(color); }
        catch { return null; }
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            if (BrushCache.TryGetValue(hex, out var cached)) return cached;

            try
            {
                string cleanHex = hex.TrimStart('#');
                byte a = 255, r = 0, g = 0, b = 0;
                if (cleanHex.Length == 8)
                {
                    a = System.Convert.ToByte(cleanHex.Substring(0, 2), 16);
                    r = System.Convert.ToByte(cleanHex.Substring(2, 2), 16);
                    g = System.Convert.ToByte(cleanHex.Substring(4, 2), 16);
                    b = System.Convert.ToByte(cleanHex.Substring(6, 2), 16);
                }
                else if (cleanHex.Length == 6)
                {
                    r = System.Convert.ToByte(cleanHex.Substring(0, 2), 16);
                    g = System.Convert.ToByte(cleanHex.Substring(2, 2), 16);
                    b = System.Convert.ToByte(cleanHex.Substring(4, 2), 16);
                }

                var brush = SafeCreateBrush(Windows.UI.Color.FromArgb(a, r, g, b));
                if (brush != null)
                {
                    if (BrushCache.Count < 64)
                    {
                        BrushCache[hex] = brush;
                    }
                    return brush;
                }
            }
            catch { }
        }
        return TransparentBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
    }
}

