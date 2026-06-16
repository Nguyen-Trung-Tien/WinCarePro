using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.Views;

public sealed partial class HardwarePage : Page
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly HardwareDriverEngine _engine = new();
    private List<DriverInfo> _allDrivers = new();

    private HardwareSpecs _specs = new();
    private bool _isLoading = true;

    public HardwareSpecs Specs
    {
        get => _specs;
        set
        {
            _specs = value;
            Bindings.Update();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            Bindings.Update();
        }
    }

    public ObservableCollection<DriverInfo> FilteredDrivers { get; } = new();

    public HardwarePage()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        IsLoading = true;
        
        var specsResult = await Task.Run(() => _engine.GetHardwareSpecifications());
        var driversResult = await Task.Run(() => _engine.GetInstalledDrivers());

        _dispatcherQueue.TryEnqueue(() =>
        {
            Specs = specsResult;
            _allDrivers = driversResult;
            ApplyDriverFilter();
            IsLoading = false;
        });
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        LoadDataAsync();
    }

    private void OnDriverSearchChanged(object sender, TextChangedEventArgs e)
    {
        ApplyDriverFilter();
    }

    private void ApplyDriverFilter()
    {
        string query = DriverSearchInput.Text.Trim().ToLower();
        FilteredDrivers.Clear();
        
        var filtered = _allDrivers.AsEnumerable();
        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(d => d.Name.ToLower().Contains(query) || d.Provider.ToLower().Contains(query) || d.DeviceClass.ToLower().Contains(query));
        }

        foreach (var driver in filtered.Take(100)) // Cap list visual presentation
        {
            FilteredDrivers.Add(driver);
        }
    }

    internal bool IsNot(bool val) => !val;

    internal string GetCpuDetailString(int cores, int threads, string speed)
    {
        return $"Cores: {cores} | Threads: {threads} | Core Clock: {speed}";
    }

    internal string GetGpuDetailString(string vram, string driverVer)
    {
        return $"Video Memory (VRAM): {vram} | Driver Version: {driverVer}";
    }

    internal string GetMotherboardString(string manufacturer, string model)
    {
        return $"{manufacturer} {model}";
    }

    internal string FormatUptime(string uptime) => $"System Uptime: {uptime}";
    internal string FormatRamSize(double ramCapacity) => $"{ramCapacity:F1} GB";
    internal string FormatRamSpeed(string ramSpeed) => $"Frequency Speed: {ramSpeed}";
    internal string FormatBiosVersion(string biosVer) => $"BIOS Version: {biosVer}";

    internal static string GetUpdateIcon(bool hasUpdate)
    {
        return hasUpdate ? "\uE895" : "\uE73E"; // Update or Checkmark glyph
    }

    internal static string GetUpdateLabel(bool hasUpdate, string availableVersion)
    {
        return hasUpdate ? $"Update Available: {availableVersion}" : "Up to Date";
    }

    internal static Brush GetUpdateColor(bool hasUpdate)
    {
        var color = hasUpdate ? Colors.Orange : Colors.LightGreen;
        return new SolidColorBrush(color);
    }
}
