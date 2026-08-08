using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;

namespace WinCarePro.Shared.Components;

public sealed partial class LoadingToggleSwitch : UserControl
{
    private long _loadingStartTimeTicks;

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(LoadingToggleSwitch),
            new PropertyMetadata(false, OnIsOnPropertyChanged));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingToggleSwitch),
            new PropertyMetadata(false, OnIsLoadingPropertyChanged));

    public static readonly DependencyProperty MinLoadingDurationMsProperty =
        DependencyProperty.Register(nameof(MinLoadingDurationMs), typeof(int), typeof(LoadingToggleSwitch),
            new PropertyMetadata(450));

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(LoadingToggleSwitch),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty OnContentProperty =
        DependencyProperty.Register(nameof(OnContent), typeof(string), typeof(LoadingToggleSwitch),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty OffContentProperty =
        DependencyProperty.Register(nameof(OffContent), typeof(string), typeof(LoadingToggleSwitch),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LoadingTextProperty =
        DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(LoadingToggleSwitch),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty UserControlIsEnabledProperty =
        DependencyProperty.Register(nameof(UserControlIsEnabled), typeof(bool), typeof(LoadingToggleSwitch),
            new PropertyMetadata(true));

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public int MinLoadingDurationMs
    {
        get => (int)GetValue(MinLoadingDurationMsProperty);
        set => SetValue(MinLoadingDurationMsProperty, value);
    }

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public string OnContent
    {
        get => (string)GetValue(OnContentProperty);
        set => SetValue(OnContentProperty, value);
    }

    public string OffContent
    {
        get => (string)GetValue(OffContentProperty);
        set => SetValue(OffContentProperty, value);
    }

    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public bool UserControlIsEnabled
    {
        get => (bool)GetValue(UserControlIsEnabledProperty);
        set => SetValue(UserControlIsEnabledProperty, value);
    }

    public event RoutedEventHandler? Toggled;

    public LoadingToggleSwitch()
    {
        this.InitializeComponent();
        this.Loaded += LoadingToggleSwitch_Loaded;
        this.Unloaded += LoadingToggleSwitch_Unloaded;
        this.IsEnabledChanged += LoadingToggleSwitch_IsEnabledChanged;
    }

    private void LoadingToggleSwitch_Loaded(object sender, RoutedEventArgs e)
    {
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
        UpdateTranslations();
    }

    private void LoadingToggleSwitch_Unloaded(object sender, RoutedEventArgs e)
    {
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void LoadingToggleSwitch_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UserControlIsEnabled = this.IsEnabled;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            UpdateTranslations();
        });
    }

    private void UpdateTranslations()
    {
        try
        {
            TranslationManager.Instance.TranslateSingleControl(HeaderTextBlock);
            TranslationManager.Instance.TranslateSingleControl(LoadingLabel);
            TranslationManager.Instance.TranslateSingleControl(InnerToggle);
        }
        catch { }
    }

    private static void OnIsOnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingToggleSwitch control)
        {
            control.InnerToggle.IsOn = (bool)e.NewValue;
        }
    }

    private static async void OnIsLoadingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingToggleSwitch control)
        {
            bool isLoading = (bool)e.NewValue;
            if (isLoading)
            {
                control._loadingStartTimeTicks = Environment.TickCount64;
                control.LoadingPanel.Visibility = Visibility.Visible;
                control.InnerToggle.IsEnabled = false;
            }
            else
            {
                long elapsed = Environment.TickCount64 - control._loadingStartTimeTicks;
                int minDuration = Math.Max(100, control.MinLoadingDurationMs);
                if (elapsed < minDuration)
                {
                    int remaining = (int)(minDuration - elapsed);
                    await Task.Delay(remaining);
                }
                control.LoadingPanel.Visibility = Visibility.Collapsed;
                control.InnerToggle.IsEnabled = control.UserControlIsEnabled;
            }
        }
    }

    public async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        _loadingStartTimeTicks = Environment.TickCount64;
        IsLoading = true;
        try
        {
            await action();
        }
        finally
        {
            long elapsed = Environment.TickCount64 - _loadingStartTimeTicks;
            int minDuration = Math.Max(100, MinLoadingDurationMs);
            if (elapsed < minDuration)
            {
                await Task.Delay((int)(minDuration - elapsed));
            }
            IsLoading = false;
        }
    }

    private void InnerToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (IsOn != InnerToggle.IsOn)
        {
            IsOn = InnerToggle.IsOn;
        }
        Toggled?.Invoke(this, e);
    }

    private Visibility HasHeader(string header) =>
        string.IsNullOrWhiteSpace(header) ? Visibility.Collapsed : Visibility.Visible;

    private Visibility HasLoadingText(string text) =>
        string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;

    private bool GetIsEnabled(bool isLoading, bool userControlEnabled) =>
        !isLoading && userControlEnabled;
}
