using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Views;

public sealed partial class StartupPage : Page
{
    public StartupViewModel ViewModel { get; }

    public static readonly DependencyProperty PublisherColWidthProperty =
        DependencyProperty.Register(nameof(PublisherColWidth), typeof(GridLength), typeof(StartupPage), new PropertyMetadata(new GridLength(2, GridUnitType.Star)));

    public GridLength PublisherColWidth
    {
        get => (GridLength)GetValue(PublisherColWidthProperty);
        set => SetValue(PublisherColWidthProperty, value);
    }

    public static readonly DependencyProperty ImpactColWidthProperty =
        DependencyProperty.Register(nameof(ImpactColWidth), typeof(GridLength), typeof(StartupPage), new PropertyMetadata(new GridLength(1.2, GridUnitType.Star)));

    public GridLength ImpactColWidth
    {
        get => (GridLength)GetValue(ImpactColWidthProperty);
        set => SetValue(ImpactColWidthProperty, value);
    }

    public static readonly DependencyProperty WideLayoutVisibilityProperty =
        DependencyProperty.Register(nameof(WideLayoutVisibility), typeof(Visibility), typeof(StartupPage), new PropertyMetadata(Visibility.Visible));

    public Visibility WideLayoutVisibility
    {
        get => (Visibility)GetValue(WideLayoutVisibilityProperty);
        set => SetValue(WideLayoutVisibilityProperty, value);
    }

