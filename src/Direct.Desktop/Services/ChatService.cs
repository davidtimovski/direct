using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Direct.Shared;
using Direct.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace Direct.Desktop.Services;

public interface IChatService
{
    event EventHandler<ConnectedContactsRetrievedEventArgs>? ConnectedContactsRetrieved;
    event EventHandler? Reconnecting;
    event EventHandler? Reconnected;
    event EventHandler<ContactConnectedEventArgs>? ContactConnected;
    event EventHandler<ContactDisconnectedEventArgs>? ContactDisconnected;
    event EventHandler<ContactAddedEventArgs>? ContactAdded;
    event EventHandler<ContactRemovedEventArgs>? ContactRemoved;
    event EventHandler<MessageSentEventArgs>? MessageSent;
    event EventHandler<MessageSendingFailedEventArgs>? MessageSendingFailed;
    event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;
    event EventHandler<MessageUpdatingFailedEventArgs>? MessageUpdatingFailed;

    Task<bool> ConnectAsync(Guid userId, HashSet<Guid> contactIds);
    Task DisconnectAsync();
    void StartConnectionRetry();
    Task SendMessageAsync(Guid toUserId, string message);
    Task UpdateMessageAsync(Guid id, Guid recipientId, string message);
    Task AddContactAsync(Guid userId);
    Task RemoveContactAsync(Guid userId);
}

public class ChatService : IChatService
{
    private readonly HubConnection _connection;
    private readonly HashSet<Guid> _contactIds = new();

    private Guid? _userId;

    public ChatService()
    {
        _connection = new HubConnectionBuilder()
           .WithUrl(Globals.ServerUri + "/chatHub")
           .AddMessagePackProtocol()
           .WithAutomaticReconnect()
           .Build();

        _connection.On<List<Guid>>(ClientEvent.ConnectedContactsRetrieved, (connectedUserIds) =>
        {
            ConnectedContactsRetrieved?.Invoke(this, new ConnectedContactsRetrievedEventArgs(connectedUserIds));
        });

        _connection.Reconnecting += (Exception? arg) =>
        {
            Reconnecting?.Invoke(this, new EventArgs());
            return Task.CompletedTask;
        };

        _connection.Reconnected += async (string? arg) =>
        {
            Reconnected?.Invoke(this, new EventArgs());
            await _connection.InvokeAsync(ServerEvent.UserJoin, _userId!.Value, _contactIds);
        };

        _connection.On<Guid>(ClientEvent.ContactConnected, (userId) =>
        {
            ContactConnected?.Invoke(this, new ContactConnectedEventArgs(userId));
        });

        _connection.On<Guid>(ClientEvent.ContactDisconnected, (userId) =>
        {
            ContactDisconnected?.Invoke(this, new ContactDisconnectedEventArgs(userId));
        });

        _connection.On<Guid, bool>(ClientEvent.ContactAdded, (userId, isConnected) =>
        {
            ContactAdded?.Invoke(this, new ContactAddedEventArgs(userId, isConnected));
        });

        _connection.On<Guid>(ClientEvent.ContactRemoved, (userId) =>
        {
            ContactRemoved?.Invoke(this, new ContactRemovedEventArgs(userId));
        });

        _connection.On<NewMessageDto>(ClientEvent.MessageSent, (message) =>
        {
            MessageSent?.Invoke(this, new MessageSentEventArgs(message));
        });

        _connection.On<Guid>(ClientEvent.MessageSendingFailed, (recipientId) =>
        {
            MessageSendingFailed?.Invoke(this, new MessageSendingFailedEventArgs(recipientId));
        });

        _connection.On<MessageUpdateDto>(ClientEvent.MessageUpdated, (message) =>
        {
            MessageUpdated?.Invoke(this, new MessageUpdatedEventArgs(message));
        });

        _connection.On<Guid, Guid>(ClientEvent.MessageUpdatingFailed, (messageId, recipientId) =>
        {
            MessageUpdatingFailed?.Invoke(this, new MessageUpdatingFailedEventArgs(messageId, recipientId));
        });
    }

    public event EventHandler<ConnectedContactsRetrievedEventArgs>? ConnectedContactsRetrieved;
    public event EventHandler? Reconnecting;
    public event EventHandler? Reconnected;
    public event EventHandler<ContactConnectedEventArgs>? ContactConnected;
    public event EventHandler<ContactDisconnectedEventArgs>? ContactDisconnected;
    public event EventHandler<ContactAddedEventArgs>? ContactAdded;
    public event EventHandler<ContactRemovedEventArgs>? ContactRemoved;
    public event EventHandler<MessageSentEventArgs>? MessageSent;
    public event EventHandler<MessageSendingFailedEventArgs>? MessageSendingFailed;
    public event EventHandler<MessageUpdatedEventArgs>? MessageUpdated;
    public event EventHandler<MessageUpdatingFailedEventArgs>? MessageUpdatingFailed;

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
        _userId = userId;

        try
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync(ServerEvent.UserJoin, _userId.Value, contactIds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
        await _connection.DisposeAsync();
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
                await _connection.StartAsync();
                await _connection.InvokeAsync(ServerEvent.UserJoin, _userId!.Value, _contactIds);

                timer.Stop();
            }
            catch
            {
                // Connection failure, continue to retry
            }
        };
        repeatingTimer.Start();
    }

    public async Task SendMessageAsync(Guid recipientId, string message)
    {
        await _connection.InvokeAsync(ServerEvent.SendMessage, recipientId, message);
    }

    public async Task UpdateMessageAsync(Guid id, Guid recipientId, string message)
    {
        await _connection.InvokeAsync(ServerEvent.UpdateMessage, id, recipientId, message);
    }

    public async Task AddContactAsync(Guid userId)
    {
        await _connection.InvokeAsync(ServerEvent.AddContact, userId);
    }

    public async Task RemoveContactAsync(Guid userId)
    {
        await _connection.InvokeAsync(ServerEvent.RemoveContact, userId);
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

public class MessageSentEventArgs : EventArgs
{
    public MessageSentEventArgs(NewMessageDto message)
    {
        Id = message.Id;
        SenderId = message.SenderId;
        RecipientId = message.RecipientId;
        Text = message.Text;
        SentAt = message.SentAtUtc.ToLocalTime();
    }

    public Guid Id { get; }
    public Guid SenderId { get; }
    public Guid RecipientId { get; }
    public string Text { get; }
    public DateTime SentAt { get; }
}

public class MessageUpdatedEventArgs : EventArgs
{
    public MessageUpdatedEventArgs(MessageUpdateDto message)
    {
        Id = message.Id;
        SenderId = message.SenderId;
        RecipientId = message.RecipientId;
        Text = message.Text;
        EditedAt = message.EditedAtUtc.ToLocalTime();
    }

    public Guid Id { get; }
    public Guid SenderId { get; }
    public Guid RecipientId { get; }
    public string Text { get; }
    public DateTime EditedAt { get; }
}

public class MessageSendingFailedEventArgs : EventArgs
{
    public MessageSendingFailedEventArgs(Guid recipientId)
    {
        RecipientId = recipientId;
    }

    public Guid RecipientId { get; init; }
}

public class MessageUpdatingFailedEventArgs : EventArgs
{
    public MessageUpdatingFailedEventArgs(Guid messageId, Guid recipientId)
    {
        MessageId = messageId;
        RecipientId = recipientId;
    }

    public Guid MessageId { get; init; }
    public Guid RecipientId { get; init; }
}
