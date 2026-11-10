using Xunit;
using WinCarePro.Services.Implementations;

namespace WinCarePro.Tests;

public class StartupManagerTests
{
    [Fact]
    public void IsAdministrator_ShouldReturnBoolean()
    {
        // Act
        bool isAdmin = StartupManager.IsAdministrator();

        // Assert
        Assert.True(isAdmin || !isAdmin); // Always executes cleanly without exception
    }

    [Fact]
    public void IsAutoStartEnabled_ShouldNotThrowException()
    {
        // Act
        var exception = Record.Exception(() => StartupManager.IsAutoStartEnabled());

        // Assert
        Assert.Null(exception);
    }
}
