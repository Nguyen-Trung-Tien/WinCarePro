using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

public interface INetworkHistoryService
{
    Task SaveSpeedTestResultAsync(SpeedTestResult result);
    Task<List<SpeedTestResult>> GetSpeedTestHistoryAsync(int limit = 120);
    Task SaveDnsBenchmarkResultAsync(List<DnsServerInfo> results);
    Task<List<List<DnsServerInfo>>> GetDnsBenchmarkHistoryAsync(int limit = 20);
    Task ClearHistoryAsync();
}
