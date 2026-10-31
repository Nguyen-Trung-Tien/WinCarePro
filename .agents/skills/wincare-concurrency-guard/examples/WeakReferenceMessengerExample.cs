using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Examples.Concurrency;

public sealed class SystemStatusChangedMessage : ValueChangedMessage<string>
{
    public SystemStatusChangedMessage(string status) : base(status) { }
}

/// <summary>
/// Reference example demonstrating decoupled cross-ViewModel messaging via WeakReferenceMessenger.
/// Invariants:
/// - Never hold direct references between ViewModels.
/// - Implement IRecipient<TMessage> or register via Messenger.
/// - Automatically unregisters on garbage collection or manually unregister in Dispose/Deactivate.
/// </summary>
public sealed class WeakReferenceMessengerExample : ViewModelBase, IRecipient<SystemStatusChangedMessage>
{
    private string _latestNotification = string.Empty;
    public string LatestNotification
    {
        get => _latestNotification;
        private set => SetProperty(ref _latestNotification, value);
    }

    public WeakReferenceMessengerExample()
    {
        // Register this recipient for SystemStatusChangedMessage
        WeakReferenceMessenger.Default.Register<SystemStatusChangedMessage>(this);
    }

    public void Receive(SystemStatusChangedMessage message)
    {
        // Safe UI thread dispatch
        RunOnUI(() =>
        {
            LatestNotification = message.Value;
        });
    }

    public static void BroadcastStatus(string newStatus)
    {
        // Publisher broadcasts without knowing any recipients
        WeakReferenceMessenger.Default.Send(new SystemStatusChangedMessage(newStatus));
    }
}

