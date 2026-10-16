# ⚡ 03. Concurrency & Threading Rules

> [⬅️ 02. Security & Safety](02_SECURITY_AND_SAFETY_RULES.md) • [🏠 Rules Index](README.md) • **Rule 03** • [Next Rule: 04. Performance & Memory Management ➡️](04_PERFORMANCE_AND_MEMORY_RULES.md)

---

## 🖥️ 1. UI Dispatcher Thread Invariants (Zero-Crash Threading)

WinUI 3 (Windows App SDK) requires all UI element mutations and observable collections (`ObservableCollection<T>`) to be executed strictly on the **UI Thread**.

### 🔴 MANDATORY INVARIANTS:
1. **NEVER update UI state from a Background Thread:** Mutating `[ObservableProperty]` or modifying an `ObservableCollection` directly inside `Task.Run()` without thread marshaling is STRICTLY FORBIDDEN.
2. **ALWAYS use `RunOnUI` in ViewModels:**

```csharp
// Inside ViewModelBase.cs:
protected void RunOnUI(Action action)
{
    if (App.MainWindow?.DispatcherQueue == null) return;

    if (App.MainWindow.DispatcherQueue.HasThreadAccess)
    {
        action();
    }
    else
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() => action());
    }
}
```

### ✅ Production-Grade ViewModel Pattern:
```csharp
[RelayCommand]
public async Task ScanAsync()
{
    IsScanning = true; // Set on UI Thread
    
    // Offload heavy I/O and scanning to thread pool
    var result = await Task.Run(() => _junkEngine.ScanJunkItemsAsync());
    
    // Safely marshal mutations back to UI Thread
    RunOnUI(() =>
    {
        JunkItems.Clear();
        foreach (var item in result.Data)
        {
            JunkItems.Add(item);
        }
        IsScanning = false;
    });
}
```

---

## 🛑 2. Cooperative Task Cancellation (`CancellationToken`)

All long-running asynchronous routines (disk scanning, duplicate file search, network diagnostics, update checking) **MUST** accept a `CancellationToken ct = default` parameter and periodically check `ct.ThrowIfCancellationRequested()` or pass it to underlying I/O APIs.

### Pacing & Cancellation Check Pattern:
```csharp
public async Task<OperationResult<List<JunkItem>>> ScanDirectoryAsync(
    string folderPath, 
    IProgress<ScanProgress>? progress, 
    CancellationToken ct = default)
{
    var list = new List<JunkItem>();
    
    foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
    {
        // 1. Cooperative cancellation check
        ct.ThrowIfCancellationRequested();
        
        // 2. Perform safety inspection and computation
        if (SafePathGuard.IsSafeToDelete(file))
        {
            list.Add(new JunkItem(file));
        }
    }
    
    return OperationResult<List<JunkItem>>.Ok(list);
}
```

### 🔴 Navigation Lifecycle & Cancellation Invariants:
1. **CancellationTokenSource Governance:** ViewModels executing long-running tasks MUST maintain `CancellationTokenSource? _cts`.
2. **Cancel on Departure (OnNavigatedFrom / Cleanup):** When the user navigates away or triggers cancellation, the ViewModel/Page **MUST** invoke `_cts?.Cancel()` and `_cts?.Dispose()`.
3. **Eliminate Orphaned Tasks:** NEVER allow background tasks to continue executing and invoking `RunOnUI` after the associated View has been unloaded or disposed.

---

## 💾 3. Database Synchronization (`_dbLock` & SQLite WAL)

Although SQLite in **Write-Ahead Logging (WAL)** mode supports concurrent readers, write operations (INSERT, UPDATE, DELETE) require serialized access to prevent `SQLite busy (database is locked)` errors.

### 🔴 MANDATORY INVARIANTS IN `DbManager`:
1. **Centralized Lock Guard:** ALL database queries and command executions MUST be guarded inside `lock (_dbLock)`.
2. **Deadlock Elimination:** NEVER invoke external delegates, UI dispatchers, or HTTP requests while holding `lock (_dbLock)`.

```csharp
private readonly object _dbLock = new();

public void InsertLog(string action, string module, string status)
{
    lock (_dbLock)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Logs (Action, Module, Status) VALUES ($act, $mod, $stat)";
        cmd.Parameters.AddWithValue("$act", action);
        cmd.Parameters.AddWithValue("$mod", module);
        cmd.Parameters.AddWithValue("$stat", status);
        cmd.ExecuteNonQuery();
    }
}
```

---

## 🚫 4. Sync-Over-Async Elimination (`.Result` or `.Wait()`)

- ❌ **FORBIDDEN:** Calling `task.Result` or `task.Wait()` on the UI Thread — primary cause of UI deadlocks in Windows App SDK.
- ✅ **ALWAYS USE:** The `await` keyword inside methods declared `async Task` or `async Task<T>`.
- ❌ **FORBIDDEN `async void`:** Except for XAML UI Event Handlers, all asynchronous methods MUST return `Task` or `Task<T>`.

---

## 🔒 5. State Restoration Invariant (`finally` Lifecycle Pattern)

Whenever a ViewModel sets `IsBusy = true` or `SetOperationState(OperationState.Running)`:
1. **Mandatory `finally` Block:** All async workflows MUST include a `finally` block ensuring that regardless of exceptions or cancellation, `IsBusy` is reset to `false` and `OperationState` returns to `OperationState.Idle`.
2. **No Early Returns Without Cleanup:** NEVER return early upon cancellation check (e.g. `if (token.IsCancellationRequested) return;`) outside a `try / finally` structure.
3. **`RunOnUI` Fallback for Unit Testing:** When `DispatcherQueue == null`, provide direct synchronous execution fallback to enable standalone xUnit / stress tests without a WinUI thread host.

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

