namespace Direct.Web.Models.ChatService;

public class AddContactResult
{
    public AddContactResult()
    {
    }

    public AddContactResult(Guid userId, List<string> contactConnectionIds)
    {
        UserId = userId;
        ContactsMatch = true;
        ContactConnectionIds = contactConnectionIds;
    }

    public Guid? UserId { get; }
    public bool ContactsMatch { get; }
    public List<string> ContactConnectionIds { get; } = new();
}
