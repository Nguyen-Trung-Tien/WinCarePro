# 🛡️ WinCare Pro — Error Resilience Matrix

| Architectural Layer | Error Handling Strategy | Permitted Return Types |
| :--- | :--- | :--- |
| **Core / Guards** | Throw specific guard exceptions or return boolean `IsSafeToDelete(...)` | `bool`, `void` (guards) |
| **Infrastructure (DB / PInvoke)** | Log via `CrashLogger.LogError(ex)` and wrap in `OperationResult<T>` | `OperationResult`, `OperationResult<T>` |
| **Engines (Business Logic)** | Catch unexpected faults, log via `CrashLogger`, return `OperationResult<T>.Fail(...)` | `OperationResult`, `OperationResult<T>` |
| **ViewModels** | Catch exceptions, log via `CrashLogger`, display user-friendly message, reset `IsBusy = false` in `finally` | `Task` |
| **Views / XAML Code-Behind** | Zero business logic, catch solely UI event dispatcher faults | `void` (UI event handlers) |

