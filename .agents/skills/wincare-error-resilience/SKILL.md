---
name: wincare-error-resilience
description: >-
  Zero-Silent Catch and error resilience patterns for WinCare Pro. Covers OperationResult/OperationResult<T> usage, CrashLogger exception tracking, preventing empty catches, graceful degradation for Win32/WMI failures, and user-facing error reporting via InfoBar/NotificationCenter. Use whenever handling exceptions, designing API results, or writing fault-tolerant system code.
---

# 🛡️ WinCare Pro — Fault Tolerance & Zero-Silent Catch Standards (Error Resilience)

WinCare Pro interacts directly with critical Windows subsystems (Registry, File System, Win32 APIs, Services, WMI). Fault conditions such as I/O exceptions, Access Denied errors, and Win32 failures are inevitable. The system **STRICTLY PROHIBITS** silent exception catching and mandates returning all diagnostic outcomes via the `OperationResult` pattern.

---

## 🚫 1. Exception Handling Enforcement Matrix

| Scenario | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **Exception Handling** | `catch { }` or `catch (Exception) { /* no-op */ }` | ALWAYS log via `CrashLogger.LogError(ex)` and return `OperationResult.Fail(ex)` |
| **Engine Return Type** | Returning `void` or throwing unhandled exceptions across boundaries | Return `OperationResult` or `OperationResult<T>` |
| **Authorization Failures (UnauthorizedAccess)** | Permitting unhandled crashes | Catch specific `UnauthorizedAccessException`, alert requirement for Administrator elevation |
| **P/Invoke or Win32 API Errors** | Omitting return code checks | Inspect `Marshal.GetLastWin32Error()` or evaluate `HRESULT` |
| **UI Error Presentation** | Modal popups via legacy WinForms `MessageBox.Show()` | Use `InfoBar`, `ContentDialog`, or `NotificationCenter` aligned with Fluent 2.0 |

---

## 📦 2. Standardized Result Pattern (`OperationResult<T>`)

All methods in `Engines` and `Infrastructure` layers MUST wrap outcomes using [OperationResult.cs](../../Core/Models/OperationResult.cs):

### Method Without Return Payload:
```csharp
public async Task<OperationResult> FlushDnsAsync(CancellationToken ct = default)
{
    try
    {
        var procResult = await ProcessRunner.RunAsync("ipconfig.exe", new[] { "/flushdns" }, ct);
        if (procResult.ExitCode == 0)
        {
            AppLogger.LogInfo("DNS cache flushed successfully.");
            return OperationResult.Ok();
        }

        return OperationResult.Fail($"ipconfig failed with exit code {procResult.ExitCode}: {procResult.StdErr}");
    }
    catch (OperationCanceledException)
    {
        return OperationResult.Fail("Operation was canceled by user.");
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, nameof(FlushDnsAsync));
        return OperationResult.Fail(ex);
    }
}
```

### Method Returning Data Payload (`OperationResult<T>`):
```csharp
public OperationResult<DriveHealthInfo> GetDiskHealth(string driveLetter)
{
    try
    {
        if (string.IsNullOrWhiteSpace(driveLetter))
            return OperationResult<DriveHealthInfo>.Fail("Invalid drive letter.");

        var info = QuerySmartAttributes(driveLetter);
        return OperationResult<DriveHealthInfo>.Ok(info);
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, $"{nameof(GetDiskHealth)}: {driveLetter}");
        return OperationResult<DriveHealthInfo>.Fail(ex);
    }
}
```

---

## 📝 3. Logging Hierarchy & Discipline

The system enforces a strict dichotomy between telemetry and crash logging:

1. **`CrashLogger.LogError(Exception ex, string context)`**:
   - Scope: Unexpected exceptions, I/O faults, SQLite corruptions, Win32 failures, or caught crashes.
   - Storage: `%AppData%\WinCare\Logs\crash_*.log` containing comprehensive stack traces.

2. **`AppLogger.LogInfo` / `AppLogger.LogWarning`**:
   - Scope: Operational business lifecycle events (scan initialization, 150 MB junk purged, user cancellation, protected service detection).

---

## 🛡️ 4. Graceful Degradation Strategy

When low-level system APIs fail or are unavailable (e.g., missing OS optional features, non-elevated user accounts, corrupted WMI repository):

1. **Prevent cascading process failure:** Attempt alternative fallbacks (e.g., WMI corrupted $\rightarrow$ fallback to Registry probes or Performance Counters).
2. **Surface explicit, actionable UI feedback:**
   ```csharp
   if (!IsAdministrator())
   {
       RunOnUI(() =>
       {
           WarningMessage = TranslationManager.Instance.Get("Err_AdminRequiredForThisAction");
           ShowAdminShieldBadge = true;
       });
       return OperationResult.Fail("Administrator privileges required.");
   }
   ```
3. **Preserve application responsiveness:** Unrelated modules and active views MUST continue functioning smoothly without degradation.
