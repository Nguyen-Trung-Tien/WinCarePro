using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class RegistryPage : Page
{
    public RegistryViewModel ViewModel { get; }

    public static readonly DependencyProperty WideLayoutVisibilityProperty =
        DependencyProperty.Register(nameof(WideLayoutVisibility), typeof(Visibility), typeof(RegistryPage), new PropertyMetadata(Visibility.Visible));

    public Visibility WideLayoutVisibility
    {
        get => (Visibility)GetValue(WideLayoutVisibilityProperty);
        set => SetValue(WideLayoutVisibilityProperty, value);
    }

    public RegistryPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RegistryViewModel>();
        this.DataContext = ViewModel;

        this.SizeChanged += (s, e) =>
        {
            bool isWide = e.NewSize.Width >= 800;
            WideLayoutVisibility = isWide ? Visibility.Visible : Visibility.Collapsed;

            if (LeftCol != null && RightCol != null)
            {
                if (isWide)
                {
                    LeftCol.Width = new GridLength(1, GridUnitType.Star);
                    RightCol.Width = new GridLength(380, GridUnitType.Pixel);
                }
                else
                {
                    LeftCol.Width = new GridLength(1, GridUnitType.Star);
                    RightCol.Width = new GridLength(0, GridUnitType.Pixel);
                }
            }
        };
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanRegistryAsync();
    }

    private async void OnFixClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairSelectedAsync();
    }

    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BackupRegistryAsync();
    }

    internal bool IsNot(bool b) => !b;
}
