using System;
using Microsoft.UI.Xaml.Data;

namespace WinCarePro.Services;

/// <summary>
/// XAML ValueConverter for dynamic localized UI binding with TranslationManager.
/// </summary>
public class TranslationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string key)
        {
            return TranslationManager.Instance.T(key);
        }
        return value ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

