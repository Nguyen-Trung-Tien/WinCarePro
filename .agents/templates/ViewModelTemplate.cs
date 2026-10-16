using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Modules.Template;

/// <summary>
/// Canonical production ViewModel template for WinCare Pro.
/// Inherits from ViewModelBase, wraps all UI state mutations in RunOnUI, and guarantees busy-state cleanup in finally.
/// </summary>
public partial class TemplateViewModel : ViewModelBase
{
    private readonly TemplateEngine _engine;
    private readonly CrashLogger _crashLogger;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private double _progressValue;

    public ObservableCollection<string> Items { get; } = new();

    public TemplateViewModel(TemplateEngine engine, CrashLogger crashLogger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _crashLogger = crashLogger ?? throw new ArgumentNullException(nameof(crashLogger));
    }

    [RelayCommand]
    private async Task StartOperationAsync()
    {
        if (IsBusy) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            IsBusy = true;
            OperationState = OperationState.Running;
            StatusText = "Processing...";
            ProgressValue = 0;

            var result = await _engine.ExecuteSafeOperationAsync(@"C:\SafePath", _cts.Token);

            RunOnUI(() =>
            {
                if (result.Success)
                {
                    StatusText = result.Data ?? "Success";
                    Items.Add(StatusText);
                }
                else
                {
                    StatusText = $"Error: {result.ErrorMessage}";
                }
            });
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() => StatusText = "Operation cancelled.");
        }
        catch (Exception ex)
        {
            _crashLogger.LogError(ex, "TemplateViewModel.StartOperationAsync");
            RunOnUI(() => StatusText = $"Unexpected error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            OperationState = OperationState.Idle;
        }
    }

    [RelayCommand]
    private void CancelOperation()
    {
        _cts?.Cancel();
    }
}
