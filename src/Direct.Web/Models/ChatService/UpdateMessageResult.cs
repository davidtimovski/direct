using Direct.Shared.Models;

namespace Direct.Web.Models.ChatService;

public record UpdateMessageResult(bool IsSuccessful, IReadOnlyList<string> ConnectionIds, MessageUpdateDto? Message);
