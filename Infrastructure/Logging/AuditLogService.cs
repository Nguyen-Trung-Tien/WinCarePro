using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WinCarePro.Services.Implementations;

public class AuditLogService
{
    private readonly string _logFilePath;

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
            
            // Simple synchronous append with thread safety via locking
            lock (this)
            {
                File.AppendAllText(_logFilePath, entry + Environment.NewLine);
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
            
            // Use async file write
            byte[] encodedText = Encoding.UTF8.GetBytes(entry + Environment.NewLine);
            using (var sourceStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText.AsMemory(0, encodedText.Length));
            }
        }
        catch
        {
            // Fail silently
        }
    }
}
