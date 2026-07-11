using System;

namespace WinCarePro.Models;

public class RegistryIssue
{
    public string Section { get; set; } = ""; // e.g. "Shared DLLs", "Startup Programs"
    public string KeyPath { get; set; } = "";
    public string ValueName { get; set; } = "";
    public string ValueData { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSelected { get; set; } = true;
}

public class RegistryBackupItem
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
}
