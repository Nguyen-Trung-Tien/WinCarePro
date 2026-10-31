---
name: wincare-db-wal
description: >-
  SQLite 3 WAL Mode persistence and synchronization rules for WinCare Pro. Covers DbManager usage, mandatory lock (_dbLock) synchronization, parameterized queries, schema migrations, avoiding lock contention, and preventing test collisions via DatabaseTestCollection. Use whenever accessing, querying, or modifying the SQLite database.
---

# 🗄️ WinCare Pro — SQLite 3 WAL Mode Persistence Standards

WinCare Pro utilizes SQLite 3 configured with Write-Ahead Logging (WAL Mode) for safe and performant concurrent local persistence.

## 📁 Skill Assets & Helpers
- **Inspection Script:** [`scripts/verify-db-wal-schema.ps1`](./scripts/verify-db-wal-schema.ps1)
- **Reference Examples:**
  - Thread-Safe CRUD: [`examples/DbManagerThreadSafeCrudExample.cs`](./examples/DbManagerThreadSafeCrudExample.cs)
  - xUnit Test Fixture: [`examples/DatabaseTestFixtureExample.cs`](./examples/DatabaseTestFixtureExample.cs)
- **Configuration & Architecture:**
  - WAL Pragmas & Guidelines: [`resources/sqlite-wal-configuration.md`](./resources/sqlite-wal-configuration.md)

---

## 🏛️ 1. Inviolable SQLite Concurrency Rules

1. **Centralized Lock Synchronization:**
   Every SQLite read or write in [DbManager.cs](../../Infrastructure/Database/DbManager.cs) MUST be guarded with `lock (_dbLock)`:
   ```csharp
   lock (_dbLock)
   {
       using var connection = CreateConnection();
       connection.Open();
       // Execute parameterized query
   }
   ```
2. **Never Execute I/O Inside Lock:**
   Network calls, file copies, and heavy algorithms must never run inside the database lock.
3. **Always Parameterize Queries:**
   Direct string interpolation (`$"SELECT * FROM ... WHERE id = '{id}'"`) is strictly forbidden.
4. **xUnit Database Collection:**
   Tests touching SQLite must include:
   ```csharp
   [Collection(nameof(DatabaseTestCollection))]
   ```
