---
name: wincare-concurrency-guard
description: >-
  WinUI 3 threading and concurrency rules for WinCare Pro. Covers RunOnUI dispatching, safe async/await, RelayCommands with cancellation, background worker thread safety, avoiding sync-over-async deadlocks, and WeakReferenceMessenger message passing. Use whenever writing async methods, background tasks, or UI-updating code.
---

# ⚡ WinCare Pro — Concurrency & WinUI 3 Threading Standards

WinUI 3 enforces strict thread affinity: the UI thread and `ObservableCollection<T>` must **never** be accessed directly from worker threads. Deadlocks and sync-over-async are strictly prohibited.

## 📁 Skill Assets & Helpers
- **Static Analysis Script:** [`scripts/scan-concurrency-issues.ps1`](./scripts/scan-concurrency-issues.ps1)
- **Reference Examples:**
  - UI Marshaling: [`examples/RunOnUiExample.cs`](./examples/RunOnUiExample.cs)
  - Cancellation & Hygiene: [`examples/RelayCommandWithCancellationExample.cs`](./examples/RelayCommandWithCancellationExample.cs)
  - Weak Reference Messenger: [`examples/WeakReferenceMessengerExample.cs`](./examples/WeakReferenceMessengerExample.cs)
- **Guidelines & Best Practices:**
  - Threading Architecture: [`resources/concurrency-best-practices.md`](./resources/concurrency-best-practices.md)

---

## 🚫 1. Concurrency Anti-Pattern Reference Matrix

| Concurrency Scenario | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **Sync-over-Async** | `var r = task.Result;` or `task.Wait();` | `var r = await task;` |
| **UI Updates from Thread** | `Items.Add(item);` from `Task.Run` | `RunOnUI(() => Items.Add(item));` |
| **Async Method Signature** | `public async void DoWork()` | `public async Task DoWorkAsync()` |
| **Cancellation Handling** | Ignoring `CancellationToken` | Accept `CancellationToken ct = default`, periodically call `ct.ThrowIfCancellationRequested()` |
| **Busy State Teardown** | Omitting `IsBusy = false` on failure | `try { IsBusy = true; ... } finally { IsBusy = false; OperationState = OperationState.Idle; }` |
| **Cross-VM References** | Directly referencing other ViewModels | Route via `WeakReferenceMessenger.Default.Send(...)` |

---

## 🧵 2. WinUI 3 Thread Marshaling (`RunOnUI`)

All ViewModels in WinCare Pro inherit from `ViewModelBase.cs`, which provides the `RunOnUI()` helper:

```csharp
// Inside ViewModel:
await Task.Run(() =>
{
    // Background computation
    var metrics = GatherMetrics();

    // Marshal updates back to UI Thread safely
    RunOnUI(() =>
    {
        CpuUsage = metrics.CpuPercent;
        MemoryUsage = metrics.RamPercent;
        TelemetryLog.Add($"Metrics updated at {DateTime.Now:T}");
    });
});
```

---

## 🛑 3. RelayCommand Cancellation Pattern

Always configure async commands with cancellation support using CommunityToolkit.Mvvm:

```csharp
[RelayCommand(IncludeCancelCommand = true)]
private async Task ExecuteScanAsync(CancellationToken ct)
{
    try
    {
        IsBusy = true;
        OperationState = OperationState.Running;
        StatusText = "Scanning...";

        await _scanEngine.ScanAsync(ct);

        StatusText = "Scan Complete";
    }
    catch (OperationCanceledException)
    {
        StatusText = "Scan Cancelled";
    }
    finally
    {
        IsBusy = false;
        OperationState = OperationState.Idle;
    }
}
```

---

## 📬 4. Cross-Module Communication (`WeakReferenceMessenger`)

Never inject ViewModels into other ViewModels. Decouple via `WeakReferenceMessenger`:

```csharp
// Publisher (e.g. SettingsViewModel):
WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(newTheme));

// Subscriber (e.g. MainViewModel):
WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
{
    RunOnUI(() => ApplyTheme(m.Value));
});
```
