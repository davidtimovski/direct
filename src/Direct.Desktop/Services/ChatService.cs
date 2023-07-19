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
    event EventHandler<JoinedEventArgs>? Joined;
    event EventHandler<ContactJoinedEventArgs>? ContactJoined;
    event EventHandler<ContactLeftEventArgs>? ContactLeft;
    event EventHandler<MessageSentEventArgs>? MessageSent;
    event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;

    Task ConnectAsync(Guid id, string nickname);
    Task DisconnectAsync();
    Task SendMessageAsync(Guid toUserId, string message);
    Task UpdateMessageAsync(Guid id, Guid recipientId, string message);
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

        _connection.On<List<ContactDto>>(ClientEvent.Joined, (contacts) =>
        {
            Joined?.Invoke(this, new JoinedEventArgs(contacts));
        });

        _connection.On<ContactDto>(ClientEvent.ContactJoined, (contact) =>
        {
            ContactJoined?.Invoke(this, new ContactJoinedEventArgs(contact));
        });

        _connection.On<Guid>(ClientEvent.ContactLeft, (userId) =>
        {
            ContactLeft?.Invoke(this, new ContactLeftEventArgs(userId));
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

    public event EventHandler<JoinedEventArgs>? Joined;
    public event EventHandler<ContactJoinedEventArgs>? ContactJoined;
    public event EventHandler<ContactLeftEventArgs>? ContactLeft;
    public event EventHandler<MessageSentEventArgs>? MessageSent;
    public event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;

    public async Task ConnectAsync(Guid id, string nickname)
    {
        _userId = id;

        await _connection.StartAsync();
        await _connection.InvokeAsync(ServerEvent.Join, _userId.Value, nickname, new Guid[] { Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"), Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d") });
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
}

public class JoinedEventArgs : EventArgs
{
    public JoinedEventArgs(List<ContactDto> contacts)
    {
        Contacts = contacts;
    }

    public List<ContactDto> Contacts { get; init; }
}

public class ContactJoinedEventArgs : EventArgs
{
    public ContactJoinedEventArgs(ContactDto contact)
    {
        Contact = contact;
    }

    public ContactDto Contact { get; init; }
}

public class ContactLeftEventArgs : EventArgs
{
    public ContactLeftEventArgs(Guid userId)
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
