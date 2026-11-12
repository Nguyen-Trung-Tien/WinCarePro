using System;

namespace WinCarePro.Models;

public class DnsProfileOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string PrimaryDns { get; set; } = string.Empty;
    public string SecondaryDns { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDhcp => string.IsNullOrWhiteSpace(PrimaryDns);

    public string DisplayText => IsDhcp ? $"{Name} (DHCP)" : $"{Name} ({PrimaryDns} / {SecondaryDns})";

    public override string ToString() => DisplayText;
}

