using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace WinCarePro.Infrastructure.Logging;

public static class CrashLogger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinCarePro", "Logs"
    );

    private static readonly SemaphoreSlim FileLock = new(1, 1);

    // Regex to redact sensitive patterns (e.g. usernames, passwords, API tokens, file paths)
    private static readonly Regex PasswordRegex = new(@"password\s*=\s*[^;\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TokenRegex = new(@"(bearer|token|secret)\s*[:=]\s*[^;\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ApiKeyRegex = new(@"(api[_\-]?key|apikey|x-api-key)\s*[:=]\s*[^;\s&]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UserPathRegex = new(@"(C:\\Users\\|/home/)[^\\\s/]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);

    public static void LogException(string context, Exception ex)
    {
        try
        {
            string sanitizedMessage = Sanitize(ex.ToString());
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{context}]\n{sanitizedMessage}\n{new string('-', 60)}\n";

            EnsureDirectoryExists();
            string filePath = Path.Combine(LogDir, "crash_log.txt");

            FileLock.Wait();
            try
            {
                // Rotate log file if it exceeds 5 MB to prevent unbounded growth
                const long MaxLogSize = 5 * 1024 * 1024;
                try
                {
                    if (File.Exists(filePath) && new FileInfo(filePath).Length > MaxLogSize)
                    {
                        string oldPath = filePath + ".old";
                        File.Move(filePath, oldPath, true);
                    }
                }
                catch { }

                File.AppendAllText(filePath, logEntry);
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch (Exception writeEx)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashLogger] Failed to write log: {writeEx.Message}");
        }
    }

    public static void LogMessage(string category, string message)
    {
        try
        {
            string sanitizedMessage = Sanitize(message);
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] {sanitizedMessage}\n";

            EnsureDirectoryExists();
            string filePath = Path.Combine(LogDir, "app.log");

            FileLock.Wait();
            try
            {
                File.AppendAllText(filePath, logEntry);
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch { }
    }

    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        string result = PasswordRegex.Replace(input, "password=***REDACTED***");
        result = TokenRegex.Replace(result, "$1=***REDACTED***");
        result = ApiKeyRegex.Replace(result, "$1=***REDACTED***");
        result = UserPathRegex.Replace(result, "$1***REDACTED***");
        result = EmailRegex.Replace(result, "***REDACTED_EMAIL***");
        return result;
    }

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(LogDir))
        {
            Directory.CreateDirectory(LogDir);
        }
    }
}
