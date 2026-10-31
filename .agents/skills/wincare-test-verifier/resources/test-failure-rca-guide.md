# 🔍 WinCare Pro — Test Failure Root Cause Analysis (RCA) Guide

## Common Failure Classes

### 1. SQLite Busy / Lock Contention
- **Symptom:** `SqliteException: SQLite Error 5: 'database is locked'`.
- **Root Cause:** Two parallel xUnit test classes attempted to write to `wincare.db` concurrently without sharing a collection.
- **Fix:** Decorate the test class with `[Collection(nameof(DatabaseTestCollection))]`.

### 2. Path Access Denied
- **Symptom:** `UnauthorizedAccessException: Access to the path is denied`.
- **Root Cause:** Test attempted to write to a protected root directory or system folder instead of `Path.GetTempPath()`.
- **Fix:** Ensure tests execute within isolated temporary test directories cleaned up in `Dispose()`.

### 3. Culture / Decimal Separator Mismatch
- **Symptom:** String format asserts fail on commas vs dots (`"12,5%"` vs `"12.5%"`).
- **Root Cause:** Formatted values depend on current system culture.
- **Fix:** Format numeric metrics using `CultureInfo.InvariantCulture`.

