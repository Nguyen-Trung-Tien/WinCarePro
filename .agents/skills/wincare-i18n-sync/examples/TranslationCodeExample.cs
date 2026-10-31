using WinCarePro.Services;

namespace WinCarePro.Examples.I18n;

/// <summary>
/// Reference example demonstrating programmatic retrieval of translated strings.
/// Invariants:
/// - NEVER hardcode Vietnamese or English text directly in code.
/// - Retrieve via TranslationManager.Instance.GetString(key).
/// </summary>
public static class TranslationCodeExample
{
    public static string GetFormattedStatus(int count)
    {
        string template = TranslationManager.Instance.GetString("Items Cleaned Successfully");
        return $"{count} {template}";
    }

    public static string GetCurrentLanguageName()
    {
        return TranslationManager.Instance.CurrentLanguage switch
        {
            "vi" => "Tiếng Việt",
            "en" => "English",
            _ => "English"
        };
    }
}

