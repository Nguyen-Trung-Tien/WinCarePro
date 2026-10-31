---
name: wincare-error-resilience
description: >-
  Zero-Silent Catch and error resilience patterns for WinCare Pro. Covers OperationResult/OperationResult<T> usage, CrashLogger exception tracking, preventing empty catches, graceful degradation for Win32/WMI failures, and user-facing error reporting via InfoBar/NotificationCenter. Use whenever handling exceptions, designing API results, or writing fault-tolerant system code.
---

# 🛡️ WinCare Pro — Error Resilience & Zero-Silent Catch Standards

WinCare Pro prioritizes stability and correctness. Applications that swallow exceptions silently or crash on Win32/WMI faults violate fundamental system integrity invariants.

## 📁 Skill Assets & Helpers
- **Static Analysis Script:** [`scripts/scan-silent-catch.ps1`](./scripts/scan-silent-catch.ps1)
- **Reference Examples:**
  - Result Pattern: [`examples/OperationResultPatternExample.cs`](./examples/OperationResultPatternExample.cs)
  - Fault-Tolerant Win32: [`examples/FaultTolerantWin32Example.cs`](./examples/FaultTolerantWin32Example.cs)
- **Matrix & Policies:**
  - Architecture Error Matrix: [`resources/error-handling-matrix.md`](./resources/error-handling-matrix.md)

---

## 🚫 1. Anti-Pattern: Silent Catches Forbidden

```csharp
// ❌ STRICTLY FORBIDDEN: Silent catch leaves the system in an indeterminate state
try
{
    DeleteTempFiles();
}
catch { }

// ✅ MANDATORY PATTERN: Log via CrashLogger and return OperationResult
try
{
    DeleteTempFiles();
    return OperationResult.Ok("Temp files deleted.");
}
catch (Exception ex)
{
    CrashLogger.LogError(ex);
    return OperationResult.Fail($"Failed to delete temp files: {ex.Message}");
}
```

---

## 📦 2. Result Pattern Standards (`OperationResult<T>`)

Methods across the Engine tier MUST return `OperationResult` or `OperationResult<T>`. This eliminates unhandled exception bubbling to the UI layer and guarantees deterministic state communication.
