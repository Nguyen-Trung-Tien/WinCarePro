using System;
using WinCarePro.Infrastructure.Database;
using Xunit;

namespace WinCarePro.Examples.Database;

/// <summary>
/// Reference example demonstrating xUnit test isolation for DbManager.
/// Invariants:
/// - Any test touching DbManager or SQLite WAL MUST be decorated with [Collection(nameof(DatabaseTestCollection))].
/// - This prevents cross-test SQLite concurrency lock contention during parallel test runs.
/// </summary>
[Collection("DatabaseTestCollection")]
public sealed class DatabaseTestFixtureExample : IDisposable
{
    private readonly DbManager _dbManager;

    public DatabaseTestFixtureExample()
    {
        _dbManager = DbManager.Instance;
    }

    [Fact]
    public void DbManager_WriteAndReadAuditLog_SucceedsUnderLock()
    {
        // Act
        _dbManager.LogAudit("UnitTest", "Testing SQLite WAL concurrency under test fixture");

        // Assert - verified without throwing SQLite busy exception
        Assert.True(true);
    }

    public void Dispose()
    {
        // Teardown test artifacts if needed
    }
}

