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

    public void LogAction(string category, string actionName, string target, string result, string details = "")
    {
        try
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category.ToUpper()}] Action: {actionName} | Target: {target} | Result: {result} | Details: {details}";
            
            _logSemaphore.Wait();
            try
            {
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
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category.ToUpper()}] Action: {actionName} | Target: {target} | Result: {result} | Details: {details}";
            
            byte[] encodedText = Encoding.UTF8.GetBytes(entry + Environment.NewLine);
            await _logSemaphore.WaitAsync();
            try
            {
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
