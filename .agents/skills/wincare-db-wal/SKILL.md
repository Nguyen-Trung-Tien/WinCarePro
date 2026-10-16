---
name: wincare-db-wal
description: >-
  SQLite 3 WAL Mode persistence and synchronization rules for WinCare Pro. Covers DbManager usage, mandatory lock (_dbLock) synchronization, parameterized queries, schema migrations, avoiding lock contention, and preventing test collisions via DatabaseTestCollection. Use whenever accessing, querying, or modifying the SQLite database.
---

# 🗄️ WinCare Pro — SQLite WAL Persistence & Safe Concurrency Standards (Db WAL Guard)

WinCare Pro utilizes **SQLite 3** in **Write-Ahead Logging (WAL)** mode as its local database to persist configuration settings, scan history, system intervention audit trails, and Undo rollback points.

Because SQLite in desktop application environments operates on a Single-Writer model, all read/write operations **MUST** strictly adhere to centralized synchronization via [DbManager.cs](../../Infrastructure/Database/DbManager.cs).

---

## 🚫 1. Database Operations Enforcement Matrix

| Scenario | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **SQLite Access** | Opening independent connections outside `DbManager` | ALWAYS route calls through `DbManager.Instance` or injected DI service |
| **Multi-thread Synchronization** | Executing queries without synchronization | Enclose 100% of calls in `lock (_dbLock) { /* SQLite operations */ }` |
| **Operations Inside Lock** | Long computations, network I/O, UI dispatching inside lock | Confine scope exclusively to native SQLite commands |
| **SQL Query Construction** | String interpolation `$"SELECT * FROM Audit WHERE Path = '{p}'"` | Parameterized binding REQUIRED: `cmd.Parameters.AddWithValue("$path", p)` |
| **Database Unit Testing** | Unconstrained parallel database test execution | Apply `[Collection(nameof(DatabaseTestCollection))]` attribute |

---

## 🔒 2. Centralized Synchronization Pattern (`lock (_dbLock)`)

All operations within `DbManager` MUST strictly conform to the following pattern:

```csharp
public OperationResult SaveAuditRecord(AuditRecord record)
{
    if (record == null) return OperationResult.Fail("Audit record cannot be null.");

    // Prepare data OUTSIDE the lock
    var timestamp = DateTime.UtcNow.ToString("o");

    lock (_dbLock)
    {
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AuditRecords (Module, Action, TargetPath, Details, Timestamp, Success)
                VALUES ($mod, $act, $target, $details, $ts, $success);";

            cmd.Parameters.AddWithValue("$mod", record.Module);
            cmd.Parameters.AddWithValue("$act", record.Action);
            cmd.Parameters.AddWithValue("$target", record.TargetPath ?? string.Empty);
            cmd.Parameters.AddWithValue("$details", record.Details ?? string.Empty);
            cmd.Parameters.AddWithValue("$ts", timestamp);
            cmd.Parameters.AddWithValue("$success", record.Success ? 1 : 0);

            cmd.ExecuteNonQuery();
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex, $"{nameof(DbManager)}.{nameof(SaveAuditRecord)}");
            return OperationResult.Fail(ex);
        }
    }
}
```

---

## ⚡ 3. Thread Non-Blocking Discipline (Zero-Blocking Inside Lock)

### ❌ Anti-Pattern Causing UI Deadlocks & Lock Contention:
```csharp
lock (_dbLock)
{
    // FORBIDDEN: Invoking disk or network I/O inside SQLite lock!
    var fileSize = new FileInfo(path).Length; 
    var hash = ComputeFileSha256(path); 
    
    // FORBIDDEN: Dispatching UI updates while holding database lock!
    RunOnUI(() => StatusText = "Saving to database...");
    
    cmd.ExecuteNonQuery();
}
```

### ✅ Mandatory Best Practices:
1. Pre-compute SHA-256 hashes, file metrics, and path existence **prior** to acquiring `lock (_dbLock)`.
2. Enter the lock, execute the SQLite transaction/command expeditiously within sub-millisecond budgets.
3. Exit the lock before dispatching UI state changes via `RunOnUI()`.

---

## 🛡️ 4. Zero SQL Injection Armor

All inputs supplied by users or retrieved from system states (paths, process names, registry keys) **MUST NEVER be interpolated directly into SQL command strings**.

- ALWAYS use `$`, `@`, or `:` prefix tokens compliant with `Microsoft.Data.Sqlite`.
- Bind parameters explicitly via `cmd.Parameters.AddWithValue("$paramName", value ?? DBNull.Value)`.

---

## 🧪 5. Safe Concurrency Unit Testing (DatabaseTestCollection)

To eliminate `SQLite Error 5: 'database is locked'` exceptions when xUnit executes test runners concurrently:

```csharp
using Xunit;
using WinCarePro.Tests.Fixtures;

namespace WinCarePro.Tests.Database;

[Collection(nameof(DatabaseTestCollection))]
public class DbManagerTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public DbManagerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SaveAndRetrieveAuditRecord_ShouldSucceed()
    {
        // Executes database tests sequentially in isolated SQLite WAL test fixture
    }
}
```
