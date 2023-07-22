using System.Collections.Concurrent;
using Direct.Shared.Models;
using Direct.Web.Models;

namespace Direct.Web.Services;

public interface IChatService
{
    List<string> AddConnection(Guid userId, HashSet<Guid> contactIds, string connectionId);
    List<string> RemoveConnection(string connectionId);
    Guid? GetUserId(string connectionId);
    bool AddContact(string connectionId, Guid contactId);
    List<Guid> GetConnectedContacts(HashSet<Guid> userIds);
    SendMessageResult SendMessage(string senderConnectionId, Guid recipientId, string message);
    UpdateMessageResult UpdateMessage(string senderConnectionId, Guid messageId, Guid recipientId, string text);
}

public class ChatService : IChatService
{
    private readonly ConcurrentDictionary<Guid, ConnectedUser> _connectedUsers = new();

    public ChatService()
    {
        _connectedUsers.TryAdd(Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"),
            new HashSet<Guid> { new Guid("a4fb87bb-93ae-43f2-baac-2ef0282f72eb") },
            "something else"
        ));

        _connectedUsers.TryAdd(Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"),
            new HashSet<Guid> { new Guid("a4fb87bb-93ae-43f2-baac-2ef0282f72eb") },
            "something"
        ));
    }

    public List<string> AddConnection(Guid userId, HashSet<Guid> contactIds, string connectionId)
    {
        if (_connectedUsers.TryGetValue(userId, out ConnectedUser? connectedUser))
        {
            connectedUser.ContactIds.UnionWith(contactIds);
            connectedUser.ConnectionIds.Add(connectionId);
        }
        else
        {
            connectedUser = new ConnectedUser
            (
                userId,
                contactIds,
                connectionId
            );
            _connectedUsers.TryAdd(userId, connectedUser);
        }

        return GetContactConnectionIds(userId, connectedUser.ContactIds);
    }

    public List<string> RemoveConnection(string connectionId)
    {
        var connectedUser = GetUser(connectionId);
        if (connectedUser is null)
        {
            return new List<string>();        
        }

        if (connectedUser.ConnectionIds.Count > 1)
        {
            connectedUser.ConnectionIds.Remove(connectionId);
        }
        else
        {
            _ = _connectedUsers.Remove(connectedUser.Id, out _);
        }

        return GetContactConnectionIds(connectedUser.Id, connectedUser.ContactIds);
    }

    public Guid? GetUserId(string connectionId)
    {
        var user = GetUser(connectionId);
        return user?.Id;
    }

    /// <summary>
    /// Adds the contact and returns whether the contact is online.
    /// </summary>
    public bool AddContact(string connectionId, Guid contactId)
    {
        ConnectedUser? user = GetUser(connectionId) ?? throw new InvalidOperationException($"Could not find user for this connectionId: {connectionId}");

        if (!user.ContactIds.Contains(contactId))
        {
            user.ContactIds.Add(contactId);
        }

        var contact = _connectedUsers[contactId];

        return contact.ContactIds.Contains(user.Id);
    }

    public List<Guid> GetConnectedContacts(HashSet<Guid> userIds)
    {
        return _connectedUsers.Values
            .Where(x => userIds.Contains(x.Id))
            .Select(x => x.Id).ToList();
    }

    public SendMessageResult SendMessage(string senderConnectionId, Guid recipientId, string message)
    {
        Guid? senderId = GetUserId(senderConnectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {senderConnectionId}");

        if (!CanSendMessageTo(senderId.Value, recipientId))
        {
            return new SendMessageResult(
                false,
                new List<string>(),
                null);
        }

        var connectionIds = GetConnectionIds(senderId.Value, recipientId);

        return new SendMessageResult(
            true,
            connectionIds,
            new NewMessageDto
            {
                Id = Guid.NewGuid(),
                SenderId = senderId.Value,
                RecipientId = recipientId,
                Text = message,
                SentAtUtc = DateTime.UtcNow
            });
    }

    public UpdateMessageResult UpdateMessage(string senderConnectionId, Guid messageId, Guid recipientId, string text)
    {
        Guid? senderId = GetUserId(senderConnectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {senderConnectionId}");

        if (!CanSendMessageTo(senderId.Value, recipientId))
        {
            return new UpdateMessageResult(
                false,
                new List<string>(),
                null);
        }

        var connectionIds = GetConnectionIds(senderId.Value, recipientId);

        return new UpdateMessageResult(
            true,
            connectionIds,
            new MessageUpdateDto
            {
                Id = messageId,
                SenderId = senderId.Value,
                RecipientId = recipientId,
                Text = text,
                EditedAtUtc = DateTime.UtcNow
            });
    }

    private bool CanSendMessageTo(Guid senderId, Guid recipientId)
    {
        if (_connectedUsers.TryGetValue(recipientId, out var recipient))
        {
            return recipient.ContactIds.Contains(senderId);
        }

        return false;
    }

    private ConnectedUser? GetUser(string connectionId)
    {
        foreach (var contactKvp in _connectedUsers)
        {
            if (contactKvp.Value.ConnectionIds.Contains(connectionId))
            {
                return contactKvp.Value;
            }
        }

        return null;
    }

    private List<string> GetConnectionIds(Guid senderId, Guid recipientId)
    {
        return _connectedUsers[senderId].ConnectionIds.Concat(_connectedUsers[recipientId].ConnectionIds).ToList();
    }

    private List<string> GetContactConnectionIds(Guid userId, HashSet<Guid> contactIds)
    {
        return _connectedUsers.Where(x => contactIds.Contains(x.Value.Id) && x.Value.ContactIds.Contains(userId)).SelectMany(x => x.Value.ConnectionIds).ToList();
    }
}

public class SendMessageResult
{
    public SendMessageResult(bool isSuccessful, List<string> connectionIds, NewMessageDto? message)
    {
        IsSuccessful = isSuccessful;
        ConnectionIds = connectionIds;
        Message = message;
    }

    public bool IsSuccessful { get; set; }
    public List<string> ConnectionIds { get; set; }
    public NewMessageDto? Message { get; set; }
}

public class UpdateMessageResult
{
    public UpdateMessageResult(bool isSuccessful, List<string> connectionIds, MessageUpdateDto? message)
    {
        IsSuccessful = isSuccessful;
        ConnectionIds = connectionIds;
        Message = message;
    }

    public bool IsSuccessful { get; set; }
    public List<string> ConnectionIds { get; set; }
    public MessageUpdateDto? Message { get; set; }
}
