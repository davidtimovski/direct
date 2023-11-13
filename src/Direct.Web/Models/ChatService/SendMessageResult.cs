using Direct.Shared.Models;

namespace Direct.Web.Models.ChatService;

public record SendMessageResult(bool IsSuccessful, IReadOnlyList<string> ConnectionIds, NewMessageDto? Message);
