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

namespace Direct.Desktop.Services;

public interface IPullProxy
{
    /// <summary>
    /// Invoked when the client starts upstreaming messages to the server.
    /// </summary>
    event EventHandler<MessagePullUpstreamEventArgs>? UpstreamStarted;

    /// <summary>
    /// Invoked when the client starts completes the message upstreaming.
    /// </summary>
    event EventHandler<MessagePullUpstreamEventArgs>? UpstreamCompleted;

    /// <summary>
    /// Invoked when a batch of 200 messages were received through the downstream.
    /// </summary>
    event EventHandler<MessagePullBatchReceivedEventArgs>? BatchReceived;

    /// <summary>
    /// Invoked when the message downstream completed.
    /// </summary>
    event EventHandler<MessagePullCompletedEventArgs>? Completed;

    /// <summary>
    /// Invoked when the message downstream was cancelled by the client.
    /// </summary>
    event EventHandler<MessagePullCanceledEventArgs>? Canceled;

    Task RequestMessagePullAsync(Guid contactId);
    void CancelMessagePull();
}

public class PullProxy : IPullProxy
{
    private const int StreamingNotificationBatchSize = 200;

    private readonly IConnectionService _connectionService;

    private CancellationTokenSource? streamingCTS;
    private Guid? steamingSenderId;

    public PullProxy(IConnectionService connectionService)
    {
        _connectionService = connectionService;

        _connectionService.Connection.On<Guid>(ClientEvent.StartMessageUpstream, async (contactId) =>
        {
            UpstreamStarted?.Invoke(this, new MessagePullUpstreamEventArgs { ContactId = contactId });

            var messages = await Repository.GetMessagesForSyncAsync(contactId);
            var streamedMessages = messages.Select(x => new StreamedMessageDto(x.Id, x.IsRecipient, x.Text, x.Reaction, x.SentAt.ToUniversalTime(), x.EditedAt?.ToUniversalTime()));

            async IAsyncEnumerable<StreamedMessageDto> streamData()
            {
                foreach (var message in streamedMessages)
                {
                    yield return await Task.FromResult(message);
                }
            }

            await _connectionService.Connection.SendAsync(ServerEvent.MessageUpstream, streamData());

            UpstreamCompleted?.Invoke(this, new MessagePullUpstreamEventArgs { ContactId = contactId });
        });

        _connectionService.Connection.On(ClientEvent.StartMessageDownstream, async () =>
        {
            var messageStream = _connectionService.Connection.StreamAsync<StreamedMessageDto>(ServerEvent.MessageDownstream, streamingCTS!.Token);

            var messages = new List<Message>();
            var pulled = 0;
            var batch = 1;
            await foreach (var message in messageStream)
            {
                messages.Add(new Message(message.Id, steamingSenderId!.Value, !message.IsRecipient, message.Text, message.Reaction, message.SentAtUtc.ToLocalTime(), message.EditedAtUtc?.ToLocalTime()));
                pulled++;

                if (pulled == batch * StreamingNotificationBatchSize)
                {
                    BatchReceived?.Invoke(this, new MessagePullBatchReceivedEventArgs { Count = pulled });
                    batch++;
                }
            }

            var created = await Repository.CreateMissingMessagesAsync(messages);

            Completed?.Invoke(this, new MessagePullCompletedEventArgs { ContactId = steamingSenderId!.Value, Pulled = pulled, Created = created });
        });
    }

    public event EventHandler<MessagePullUpstreamEventArgs>? UpstreamStarted;
    public event EventHandler<MessagePullUpstreamEventArgs>? UpstreamCompleted;
    public event EventHandler<MessagePullBatchReceivedEventArgs>? BatchReceived;
    public event EventHandler<MessagePullCompletedEventArgs>? Completed;
    public event EventHandler<MessagePullCanceledEventArgs>? Canceled;

    public async Task RequestMessagePullAsync(Guid contactId)
    {
        streamingCTS = new CancellationTokenSource();
        steamingSenderId = contactId;
        await _connectionService.Connection.InvokeAsync(ServerEvent.RequestMessagePull, contactId);
    }

    public void CancelMessagePull()
    {
        streamingCTS?.Cancel();

        Canceled?.Invoke(this, new MessagePullCanceledEventArgs { ContactId = steamingSenderId!.Value });

        steamingSenderId = null;
    }
}

public class MessagePullUpstreamEventArgs : EventArgs
{
    public required Guid ContactId { get; init; }
}

public class MessagePullBatchReceivedEventArgs : EventArgs
{
    public required int Count { get; init; }
}

public class MessagePullCompletedEventArgs : EventArgs
{
    public required Guid ContactId { get; init; }
    public required int Pulled { get; init; }
    public required int Created { get; init; }
}

public class MessagePullCanceledEventArgs : EventArgs
{
    public required Guid ContactId { get; init; }
}
