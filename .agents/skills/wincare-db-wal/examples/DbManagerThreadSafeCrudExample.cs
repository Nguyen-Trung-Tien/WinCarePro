using System;
using Microsoft.Data.Sqlite;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Database;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.Database;

/// <summary>
/// Reference example demonstrating thread-safe SQLite queries guarded strictly by lock (_dbLock).
/// Invariants:
/// - ALL SQLite operations in DbManager MUST be wrapped in lock (_dbLock).
/// - Parameterize all arguments to prevent SQL injection.
/// - Never execute external I/O or long-running CPU work inside lock (_dbLock).
/// </summary>
public sealed class DbManagerThreadSafeCrudExample
{
    private readonly object _dbLock = new();
    private readonly string _connectionString;

    public DbManagerThreadSafeCrudExample(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public OperationResult<bool> InsertAuditRecord(string action, string details)
    {
        try
        {
            lock (_dbLock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO AuditLogs (Timestamp, Action, Details)
                    VALUES (@timestamp, @action, @details);";

                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("O"));
                command.Parameters.AddWithValue("@action", action ?? string.Empty);
                command.Parameters.AddWithValue("@details", details ?? string.Empty);

                command.ExecuteNonQuery();
            }

            return OperationResult<bool>.Ok(true, "Audit record inserted successfully.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult<bool>.Fail($"Failed to insert audit record: {ex.Message}");
        }
    }
}

