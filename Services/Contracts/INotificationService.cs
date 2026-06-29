using System;
using System.Collections.Generic;

namespace WinCarePro.Services.Contracts;

public enum NotificationSeverity
{
    Info,
    Warning,
    Error,
    Success,
    Critical
}

public class NotificationAction
{
    public string Label { get; set; } = "";
    public Action Action { get; set; } = () => { };
}

public interface INotificationService
{
    void ShowToast(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, List<NotificationAction>? actions = null);
    void ShowInAppAlert(string title, string message, NotificationSeverity severity = NotificationSeverity.Info);

    // Severity Helpers
    void ShowInfo(string message, List<NotificationAction>? actions = null);
    void ShowInfo(string title, string message, List<NotificationAction>? actions = null);
    
    void ShowSuccess(string message, List<NotificationAction>? actions = null);
    void ShowSuccess(string title, string message, List<NotificationAction>? actions = null);
    
    void ShowWarning(string message, List<NotificationAction>? actions = null);
    void ShowWarning(string title, string message, List<NotificationAction>? actions = null);
    
    void ShowError(string message, List<NotificationAction>? actions = null);
    void ShowError(string title, string message, List<NotificationAction>? actions = null);
    
    void ShowCritical(string message, List<NotificationAction>? actions = null);
    void ShowCritical(string title, string message, List<NotificationAction>? actions = null);

    // Queue Management
    void EnqueueNotification(string title, string message, NotificationSeverity severity, List<NotificationAction>? actions = null, bool saveToDb = true);
    void ClearAllNotifications();
}

