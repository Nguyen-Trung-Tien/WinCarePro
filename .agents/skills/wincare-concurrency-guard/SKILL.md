---
name: wincare-concurrency-guard
description: >-
  WinUI 3 threading and concurrency rules for WinCare Pro. Covers RunOnUI dispatching, safe async/await, RelayCommands with cancellation, background worker thread safety, avoiding sync-over-async deadlocks, and WeakReferenceMessenger message passing. Use whenever writing async methods, background tasks, or UI-updating code.
---

# ⚡ WinCare Pro — Threading & Safe Concurrency Guard

The **Windows App SDK (WinUI 3)** environment strictly enforces a Single-Threaded Apartment (STA) model for the UI Thread. Cross-thread access faults trigger `COMException (0x8001010E RPC_E_WRONG_THREAD)` or hardlock the presentation layer.

---

## 🚫 1. Concurrency Invariants Matrix

| Scenario | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **UI Updates from Background Thread** | `Items.Add(item);` or `StatusText = "...";` | `RunOnUI(() => { Items.Add(item); StatusText = "..."; });` |
| **Awaiting Async Tasks** | `var res = myTask.Result;` or `myTask.Wait();` | `var res = await myTask;` (Zero Sync-over-Async) |
| **Async Method Signature** | `public async void DoWork()` (except UI events) | `public async Task DoWorkAsync(CancellationToken ct = default)` |
| **Long-Running Task Cancellation** | Omitting `CancellationToken`, orphaned background workers | Accept `CancellationToken ct`, check `ct.ThrowIfCancellationRequested()` |
| **Busy State Teardown** | Omitting state reset on exceptions | ALWAYS wrap in `finally { IsBusy = false; OperationState = OperationState.Idle; }` |
| **Cross-ViewModel Messaging** | Direct references: `_otherViewModel.DoSomething()` | Decoupled messaging: `WeakReferenceMessenger.Default.Send(...)` |

---

## 🧵 2. `RunOnUI` Dispatching & WinUI 3 DispatcherQueue

### Golden Rule:
Any property triggering `INotifyPropertyChanged` data-binding or modifying an `ObservableCollection<T>` from a background thread (ThreadPool, `Task.Run`, `Timer`, background callback) **MUST** be marshaled via `RunOnUI()`.

```csharp
// Inside a ViewModel inheriting ViewModelBase:
public async Task LoadTelemetryAsync(CancellationToken ct)
{
    try
    {
        IsBusy = true;
        OperationState = OperationState.Running;

        // Execute compute-intensive retrieval on thread pool
        var report = await Task.Run(() => _telemetryEngine.CollectMetrics(ct), ct);

        // Safely marshal state mutations to UI Thread
        RunOnUI(() =>
        {
            CpuUsage = report.CpuPercent;
            RamUsage = report.RamUsedMb;
            ProcessList.Clear();
            foreach (var proc in report.TopProcesses)
            {
                ProcessList.Add(proc);
            }
        });
    }
    catch (OperationCanceledException)
    {
        RunOnUI(() => StatusMessage = "Operation was canceled.");
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, nameof(LoadTelemetryAsync));
        RunOnUI(() => StatusMessage = "Failed to retrieve telemetry metrics.");
    }
    finally
    {
        RunOnUI(() =>
        {
            IsBusy = false;
            OperationState = OperationState.Idle;
        });
    }
}
```

---

## 🛑 3. Deadlock Elimination (Anti-Sync-over-Async)

### ❌ Fatal Anti-Pattern:
```csharp
// INSTANT DEADLOCK when invoked from the UI thread!
var data = _engine.ScanAsync().Result;
_engine.CleanAsync().Wait();
```

### ✅ Asynchronous Standards:
1. All I/O, Win32, Registry, or WMI routines with durations $> 50\text{ ms}$ MUST return `Task` or `Task<T>`.
2. Method identifiers MUST include the `Async` suffix (e.g. `ScanJunkAsync`, `RepairFilesAsync`).
3. Maintain `await` throughout the entire call graph (Async All The Way).

---

## 🎮 4. `[RelayCommand]` & Cooperative Cancellation Pattern

Utilizing `CommunityToolkit.Mvvm`:

```csharp
[ObservableProperty]
private bool _isBusy;

[ObservableProperty]
private OperationState _operationState = OperationState.Idle;

// Automatically generates ScanCommand and ScanCancelCommand
[RelayCommand(IncludeCancelCommand = true)]
private async Task ScanAsync(CancellationToken ct)
{
    try
    {
        IsBusy = true;
        OperationState = OperationState.Running;
        StatusMessage = "Scanning system...";

        var result = await _engine.ExecuteScanAsync(progress: new Progress<int>(percent =>
        {
            RunOnUI(() => ScanProgress = percent);
        }), ct);

        if (result.Success)
        {
            RunOnUI(() => StatusMessage = "Scan completed.");
        }
    }
    catch (OperationCanceledException)
    {
        RunOnUI(() => StatusMessage = "Scan canceled by user.");
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, nameof(ScanAsync));
        RunOnUI(() => StatusMessage = "An error occurred during system scan.");
    }
    finally
    {
        RunOnUI(() =>
        {
            IsBusy = false;
            OperationState = OperationState.Idle;
        });
    }
}
```

---

## 📡 5. Decoupled Communication via `WeakReferenceMessenger`

To eliminate strong-reference memory leaks across views:

### Register Message Subscriber (in ViewModel initialization):
```csharp
WeakReferenceMessenger.Default.Register<SystemOptimizedMessage>(this, (r, m) =>
{
    RunOnUI(() =>
    {
        RefreshDashboard();
    });
});
```

### Unregister on Teardown or Unloaded:
```csharp
public override void Cleanup()
{
    WeakReferenceMessenger.Default.UnregisterAll(this);
    base.Cleanup();
}
```

### Broadcast Event:
```csharp
WeakReferenceMessenger.Default.Send(new SystemOptimizedMessage(freedBytes));
```
