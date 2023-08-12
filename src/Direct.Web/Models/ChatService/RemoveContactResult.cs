namespace Direct.Web.Models.ChatService;

public class RemoveContactResult
{
    public RemoveContactResult()
    {
    }

    public RemoveContactResult(Guid userId, List<string> contactConnectionIds)
    {
        UserId = userId;
        ContactsMatch = true;
        ContactConnectionIds = contactConnectionIds;
    }

    public Guid? UserId { get; }
    public bool ContactsMatch { get; }
    public List<string> ContactConnectionIds { get; } = new();
}
