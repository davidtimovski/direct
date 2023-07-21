namespace Direct.Web.Models;

public class ConnectedUser
{
    public ConnectedUser(Guid id, HashSet<Guid> contactIds, string connectionId)
    {
        Id = id;
        ContactIds = contactIds;
        ConnectionIds = new HashSet<string> { connectionId };
    }

    public Guid Id { get; set; }
    public HashSet<Guid> ContactIds { get; set; }
    public HashSet<string> ConnectionIds { get; set; }
}
