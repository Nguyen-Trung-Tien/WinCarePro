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
}
