using System;
using System.Collections.Generic;

namespace WinCarePro.Services;

/// <summary>
/// Localized UI text mappings for WinCare Pro.
/// Central coordinator class dispatching to 15 modular partial translation classes.
/// Pre-allocates dictionary capacity (~4000 items) to prevent memory churn and GC fragmentation.
/// </summary>
public partial class TranslationManager
{
    private readonly Dictionary<string, IDictionary<string, string>> _cultureTranslations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indexer for accessing localized strings by key with zero breaking change.
    /// </summary>
    public string this[string key] => T(key);

    /// <summary>
    /// Retrieves localized string by key (alias for T).
    /// </summary>
    public string GetString(string key) => T(key);

    /// <summary>
    /// Gets translation dictionary for specific culture.
    /// </summary>
    public IDictionary<string, string> GetTranslations(string culture)
    {
        if (_cultureTranslations.TryGetValue(culture, out var dict))
        {
            return dict;
        }
        return _translations;
    }

    private void InitializeTranslations()
    {
        LoadAllTranslations();
        BuildReverseTranslations();
    }

    /// <summary>
    /// Coordinates loading of all 15 modular translations with pre-allocated capacity to prevent RAM reallocation.
    /// </summary>
    public void LoadAllTranslations()
    {
        _translations.EnsureCapacity(4000);
        var en = new Dictionary<string, string>(4000, StringComparer.OrdinalIgnoreCase);

        // Core & Shell
        RegisterCommonTranslations(_translations, en);
        RegisterDashboardTranslations(_translations, en);
        RegisterNotificationsTranslations(_translations, en);

        // Intelligence & Diagnostics
        RegisterRepairTranslations(_translations, en);

        // System Care & Storage
        RegisterDiskCleanerTranslations(_translations, en);
        RegisterUninstallTranslations(_translations, en);

        // Network
        RegisterNetworkTranslations(_translations, en);

        // Maintenance & Settings
        RegisterUpdatesTranslations(_translations, en);
        RegisterSettingsTranslations(_translations, en);

        // Populate English dictionary with identity fallback if not explicitly registered
        foreach (var key in _translations.Keys)
        {
            en.TryAdd(key, key);
        }

        _cultureTranslations["vi-VN"] = _translations;
        _cultureTranslations["vi"] = _translations;
        _cultureTranslations["en-US"] = en;
        _cultureTranslations["en"] = en;
    }

    // ========================================================================
    // Backward Compatibility Shims for Legacy Internal Loaders
    // ========================================================================
    private void LoadNavigationAndShellTranslations() => RegisterCommonTranslations(_translations, null!);
    private void LoadDashboardCockpitTranslations() => RegisterDashboardTranslations(_translations, null!);
    private void LoadCleanerStorageTranslations() => RegisterDiskCleanerTranslations(_translations, null!);
    private void LoadAppStudioTranslations() => RegisterUninstallTranslations(_translations, null!);
    private void LoadSpeedProcessTranslations() { }
    private void LoadSystemDoctorRegistryTranslations() => RegisterRepairTranslations(_translations, null!);
    private void LoadNetworkSecurityTranslations() => RegisterNetworkTranslations(_translations, null!);
    private void LoadSettingsCommonTranslations()
    {
        RegisterNotificationsTranslations(_translations, null!);
        RegisterUpdatesTranslations(_translations, null!);
        RegisterSettingsTranslations(_translations, null!);
    }
}
