using System.Collections.Concurrent;
using Direct.Shared.Models;
using Direct.Web.Models.PullService;

namespace Direct.Web.Services;

public interface IPullService
{
    /// <summary>
    /// Creates a sync operation. If one already exists, removes it and recreates.
    /// </summary>
    void Create(string senderConnectionId, string toConnectionId);

    void AddMessage(string senderConnectionId, StreamedMessageDto message);
    string GetRecipientConnectionId(string senderConnectionId);
    IReadOnlyList<StreamedMessageDto> GetMessages(string recipientConnectionId);

    /// <summary>
    /// Removes the sync operation.
    /// </summary>
    void Complete(string recipientConnectionId);

    /// <summary>
    /// Removes sync operations that are expired.
    /// </summary>
    void RemoveExpiredOperations();
}

public class PullService : IPullService
{
    private readonly ConcurrentDictionary<string, PullOperation> _syncOperations = new();

    /// <inheritdoc />
    public void Create(string senderConnectionId, string recipientConnectionId)
    {
        if (senderConnectionId == recipientConnectionId)
        {
            throw new InvalidOperationException("Cannot create sync operation - sender and recipient are the same");
        }

        if (_syncOperations.ContainsKey(senderConnectionId))
        {
            _syncOperations.Remove(senderConnectionId, out PullOperation? _);
        }

        _syncOperations.TryAdd(senderConnectionId, new PullOperation
        {
            Started = DateTime.UtcNow,
            SenderConnectionId = senderConnectionId,
            RecipientConnectionId = recipientConnectionId
        });
    }

    public void AddMessage(string senderConnectionId, StreamedMessageDto message)
    {
        _syncOperations[senderConnectionId].Messages.Add(message);
    }

    public string GetRecipientConnectionId(string senderConnectionId)
    {
        if (_syncOperations.TryGetValue(senderConnectionId, out PullOperation? syncOperation))
        {
            return syncOperation.RecipientConnectionId;
        }
        else
        {
            throw new InvalidOperationException($"There is no sync operation for the connectionId: {senderConnectionId}");
        }
    }

    public IReadOnlyList<StreamedMessageDto> GetMessages(string recipientConnectionId)
    {
        foreach (var operationKvp in _syncOperations)
        {
            if (operationKvp.Value.RecipientConnectionId == recipientConnectionId)
            {
                return operationKvp.Value.Messages;
            }
        }

        throw new InvalidOperationException($"There is no sync operation for the recipient connectionId: {recipientConnectionId}");
    }

    public void Complete(string recipientConnectionId)
    {
        PullOperation? syncOperation = null;
        foreach (var operationKvp in _syncOperations)
        {
            if (operationKvp.Value.RecipientConnectionId == recipientConnectionId)
            {
                syncOperation = operationKvp.Value;
                break;
            }
        }

        if (syncOperation is null)
        {
            throw new InvalidOperationException($"There is no sync operation for the recipient connectionId: {recipientConnectionId}");
        }

        _syncOperations.Remove(syncOperation.SenderConnectionId, out PullOperation? _);
    }

    public void RemoveExpiredOperations()
    {
        foreach (var operationKvp in _syncOperations)
        {
            if (operationKvp.Value.IsExpired())
            {
                _syncOperations.TryRemove(operationKvp);
            }
        }
    }
}
