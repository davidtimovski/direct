using System.Collections.Concurrent;
using Direct.Shared.Models;
using Direct.Web.Models;

namespace Direct.Web.Services;

public interface IChatService
{
    List<string> AddConnection(Guid userId, HashSet<Guid> contactIds, string connectionId);
    List<string> RemoveConnection(string connectionId);
    Guid? GetUserId(string connectionId);
    bool ContactIsConnected(Guid userId);
    List<Guid> GetConnectedContacts(HashSet<Guid> userIds);
    SendMessageResult SendMessage(string connectionId, Guid recipientId, string message);
    UpdateMessageResult UpdateMessage(string connectionId, Guid id, Guid recipientId, string text);
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

        return GetConnectionIds(connectedUser.ContactIds);
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

        return GetConnectionIds(connectedUser.ContactIds);
    }

    public Guid? GetUserId(string connectionId)
    {
        var user = GetUser(connectionId);
        return user?.Id;
    }

    public bool ContactIsConnected(Guid userId)
    {
        return _connectedUsers.ContainsKey(userId);
    }

    public List<Guid> GetConnectedContacts(HashSet<Guid> userIds)
    {
        return _connectedUsers.Values
            .Where(x => userIds.Contains(x.Id))
            .Select(x => x.Id).ToList();
    }

    public SendMessageResult SendMessage(string connectionId, Guid recipientId, string message)
    {
        Guid? senderId = GetUserId(connectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {connectionId}");

        var connectionIds = GetConnectionIds(new HashSet<Guid> { senderId.Value, recipientId });

        return new SendMessageResult(connectionIds, new NewMessageDto
        {
            Id = Guid.NewGuid(),
            SenderId = senderId.Value,
            RecipientId = recipientId,
            Text = message,
            SentAtUtc = DateTime.UtcNow
        });
    }

    public UpdateMessageResult UpdateMessage(string connectionId, Guid id, Guid recipientId, string text)
    {
        Guid? senderId = GetUserId(connectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {connectionId}");

        var connectionIds = GetConnectionIds(new HashSet<Guid> { senderId.Value, recipientId });

        return new UpdateMessageResult(connectionIds, new MessageUpdateDto
        {
            Id = id,
            SenderId = senderId.Value,
            RecipientId = recipientId,
            Text = text,
            EditedAtUtc = DateTime.UtcNow
        });
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

    private List<string> GetConnectionIds(HashSet<Guid> userIds)
    {
        return _connectedUsers.Where(x => userIds.Contains(x.Value.Id)).SelectMany(x => x.Value.ConnectionIds).ToList();
    }
}

public class SendMessageResult
{
    public SendMessageResult(List<string> connectionIds, NewMessageDto message)
    {
        ConnectionIds = connectionIds;
        Message = message;
    }

    public List<string> ConnectionIds { get; set; }
    public NewMessageDto Message { get; set; }
}

public class UpdateMessageResult
{
    public UpdateMessageResult(List<string> connectionIds, MessageUpdateDto message)
    {
        ConnectionIds = connectionIds;
        Message = message;
    }

    public List<string> ConnectionIds { get; set; }
    public MessageUpdateDto Message { get; set; }
}
