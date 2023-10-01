namespace Direct.Web.Models.ChatService;

public record RequestMessagePullResult(Guid RecipientUserId, string SenderConnectionId);
