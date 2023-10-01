using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Direct.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace Direct.Desktop.Services;

public interface IConnectionService
{
    HubConnection Connection { get; }

    /// <summary>
    /// Invoked when the server returns the contacts that are online.
    /// </summary>
    event EventHandler<ConnectedContactsRetrievedEventArgs>? ConnectedContactsRetrieved;

    /// <summary>
    /// Invoked when the connection to the server is lost.
    /// </summary>
    event EventHandler? Reconnecting;

    /// <summary>
    /// Invoked when the connection to the server is reestablished.
    /// </summary>
    event EventHandler? Reconnected;

    Task<bool> ConnectAsync(Guid userId, HashSet<Guid> contactIds);
    Task DisconnectAsync();
    void StartConnectionRetry();
}

public class ConnectionService : IConnectionService
{
    private readonly HashSet<Guid> _contactIds = new();
    private Guid? userId;

    public ConnectionService()
    {
        Connection = new HubConnectionBuilder()
           .WithUrl(Globals.ServerUri + "/chatHub")
           .AddMessagePackProtocol()
           .WithAutomaticReconnect()
           .Build();

        Connection.On<List<Guid>>(ClientEvent.ConnectedContactsRetrieved, (connectedUserIds) =>
        {
            ConnectedContactsRetrieved?.Invoke(this, new ConnectedContactsRetrievedEventArgs(connectedUserIds));
        });

        Connection.Reconnecting += (Exception? arg) =>
        {
            Reconnecting?.Invoke(this, new EventArgs());
            return Task.CompletedTask;
        };

        Connection.Reconnected += async (string? arg) =>
        {
            Reconnected?.Invoke(this, new EventArgs());
            await Connection.InvokeAsync(ServerEvent.UserJoin, userId!.Value, _contactIds);
        };
    }

    public HubConnection Connection { get; }

    public event EventHandler<ConnectedContactsRetrievedEventArgs>? ConnectedContactsRetrieved;
    public event EventHandler? Reconnecting;
    public event EventHandler? Reconnected;

    public async Task<bool> ConnectAsync(Guid userId, HashSet<Guid> contactIds)
    {
        foreach (var contactId in contactIds)
        {
            if (_contactIds.Contains(contactId))
            {
                continue;
            }

            _contactIds.Add(contactId);
        }
        this.userId = userId;

        try
        {
            await Connection.StartAsync();
            await Connection.InvokeAsync(ServerEvent.UserJoin, this.userId.Value, contactIds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        await Connection.StopAsync();
        await Connection.DisposeAsync();
    }

    public void StartConnectionRetry()
    {
        var queueController = DispatcherQueueController.CreateOnDedicatedThread();
        var queue = queueController.DispatcherQueue;

        var repeatingTimer = queue.CreateTimer();
        repeatingTimer.Interval = TimeSpan.FromMinutes(1);
        repeatingTimer.Tick += async (timer, _) =>
        {
            try
            {
                await Connection.StartAsync();
                await Connection.InvokeAsync(ServerEvent.UserJoin, userId!.Value, _contactIds);

                timer.Stop();
            }
            catch
            {
                // Connection failure, continue to retry
            }
        };
        repeatingTimer.Start();
    }
}

public class ConnectedContactsRetrievedEventArgs : EventArgs
{
    public ConnectedContactsRetrievedEventArgs(List<Guid> connectedUserIds)
    {
        ConnectedUserIds = connectedUserIds;
    }

    public List<Guid> ConnectedUserIds { get; init; }
}
