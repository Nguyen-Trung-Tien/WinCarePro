using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
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
    public bool Success => ExitCode == 0 && !TimedOut;
}

public static class ProcessRunner
{
    /// <summary>
    /// Executes a system process asynchronously with structured arguments (safe against command injection).
    /// </summary>
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        System.Collections.Generic.IEnumerable<string> argumentList,
        TimeSpan timeout,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ProcessResult();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        Encoding encoding = Encoding.UTF8;
        try
        {
            encoding = Encoding.GetEncoding(Console.OutputEncoding.CodePage);
        }
        catch
        {
            encoding = Encoding.UTF8;
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? ""
        };

        if (argumentList != null)
        {
            foreach (var arg in argumentList)
            {
                psi.ArgumentList.Add(arg);
            }
        }

        using var process = new Process { StartInfo = psi };
        return await ExecuteProcessCoreAsync(process, timeout, outputBuilder, errorBuilder, onOutput, onError, cancellationToken);
    }

    /// <summary>
    /// Executes a system process asynchronously with strict timeout and output capturing.
    /// </summary>
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        Encoding encoding = Encoding.UTF8;
        try
        {
            encoding = Encoding.GetEncoding(Console.OutputEncoding.CodePage);
        }
        catch
        {
            encoding = Encoding.UTF8;
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? ""
            }
        };

        return await ExecuteProcessCoreAsync(process, timeout, outputBuilder, errorBuilder, onOutput, onError, cancellationToken);
    }

    private static async Task<ProcessResult> ExecuteProcessCoreAsync(
        Process process,
        TimeSpan timeout,
        StringBuilder outputBuilder,
        StringBuilder errorBuilder,
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken)
    {
        var result = new ProcessResult();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!process.Start())
            {
                result.ExitCode = -1;
                result.Error = "Failed to start process.";
                return result;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var outputTask = ReadStreamAsync(process.StandardOutput, outputBuilder, onOutput, cts.Token);
            var errorTask = ReadStreamAsync(process.StandardError, errorBuilder, onError, cts.Token);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try { process.Kill(true); } catch { }
                    throw;
                }

                // If cancelled because of timeout
                result.TimedOut = true;
                try { process.Kill(true); } catch { }
                result.ExitCode = -1;
                result.Error = "Process execution timed out.";
            }

            if (!result.TimedOut)
            {
                result.ExitCode = process.ExitCode;

                // Allow stream readers to read the final buffered output to EOF
                var streamReadTasks = Task.WhenAll(outputTask, errorTask);
                if (await Task.WhenAny(streamReadTasks, Task.Delay(2000)) != streamReadTasks)
                {
                    try { cts.Cancel(); } catch { }
                }
            }
            else
            {
                try { cts.Cancel(); } catch { }
                await Task.WhenAll(
                    Task.WhenAny(outputTask, Task.Delay(500)),
                    Task.WhenAny(errorTask, Task.Delay(500))
                );
            }
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
            throw;
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
    /// Fast helper to execute short-lived commands with structured arguments without throwing exceptions on non-zero exit code.
    /// </summary>
    public static Task<ProcessResult> RunHiddenAsync(
        string fileName,
        System.Collections.Generic.IEnumerable<string> argumentList,
        int timeoutSeconds = 5,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(fileName, argumentList, TimeSpan.FromSeconds(timeoutSeconds), null, null, null, cancellationToken);
    }

    /// <summary>
    /// Fast helper to execute short-lived commands without throwing exceptions on non-zero exit code.
    /// </summary>
    public static Task<ProcessResult> RunHiddenAsync(
        string fileName,
        string arguments,
        int timeoutSeconds = 5,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(fileName, arguments, TimeSpan.FromSeconds(timeoutSeconds), null, null, null, cancellationToken);
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder fullBuilder,
        Action<string>? onLine,
        CancellationToken cancellationToken)
    {
        char[] rentedBuffer = ArrayPool<char>.Shared.Rent(2048);
        var currentLine = new StringBuilder();

        try
        {
            int read;
            while ((read = await reader.ReadAsync(rentedBuffer.AsMemory(0, 2048), cancellationToken)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    char c = rentedBuffer[i];
                    fullBuilder.Append(c);

                    if (c == '\r' || c == '\n')
                    {
                        if (currentLine.Length > 0)
                        {
                            string line = currentLine.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                onLine?.Invoke(line);
                            }
                            currentLine.Clear();
                        }
                    }
                    else
                    {
                        currentLine.Append(c);
                    }
                }
            }

            if (currentLine.Length > 0)
            {
                string line = currentLine.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    onLine?.Invoke(line);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading process stream: {ex.Message}");
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// Escapes command line arguments safely to prevent command injection risks.
    /// Strips dangerous shell metacharacters that enable command chaining or injection.
    /// </summary>
    public static string SanitizeArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        // Remove dangerous command chaining and shell injection characters
        string sanitized = argument
            .Replace("\r", "").Replace("\n", "")
            .Replace("|", "").Replace("&", "")
            .Replace(";", "").Replace("`", "")
            .Replace("$(", "").Replace("${", "")
            .Replace("<", "").Replace(">", "");
        
        // Wrap in quotes if contains whitespace or special chars
        if (sanitized.Contains(' ') || sanitized.Contains('\t') || sanitized.Contains('\"'))
        {
            sanitized = "\"" + sanitized.Replace("\"", "\\\"") + "\"";
        }

        return sanitized;
    }

    /// <summary>
    /// Validates that a service name contains only safe alphanumeric characters and underscores.
    /// Returns true if the name is safe, false if it contains potential injection characters.
    /// </summary>
    public static bool IsValidServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(serviceName, @"^[a-zA-Z0-9_\-\.]+$");
    }
}

