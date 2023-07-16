using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Direct.Shared;
using Direct.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Direct.Services;

public interface IChatService
{
    event EventHandler<JoinedEventArgs>? Joined;
    event EventHandler<ContactJoinedEventArgs>? ContactJoined;
    event EventHandler<ContactLeftEventArgs>? ContactLeft;
    event EventHandler<MessageSentEventArgs>? MessageSent;
    event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;

    Task ConnectAsync(string passwordHash, string nickname);
    Task DisconnectAsync();
    Task SendMessageAsync(Guid toUserId, string message);
    Task UpdateMessageAsync(Guid id, string message);
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

        _connection.On<Guid, List<ContactDto>>(ClientEvent.Joined, (userId, contacts) =>
        {
            _userId = userId;
            Joined?.Invoke(this, new JoinedEventArgs(userId, contacts));
        });

        _connection.On<ContactDto>(ClientEvent.ContactJoined, (contact) =>
        {
            ContactJoined?.Invoke(this, new ContactJoinedEventArgs(contact));
        });

        _connection.On<Guid>(ClientEvent.ContactLeft, (userId) =>
        {
            ContactLeft?.Invoke(this, new ContactLeftEventArgs(userId));
        });

        _connection.On<MessageDto>(ClientEvent.MessageSent, (message) =>
        {
            message.UserIsSender = message.SenderId == _userId!.Value;
            MessageSent?.Invoke(this, new MessageSentEventArgs(message));
        });

        _connection.On<MessageDto>(ClientEvent.MessageUpdated, (message) =>
        {
            message.UserIsSender = message.SenderId == _userId!.Value;
            MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs(message));
        });
    }

    public event EventHandler<JoinedEventArgs>? Joined;
    public event EventHandler<ContactJoinedEventArgs>? ContactJoined;
    public event EventHandler<ContactLeftEventArgs>? ContactLeft;
    public event EventHandler<MessageSentEventArgs>? MessageSent;
    public event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;

    public async Task ConnectAsync(string passwordHash, string nickname)
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync(ServerEvent.Join, passwordHash, nickname);
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

    public async Task UpdateMessageAsync(Guid id, string message)
    {
        await _connection.InvokeAsync(ServerEvent.UpdateMessage, id, message);
    }
}

public class JoinedEventArgs : EventArgs
{
    public JoinedEventArgs(Guid userId, List<ContactDto> contacts)
    {
        UserId = userId;
        Contacts = contacts;
    }

    public Guid UserId { get; init; }
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

public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(MessageDto message)
    {
        Message = message;
    }

    public MessageDto Message { get; init; }
}

public class MessageSentEventArgs : EventArgs
{
    public MessageSentEventArgs(MessageDto message)
    {
        Message = message;
    }

    public MessageDto Message { get; init; }
}

public class MessageUpdatedEventArgs : EventArgs
{
    public MessageUpdatedEventArgs(MessageDto message)
    {
        Message = message;
    }

    public MessageDto Message { get; init; }
}
