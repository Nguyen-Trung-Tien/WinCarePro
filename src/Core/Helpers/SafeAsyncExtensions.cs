using System;
using System.Threading.Tasks;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Safe execution wrappers for UI event handlers and fire-and-forget async routines
/// to prevent unhandled exceptions in <c>async void</c> methods from crashing the WinUI 3 process.
/// </summary>
public static class SafeAsyncExtensions
{
    public static async Task RunSafeAsync(Func<Task> action, string context)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // Expected cancellation during navigation or user abort
        }
        catch (Exception ex)
        {
            CrashLogger.LogException(context, ex);
        }
    }
}
