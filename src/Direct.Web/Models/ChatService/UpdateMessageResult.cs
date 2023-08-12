using Direct.Shared.Models;

namespace Direct.Web.Models.ChatService;

public class UpdateMessageResult
{
    public UpdateMessageResult(bool isSuccessful, List<string> connectionIds, MessageUpdateDto? message)
    {
        IsSuccessful = isSuccessful;
        ConnectionIds = connectionIds;
        Message = message;
    }

    public bool IsSuccessful { get; }
    public List<string> ConnectionIds { get; }
    public MessageUpdateDto? Message { get; }
}
