using System;
using System.Threading.Tasks;
using Direct.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Direct.Desktop.Services;

public interface IContactProxy
{
    /// <summary>
    /// Invoked when a contact comes online.
    /// </summary>
    event EventHandler<ContactConnectedEventArgs>? Connected;

    /// <summary>
    /// Invoked when a contact goes offline.
    /// </summary>
    event EventHandler<ContactDisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// Invoked when a contact is added to the server.
    /// </summary>
    event EventHandler<ContactAddedEventArgs>? Added;

    /// <summary>
    /// Invoked when a contact is removed from the server.
    /// </summary>
    event EventHandler<ContactRemovedEventArgs>? Removed;

    Task AddContactAsync(Guid userId);
    Task RemoveContactAsync(Guid userId);
}

public class ContactProxy : IContactProxy
{
    private readonly IConnectionService _connectionService;

    public ContactProxy(IConnectionService connectionService)
    {
        _connectionService = connectionService;

        _connectionService.Connection.On<Guid>(ClientEvent.ContactConnected, (userId) =>
        {
            Connected?.Invoke(this, new ContactConnectedEventArgs(userId));
        });

        _connectionService.Connection.On<Guid>(ClientEvent.ContactDisconnected, (userId) =>
        {
            Disconnected?.Invoke(this, new ContactDisconnectedEventArgs(userId));
        });

        _connectionService.Connection.On<Guid, bool>(ClientEvent.ContactAdded, (userId, isConnected) =>
        {
            Added?.Invoke(this, new ContactAddedEventArgs(userId, isConnected));
        });

        _connectionService.Connection.On<Guid>(ClientEvent.ContactRemoved, (userId) =>
        {
            Removed?.Invoke(this, new ContactRemovedEventArgs(userId));
        });
    }

    public event EventHandler<ContactConnectedEventArgs>? Connected;
    public event EventHandler<ContactDisconnectedEventArgs>? Disconnected;
    public event EventHandler<ContactAddedEventArgs>? Added;
    public event EventHandler<ContactRemovedEventArgs>? Removed;

    public async Task AddContactAsync(Guid userId)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.AddContact, userId);
    }

    public async Task RemoveContactAsync(Guid userId)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.RemoveContact, userId);
    }
}

public class ContactConnectedEventArgs : EventArgs
{
    public ContactConnectedEventArgs(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; init; }
}

public class ContactAddedEventArgs : EventArgs
{
    public ContactAddedEventArgs(Guid userId, bool isConnected)
    {
        UserId = userId;
        IsConnected = isConnected;
    }

    public Guid UserId { get; init; }
    public bool IsConnected { get; init; }
}

public class ContactRemovedEventArgs : EventArgs
{
    public ContactRemovedEventArgs(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; init; }
}

public class ContactDisconnectedEventArgs : EventArgs
{
    public ContactDisconnectedEventArgs(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; init; }
}
