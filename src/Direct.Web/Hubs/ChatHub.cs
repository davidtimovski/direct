using System.Runtime.CompilerServices;
using Direct.Shared;
using Direct.Shared.Models;
using Direct.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace Direct.Web.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IPullService _pullService;

    public ChatHub(IChatService chatService, IPullService pullService)
    {
        _chatService = chatService;
        _pullService = pullService;
    }

    public async Task UserJoin(Guid userId, HashSet<Guid> contactIds)
    {
        var contactConnectionIds = _chatService.AddConnection(userId, contactIds, Context.ConnectionId);

        var connected = _chatService.GetConnectedContacts(userId, contactIds);

        await Clients.Caller.SendAsync(ClientEvent.ConnectedContactsRetrieved, connected);
        await Clients.Clients(contactConnectionIds).SendAsync(ClientEvent.ContactConnected, userId);
    }

    public async Task SendMessage(Guid recipientId, string message)
    {
        var result = _chatService.SendMessage(Context.ConnectionId, recipientId, message);

        if (result.IsSuccessful)
        {
            await Clients.Clients(result.ConnectionIds).SendAsync(ClientEvent.MessageSent, result.Message);
        }
        else
        {
            await Clients.Caller.SendAsync(ClientEvent.MessageSendingFailed, recipientId);
        }
    }

    public async Task UpdateMessage(Guid messageId, Guid recipientId, string text)
    {
        var result = _chatService.UpdateMessage(Context.ConnectionId, messageId, recipientId, text);

        if (result.IsSuccessful)
        {
            await Clients.Clients(result.ConnectionIds).SendAsync(ClientEvent.MessageUpdated, result.Message);
        }
        else
        {
            await Clients.Caller.SendAsync(ClientEvent.MessageUpdatingFailed, recipientId);
        }
    }

    public async Task AddContact(Guid contactId)
    {
        var result = _chatService.AddContact(Context.ConnectionId, contactId);

        await Clients.Caller.SendAsync(ClientEvent.ContactAdded, contactId, result.ContactsMatch);

        // Notify added contact
        if (result.ContactsMatch)
        {
            await Clients.Clients(result.ContactConnectionIds).SendAsync(ClientEvent.ContactConnected, result.UserId!.Value);
        }
    }

    public async Task RemoveContact(Guid contactId)
    {
        var result = _chatService.RemoveContact(Context.ConnectionId, contactId);

        await Clients.Caller.SendAsync(ClientEvent.ContactRemoved, contactId);

        // Notify removed contact
        if (result.ContactsMatch)
        {
            await Clients.Clients(result.ContactConnectionIds).SendAsync(ClientEvent.ContactDisconnected, result.UserId!.Value);
        }
    }

    public async Task RequestMessagePull(Guid contactId)
    {
        var result = _chatService.RequestMessagePull(Context.ConnectionId, contactId);

        _pullService.Create(result.SenderConnectionId, Context.ConnectionId);

        await Clients.Client(result.SenderConnectionId).SendAsync(ClientEvent.StartMessageUpstream, result.RecipientUserId);
    }

    public async Task MessageUpstream(IAsyncEnumerable<StreamedMessageDto> messageStream)
    {
        await foreach (var message in messageStream)
        {
            _pullService.AddMessage(Context.ConnectionId, message);
        }

        var recipientConnectionId = _pullService.GetRecipientConnectionId(Context.ConnectionId);
        await Clients.Client(recipientConnectionId).SendAsync(ClientEvent.StartMessageDownstream);
    }

    public async IAsyncEnumerable<StreamedMessageDto> MessageDownstream([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = _pullService.GetMessages(Context.ConnectionId);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return message;
        }

        _pullService.Complete(Context.ConnectionId);
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
