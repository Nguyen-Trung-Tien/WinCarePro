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
        "StateRepository",// State Repository Service
        "gpsvc",         // Group Policy Client
        "ProfSvc",       // User Profile Service
        "BFE",           // Base Filtering Engine
        "BrokerInfrastructure", // Background Tasks Infrastructure
        "SystemEventsBroker"    // System Events Broker
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
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        return ProtectedServices.Contains(serviceName.Trim());
    }

    public bool IsSecurityService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        string name = serviceName.Trim();
        return SecurityRelatedServices.Contains(name) || name.Contains("defender", StringComparison.OrdinalIgnoreCase) || name.Contains("antivirus", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsProtectedService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        return IsCriticalService(serviceName) || IsSecurityService(serviceName);
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
