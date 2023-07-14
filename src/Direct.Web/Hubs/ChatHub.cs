using Direct.Shared;
using Direct.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace Direct.Web.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
       _chatService = chatService;
    }

    public async Task Join(string passwordHash, string nickname)
    {
        var valid = Validation.PasswordHashIsValid(passwordHash);
        if (!valid)
        {
            throw new InvalidOperationException("Password hash is not valid");
        }

        var nicknameError = Validation.ValidateNickname(nickname);
        if (nicknameError != null)
        {
            throw new InvalidOperationException(nicknameError);
        }

        var contact = await _chatService.AddContactAsync(passwordHash, nickname, Context.ConnectionId);

        var contacts = await _chatService.GetContactsAsync(passwordHash);

        await Clients.Caller.SendAsync(ClientEvent.Joined, contact.Id, contacts);

        await Clients.Others.SendAsync(ClientEvent.ContactJoined, contact);
    }

    public async Task SendMessage(Guid recipientId, string text)
    {
        var result = await _chatService.SendMessageAsync(Context.ConnectionId, recipientId, text);
        foreach (var connectionId in result.ConnectionIds)
        {
            await Clients.Client(connectionId).SendAsync(ClientEvent.MessageSent, result.Message);
        }
    }

    public async Task UpdateMessage(Guid id, string text)
    {
        var result = await _chatService.UpdateMessageAsync(Context.ConnectionId, id, text);
        foreach (var connectionId in result.ConnectionIds)
        {
            await Clients.Client(connectionId).SendAsync(ClientEvent.MessageUpdated, result.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Guid? userId = _chatService.GetUserId(Context.ConnectionId);

        _chatService.RemoveUser(Context.ConnectionId);

        if (userId == null)
        {
            return;
        }

        await Clients.All.SendAsync(ClientEvent.ContactLeft, userId);
    }
}
