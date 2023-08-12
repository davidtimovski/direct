namespace Direct.Web.Models.ChatService;

public class ConnectedUser
{
    public ConnectedUser(Guid id, HashSet<Guid> contactIds, string connectionId)
    {
        Id = id;
        ContactIds = contactIds;
        ConnectionIds = new HashSet<string> { connectionId };
    }

    public Guid Id { get; }
    public HashSet<Guid> ContactIds { get; }
    public HashSet<string> ConnectionIds { get; }
}
