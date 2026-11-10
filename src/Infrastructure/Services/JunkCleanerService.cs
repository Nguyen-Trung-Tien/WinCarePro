using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;
using WinCarePro.Engines;
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro.Services.Implementations;

public class JunkCleanerService : IJunkCleanerService
{
    private readonly JunkCleanerEngine _engine;

    public event Action<string>? ProgressMessage;
    public event Action<int>? ProgressChanged;

    public JunkCleanerService() : this(App.Services?.GetService<JunkCleanerEngine>() ?? new JunkCleanerEngine())
    {
    }

    public JunkCleanerService(JunkCleanerEngine engine)
    {
        _engine = engine;
        _engine.ProgressMessage += msg => ProgressMessage?.Invoke(msg);
        _engine.ProgressChanged += pct => ProgressChanged?.Invoke(pct);
    }

    public Task<List<JunkCategory>> ScanJunkAsync(System.Threading.CancellationToken token = default)
    {
        return _engine.ScanJunkAsync(token);
    }

    public Task<long> CleanJunkAsync(List<JunkCategory> categories)
    {
        return _engine.CleanJunkAsync(categories);
    }
}
