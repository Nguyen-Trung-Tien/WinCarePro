using System;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;
using Xunit;

namespace WinCarePro.Tests;

/// <summary>
/// Canonical xUnit test template for WinCare Pro.
/// Demonstrates proper mocking/isolation, cancellation tests, and database collection grouping if SQLite is touched.
/// </summary>
// NOTE: Uncomment the [Collection] attribute below if this test class interacts with DbManager / SQLite:
// [Collection(nameof(DatabaseTestCollection))]
public class TemplateEngineTests
{
    private readonly CrashLogger _crashLogger;
    private readonly TemplateEngine _engine;

    public TemplateEngineTests()
    {
        _crashLogger = new CrashLogger();
        _engine = new TemplateEngine(_crashLogger);
    }

    [Fact]
    public async Task ExecuteSafeOperation_WithEmptyPath_ReturnsFail()
    {
        // Act
        var result = await _engine.ExecuteSafeOperationAsync(string.Empty, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteSafeOperation_WithSystemProtectedPath_IsBlockedByGuard()
    {
        // Act: Attempt to target protected Windows directory
        var result = await _engine.ExecuteSafeOperationAsync(@"C:\Windows\System32", CancellationToken.None);

        // Assert: SafePathGuard must block it
        Assert.False(result.Success);
        Assert.Contains("protected", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteSafeOperation_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _engine.ExecuteSafeOperationAsync(@"C:\SafePath", cts.Token);
        });
    }
}
