using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;
using WinCarePro.Shared.Components;

namespace WinCarePro.Views;

public sealed partial class DiskPage : Page
{
    public DiskViewModel ViewModel { get; }

    public DiskPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<DiskViewModel>();
        this.DataContext = ViewModel;
        this.Loaded += (s, e) => TranslationManager.Instance.Translate(this);

        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;
        this.Unloaded += (s, e) =>
        {
            TranslationManager.Instance.LanguageChanged -= langHandler;
            ViewModel.Cleanup();
        };
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    private async void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                ViewModel.StorageScanPath = folder.Path;
            }
        }
        catch { }
    }

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.AnalyzeStorageAsync();
    }

    private async void OnAnalyzeSpaceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.AnalyzeStorageAsync();
    }

    private async void OnGoUpDirectoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            var parent = System.IO.Directory.GetParent(ViewModel.StorageScanPath);
            if (parent != null && parent.Exists)
            {
                ViewModel.StorageScanPath = parent.FullName;
                await ViewModel.AnalyzeStorageAsync();
            }
        }
        catch { }
    }

    private async void OnScanDuplicatesClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.FindDuplicatesAsync();
    }

    private async void OnDeleteDuplicatesClick(object sender, RoutedEventArgs e)
    {
        int count = 0;
        foreach (var group in ViewModel.DuplicateGroups)
        {
            foreach (var item in group.Items)
            {
                if (item.IsSelectedForDeletion) count++;
            }
        }

        if (count == 0)
        {
            await ResultDialogHelper.ShowWarningAsync(
                this.XamlRoot,
                "No Items Selected",
                "Please select at least one duplicate file to delete.");
            return;
        }

        bool confirmed = await ResultDialogHelper.ShowConfirmAsync(
            this.XamlRoot,
            "Confirm Duplicate File Deletion",
            $"Are you sure you want to permanently delete {count} selected duplicate file(s)? This action cannot be undone.",
            confirmText: "Delete Permanently",
            cancelText: "Cancel",
            isDestructive: true);

        if (confirmed)
        {
            await ViewModel.CleanSelectedDuplicatesAsync();
        }
    }

    private async void OnRunChkdskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WinCarePro.Models.DriveHealthInfo drive)
        {
            await ViewModel.RunChkdskAsync(drive.Name);
        }
    }

    private async void OnCleanEmptyFoldersClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearEmptyFoldersAsync();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e) => ViewModel.SelectAllDuplicates();
    private void OnDeselectAllClick(object sender, RoutedEventArgs e) => ViewModel.DeselectAllDuplicates();
    private void OnKeepNewestClick(object sender, RoutedEventArgs e) => ViewModel.SelectKeepNewest();
    private void OnKeepOldestClick(object sender, RoutedEventArgs e) => ViewModel.SelectKeepOldest();

    private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Optional tracking or cleanup
    }

    private async void StorageItem_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is WinCarePro.Models.StorageItem item)
        {
            if (item.IsDirectory)
            {
                ViewModel.StorageScanPath = item.Path;
                await ViewModel.AnalyzeStorageAsync();
            }
        }
    }

    public bool IsNot(bool val) => !val;

    public static Brush GetHealthColor(string status)
    {
        var color = (status == "Healthy" || status == "OK") ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 239, 68, 68);
        return new SolidColorBrush(color);
    }

    public static Brush GetHealthBadgeBg(string status)
    {
        var color = (status == "Healthy" || status == "OK") ? Windows.UI.Color.FromArgb(30, 16, 185, 129) : Windows.UI.Color.FromArgb(30, 239, 68, 68);
        return new SolidColorBrush(color);
    }

    public static Brush GetTempColor(double temp)
    {
        var color = temp > 45.0 ? Windows.UI.Color.FromArgb(255, 245, 158, 11) : Windows.UI.Color.FromArgb(255, 16, 185, 129);
        return new SolidColorBrush(color);
    }

    public static Brush GetTempBadgeBg(double temp)
    {
        var color = temp > 45.0 ? Windows.UI.Color.FromArgb(30, 245, 158, 11) : Windows.UI.Color.FromArgb(30, 16, 185, 129);
        return new SolidColorBrush(color);
    }

    public static string GetTypeIcon(bool isDirectory)
    {
        return isDirectory ? "\uE8B7" : "\uE7C3"; // Folder or File glyph
    }

    public static string FormatTemp(double temp) => $"{temp:F0}°C";
    public static string FormatDuplicateGroupSize(string size) => $"Duplicate Group - Size: {size}";
}
