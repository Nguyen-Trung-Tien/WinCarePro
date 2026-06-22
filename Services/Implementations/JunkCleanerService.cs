using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;
using WinCarePro.Engines;

namespace WinCarePro.Services.Implementations;

public class JunkCleanerService : IJunkCleanerService
{
    private readonly JunkCleanerEngine _engine;

    public event Action<string>? ProgressMessage;
    public event Action<int>? ProgressChanged;

    public JunkCleanerService()
    {
        _engine = new JunkCleanerEngine();
        _engine.ProgressMessage += msg => ProgressMessage?.Invoke(msg);
        _engine.ProgressChanged += pct => ProgressChanged?.Invoke(pct);
    }

    public Task<List<JunkCategory>> ScanJunkAsync()
    {
        return _engine.ScanJunkAsync();
    }

    public Task<long> CleanJunkAsync(List<JunkCategory> categories)
    {
        return _engine.CleanJunkAsync(categories);
    }
}
