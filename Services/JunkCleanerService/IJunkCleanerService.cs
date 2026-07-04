using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

public interface IJunkCleanerService
{
    event Action<string>? ProgressMessage;
    event Action<int>? ProgressChanged;
    Task<List<JunkCategory>> ScanJunkAsync(System.Threading.CancellationToken token = default);
    Task<long> CleanJunkAsync(List<JunkCategory> categories);
}
