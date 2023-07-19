using Direct.Shared;
using Direct.Shared.Models;
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

    public async Task Join(Guid id, string nickname, Guid[] userIds)
    {
        var nicknameError = Validation.ValidateNickname(nickname);
        if (nicknameError != null)
        {
            throw new InvalidOperationException(nicknameError);
        }

        _chatService.AddContact(id, nickname, Context.ConnectionId);
        var contacts = _chatService.GetContacts(userIds);

        await Clients.Caller.SendAsync(ClientEvent.Joined, contacts);
        await Clients.Others.SendAsync(ClientEvent.ContactJoined, new ContactDto
        {
            Id = id,
            Nickname = nickname
        });
    }

    public async Task SendMessage(Guid recipientId, string text)
    {
        var result = _chatService.SendMessage(Context.ConnectionId, recipientId, text);
        foreach (var connectionId in result.ConnectionIds)
        {
            await Clients.Client(connectionId).SendAsync(ClientEvent.MessageSent, result.Message);
        }
    }

    public async Task UpdateMessage(Guid id, Guid recipientId, string text)
    {
        var result = _chatService.UpdateMessage(Context.ConnectionId, id, recipientId, text);
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
