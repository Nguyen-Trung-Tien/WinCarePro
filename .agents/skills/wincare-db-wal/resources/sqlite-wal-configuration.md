# 🗄️ WinCare Pro — SQLite 3 WAL Mode Configuration

## Pragma Directives
WinCare Pro initializes SQLite with Write-Ahead Logging (WAL) for high concurrency:
```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA busy_timeout = 5000;
PRAGMA foreign_keys = ON;
PRAGMA temp_store = MEMORY;
```

## Architectural Invariants
1. **Single Centralized Lock:** All operations inside `DbManager.cs` synchronize via `lock (_dbLock)`.
2. **Zero Nested Locks:** Never call other locking methods inside `lock (_dbLock)` to prevent deadlocks.
3. **Short Hold Time:** Minimize the scope of the lock: open connection, execute parameterized command, read data, and release lock immediately.
4. **xUnit Concurrency Armor:** Decorate test classes accessing `DbManager` with `[Collection(nameof(DatabaseTestCollection))]`.

