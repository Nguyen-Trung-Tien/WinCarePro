# 💻 05. C# 13 & .NET 10 Coding Standards & Conventions

> [⬅️ 04. Performance & Memory Management](04_PERFORMANCE_AND_MEMORY_RULES.md) • [🏠 Rules Index](README.md) • **Rule 05** • [Next Rule: 06. UI/UX & Aesthetics ➡️](06_UI_UX_AND_AESTHETICS_RULES.md)

---

## 🏗️ 1. Mandatory Operation Result Pattern (`OperationResult<T>`)

All methods in the Engine and Service layers performing fallible operations (I/O, Registry, Win32 API, Network) **MUST** return `OperationResult` or `OperationResult<T>` rather than throwing uncaught exceptions across layer boundaries.

```csharp
// Canonical OperationResult structure:
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

    public static OperationResult Ok(string message = "") => new() { Success = true, Message = message };
    public static OperationResult Fail(string error, Exception? ex = null, string code = "") => 
        new() { Success = false, Message = error, Exception = ex, ErrorCode = code };
}
```

---

## 🚫 2. Exception Handling Policy (Zero-Silent Catch)

1. **Empty Catch Blocks Strictly FORBIDDEN:**
   - ❌ **FORBIDDEN:** `catch { }` or `catch (Exception) { /* do nothing */ }`.
   - Every caught exception MUST be logged via `CrashLogger` or `AuditLogService`, or propagated through `OperationResult.Fail()`.
2. **Structured Audit Logging:**
   - Use `AuditLogService.LogAction(action, module, status, details)` to record high-impact user operations in SQLite.

---

## 🌐 3. Internationalization Governance (i18n & Zero Hardcoded Strings)

1. **Zero Hardcoded Display Strings:**
   - All UI labels, page headers, button text, dialog descriptions, and Toast notifications **MUST** resolve from translation keys.
2. **Syntax Across XAML & C#:**
   - In XAML: Use `x:Uid` or `Text="{Binding Key, Converter={StaticResource TranslationConverter}}"`.
   - In C#: Use `TranslationManager.Instance.GetString("Key")` or `"Key".Translate()`.
3. **Dual Dictionary Parity:**
   - Every newly created translation key MUST be added simultaneously to both `vi-VN` and `en-US` dictionaries in [TranslationManager.Translations.cs](../../Services/TranslationService/TranslationManager.Translations.cs).

---

## 🔤 4. Identifier Naming Conventions

| Symbol Type | Convention | Canonical Example |
| :--- | :--- | :--- |
| **Class, Struct, Record** | PascalCase | `JunkCleanerEngine`, `SafePathGuard` |
| **Interface** | Prefix `I` + PascalCase | `IServiceSafetyService`, `IDbManager` |
| **Method** | PascalCase (Async methods MUST have `Async` suffix) | `ScanJunkAsync()`, `EmptyWorkingSet()` |
| **Private Field** | Prefix `_` + camelCase | `private readonly object _dbLock;` |
| **Property** | PascalCase | `public double CpuUsagePercentage { get; set; }` |
| **Constants** | PascalCase or UPPER_SNAKE | `MaxHistoryEntries`, `DEFAULT_TIMEOUT_MS` |
| **File Name** | Exact match with primary class declaration | `SystemOptimizerEngine.cs` |

---

## 🛡️ 5. Nullable Reference Safety

The solution enforces `<Nullable>enable</Nullable>`.
- ALWAYS validate input references for `null` or utilize safe navigation operators (`?.`, `??`).
- DO NOT use the null-forgiving operator (`!`) unless backed by provable architectural invariants guaranteeing non-null state.

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>
