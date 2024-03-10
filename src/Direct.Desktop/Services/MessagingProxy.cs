using System;
using System.Threading.Tasks;
using Direct.Shared;
using Direct.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Direct.Desktop.Services;

public interface IMessagingProxy
{
    /// <summary>
    /// Invoked when the server responds that a message was successfully sent.
    /// </summary>
    event EventHandler<MessageSentEventArgs>? Sent;

    /// <summary>
    /// Invoked when the server responds that a message could not be sent.
    /// </summary>
    event EventHandler<MessageSendingFailedEventArgs>? SendingFailed;

    /// <summary>
    /// Invoked when the server responds that a message was successfully updated.
    /// </summary>
    event EventHandler<MessageUpdatedEventArgs>? Updated;

    /// <summary>
    /// Invoked when the server responds that a message could not be updated.
    /// </summary>
    event EventHandler<MessageUpdatingFailedEventArgs>? UpdatingFailed;

    Task SendMessageAsync(Guid toUserId, string message);
    Task UpdateMessageAsync(Guid id, Guid recipientId, string message);
}

public class MessagingProxy : IMessagingProxy
{
    private readonly IConnectionService _connectionService;

    public MessagingProxy(IConnectionService connectionService)
    {
        _connectionService = connectionService;

        _connectionService.Connection.On<NewMessageDto>(ClientEvent.MessageSent, (message) =>
        {
            Sent?.Invoke(this, new MessageSentEventArgs(message));
        });

        _connectionService.Connection.On<Guid>(ClientEvent.MessageSendingFailed, (recipientId) =>
        {
            SendingFailed?.Invoke(this, new MessageSendingFailedEventArgs { RecipientId = recipientId });
        });

        _connectionService.Connection.On<MessageUpdateDto>(ClientEvent.MessageUpdated, (message) =>
        {
            Updated?.Invoke(this, new MessageUpdatedEventArgs(message));
        });

        _connectionService.Connection.On<Guid, Guid>(ClientEvent.MessageUpdatingFailed, (messageId, recipientId) =>
        {
            UpdatingFailed?.Invoke(this, new MessageUpdatingFailedEventArgs { MessageId = messageId, RecipientId = recipientId });
        });
    }

    public event EventHandler<MessageSentEventArgs>? Sent;
    public event EventHandler<MessageSendingFailedEventArgs>? SendingFailed;
    public event EventHandler<MessageUpdatedEventArgs>? Updated;
    public event EventHandler<MessageUpdatingFailedEventArgs>? UpdatingFailed;

    public async Task SendMessageAsync(Guid recipientId, string message)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.SendMessage, recipientId, message);
    }

    public async Task UpdateMessageAsync(Guid id, Guid recipientId, string message)
    {
        await _connectionService.Connection.InvokeAsync(ServerEvent.UpdateMessage, id, recipientId, message);
    }
}

public class MessageSentEventArgs(NewMessageDto message) : EventArgs
{
    public Guid Id { get; } = message.Id;
    public Guid SenderId { get; } = message.SenderId;
    public Guid RecipientId { get; } = message.RecipientId;
    public string Text { get; } = message.Text;
    public DateTime SentAt { get; } = message.SentAtUtc.ToLocalTime();
}

public class MessageUpdatedEventArgs(MessageUpdateDto message) : EventArgs
{
    public Guid Id { get; } = message.Id;
    public Guid SenderId { get; } = message.SenderId;
    public Guid RecipientId { get; } = message.RecipientId;
    public string Text { get; } = message.Text;
    public DateTime EditedAt { get; } = message.EditedAtUtc.ToLocalTime();
}

public class MessageSendingFailedEventArgs : EventArgs
{
    public required Guid RecipientId { get; init; }
}

public class MessageUpdatingFailedEventArgs : EventArgs
{
    public required Guid MessageId { get; init; }
    public required Guid RecipientId { get; init; }
}
