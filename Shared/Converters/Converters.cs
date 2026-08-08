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

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string category = GetStatusCategory(value as string);
        return category switch
        {
            "Green" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            "Amber" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
            "Red" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            _ => Application.Current?.Resources["SystemControlPageTextBaseMediumBrush"] as Microsoft.UI.Xaml.Media.Brush ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
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
