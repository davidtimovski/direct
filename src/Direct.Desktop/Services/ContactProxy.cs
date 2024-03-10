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

    /// <summary>
    /// Invoked when a contact updates their profile image.
    /// </summary>
    event EventHandler<ContactUpdatedProfileImageEventArgs>? UpdatedProfileImage;

    Task AddContactAsync(Guid userId);
    Task RemoveContactAsync(Guid userId);
    Task UpdateProfileImageAsync(string profileImage);
}

public class ContactProxy : IContactProxy
{
    private readonly IConnectionService _connectionService;

    public ContactProxy(IConnectionService connectionService)
    {
        _connectionService = connectionService;

        _connectionService.Connection.On<Guid>(ClientEvent.ContactConnected, (userId) =>
        {
            Connected?.Invoke(this, new ContactConnectedEventArgs { UserId = userId });
        });

        _connectionService.Connection.On<Guid>(ClientEvent.ContactDisconnected, (userId) =>
        {
            Disconnected?.Invoke(this, new ContactDisconnectedEventArgs { UserId = userId });
        });

        _connectionService.Connection.On<Guid, bool>(ClientEvent.ContactAdded, (userId, isConnected) =>
        {
            Added?.Invoke(this, new ContactAddedEventArgs { UserId = userId, IsConnected = isConnected });
        });

        _connectionService.Connection.On<Guid>(ClientEvent.ContactRemoved, (userId) =>
        {
            Removed?.Invoke(this, new ContactRemovedEventArgs { UserId = userId });
        });

        _connectionService.Connection.On<Guid, string>(ClientEvent.ContactUpdatedProfileImage, (userId, profileImage) =>
        {
            UpdatedProfileImage?.Invoke(this, new ContactUpdatedProfileImageEventArgs { UserId = userId, ProfileImage = profileImage });
        });
    }

    public event EventHandler<ContactConnectedEventArgs>? Connected;
    public event EventHandler<ContactDisconnectedEventArgs>? Disconnected;
    public event EventHandler<ContactAddedEventArgs>? Added;
    public event EventHandler<ContactRemovedEventArgs>? Removed;
    public event EventHandler<ContactUpdatedProfileImageEventArgs>? UpdatedProfileImage;

    public async Task AddContactAsync(Guid userId)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.AddContact, userId);
    }

    public async Task RemoveContactAsync(Guid userId)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.RemoveContact, userId);
    }

    public async Task UpdateProfileImageAsync(string profileImage)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.UpdateProfileImage, profileImage);
    }
}

public class ContactConnectedEventArgs : EventArgs
{
    public required Guid UserId { get; init; }
}

public class ContactDisconnectedEventArgs : EventArgs
{
    public required Guid UserId { get; init; }
}

public class ContactAddedEventArgs : EventArgs
{
    public required Guid UserId { get; init; }
    public required bool IsConnected { get; init; }
}

public class ContactRemovedEventArgs : EventArgs
{
    public required Guid UserId { get; init; }
}

public class ContactUpdatedProfileImageEventArgs : EventArgs
{
    public required Guid UserId { get; init; }
    public required string ProfileImage { get; init; }
}
