using System.Collections.Concurrent;
using Direct.Shared.Models;
using Direct.Web.Models.ChatService;

namespace Direct.Web.Services;

public interface IChatService
{
    List<string> AddConnection(Guid userId, HashSet<Guid> contactIds, string connectionId);
    List<string> RemoveConnection(string connectionId);
    Guid? GetUserId(string connectionId);

    /// <summary>
    /// Adds the contact and returns whether the contact is online.
    /// </summary>
    AddContactResult AddContact(string connectionId, Guid contactId);
    RemoveContactResult RemoveContact(string connectionId, Guid contactId);

    /// <summary>
    /// Gets the contacts which also have the user as a contact.
    /// </summary>
    List<Guid> GetConnectedContacts(Guid userId, HashSet<Guid> userIds);

    string GetFirstConnectionId(Guid userId);
    SendMessageResult SendMessage(string senderConnectionId, Guid recipientId, string message);
    UpdateMessageResult UpdateMessage(string senderConnectionId, Guid messageId, Guid recipientId, string text);
    RequestMessagePullResult RequestMessagePull(string requestorConnectionId, Guid contactId);
}

public class ChatService : IChatService
{
    private readonly ConcurrentDictionary<Guid, ConnectedUser> _connectedUsers = new();

    public ChatService(IConfiguration configuration)
    {
        // Test data
        var testUserId = Guid.ParseExact(configuration["TestUserId"]!.ToString(), "N");

        _connectedUsers.TryAdd(Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"),
            new HashSet<Guid> { testUserId },
            new HashSet<string> { "something else" }
        ));

        _connectedUsers.TryAdd(Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"),
            new HashSet<Guid> { testUserId },
            new HashSet<string> { "something" }
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
                new HashSet<string> { connectionId }
            );
            _connectedUsers.TryAdd(userId, connectedUser);
        }

        return GetMatchingContactConnectionIds(userId, connectedUser.ContactIds);
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

        return GetMatchingContactConnectionIds(connectedUser.Id, connectedUser.ContactIds);
    }

    public Guid? GetUserId(string connectionId)
    {
        var user = GetUser(connectionId);
        return user?.Id;
    }

    /// <inheritdoc />
    public AddContactResult AddContact(string connectionId, Guid contactId)
    {
        ConnectedUser? user = GetUser(connectionId) ?? throw new InvalidOperationException($"Could not find a user for this connectionId: {connectionId}");

        if (user.Id == contactId)
        {
            throw new InvalidOperationException("User cannot add themselves as a contact");
        }

        if (!user.ContactIds.Contains(contactId))
        {
            user.ContactIds.Add(contactId);
        }

        if (_connectedUsers.TryGetValue(contactId, out var contact))
        {
            // Contact is online, check whether they match
            if (contact.ContactIds.Contains(user.Id))
            {
                return new AddContactResult(true, user.Id, contact.ConnectionIds.ToList());
            }
        }

        // Contact is not online
        return new AddContactResult(false, null, new List<string>());
    }

    public RemoveContactResult RemoveContact(string connectionId, Guid contactId)
    {
        ConnectedUser? user = GetUser(connectionId) ?? throw new InvalidOperationException($"Could not find a user for this connectionId: {connectionId}");

        if (user.ContactIds.Contains(contactId))
        {
            user.ContactIds.Remove(contactId);
        }

        if (_connectedUsers.TryGetValue(contactId, out var contact))
        {
            // Contact is online, check whether they match
            if (contact.ContactIds.Contains(user.Id))
            {
                return new RemoveContactResult(true, user.Id, contact.ConnectionIds.ToList());
            }
        }

        return new RemoveContactResult(false, null, new List<string>());
    }

    /// <inheritdoc />
    public List<Guid> GetConnectedContacts(Guid userId, HashSet<Guid> userIds)
    {
        return _connectedUsers.Values
            .Where(x => userIds.Contains(x.Id) && x.ContactIds.Contains(userId))
            .Select(x => x.Id).ToList();
    }

    public string GetFirstConnectionId(Guid userId)
    {
        return _connectedUsers[userId].ConnectionIds.First();
    }

    public SendMessageResult SendMessage(string senderConnectionId, Guid recipientId, string message)
    {
        Guid? senderId = GetUserId(senderConnectionId) ?? throw new InvalidOperationException($"Could not find a userId for this connectionId: {senderConnectionId}");

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
            (
                Guid.NewGuid(),
                senderId.Value,
                recipientId,
                message,
                DateTime.UtcNow
            ));
    }

    public UpdateMessageResult UpdateMessage(string senderConnectionId, Guid messageId, Guid recipientId, string text)
    {
        Guid? senderId = GetUserId(senderConnectionId) ?? throw new InvalidOperationException($"Could not find a userId for this connectionId: {senderConnectionId}");

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
            (
                messageId,
                senderId.Value,
                recipientId,
                text,
                DateTime.UtcNow
            ));
    }

    public RequestMessagePullResult RequestMessagePull(string requestorConnectionId, Guid contactId)
    {
        Guid? recipientUserId = GetUserId(requestorConnectionId) ?? throw new InvalidOperationException($"Could not find a userId for this connectionId: {requestorConnectionId}");

        if (!CanSendMessageTo(recipientUserId.Value, contactId))
        {
            throw new InvalidOperationException("Message sync requestor isn't allowed to request from the specified contact");
        }

        var senderConnectionId = GetFirstConnectionId(contactId);

        return new RequestMessagePullResult(recipientUserId.Value, senderConnectionId);
    }

    private bool CanSendMessageTo(Guid senderId, Guid recipientId)
    {
        if (senderId == recipientId)
        {
            return false;
        }

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

    private List<string> GetMatchingContactConnectionIds(Guid userId, HashSet<Guid> contactIds)
    {
        return _connectedUsers.Where(x => contactIds.Contains(x.Value.Id) && x.Value.ContactIds.Contains(userId)).SelectMany(x => x.Value.ConnectionIds).ToList();
    }
}
