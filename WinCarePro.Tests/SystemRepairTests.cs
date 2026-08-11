using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using Xunit;

namespace WinCarePro.Tests;

public class SystemRepairTests
{
    private static readonly Regex PercentRegex = new(@"(\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);

    [Theory]
    [InlineData("Beginning verification phase of system scan.", -1)]
    [InlineData("Verification 15% complete.", 15)]
    [InlineData("Verification 85% complete.", 85)]
    [InlineData("Verification 100% complete.", 100)]
    [InlineData("[=========================== 45.0% ========================= ]", 45)]
    [InlineData("[==========================100.0%==========================]", 100)]
    public void PercentRegex_ExtractsProgressPercentageCorrectly(string line, int expectedPercent)
    {
        var match = PercentRegex.Match(line);
        if (expectedPercent < 0)
        {
            Assert.False(match.Success);
        }
        else
        {
            Assert.True(match.Success);
            Assert.True(double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out double pct));
            Assert.Equal(expectedPercent, (int)pct);
        }
    }

    [Theory]
    [InlineData("check", "/online /cleanup-image /checkhealth")]
    [InlineData("checkhealth", "/online /cleanup-image /checkhealth")]
    [InlineData("scan", "/online /cleanup-image /scanhealth")]
    [InlineData("scanhealth", "/online /cleanup-image /scanhealth")]
    [InlineData("restore", "/online /cleanup-image /restorehealth")]
    [InlineData("restorehealth", "/online /cleanup-image /restorehealth")]
    [InlineData("clean", "/online /cleanup-image /startcomponentcleanup")]
    [InlineData("cleancomponent", "/online /cleanup-image /startcomponentcleanup")]
    public void DismMode_MapsToCorrectArguments(string modeInput, string expectedArguments)
    {
        string modeClean = (modeInput ?? "").ToLowerInvariant().Trim();
        string arguments = modeClean switch
        {
            "check" or "checkhealth" => "/online /cleanup-image /checkhealth",
            "scan" or "scanhealth" => "/online /cleanup-image /scanhealth",
            "restore" or "restorehealth" => "/online /cleanup-image /restorehealth",
            "clean" or "cleancomponent" => "/online /cleanup-image /startcomponentcleanup",
            _ => "/online /cleanup-image /checkhealth"
        };

        Assert.Equal(expectedArguments, arguments);
    }

    [Fact]
    public async Task ProcessRunner_SupportsImmediateCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ProcessRunner.RunAsync(
                "cmd.exe",
                "/c ping 127.0.0.1 -n 10",
                TimeSpan.FromMinutes(1),
                cancellationToken: cts.Token
            );
        });
    }
}
