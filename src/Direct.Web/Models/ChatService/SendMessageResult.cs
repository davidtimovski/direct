using Direct.Shared.Models;

namespace Direct.Web.Models.ChatService;

public class SendMessageResult
{
    public SendMessageResult(bool isSuccessful, List<string> connectionIds, NewMessageDto? message)
    {
        IsSuccessful = isSuccessful;
        ConnectionIds = connectionIds;
        Message = message;
    }

    public bool IsSuccessful { get; }
    public List<string> ConnectionIds { get; }
    public NewMessageDto? Message { get; }
}
