using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Engines.Templates;
using Xunit;

namespace WinCarePro.Tests.Templates;

/// <summary>
/// Canonical unit test template for WinCare Pro modules.
/// If database operations are involved, decorate with [Collection(nameof(DatabaseTestCollection))].
/// </summary>
public sealed class ModuleTestsTemplate
{
    private readonly EngineTemplate _engine;

    public ModuleTestsTemplate()
    {
        _engine = new EngineTemplate();
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithSafePath_ReturnsSuccess()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        string safePath = @"C:\Users\Public\Temp\test.tmp";

        // Act
        var result = await _engine.ExecuteTaskAsync(safePath, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WhenCancelled_ReturnsFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        // Act
        var result = await _engine.ExecuteTaskAsync(@"C:\Users\Public\Temp\test.tmp", cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Message, System.StringComparison.OrdinalIgnoreCase);
    }
}

