using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

namespace Direct.Desktop.ViewModels;

public partial class ConnectionStatusViewModel : ObservableObject
{
    private static readonly Brush ConnectedBrush = ResourceUtil.GetBrush("ConnectedContactBrush");
    private static readonly Brush DisconnectedBrush = ResourceUtil.GetBrush("ConnectionStatusDisconnectedBrush");

    public ConnectionStatusViewModel(IChatService chatService, DispatcherQueue dispatcherQueue)
    {
        chatService.Reconnecting += (object? sender, EventArgs _) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                SetReconnecting();
            });
        };

        chatService.Reconnected += (object? sender, EventArgs _) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                SetConnected();
            });
        };

        Fill = ConnectedBrush;
        Tooltip = "Connected";
    }

    [ObservableProperty]
    private Brush fill;

    [ObservableProperty]
    private string tooltip;

    private void SetConnected()
    {
        Fill = ConnectedBrush;
        Tooltip = "Connected";
    }

    private void SetDisconnected()
    {
        Fill = DisconnectedBrush;
        Tooltip = "Disconnected";
    }

    private void SetReconnecting()
    {
        Fill = DisconnectedBrush;
        Tooltip = "Reconnecting";
    }
}
