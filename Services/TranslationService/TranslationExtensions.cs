using System;

namespace WinCarePro.Services;

public static class TranslationExtensions
{
    public static string T(this string? text)
    {
        return TranslationManager.Instance.T(text);
    }
}
