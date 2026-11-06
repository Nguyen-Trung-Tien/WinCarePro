using System;
using System.IO;

namespace WinCarePro.Models;

public enum RegistryRiskLevel
{
    Safe = 0,
    Caution = 1,
    High = 2
}

public class RegistryIssue
{
    public string Section { get; set; } = ""; // e.g. "Shared DLLs", "Startup Programs"
    public string KeyPath { get; set; } = "";
    public string ValueName { get; set; } = "";
    public string ValueData { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSelected { get; set; } = true;
    public RegistryRiskLevel RiskLevel { get; set; } = RegistryRiskLevel.Safe;
    public string AiRecommendation { get; set; } = "";
    public string RootCauseApp { get; set; } = "";
    public string VisualDiffAction { get; set; } = "";

    public string RiskBadgeText => RiskLevel switch
    {
        RegistryRiskLevel.Safe => "Safe (100%)",
        RegistryRiskLevel.Caution => "Caution",
        _ => "High Risk"
    };

    public string RiskColorHex => RiskLevel switch
    {
        RegistryRiskLevel.Safe => "#10B981",
        RegistryRiskLevel.Caution => "#F59E0B",
        _ => "#EF4444"
    };

    private string _title = "";
    public string Title
    {
        get => !string.IsNullOrEmpty(_title) ? _title : (!string.IsNullOrEmpty(Section) ? Section : Description);
        set => _title = value;
    }
}

public class RegistryBackupItem
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";

    public string FormattedDate
    {
        get
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    return File.GetLastWriteTime(FilePath).ToString("yyyy-MM-dd HH:mm");
                }
                catch { }
            }
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public string FormattedSize
    {
        get
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    long bytes = new FileInfo(FilePath).Length;
                    if (bytes > 1024 * 1024)
                        return $"{(bytes / 1024.0 / 1024.0):F1} MB";
                    if (bytes > 1024)
                        return $"{(bytes / 1024.0):F0} KB";
                    return $"{bytes} B";
                }
                catch { }
            }
            return "--";
        }
    }
}
