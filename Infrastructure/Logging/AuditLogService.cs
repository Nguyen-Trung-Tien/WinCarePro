using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinCarePro.Services.Implementations;

public class AuditLogService
{
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _logSemaphore = new(1, 1);

    public AuditLogService()
    {
        string logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinCarePro",
            "Logs"
        );

        try
        {
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
        }
        catch { }

        _logFilePath = Path.Combine(logsDir, "audit.log");
    }

    private static string SanitizeForLog(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private void EnsureLogRotation()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                var fileInfo = new FileInfo(_logFilePath);
                // Rotate if log exceeds 10MB
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    string oldPath = _logFilePath + ".old";
                    if (File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                    File.Move(_logFilePath, oldPath);
                }
            }
        }
        catch { }
    }

    public void LogAction(string category, string actionName, string target, string result, string details = "")
    {
        try
        {
            string cat = SanitizeForLog(category).ToUpperInvariant();
            string act = SanitizeForLog(actionName);
            string tgt = SanitizeForLog(target);
            string res = SanitizeForLog(result);
            string det = SanitizeForLog(details);

            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{cat}] Action: {act} | Target: {tgt} | Result: {res} | Details: {det}";
            
            _logSemaphore.Wait();
            try
            {
                EnsureLogRotation();
                File.AppendAllText(_logFilePath, entry + Environment.NewLine);
            }
            finally
            {
                _logSemaphore.Release();
            }
        }
        catch
        {
            // Fail silently
        }
    }

    public async Task LogActionAsync(string category, string actionName, string target, string result, string details = "")
    {
        try
        {
            string cat = SanitizeForLog(category).ToUpperInvariant();
            string act = SanitizeForLog(actionName);
            string tgt = SanitizeForLog(target);
            string res = SanitizeForLog(result);
            string det = SanitizeForLog(details);

            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{cat}] Action: {act} | Target: {tgt} | Result: {res} | Details: {det}";
            
            byte[] encodedText = Encoding.UTF8.GetBytes(entry + Environment.NewLine);
            await _logSemaphore.WaitAsync();
            try
            {
                EnsureLogRotation();
                using (var sourceStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await sourceStream.WriteAsync(encodedText.AsMemory(0, encodedText.Length));
                }
            }
            finally
            {
                _logSemaphore.Release();
            }
        }
        catch
        {
            // Fail silently
        }
    }
}
