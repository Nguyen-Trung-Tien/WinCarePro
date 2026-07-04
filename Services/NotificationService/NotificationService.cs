using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class NotificationService : INotificationService
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private class QueuedNotification
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationSeverity Severity { get; set; }
        public List<NotificationAction>? Actions { get; set; }
        public int RepeatCount { get; set; } = 1;
    }

    private static readonly Queue<QueuedNotification> _queue = new();
    private static readonly List<Components.ToastNotification> _activeToasts = new();
    private static readonly Stack<Components.ToastNotification> _toastPool = new();
    private static readonly object _queueLock = new();
    
    private static DateTime _lastToastShownTime = DateTime.MinValue;
    private static bool _isProcessingQueue = false;

    // For backwards compatibility and INotificationService compliance
    public void ShowToast(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, List<NotificationAction>? actions = null)
    {
        EnqueueNotification(title, message, severity, actions, saveToDb: true);
    }

    // Older signature implementation
    public void ShowToast(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        ShowToast(title, message, severity, null);
    }

    public void ShowInAppAlert(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        ShowToast(title, message, severity);
    }

    // Severity Helpers
    public void ShowInfo(string message, List<NotificationAction>? actions = null) => ShowInfo("Info", message, actions);
    public void ShowInfo(string title, string message, List<NotificationAction>? actions = null) => EnqueueNotification(title, message, NotificationSeverity.Info, actions, saveToDb: true);

    public void ShowSuccess(string message, List<NotificationAction>? actions = null) => ShowSuccess("Success", message, actions);
    public void ShowSuccess(string title, string message, List<NotificationAction>? actions = null) => EnqueueNotification(title, message, NotificationSeverity.Success, actions, saveToDb: true);

    public void ShowWarning(string message, List<NotificationAction>? actions = null) => ShowWarning("Warning", message, actions);
    public void ShowWarning(string title, string message, List<NotificationAction>? actions = null) => EnqueueNotification(title, message, NotificationSeverity.Warning, actions, saveToDb: true);

    public void ShowError(string message, List<NotificationAction>? actions = null) => ShowError("Error", message, actions);
    public void ShowError(string title, string message, List<NotificationAction>? actions = null) => EnqueueNotification(title, message, NotificationSeverity.Error, actions, saveToDb: true);

    public void ShowCritical(string message, List<NotificationAction>? actions = null) => ShowCritical("Critical Alert", message, actions);
    public void ShowCritical(string title, string message, List<NotificationAction>? actions = null) => EnqueueNotification(title, message, NotificationSeverity.Critical, actions, saveToDb: true);

    // Queue and Lifecyle manager
    public void EnqueueNotification(string title, string message, NotificationSeverity severity, List<NotificationAction>? actions = null, bool saveToDb = true)
    {
        string dbSeverity = severity switch
        {
            NotificationSeverity.Warning => "Warning",
            NotificationSeverity.Error => "Error",
            NotificationSeverity.Success => "Success",
            NotificationSeverity.Critical => "Critical",
            _ => "Info"
        };

        if (saveToDb)
        {
            try
            {
                Database.DbManager.AddNotification(title, message, dbSeverity, showToast: false);
            }
            catch { }
        }

        // Check user settings for notifications filtering
        if (!ShouldShowNotification(title, message, dbSeverity))
        {
            return;
        }

        var dispatcher = App.MainDispatcherQueue;
        if (dispatcher == null) return;

        dispatcher.TryEnqueue(() =>
        {
            lock (_queueLock)
            {
                // Check if duplicate grouping is possible in active toasts
                var activeDup = _activeToasts.FirstOrDefault(t => 
                    t.TitleText.Equals(title, StringComparison.OrdinalIgnoreCase) && 
                    t.SeverityText.Equals(dbSeverity, StringComparison.OrdinalIgnoreCase));
                
                if (activeDup != null)
                {
                    int currentCount = activeDup.CurrentRepeatCount + 1;
                    activeDup.Update(title, message, dbSeverity, actions, currentCount);
                    
                    ResetDismissTimer(activeDup);
                    return;
                }

                // Check if duplicate grouping is possible in queue
                var queuedDup = _queue.FirstOrDefault(q => 
                    q.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && 
                    q.Severity == severity);

                if (queuedDup != null)
                {
                    queuedDup.RepeatCount++;
                    queuedDup.Message = message; 
                    queuedDup.Actions = actions;
                    return;
                }

                // Add to FIFO Queue
                var notification = new QueuedNotification
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                    Actions = actions
                };

                _queue.Enqueue(notification);
            }

            ProcessQueue();
        });
    }

    public void ClearAllNotifications()
    {
        var dispatcher = App.MainDispatcherQueue;
        if (dispatcher == null) return;

        dispatcher.TryEnqueue(() =>
        {
            lock (_queueLock)
            {
                _queue.Clear();
                
                var activeList = _activeToasts.ToList();
                foreach (var toast in activeList)
                {
                    DismissToastImmediate(toast);
                }
            }
        });
    }

    private static bool ShouldShowNotification(string title, string message, string dbSeverity)
    {
        try
        {
            string raw = Database.DbManager.GetSettings();
            if (string.IsNullOrEmpty(raw)) return true;

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("ShowNotifications", out var showProp) && !showProp.GetBoolean())
            {
                return false;
            }
            
            // Specific filter conditions
            if (dbSeverity.Equals("Warning", StringComparison.OrdinalIgnoreCase))
            {
                if (title.Contains("Update") && root.TryGetProperty("ShowUpdateNotifications", out var upProp) && !upProp.GetBoolean())
                    return false;
                else if (root.TryGetProperty("NotifyOnLowHealth", out var lhProp) && !lhProp.GetBoolean())
                    return false;
            }
            else if (dbSeverity.Equals("Info", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("NotifyOnMaintenance", out var maintProp) && !maintProp.GetBoolean())
                    return false;
            }
        }
        catch { }
        return true;
    }

    private static void ProcessQueue()
    {
        var dispatcher = App.MainDispatcherQueue;
        if (dispatcher == null || _isProcessingQueue) return;

        lock (_queueLock)
        {
            if (_queue.Count == 0) return;

            // Limit maximum visible toasts to 3
            if (_activeToasts.Count >= 3)
            {
                // Critical alert bypass: Dismiss oldest non-critical active toast
                var firstInQueue = _queue.Peek();
                if (firstInQueue.Severity == NotificationSeverity.Critical)
                {
                    var oldestNonCritical = _activeToasts.FirstOrDefault(t => !t.SeverityText.Equals("Critical", StringComparison.OrdinalIgnoreCase));
                    if (oldestNonCritical != null)
                    {
                        DismissToastImmediate(oldestNonCritical);
                    }
                    else
                    {
                        return; // All 3 visible are critical, must wait.
                    }
                }
                else
                {
                    return; // Max capacity, wait.
                }
            }

            // Rate Limit check (min 800ms between appearances, except critical)
            var nextNotification = _queue.Peek();
            double elapsed = (DateTime.Now - _lastToastShownTime).TotalMilliseconds;
            
            if (nextNotification.Severity == NotificationSeverity.Critical || elapsed >= 800)
            {
                _queue.Dequeue();
                _lastToastShownTime = DateTime.Now;
                ShowToastUI(nextNotification);
            }
            else
            {
                // Wait remaining time and retry processing
                _isProcessingQueue = true;
                int delayMs = 800 - (int)elapsed;
                Task.Delay(delayMs).ContinueWith(_ => 
                {
                    _isProcessingQueue = false;
                    dispatcher.TryEnqueue(() => ProcessQueue());
                });
            }
        }
    }

    private static void ShowToastUI(QueuedNotification item)
    {
        var win = App.MainWindowInstance;
        if (win == null || win.ToastStackContainer == null) return;

        // Check if sound settings are enabled
        bool playSound = true;
        try
        {
            string raw = Database.DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("NotificationSound", out var soundProp) && !soundProp.GetBoolean())
                {
                    playSound = false;
                }
            }
        }
        catch { }

        if (playSound)
        {
            try 
            {
                MessageBeep(0);
            } 
            catch { }
        }

        string severityStr = item.Severity switch
        {
            NotificationSeverity.Warning => "Warning",
            NotificationSeverity.Error => "Error",
            NotificationSeverity.Success => "Success",
            NotificationSeverity.Critical => "Critical",
            _ => "Info"
        };

        // Object Pooling: Get from pool or create new
        Components.ToastNotification toast;
        lock (_toastPool)
        {
            if (_toastPool.Count > 0)
            {
                toast = _toastPool.Pop();
            }
            else
            {
                toast = new Components.ToastNotification();
            }
        }

        toast.Update(item.Title, item.Message, severityStr, item.Actions, item.RepeatCount);
        
        toast.DismissRequested = (t) => DismissToast(t);

        win.ToastStackContainer.Children.Add(toast);
        _activeToasts.Add(toast);

        toast.AnimateIn();

        // Setup auto-dismiss timeout (6 seconds)
        var tokenSource = new System.Threading.CancellationTokenSource();
        toast.Tag = tokenSource;

        Task.Delay(6000, tokenSource.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                App.MainDispatcherQueue?.TryEnqueue(() => DismissToast(toast));
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private static void ResetDismissTimer(Components.ToastNotification toast)
    {
        if (toast.Tag is System.Threading.CancellationTokenSource cts)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
        }

        var newCts = new System.Threading.CancellationTokenSource();
        toast.Tag = newCts;

        Task.Delay(6000, newCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                App.MainDispatcherQueue?.TryEnqueue(() => DismissToast(toast));
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public static void DismissToast(Components.ToastNotification toast)
    {
        if (toast == null) return;
        
        lock (_queueLock)
        {
            if (!_activeToasts.Contains(toast)) return;
            _activeToasts.Remove(toast);
        }

        if (toast.Tag is System.Threading.CancellationTokenSource cts)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
            toast.Tag = null;
        }

        toast.AnimateOut(() =>
        {
            var win = App.MainWindowInstance;
            if (win != null && win.ToastStackContainer != null)
            {
                win.ToastStackContainer.Children.Remove(toast);
            }

            toast.Reset();
            lock (_toastPool)
            {
                _toastPool.Push(toast);
            }

            ProcessQueue();
        });
    }

    private static void DismissToastImmediate(Components.ToastNotification toast)
    {
        if (toast == null) return;

        lock (_queueLock)
        {
            _activeToasts.Remove(toast);
        }

        if (toast.Tag is System.Threading.CancellationTokenSource cts)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
            toast.Tag = null;
        }

        var win = App.MainWindowInstance;
        if (win != null && win.ToastStackContainer != null)
        {
            win.ToastStackContainer.Children.Remove(toast);
        }

        toast.Reset();
        lock (_toastPool)
        {
            _toastPool.Push(toast);
        }
    }
}

