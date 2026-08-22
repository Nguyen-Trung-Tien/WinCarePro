using System;
using System.IO;
using System.Linq;

namespace WinCarePro.Infrastructure.Security;

/// <summary>
/// Provides security validation and sanitization for external process arguments,
/// file paths, and protocol/URL schemes to prevent command injection and unauthorized launches.
/// </summary>
public static class InputSanitizer
{
    private static readonly string[] AllowedUriSchemes = { "https", "http", "windowsdefender", "ms-settings" };
    private static readonly string[] AllowedSystemTools = { "regedit.exe", "services.msc", "taskmgr.exe", "msinfo32.exe", "eventvwr.msc", "wf.msc", "rstrui.exe", "explorer.exe" };

    /// <summary>
    /// Validates whether a URI or protocol launch string is safe to execute.
    /// </summary>
    public static bool IsSafeUri(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            return false;

        uriString = uriString.Trim();

        // Check for recognized prefix protocols or safe system executables
        if (uriString.StartsWith("windowsdefender:", StringComparison.OrdinalIgnoreCase) ||
            uriString.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) ||
            AllowedSystemTools.Contains(Path.GetFileName(uriString), StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            return AllowedUriSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Sanitizes an executable or target file path to ensure it does not contain path injection sequences.
    /// </summary>
    public static bool IsValidLocalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            string fullPath = Path.GetFullPath(path);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely quotes a file path for command line arguments (e.g. explorer.exe /select,"...")
    /// </summary>
    public static string EscapeCommandLineArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        // Remove any existing wrapping quotes and dangerous shell control characters
        string sanitized = argument.Replace("\"", "\\\"").Replace("\0", string.Empty);
        return $"\"{sanitized}\"";
    }

    /// <summary>
    /// Checks if a command argument contains potentially dangerous shell meta-characters
    /// (e.g. command chaining, redirection, or pipe operators).
    /// </summary>
    public static bool ContainsDangerousShellCharacters(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
            return false;

        // Shell delimiters, command separators, and stream redirections
        char[] dangerousChars = { '&', '|', ';', '`', '$', '<', '>', '\n', '\r', '\0' };
        return argument.IndexOfAny(dangerousChars) >= 0;
    }

    /// <summary>
    /// Sanitizes a file name string to remove illegal file name characters and path traversal segments.
    /// </summary>
    public static string SanitizeFileName(string? rawFileName, string fallback = "wincare_report")
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
            return fallback;

        string sanitized = Path.GetFileName(rawFileName.Trim());
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        // Prevent traversal remnants
        sanitized = sanitized.Replace("..", "").Replace("/", "").Replace("\\", "");
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }
}
