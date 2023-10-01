using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Direct.Desktop.Storage;
using Direct.Desktop.Storage.Entities;
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
    event EventHandler<MessagePullBatchReceivedEventArgs>? MessagePullBatchReceived;
    event EventHandler<MessagePullCompletedEventArgs>? MessagePullCompleted;
    event EventHandler<MessagePullCanceledEventArgs>? MessagePullCanceled;

    Task<bool> ConnectAsync(Guid userId, HashSet<Guid> contactIds);
    Task DisconnectAsync();
    void StartConnectionRetry();
    Task SendMessageAsync(Guid toUserId, string message);
    Task UpdateMessageAsync(Guid id, Guid recipientId, string message);
    Task AddContactAsync(Guid userId);
    Task RemoveContactAsync(Guid userId);
    Task RequestMessagePullAsync(Guid contactId);
    void CancelMessagePull();
}

public class ChatService : IChatService
{
    private const int StreamingNotificationBatchSize = 200;

    private readonly HubConnection _connection;
    private readonly HashSet<Guid> _contactIds = new();

    private Guid? userId;
    private CancellationTokenSource? streamingCTS;
    private Guid? steamingSenderId;

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
            await _connection.InvokeAsync(ServerEvent.UserJoin, userId!.Value, _contactIds);
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

        _connection.On<Guid>(ClientEvent.StartMessageUpstream, async (contactId) =>
        {
            var messages = await Repository.GetMessagesForSyncAsync(contactId);
            var streamedMessages = messages.Select(x => new StreamedMessageDto(x.Id, x.IsRecipient, x.Text, x.Reaction, x.SentAt.ToUniversalTime(), x.EditedAt?.ToUniversalTime()));

            async IAsyncEnumerable<StreamedMessageDto> clientStreamData()
            {
                foreach (var message in streamedMessages)
                {
                    yield return await Task.FromResult(message);
                }
            }

            await _connection.SendAsync(ServerEvent.MessageUpstream, clientStreamData());
        });

        _connection.On(ClientEvent.StartMessageDownstream, async () =>
        {
            var messageStream = _connection.StreamAsync<StreamedMessageDto>(ServerEvent.MessageDownstream, streamingCTS!.Token);

            var messages = new List<Message>();
            var pulled = 0;
            var batch = 1;
            await foreach (var message in messageStream)
            {
                messages.Add(new Message(message.Id, steamingSenderId!.Value, !message.IsRecipient, message.Text, message.Reaction, message.SentAtUtc.ToLocalTime(), message.EditedAtUtc?.ToLocalTime()));
                pulled++;

                if (pulled == batch * StreamingNotificationBatchSize)
                {
                    MessagePullBatchReceived?.Invoke(this, new MessagePullBatchReceivedEventArgs(pulled));
                    batch++;
                }
            }

            var created = await Repository.CreateMissingMessagesAsync(messages);

            MessagePullCompleted?.Invoke(this, new MessagePullCompletedEventArgs(steamingSenderId!.Value, pulled, created));
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
    public event EventHandler<MessagePullBatchReceivedEventArgs>? MessagePullBatchReceived;
    public event EventHandler<MessagePullCompletedEventArgs>? MessagePullCompleted;
    public event EventHandler<MessagePullCanceledEventArgs>? MessagePullCanceled;

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
            await _connection.StartAsync();
            await _connection.InvokeAsync(ServerEvent.UserJoin, this.userId.Value, contactIds);
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
                await _connection.InvokeAsync(ServerEvent.UserJoin, userId!.Value, _contactIds);

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

    public async Task RequestMessagePullAsync(Guid contactId)
    {
        streamingCTS = new CancellationTokenSource();
        steamingSenderId = contactId;
        await _connection.InvokeAsync(ServerEvent.RequestMessagePull, contactId);
    }

    public void CancelMessagePull()
    {
        streamingCTS?.Cancel();

        MessagePullCanceled?.Invoke(this, new MessagePullCanceledEventArgs(steamingSenderId!.Value));

        steamingSenderId = null;
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

public class MessagePullBatchReceivedEventArgs : EventArgs
{
    public MessagePullBatchReceivedEventArgs(int count)
    {
        Count = count;
    }

    public int Count { get; init; }
}

public class MessagePullCompletedEventArgs : EventArgs
{
    public MessagePullCompletedEventArgs(Guid contactId, int pulled, int created)
    {
        ContactId = contactId;
        Pulled = pulled;
        Created = created;
    }

    public Guid ContactId { get; init; }
    public int Pulled { get; init; }
    public int Created { get; init; }
}

public class MessagePullCanceledEventArgs : EventArgs
{
    public MessagePullCanceledEventArgs(Guid contactId)
    {
        ContactId = contactId;
    }

    public Guid ContactId { get; init; }
}
