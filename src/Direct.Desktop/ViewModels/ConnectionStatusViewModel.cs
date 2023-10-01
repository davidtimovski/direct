using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

namespace Direct.Desktop.ViewModels;

public partial class ConnectionStatusViewModel : ObservableObject
{
    private const string ConnectedTooltip = "Connected";
    private const string ConnectingTooltip = "Connecting..";

    private static readonly Brush ConnectedBrush = ResourceUtil.GetBrush("CyanBrush");
    private static readonly Brush ConnectingBrush = ResourceUtil.GetBrush("OrangeBrush");

    public ConnectionStatusViewModel(IConnectionService connectionService, DispatcherQueue dispatcherQueue)
    {
        connectionService.ConnectedContactsRetrieved += (object? _, ConnectedContactsRetrievedEventArgs _) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                SetConnected();
            });
        };

        connectionService.Reconnecting += (object? _, EventArgs _) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                SetConnecting();
            });
        };

        connectionService.Reconnected += (object? _, EventArgs _) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                SetConnected();
            });
        };

        Fill = ConnectingBrush;
        Tooltip = ConnectingTooltip;
    }

    [ObservableProperty]
    private bool connected;

    [ObservableProperty]
    private Brush fill;

    [ObservableProperty]
    private string tooltip;

    private void SetConnected()
    {
        Connected = true;
        Fill = ConnectedBrush;
        Tooltip = ConnectedTooltip;
    }

    private void SetConnecting()
    {
        Connected = false;
        Fill = ConnectingBrush;
        Tooltip = ConnectingTooltip;
    }
}
