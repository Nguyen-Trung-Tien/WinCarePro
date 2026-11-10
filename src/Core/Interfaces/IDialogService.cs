using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

public interface IDialogService
{
    void SetXamlRoot(XamlRoot xamlRoot);
    Task<CleaningAction> ShowLockingAppsDialogAsync(List<LockingAppInfo> apps);
    Task<bool> ShowForceClosePromptAsync(string appName);
    Task ShowMessageAsync(string title, string content);
    Task<bool> ShowForceUninstallPromptAsync(string appName);
    Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel", bool isDestructive = false);
    Task ShowSuccessAsync(string title, string message, string? detailLog = null);
    Task ShowWarningAsync(string title, string message, string? detailLog = null);
    Task ShowErrorAsync(string title, string message, string? detailLog = null);
}
