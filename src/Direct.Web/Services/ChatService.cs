using System.Collections.Concurrent;
using Direct.Shared.Models;
using Direct.Web.Models;
using Direct.Web.Repositories;
using Direct.Web.Repositories.Models;

namespace Direct.Web.Services;

public interface IChatService
{
    Task<ContactDto> AddContactAsync(string passwordHash, string nickname, string connectionId);
    void RemoveUser(string connectionId);
    Guid? GetUserId(string connectionId);
    Task<List<ContactDto>> GetContactsAsync(string passwordHash);
    Task<SendMessageResult> SendMessageAsync(string connectionId, Guid recipientId, string text);
    Task<UpdateMessageResult> UpdateMessageAsync(string connectionId, Guid id, string text);
}

public class ChatService : IChatService
{
    private readonly IRepository _repository;
    private readonly ConcurrentDictionary<Guid, ConnectedUser> _users = new();

    public ChatService(IRepository repository)
    {
        _repository = repository;

        _users.TryAdd(Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbd-4af9-ab08-1bf6a2d98fe9"),
            "Zane",
            "/Assets/user.png",
            "something else"
        ));

        _users.TryAdd(Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"), new ConnectedUser
        (
            Guid.Parse("018955e6-3bbe-469d-bb1a-e0f74724d46d"),
            "Stef",
            "/Assets/user.png",
            "something"
        ));
    }

    public async Task<ContactDto> AddContactAsync(string passwordHash, string nickname, string connectionId)
    {
        ContactDto contact;

        var user = await _repository.GetUserAsync(passwordHash);
        if (user is not null)
        {
            if (_users.TryGetValue(user.Id, out ConnectedUser? connectedUser))
            {
                connectedUser.ConnectionIds.Add(connectionId);
            }
            else
            {
                connectedUser = new ConnectedUser
                (
                    user.Id,
                    nickname,
                    "/Assets/user.png",
                    connectionId
                );
                _users.TryAdd(user.Id, connectedUser);
            }

            contact = new ContactDto
            {
                Id = user.Id,
                Nickname = nickname,
                ImageUri = connectedUser.ImageUri,
                Messages = Array.Empty<MessageDto>()
            };
        }
        else
        {
            user = await _repository.CreateUserAsync(passwordHash);

            var connectedUser = new ConnectedUser
            (
                user.Id,
                nickname,
                "/Assets/user.png",
                connectionId
            );
            _users.TryAdd(user.Id, connectedUser);

            contact = new ContactDto
            {
                Id = user.Id,
                Nickname = nickname,
                ImageUri = connectedUser.ImageUri,
                Messages = Array.Empty<MessageDto>()
            };
        }

        return contact;
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
            _ = _users.Remove(user.Id, out _);
        }
    }

    public Guid? GetUserId(string connectionId)
    {
        var user = GetUser(connectionId);
        return user?.Id;
    }

    public async Task<List<ContactDto>> GetContactsAsync(string passwordHash)
    {
        var user = await _repository.GetUserAsync(passwordHash) ?? throw new InvalidOperationException("Cannot retrieve contacts for non-existing user");

        var userMessages = await _repository.GetAllAsync(user.Id);
        var userContacts = _users.Values.Where(x => x.Id != user.Id).ToList();

        var result = new List<ContactDto>(userContacts.Count);
        foreach (var contact in userContacts)
        {
            if (contact is null)
            {
                continue;
            }

            var contactMessages = userMessages.Where(x => x.SenderId == contact.Id || x.RecipientId == contact.Id)
                .OrderBy(x => x.SentAt)
                .Select(x => new MessageDto
                {
                    Id = x.Id,
                    SenderId = x.SenderId,
                    RecipientId = x.RecipientId,
                    Text = x.Text,
                    UserIsSender = x.SenderId == user.Id,
                    SentAtUtc = x.SentAt,
                    EditedAtUtc = x.EditedAt
                }).ToArray();

            var contactDto = new ContactDto
            {
                Id = contact.Id,
                Nickname = contact.Nickname,
                ImageUri = contact.ImageUri,
                Messages = contactMessages
            };
            result.Add(contactDto);
        }

        return OrderByLastMessageSent(result);
    }

    public async Task<SendMessageResult> SendMessageAsync(string connectionId, Guid recipientId, string text)
    {
        Guid? senderId = GetUserId(connectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {connectionId}");

        var message = await _repository.CreateMessageAsync(senderId.Value, recipientId, text);

        var connectionIds = GetConnectionIds(new HashSet<Guid> { senderId.Value, recipientId });

        return new SendMessageResult(connectionIds, new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            RecipientId = message.RecipientId,
            Text = message.Text,
            UserIsSender = false,
            SentAtUtc = message.SentAt,
            EditedAtUtc = message.EditedAt
        });
    }

    public async Task<UpdateMessageResult> UpdateMessageAsync(string connectionId, Guid id, string text)
    {
        Guid? senderId = GetUserId(connectionId) ?? throw new InvalidOperationException($"Could not find userId for this connectionId: {connectionId}");

        var message = await _repository.UpdateMessageAsync(id, senderId.Value, text);

        var connectionIds = GetConnectionIds(new HashSet<Guid> { senderId.Value, message.RecipientId });

        return new UpdateMessageResult(connectionIds, new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            RecipientId = message.RecipientId,
            Text = message.Text,
            UserIsSender = false,
            SentAtUtc = message.SentAt,
            EditedAtUtc = message.EditedAt
        });
    }

    private ConnectedUser? GetUser(string connectionId)
    {
        foreach (var contactKvp in _users)
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
        return _users.Where(x => userIds.Contains(x.Value.Id)).SelectMany(x => x.Value.ConnectionIds).ToList();
    }

    private static List<ContactDto> OrderByLastMessageSent(List<ContactDto> contacts)
    {
        return contacts
            .OrderByDescending(
                c => c.Messages.OrderByDescending(m => m.SentAtUtc).Select(m => m.SentAtUtc).FirstOrDefault()
            ).ToList();
    }
}

public class SendMessageResult
{
    public SendMessageResult(List<string> connectionIds, MessageDto message)
    {
        ConnectionIds = connectionIds;
        Message = message;
    }

    public List<string> ConnectionIds { get; set; }
    public MessageDto Message { get; set; }
}

public class UpdateMessageResult
{
    public UpdateMessageResult(List<string> connectionIds, MessageDto message)
    {
        ConnectionIds = connectionIds;
        Message = message;
    }

    public List<string> ConnectionIds { get; set; }
    public MessageDto Message { get; set; }
}
