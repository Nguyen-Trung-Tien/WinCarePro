using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Examples.Concurrency;

/// <summary>
/// Reference example demonstrating WinUI 3 Thread Marshaling via RunOnUI.
/// Invariants:
/// - NEVER mutate ObservableCollection or bound properties directly from background threads.
/// - ALWAYS dispatch via RunOnUI(() => { ... }).
/// </summary>
public sealed class RunOnUiExample : ViewModelBase
{
    public ObservableCollection<string> LogEntries { get; } = new();

    private string _currentStatus = string.Empty;
    public string CurrentStatus
    {
        get => _currentStatus;
        set => SetProperty(ref _currentStatus, value);
    }

    public async Task ProcessBackgroundTelemetryAsync()
    {
        // Switch to thread pool background worker
        await Task.Run(async () =>
        {
            for (int i = 1; i <= 5; i++)
            {
                await Task.Delay(100);
                string message = $"Telemetry batch {i} processed.";

                // Marshal UI mutation back to the UI DispatcherQueue
                RunOnUI(() =>
                {
                    CurrentStatus = message;
                    LogEntries.Add(message);
                });
            }
        });
    }
}