    public StartupPage()
    {
        ViewModel = App.Services.GetRequiredService<StartupViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsLoading))
            {
                UpdateLoadingOverlayState();
            }
        };

        this.Loaded += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            UpdateLoadingOverlayState();
        };

        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;
        this.Unloaded += (s, e) => TranslationManager.Instance.LanguageChanged -= langHandler;

        this.SizeChanged += (s, e) =>
        {
            bool isWide = e.NewSize.Width >= 800;
            PublisherColWidth = isWide ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
            ImpactColWidth = isWide ? new GridLength(1.2, GridUnitType.Star) : new GridLength(0);
            WideLayoutVisibility = isWide ? Visibility.Visible : Visibility.Collapsed;

        };
    }

    private async void OnReloadStartupClick(object sender, RoutedEventArgs e)
    {
        var btn = ScanButton ?? (sender as Button);
        if (btn != null) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanSystemRing, ScanSystemText, ScanSystemIcon,
            "Scanning System...", "Scan System",
            async () =>
            {
                await ViewModel.LoadAllDataAsync();
            },
            minDurationMs: 1000);
    }

    private void OnOpenServicesMscClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            if (WinCarePro.Infrastructure.Security.InputSanitizer.IsSafeUri("services.msc"))
                Process.Start(new ProcessStartInfo("services.msc") { UseShellExecute = true });
        }
        catch { }
    }

    private void OnOpenTaskMgrServicesClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            if (WinCarePro.Infrastructure.Security.InputSanitizer.IsSafeUri("taskmgr.exe"))
                Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true });
        }
        catch { }
    }

    private async void OnQuickOptimizeClick(object sender, RoutedEventArgs e)
    {
        var btn = QuickOptimizeBtn ?? (sender as Button);
        if (btn != null) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.08f, 350);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, BoostSpeedRing, BoostSpeedText, null,
            "Boosting Boot Speed...", "Boost Boot Speed",
            async () =>
            {
                await ViewModel.OptimizeStartupAppsAsync();
            },
            minDurationMs: 1200);

        var dialog = new ContentDialog
        {
            Title = "Startup & Services Optimization".T(),
            Content = ViewModel.StatusText,
            CloseButtonText = "OK".T(),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            RequestedTheme = ThemeManager.Instance.CurrentTheme
        };
        try
        {
            await dialog.ShowAsync();
        }
        catch { }
    }

    private async void OnUndoClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UndoLastChangeAsync();
    }

    private async void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsLoading) return;

        if (sender is ToggleSwitch ts && ts.DataContext is StartupEntry entry)
        {
            if (ts.FocusState == FocusState.Unfocused) return;

            if (entry.IsEnabled != ts.IsOn)
            {
                await ViewModel.ToggleStartupAppAsync(entry, ts.IsOn);
            }
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts && lts.DataContext is StartupEntry entryLts)
        {
            if (entryLts.IsEnabled != lts.IsOn)
            {
                lts.IsLoading = true;
                try
                {
                    await ViewModel.ToggleStartupAppAsync(entryLts, lts.IsOn);
                }
                finally
                {
                    lts.IsLoading = false;
                }
            }
        }
    }

    private async void OnDeleteStartupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is StartupEntry entry)
        {
            var dialogService = App.Services.GetService<IDialogService>();
            dialogService?.SetXamlRoot(this.XamlRoot);
            var dialog = new ContentDialog
            {
                Title = "Permanently Remove Startup Entry".T(),
                Content = string.Format("Are you sure you want to permanently remove '{0}' from startup? This cannot be undone automatically.".T(), entry.Name),
                PrimaryButtonText = "Remove".T(),
                CloseButtonText = "Cancel".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveStartupAppAsync(entry);
            }
        }
    }

    private async void OnServiceControlClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ServiceEntry entry && btn.Tag is string action)
        {
            await ViewModel.ControlServiceAsync(entry, action);
        }
    }

    private async void OnServiceStartupTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsLoading) return;
        if (sender is ComboBox cb && cb.DataContext is ServiceEntry entry && cb.SelectedValue is string newType)
        {
            if (cb.FocusState == FocusState.Unfocused) return;

            if (entry.StartupType != newType)
            {
                var dialogService = App.Services.GetService<IDialogService>();
                if (newType == "Disabled" && (entry.IsCriticalService || entry.IsMicrosoftService))
                {
                    dialogService?.SetXamlRoot(this.XamlRoot);
                    var dialog = new ContentDialog
                    {
                        Title = "Disable System Service".T(),
                        Content = string.Format("Disabling system service '{0}' may affect Windows stability. Do you want to proceed?".T(), entry.DisplayName),
                        PrimaryButtonText = "Disable Service".T(),
                        CloseButtonText = "Cancel".T(),
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot,
                        RequestedTheme = ThemeManager.Instance.CurrentTheme
                    };
                    var result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary)
                    {
                        cb.SelectedValue = entry.StartupType;
                        return;
                    }
                }

                await ViewModel.ChangeServiceStartupTypeAsync(entry, newType);
            }
        }
    }

    private async void OnTaskToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsLoading) return;

        if (sender is ToggleSwitch ts && ts.DataContext is ScheduledTaskEntry entry)
        {
            if (ts.FocusState == FocusState.Unfocused) return;

            if (entry.IsEnabled != ts.IsOn)
            {
                await ViewModel.ToggleScheduledTaskAsync(entry, ts.IsOn);
            }
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts && lts.DataContext is ScheduledTaskEntry entryLts)
        {
            if (entryLts.IsEnabled != lts.IsOn)
            {
                lts.IsLoading = true;
                try
                {
                    await ViewModel.ToggleScheduledTaskAsync(entryLts, lts.IsOn);
                }
                finally
                {
                    lts.IsLoading = false;
                }
            }
        }
    }

    private async void OnDeleteTaskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ScheduledTaskEntry entry)
        {
            var dialogService = App.Services.GetService<IDialogService>();
            dialogService?.SetXamlRoot(this.XamlRoot);
            
            string message = string.Format("Are you sure you want to permanently delete the scheduled task '{0}'?".T(), entry.Name);
            if (entry.IsMicrosoftTask)
            {
                message = string.Format("Warning: '{0}' is a Microsoft scheduled task. Deleting it may cause Windows system features to stop working. Are you sure you want to delete?".T(), entry.Name);
            }

            var dialog = new ContentDialog
            {
                Title = "Delete Scheduled Task".T(),
                Content = message,
                PrimaryButtonText = "Delete Task".T(),
                CloseButtonText = "Cancel".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteScheduledTaskAsync(entry);
            }
        }
    }

    private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Optional pivot focus updates
    }

    // Helper converters called directly from XAML bindings
    public Brush ScoreToBrush(double score) => score switch
    {
        >= 80 => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)), // Green
        >= 60 => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)), // Amber
        _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)) // Red
    };

    public string ScoreToString(double score) => $"{score}%";

    public string ScoreToStatusText(double score) => score switch
    {
        >= 85 => "Excellent".T(),
        >= 70 => "Good".T(),
        >= 55 => "Fair".T(),
        _ => "Needs Care".T()
    };

    public Brush ImpactToBgBrush(string impact) => impact switch
    {
        "Critical" => new SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68)),
        "High" => new SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68)),
        "Medium" => new SolidColorBrush(Windows.UI.Color.FromArgb(30, 245, 158, 11)),
        _ => new SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
    };

    public Brush ImpactToFgBrush(string impact) => impact switch
    {
        "Critical" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
        "High" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
        "Medium" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
    };

    public Brush StatusToBgBrush(string status) => status.Equals("Running", StringComparison.OrdinalIgnoreCase)
        ? new SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
        : new SolidColorBrush(Windows.UI.Color.FromArgb(30, 107, 114, 128));

    public Brush StatusToFgBrush(string status) => status.Equals("Running", StringComparison.OrdinalIgnoreCase)
        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 107, 114, 128));

    public Brush ServiceCategoryBgBrush(bool isMicrosoft) => isMicrosoft
        ? new SolidColorBrush(Windows.UI.Color.FromArgb(30, 59, 130, 246))
        : new SolidColorBrush(Windows.UI.Color.FromArgb(30, 127, 86, 217));

    public Brush ServiceCategoryFgBrush(bool isMicrosoft) => isMicrosoft
        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246))
        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 127, 86, 217));

    public string ServiceCategoryText(bool isMicrosoft) => isMicrosoft ? "System".T() : "Third-Party".T();

    public bool ServiceRunning(string status) => status.Equals("Running", StringComparison.OrdinalIgnoreCase);
    public bool ServiceNotRunning(string status) => !status.Equals("Running", StringComparison.OrdinalIgnoreCase);

    public string FormatDateTime(DateTime? dt) => dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm") : "Never".T();

    public bool IsNot(bool val) => !val;

    private void UpdateLoadingOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        if (ViewModel.IsLoading)
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
        if (!ViewModel.IsLoading && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}
