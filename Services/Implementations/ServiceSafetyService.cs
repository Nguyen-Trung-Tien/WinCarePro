using System;
using System.Collections.Generic;

namespace WinCarePro.Services.Implementations;

public class ServiceSafetyService
{
    private static readonly HashSet<string> ProtectedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "wuauserv",      // Windows Update
        "WinDefend",     // Microsoft Defender Antivirus Service
        "wscsvc",        // Windows Security Center
        "Sense",         // Windows Defender Advanced Threat Protection
        "RpcSs",         // Remote Procedure Call (RPC)
        "RpcEptMapper",  // RPC Endpoint Mapper
        "EventLog",      // Windows Event Log
        "Dhcp",          // DHCP Client
        "Dnscache",      // DNS Client
        "PlugPlay",      // Plug and Play
        "DcomLaunch",    // DCOM Server Process Launcher
        "SamSs",         // Security Accounts Manager
        "LSM",           // Local Session Manager
        "StateRepository"// State Repository Service
    };

    private static readonly HashSet<string> SecurityRelatedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "mpssvc",        // Windows Defender Firewall
        "KeyIso",        // CNG Key Isolation
        "PolicyAgent",   // IPsec Policy Agent
        "CryptSvc",      // Cryptographic Services
        "AppIDSvc"       // Application Identity
    };

    public bool IsCriticalService(string serviceName)
    {
        return ProtectedServices.Contains(serviceName);
    }

    public bool IsSecurityService(string serviceName)
    {
        return SecurityRelatedServices.Contains(serviceName) || serviceName.Contains("defender", StringComparison.OrdinalIgnoreCase) || serviceName.Contains("antivirus", StringComparison.OrdinalIgnoreCase);
    }

    public string GetSafetyWarning(string serviceName, string action)
    {
        if (IsCriticalService(serviceName))
        {
            return $"Action Blocked: '{serviceName}' is a core system service. Disabling or stopping it will cause Windows to crash or stop functioning correctly.";
        }
        if (IsSecurityService(serviceName) && (action == "Stop" || action == "Disable" || action == "Disabled"))
        {
            return $"Warning: '{serviceName}' is a security-related service. Disabling it may expose your computer to malicious software or security vulnerabilities.";
        }
        return "";
    }
}
