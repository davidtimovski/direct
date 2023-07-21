using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Direct.Shared;
using Direct.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Direct.Desktop.Services;

public interface IChatService
{
    event EventHandler<ConnectedEventArgs>? Connected;
    event EventHandler<ContactConnectedEventArgs>? ContactConnected;
    event EventHandler<ContactDisconnectedEventArgs>? ContactDisconnected;
    event EventHandler<AddedContactIsConnectedEventArgs>? AddedContactIsConnected;
    event EventHandler<MessageSentEventArgs>? MessageSent;
    event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;

    Task ConnectAsync(Guid userId, List<Guid> contactIds);
    Task DisconnectAsync();
    Task SendMessageAsync(Guid toUserId, string message);
    Task UpdateMessageAsync(Guid id, Guid recipientId, string message);
    Task RetrieveContactAsync(Guid userId);
}

public class ChatService : IChatService
{
    private readonly HubConnection _connection;
    private Guid? _userId;

    public ChatService()
    {
        _connection = new HubConnectionBuilder()
           .WithUrl(Globals.ServerUri + "/chatHub")
           .AddMessagePackProtocol()
           .WithAutomaticReconnect()
           .Build();

        _connection.On<List<Guid>>(ClientEvent.Connected, (connectedUserIds) =>
        {
            Connected?.Invoke(this, new ConnectedEventArgs(connectedUserIds));
        });

        _connection.On<Guid>(ClientEvent.ContactConnected, (userId) =>
        {
            ContactConnected?.Invoke(this, new ContactConnectedEventArgs(userId));
        });

        _connection.On<Guid>(ClientEvent.ContactDisconnected, (userId) =>
        {
            ContactDisconnected?.Invoke(this, new ContactDisconnectedEventArgs(userId));
        });

        _connection.On<Guid, bool>(ClientEvent.AddedContactIsConnected, (userId, isConnected) =>
        {
            AddedContactIsConnected?.Invoke(this, new AddedContactIsConnectedEventArgs(userId, isConnected));
        });

        _connection.On<NewMessageDto>(ClientEvent.MessageSent, (message) =>
        {
            MessageSent?.Invoke(this, new MessageSentEventArgs(message));
        });

        _connection.On<MessageUpdateDto>(ClientEvent.MessageUpdated, (message) =>
        {
            MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs(message));
        });
    }

    public event EventHandler<ConnectedEventArgs>? Connected;
    public event EventHandler<ContactConnectedEventArgs>? ContactConnected;
    public event EventHandler<ContactDisconnectedEventArgs>? ContactDisconnected;
    public event EventHandler<AddedContactIsConnectedEventArgs>? AddedContactIsConnected;
    public event EventHandler<MessageSentEventArgs>? MessageSent;
    public event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;

    public async Task ConnectAsync(Guid userId, List<Guid> contactIds)
    {
        _userId = userId;

        await _connection.StartAsync();
        await _connection.InvokeAsync(ServerEvent.Connect, _userId.Value, contactIds);
    }

    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
        await _connection.DisposeAsync();
    }

    public async Task SendMessageAsync(Guid recipientId, string message)
    {
        await _connection.InvokeAsync(ServerEvent.SendMessage, recipientId, message);
    }

    public async Task UpdateMessageAsync(Guid id, Guid recipientId, string message)
    {
        await _connection.InvokeAsync(ServerEvent.UpdateMessage, id, recipientId, message);
    }

    public async Task RetrieveContactAsync(Guid userId)
    {
        await _connection.InvokeAsync(ServerEvent.ContactIsConnected, userId);
    }
}

public class ConnectedEventArgs : EventArgs
{
    public ConnectedEventArgs(List<Guid> connectedUserIds)
    {
        ConnectedUserIds = connectedUserIds;
    }

    public List<Guid> ConnectedUserIds { get; init; }
}

public class ContactConnectedEventArgs : EventArgs
{
    public ContactConnectedEventArgs(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; init; }
}

public class AddedContactIsConnectedEventArgs : EventArgs
{
    public AddedContactIsConnectedEventArgs(Guid userId, bool isConnected)
    {
        UserId = userId;
        IsConnected = isConnected;
    }

    public Guid UserId { get; init; }
    public bool IsConnected { get; init; }
}

public class ContactDisconnectedEventArgs : EventArgs
{
    public ContactDisconnectedEventArgs(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; init; }
}

public class MessageSentEventArgs : EventArgs
{
    public MessageSentEventArgs(NewMessageDto message)
    {
        Message = message;
    }

    public NewMessageDto Message { get; init; }
}

public class MessageUpdatedEventArgs : EventArgs
{
    public MessageUpdatedEventArgs(MessageUpdateDto message)
    {
        Message = message;
    }

    public MessageUpdateDto Message { get; init; }
}
