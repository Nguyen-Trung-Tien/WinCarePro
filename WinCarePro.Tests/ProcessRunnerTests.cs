using System;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Tests;

public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_EchoCommand_ReturnsSuccessfulResult()
    {
        // Act
        var result = await ProcessRunner.RunAsync(
            "cmd.exe",
            "/c echo HelloProcessRunner",
            TimeSpan.FromSeconds(5)
        );

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("HelloProcessRunner", result.Output);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_ExitCodeCommand_ReturnsNonZeroExitCode()
    {
        // Act
        var result = await ProcessRunner.RunAsync(
            "cmd.exe",
            "/c exit 42",
            TimeSpan.FromSeconds(5)
        );

        // Assert
        Assert.Equal(42, result.ExitCode);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_CommandTimeout_ReturnsTimeoutResult()
    {
        // Act - Ping localhost 5 times takes ~4 seconds. We set a 1-second timeout.
        var result = await ProcessRunner.RunAsync(
            "ping.exe",
            "127.0.0.1 -n 5",
            TimeSpan.FromSeconds(1)
        );

        // Assert
        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
    }
}
