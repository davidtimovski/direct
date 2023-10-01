using Direct.Shared.Models;

namespace Direct.Web.Models.ChatService;

public record UpdateMessageResult(bool IsSuccessful, List<string> ConnectionIds, MessageUpdateDto? Message);
