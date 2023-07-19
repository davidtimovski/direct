using System.Collections.Concurrent;
using Direct.Shared.Models;
using Direct.Web.Models;

namespace Direct.Web.Services;

public interface IChatService
{
    void AddContact(Guid id, string nickname, string connectionId);
    void RemoveUser(string connectionId);
    Guid? GetUserId(string connectionId);
    List<ContactDto> GetContacts(Guid[] userIds);
    SendMessageResult SendMessage(string connectionId, Guid recipientId, string text);
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
            "Zane",
            "something else"
        ));

        _connectedUsers.TryAdd(Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"),
            "Stef",
            "something"
        ));
    }

    public void AddContact(Guid id, string nickname, string connectionId)
    {
        if (_connectedUsers.TryGetValue(id, out ConnectedUser? connectedUser))
        {
            connectedUser.ConnectionIds.Add(connectionId);
        }
        else
        {
            connectedUser = new ConnectedUser
            (
                id,
                nickname,
                connectionId
            );
            _connectedUsers.TryAdd(id, connectedUser);
        }
    }

    public void RemoveUser(string connectionId)
    {
        var user = GetUser(connectionId);
        if (user is null)
        {
            return;        
        }

        if (user.ConnectionIds.Count > 1)
        {
            user.ConnectionIds.Remove(connectionId);
        }
        else
        {
            _ = _connectedUsers.Remove(user.Id, out _);
        }
    }

    public Guid? GetUserId(string connectionId)
    {
        var user = GetUser(connectionId);
        return user?.Id;
    }

    public List<ContactDto> GetContacts(Guid[] userIds)
    {
        return _connectedUsers.Values
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new ContactDto
            {
                Id = x.Id,
                Nickname = x.Nickname
            }).ToList();
    }

    public SendMessageResult SendMessage(string connectionId, Guid recipientId, string text)
    {
        Guid? senderId = GetUserId(connectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {connectionId}");

        var connectionIds = GetConnectionIds(new HashSet<Guid> { senderId.Value, recipientId });

        return new SendMessageResult(connectionIds, new NewMessageDto
        {
            Id = Guid.NewGuid(),
            SenderId = senderId.Value,
            RecipientId = recipientId,
            Text = text,
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
