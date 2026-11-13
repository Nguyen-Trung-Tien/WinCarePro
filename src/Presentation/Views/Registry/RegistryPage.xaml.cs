using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;

using WinCarePro.Core.Helpers;
using WinCarePro.Shared.Components;
using System.Linq;

namespace WinCarePro.Views;

public sealed partial class RegistryPage : Page
{
    public RegistryViewModel ViewModel { get; }
    private readonly System.ComponentModel.PropertyChangedEventHandler _vmPropertyChangedHandler;

    public static readonly DependencyProperty WideLayoutVisibilityProperty =
        DependencyProperty.Register(nameof(WideLayoutVisibility), typeof(Visibility), typeof(RegistryPage), new PropertyMetadata(Visibility.Visible));

    public Visibility WideLayoutVisibility
    {
        get => (Visibility)GetValue(WideLayoutVisibilityProperty);
        set => SetValue(WideLayoutVisibilityProperty, value);
    }

    public RegistryPage()
    {
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RegistryViewModel>();
        this.DataContext = ViewModel;

        _vmPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy))
            {
                UpdateLoadingOverlayState();
            }
        };
        ViewModel.PropertyChanged += _vmPropertyChangedHandler;

        this.Loaded += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            UpdateLoadingOverlayState();
        };

        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        this.Unloaded += (s, e) =>
        {
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            TranslationManager.Instance.Translate(this);
            Bindings.Update();
        });
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel?.Initialize();
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            ViewModel.PropertyChanged += _vmPropertyChangedHandler;
        }
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
        TranslationManager.Instance.Translate(this);
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            ViewModel.Cleanup();
        }
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.ScanRegistryAsync();
    }

    private void OnIssueSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is WinCarePro.Models.RegistryIssue issue)
        {
            ViewModel.SelectedIssue = issue;
        }
    }

    private void OnSelectSafeOnlyClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectSafeOnly();
        Bindings.Update();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAll(true);
        Bindings.Update();
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAll(false);
        Bindings.Update();
    }

    private async void OnSafeClean1Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectSafeOnly();
        var safeCount = ViewModel.Issues.Count(i => i.IsSelected);
        if (safeCount == 0) return;

        var confirmed = await ResultDialogHelper.ShowConfirmAsync(
            XamlRoot,
            $"Safely clean all {safeCount} verified zero-risk registry entries?\n\n• Safety Guarantee: Only non-critical remnants (dead App Paths, missing file handler targets, and obsolete MRU records) will be removed.\n• Auto-Snapshot: A full registry rollback file will be generated automatically.",
            $"1-Click Safe Registry Clean ({safeCount} items)",
            "Clean Safe Items Now",
            "Cancel");

        if (!confirmed) return;
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.08f, 350);
        await ViewModel.RepairSelectedAsync();
        if (sender is Button btnDone) WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(btnDone, 1.10f, 400);
    }

    private async void OnRepairSingleIssueClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedIssue == null) return;
        var target = ViewModel.SelectedIssue;

        var confirmed = await ResultDialogHelper.ShowConfirmAsync(
            XamlRoot,
            $"Repair registry entry '{target.Title}'?\n\nKey: {target.KeyPath}\nAction: {target.VisualDiffAction}",
            "Repair Single Entry",
            "Repair Now",
            "Cancel");

        if (!confirmed) return;
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.RepairSingleIssueAsync(target);
    }

    private async void OnRestoreBackupItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is WinCarePro.Models.RegistryBackupItem item)
        {
            var confirmed = await ResultDialogHelper.ShowConfirmAsync(
                XamlRoot,
                $"Restore registry state from snapshot '{item.Name}'?\n\nFile: {item.FilePath}\nDate: {item.FormattedDate}\n\nThis will re-import the saved registry values back into your Windows system.",
                "Restore Registry Snapshot",
                "Restore Snapshot",
                "Cancel");

            if (!confirmed) return;
            await ViewModel.RestoreBackupAsync(item);
        }
    }

    private async void OnRepairClick(object sender, RoutedEventArgs e)
    {
        var count = ViewModel.Issues.Count(i => i.IsSelected);
        if (count == 0) return;

        var confirmed = await ResultDialogHelper.ShowConfirmAsync(
            XamlRoot,
            $"Are you sure you want to repair {count} selected registry entries?\n\n• Impact Scope: Invalid paths, orphaned shell entries, and missing MUI references.\n• Backup Availability: An automated registry backup (.reg) will be created before modifying any keys.",
            $"Repair {count} Registry Entries",
            "Repair Registry Now",
            "Cancel");

        if (!confirmed) return;

        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.08f, 350);
        await ViewModel.RepairSelectedAsync();
        if (sender is Button btnDone) WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(btnDone, 1.10f, 400);
    }

    // Registry Editor shortcut — safe alternative to registry cleaner
    private void OnOpenRegeditClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            string? safePath = WinCarePro.Core.Helpers.ProcessRunner.ResolveSafeExecutablePath("regedit.exe");
            if (safePath != null)
                Process.Start(new ProcessStartInfo(safePath) { UseShellExecute = true, WorkingDirectory = Environment.SystemDirectory });
        }
        catch { }
    }

    // System Restore shortcut
    private void OnOpenSystemRestoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            string? safePath = WinCarePro.Core.Helpers.ProcessRunner.ResolveSafeExecutablePath("rstrui.exe");
            if (safePath != null)
                Process.Start(new ProcessStartInfo(safePath) { UseShellExecute = true, WorkingDirectory = Environment.SystemDirectory });
        }
        catch { }
    }

    // Registry Backup — safe backup
    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.BackupRegistryAsync();
    }

    public bool IsNot(bool b) => !b;
    public static Visibility VisibleIfZero(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility VisibleIfPositive(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility VisibleIfNotNull(object? obj) => obj != null ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility VisibleIfNull(object? obj) => obj == null ? Visibility.Visible : Visibility.Collapsed;
    public static bool IsNotNull(object? obj) => obj != null;
    public static bool CanCleanSelected(int count, bool isBusy) => count > 0 && !isBusy;
    public static bool CanSafeClean(int safeCount, bool isBusy) => safeCount > 0 && !isBusy;

    private void UpdateLoadingOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        if (ViewModel.IsBusy)
        {
            LoadingOverlayGrid.Visibility = Visibility.Visible;
            FadeInLoading.Begin();
        }
        else
        {
            FadeOutLoading.Begin();
        }
    }

    private void FadeOutLoading_Completed(object? sender, object e)
    {
        if (!ViewModel.IsBusy && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}
