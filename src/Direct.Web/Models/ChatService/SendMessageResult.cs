using Direct.Shared.Models;

namespace Direct.Web.Models.ChatService;

public record SendMessageResult(bool IsSuccessful, List<string> ConnectionIds, NewMessageDto? Message);
