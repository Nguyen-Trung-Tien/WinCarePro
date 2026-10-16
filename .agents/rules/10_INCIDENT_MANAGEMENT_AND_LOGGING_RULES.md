# 📋 10. Incident Management, Logging & Privacy Rules

> [⬅️ 09. Registry & OS Interop](09_REGISTRY_AND_OS_INTEROP_RULES.md) • [🏠 Rules Index](README.md) • **Rule 10** • [Next Rule: 11. AI Agent Efficiency & Precision ➡️](11_AI_AGENT_EFFICIENCY_AND_PRECISION_RULES.md)

---

## 📝 1. Structured Logging Architecture

The platform maintains two isolated logging sinks partitioned by operational scope:

| Log Sink | Storage Location | Operational Purpose | Protection Policy |
| :--- | :--- | :--- | :--- |
| **Audit Log** | SQLite `Logs` table | Tracks user-initiated cleaning, optimization, and uninstallations | Queried via `AuditLogService`, indexed by module |
| **Crash Logger** | `%LocalAppData%\WinCarePro\CrashLogs\` | Captures unhandled exceptions and native stack traces | Time-stamped files, size caps, and automated retention cleanup |

---

## 🔒 2. Zero-PII Leakage & Privacy Invariants

1. **Strictly Exclude Sensitive Data from Logs:**
   - ❌ **FORBIDDEN:** Persisting user passwords, authorization tokens, decryption keys, or personal file payloads into log streams.
2. **User Profile Path Sanitization:**
   - User profile paths that may leak Windows usernames (e.g. `C:\Users\JohnDoe\AppData\...`) MUST be normalized to `%USERPROFILE%\AppData\...` or `%TEMP%` prior to disk persistence to ensure anonymity when crash logs are submitted for diagnosis.

---

## 🗄️ 3. Log Rotation & Retention Governance

To prevent disk saturation from unconstrained log growth:

1. **File Size Caps:**
   - Individual log files MUST NOT exceed **5 MB**. Exceeding files MUST be closed and rotated with an incremented sequence suffix.
2. **Retention Policy:**
   - The `CrashLogs/` directory retains a maximum of **30 days** or **10 files**, whichever is smaller.
   - On application startup, `CrashLogger.CleanupOldLogs()` MUST purge log artifacts older than 30 days.
3. **SQLite Audit Table Truncation:**
   - The SQLite `Logs` table is pruned to maintain a maximum of **1,000 entries**, guaranteeing index seek latencies remain below $10\text{ ms}$.

---

## 🚨 4. Global Unhandled Exception Hooks

In [App.xaml.cs](../../App.xaml.cs), the application **MUST** register all 3 global crash observation handlers:

```csharp
// 1. AppDomain & non-UI unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    CrashLogger.LogFatal("AppDomain Unhandled Exception", e.ExceptionObject as Exception);
};

// 2. Unobserved thread pool task exceptions
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    CrashLogger.LogError("TaskScheduler Unobserved Task Exception", e.Exception);
    e.SetObserved(); // Prevent process termination when recoverable
};

// 3. XAML UI Dispatcher unhandled exceptions
this.UnhandledException += (s, e) =>
{
    CrashLogger.LogFatal("XAML UI Unhandled Exception", e.Exception);
    e.Handled = true; // Retain process stability if non-fatal
};
```

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>
