using System;

namespace WinCarePro.Models;

public class SettingsProfile
{
    public string Theme { get; set; } = "Dark";
    public bool AutoScan { get; set; }
    public string ReportFormat { get; set; } = "TXT";
    
    // General & Update policy
    public int LanguageIndex { get; set; } = 0; // 0=English, 1=Tiếng Việt
    public bool AutoCheckUpdates { get; set; } = true;
    public bool AutoInstallUpdates { get; set; } = false; // Auto download & install in background
    public bool MinimizeToTray { get; set; } = true;
    public bool BetaUpdates { get; set; } = false; // Check for beta builds

    // Appearance
    public string AccentColor { get; set; } = "Default"; // Default, Green, Purple, Pink, Amber
    public double TransparencyLevel { get; set; } = 80.0;
    public bool EnableAnimations { get; set; } = true;

    // Auto Maintenance
    public double AutoCleanupTriggerSizeGB { get; set; } = 5.0;
    public bool TriggerSmartBoost { get; set; } = true;
    public int MaintenanceFrequencyIndex { get; set; } = 1; // 0=Daily, 1=Weekly, 2=Monthly

    // Telemetry Diagnostics
    public int TelemetryIntervalIndex { get; set; } = 1; // 0=0.5s, 1=1.0s, 2=2.0s, 3=5.0s
    public int PerformanceHistoryDurationIndex { get; set; } = 0; // 0=7 Days, 1=30 Days, 2=90 Days
    public bool EnableSensorsThread { get; set; } = true;

    // Safety & Data Security
    public bool CreateRestorePoint { get; set; } = true;
    public bool BackupRegistryHive { get; set; } = true;
    public double ConfirmationAlertsLevel { get; set; } = 2.0;

    // Advanced Developer
    public bool EnableVerboseLogs { get; set; } = false;
    public bool EnableExperimentalAi { get; set; } = false;

    // Notifications Settings
    public bool ShowNotifications { get; set; } = true;
    public double NotificationThreshold { get; set; } = 10.0; // Show notification if health score drops by more than this
    public bool NotifyOnLowHealth { get; set; } = true;
    public bool NotifyOnMaintenance { get; set; } = true;
    public bool ShowUpdateNotifications { get; set; } = true;
    public bool NotificationSound { get; set; } = true;
}
