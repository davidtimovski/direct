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

    public async Task Connect(Guid userId, HashSet<Guid> contactIds)
    {
        var contactConnectionIds = _chatService.AddConnection(userId, contactIds, Context.ConnectionId);

        var connected = _chatService.GetConnectedContacts(contactIds);

        await Clients.Caller.SendAsync(ClientEvent.Connected, connected);
        await Clients.Clients(contactConnectionIds).SendAsync(ClientEvent.ContactConnected, userId);
    }

    public async Task ContactIsConnected(Guid userId)
    {
        var isConnected = _chatService.ContactIsConnected(userId);
        await Clients.Caller.SendAsync(ClientEvent.AddedContactIsConnected, userId, isConnected);
    }

    public async Task SendMessage(Guid recipientId, string message)
    {
        var result = _chatService.SendMessage(Context.ConnectionId, recipientId, message);
        await Clients.Clients(result.ConnectionIds).SendAsync(ClientEvent.MessageSent, result.Message);
    }

    public async Task UpdateMessage(Guid id, Guid recipientId, string text)
    {
        var result = _chatService.UpdateMessage(Context.ConnectionId, id, recipientId, text);
        await Clients.Clients(result.ConnectionIds).SendAsync(ClientEvent.MessageUpdated, result.Message);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Guid? userId = _chatService.GetUserId(Context.ConnectionId);

        var contactConnectionIds = _chatService.RemoveConnection(Context.ConnectionId);

        if (userId == null)
        {
            return;
        }

        await Clients.Clients(contactConnectionIds).SendAsync(ClientEvent.ContactDisconnected, userId);
    }
}
