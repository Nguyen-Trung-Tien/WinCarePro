using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinCarePro.Core.Helpers;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public bool TimedOut { get; set; }
}

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ProcessResult();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? ""
            }
        };

        using var outputCloseEvent = new SemaphoreSlim(0);
        using var errorCloseEvent = new SemaphoreSlim(0);

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                outputCloseEvent.Release();
            }
            else
            {
                outputBuilder.AppendLine(e.Data);
                onOutput?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                errorCloseEvent.Release();
            }
            else
            {
                errorBuilder.AppendLine(e.Data);
                onError?.Invoke(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                result.ExitCode = -1;
                result.Error = "Failed to start process.";
                return result;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var processExitTask = process.WaitForExitAsync(cts.Token);
            var timeoutTask = Task.Delay(timeout, cts.Token);

            var completedTask = await Task.WhenAny(processExitTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                result.TimedOut = true;
                try
                {
                    process.Kill(true); // Kill the process and all its descendants
                }
                catch { }
                result.ExitCode = -1;
                result.Error = "Process execution timed out.";
            }
            else
            {
                cts.Cancel(); // Cancel the timeout Task.Delay task
                result.ExitCode = process.ExitCode;

                // Safely wait for the stream close events with a small timeout
                await Task.WhenAll(
                    outputCloseEvent.WaitAsync(TimeSpan.FromSeconds(2)),
                    errorCloseEvent.WaitAsync(TimeSpan.FromSeconds(2))
                );
            }
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            result.Error = ex.Message;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
        }

        result.Output = outputBuilder.ToString();
        result.Error = errorBuilder.ToString();
        return result;
    }

    /// <summary>
    /// Escapes command line arguments safely to prevent command injection risks.
    /// </summary>
    public static string SanitizeArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        // Remove dangerous command chaining characters if unquoted
        string sanitized = argument.Replace("\r", "").Replace("\n", "");
        
        // Wrap in quotes if contains whitespace or special chars
        if (sanitized.Contains(' ') || sanitized.Contains('\t') || sanitized.Contains('\"'))
        {
            sanitized = "\"" + sanitized.Replace("\"", "\\\"") + "\"";
        }

        return sanitized;
    }
}
