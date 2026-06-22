using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class AiDiagnosticsEngineTests
{
    private readonly AiDiagnosticsEngine _engine = new();

    [Fact]
    public async Task RunHealthEvaluation_CleanSystem_ReturnsPerfectScore()
    {
        // Arrange
        long junkBytes = 0;
        int registryIssues = 0;
        int outdatedApps = 0;
        double avgLatency = 45.0; // healthy
        double packetLoss = 0.0; // healthy
        int startupApps = 3; // healthy
        var securityAudits = new List<string>(); // healthy
        
        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            junkBytes,
            registryIssues,
            outdatedApps,
            avgLatency,
            packetLoss,
            startupApps,
            securityAudits,
            cpuUsage: 10.0,
            cpuTemp: 45.0,
            ramUsagePercent: 35.0,
            servicesCount: 40,
            diskActiveTime: 1.0,
            freeSpacePercent: 80.0,
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(100, result.HealthScore);
        Assert.Empty(result.Recommendations);
        Assert.True(result.Results.Count > 0);
    }

    [Fact]
    public async Task RunHealthEvaluation_HighCpuUsage_DeductsPointsCorrectly()
    {
        // Arrange
        // CPU weight: 20%. Deduct 30 points on CPU.
        // Total score impact: 30 * 20% = 6 points deduction. Expected score: 94.
        
        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            0, 0, 0, 45.0, 0.0, 3, new List<string>(),
            cpuUsage: 95.0, // High CPU usage
            cpuTemp: 45.0,
            ramUsagePercent: 35.0,
            servicesCount: 40,
            diskActiveTime: 1.0,
            freeSpacePercent: 80.0,
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(94, result.HealthScore);
        Assert.Contains(result.Results, r => r.CheckName == "High CPU Usage" && !r.IsHealthy);
        Assert.Contains(result.Recommendations, r => r.Contains("CPU"));
    }

    [Fact]
    public async Task RunHealthEvaluation_HighCpuTemp_DeductsPointsCorrectly()
    {
        // Arrange
        // CPU weight: 20%. Temp > 90C deducts 40 points on CPU.
        // Total score impact: 40 * 20% = 8 points deduction. Expected score: 92.

        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            0, 0, 0, 45.0, 0.0, 3, new List<string>(),
            cpuUsage: 10.0,
            cpuTemp: 95.0, // High CPU Temp
            ramUsagePercent: 35.0,
            servicesCount: 40,
            diskActiveTime: 1.0,
            freeSpacePercent: 80.0,
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(92, result.HealthScore);
        Assert.Contains(result.Results, r => r.CheckName == "CPU Thermal Warning" && !r.IsHealthy);
    }

    [Fact]
    public async Task RunHealthEvaluation_HighRamUsage_DeductsPointsCorrectly()
    {
        // Arrange
        // Memory weight: 20%. RAM > 85% deducts 30 points. RAM > 90% deducts additional 20 points.
        // Total score impact: 50 * 20% = 10 points deduction. Expected score: 90.

        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            0, 0, 0, 45.0, 0.0, 3, new List<string>(),
            cpuUsage: 10.0,
            cpuTemp: 45.0,
            ramUsagePercent: 95.0, // High RAM usage (> 90%)
            servicesCount: 40,
            diskActiveTime: 1.0,
            freeSpacePercent: 80.0,
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(90, result.HealthScore);
        Assert.Contains(result.Results, r => r.CheckName == "High RAM Usage" && !r.IsHealthy);
        Assert.Contains(result.Results, r => r.CheckName == "Critical Commit Charge" && !r.IsHealthy);
    }

    [Fact]
    public async Task RunHealthEvaluation_LowDiskSpace_DeductsPointsCorrectly()
    {
        // Arrange
        // Storage weight: 15%. Free space < 10% deducts 30 points.
        // Total score impact: 30 * 15% = 4.5 points deduction (rounded to 4 or 5 depending on casting/clamping).
        // Since 30 * 0.15 = 4.5, 100 - 4.5 = 95.5. HealthScore is cast to int: (int)95.5 = 95.

        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            0, 0, 0, 45.0, 0.0, 3, new List<string>(),
            cpuUsage: 10.0,
            cpuTemp: 45.0,
            ramUsagePercent: 35.0,
            servicesCount: 40,
            diskActiveTime: 1.0,
            freeSpacePercent: 5.0, // Low Disk Space
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(95, result.HealthScore);
        Assert.Contains(result.Results, r => r.CheckName == "Low Free Space" && !r.IsHealthy);
    }

    [Fact]
    public async Task RunHealthEvaluation_SevereNetworkIssues_DeductsPointsCorrectly()
    {
        // Arrange
        // Network weight: 5%. Packet loss > 5% deducts 50 points. Latency > 200ms deducts 30 points.
        // Total network score = 100 - 80 = 20.
        // Total score impact: 80 * 5% = 4 points deduction. Expected score: 96.

        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            0, 0, 0, 250.0, 8.0, 3, new List<string>(), // high latency and packet loss
            cpuUsage: 10.0,
            cpuTemp: 45.0,
            ramUsagePercent: 35.0,
            servicesCount: 40,
            diskActiveTime: 1.0,
            freeSpacePercent: 80.0,
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(96, result.HealthScore);
        Assert.Contains(result.Results, r => r.CheckName == "Packet Loss Detection" && !r.IsHealthy);
        Assert.Contains(result.Results, r => r.CheckName == "Network Latency" && !r.IsHealthy);
    }

    [Fact]
    public async Task RunHealthEvaluation_HighServicesCount_DeductsPointsCorrectly()
    {
        // Arrange
        // Services weight: 10%. Services > 120 deducts 40 points.
        // Total score impact: 40 * 10% = 4 points deduction. Expected score: 96.

        // Act
        var result = await _engine.RunHealthEvaluationAsync(
            0, 0, 0, 45.0, 0.0, 3, new List<string>(),
            cpuUsage: 10.0,
            cpuTemp: 45.0,
            ramUsagePercent: 35.0,
            servicesCount: 130, // High services count
            diskActiveTime: 1.0,
            freeSpacePercent: 80.0,
            ssdHealthPercent: 100.0,
            isThrottling: false,
            isExplorerOptimized: true
        );

        // Assert
        Assert.Equal(96, result.HealthScore);
        Assert.Contains(result.Results, r => r.CheckName == "Excessive Background Services" && !r.IsHealthy);
    }
}
