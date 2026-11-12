using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;

namespace WinCarePro.Presentation.Controls;

public sealed partial class ExecutionProgressBanner : UserControl
{
    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(ExecutionProgressBanner),
            new PropertyMetadata("Executing task..."));

    public static readonly DependencyProperty TargetItemProperty =
        DependencyProperty.Register(nameof(TargetItem), typeof(string), typeof(ExecutionProgressBanner),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ProgressPercentageProperty =
        DependencyProperty.Register(nameof(ProgressPercentage), typeof(double), typeof(ExecutionProgressBanner),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(ExecutionProgressBanner),
            new PropertyMetadata(false));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(ExecutionProgressBanner),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CanCancelProperty =
        DependencyProperty.Register(nameof(CanCancel), typeof(bool), typeof(ExecutionProgressBanner),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CancelTextProperty =
        DependencyProperty.Register(nameof(CancelText), typeof(string), typeof(ExecutionProgressBanner),
            new PropertyMetadata("Cancel"));

    public ExecutionProgressBanner()
    {
        InitializeComponent();
        CancelText = "Cancel".T();
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public string TargetItem
    {
        get => (string)GetValue(TargetItemProperty);
        set => SetValue(TargetItemProperty, value);
    }

    public double ProgressPercentage
    {
        get => (double)GetValue(ProgressPercentageProperty);
        set => SetValue(ProgressPercentageProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public bool CanCancel
    {
        get => (bool)GetValue(CanCancelProperty);
        set => SetValue(CanCancelProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public static string FormatPercent(double percent) => $"{(int)Math.Clamp(percent, 0, 100)}%";

    public static Visibility DeterminateVisibility(bool isIndeterminate) =>
        isIndeterminate ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility HasTargetItemVisibility(string? targetItem) =>
        string.IsNullOrWhiteSpace(targetItem) ? Visibility.Collapsed : Visibility.Visible;
}
