using System;

namespace WinCarePro.Models;

public class OptimizationSummary
{
    public long JunkBytesCleaned { get; set; }
    public int RegistryIssuesFixed { get; set; }
    public long RamBytesReclaimed { get; set; }
    public int RamProcessesOptimized { get; set; }
    public long DoCacheBytesCleaned { get; set; }
    public bool DnsCacheFlushed { get; set; }
    public int TweaksApplied { get; set; }
}
