namespace Direct.Web.Models;

public class ConnectedUser
{
    public ConnectedUser(Guid id, string nickname, string connectionId)
    {
        Id = id;
        Nickname = nickname;
        ConnectionIds = new HashSet<string> { connectionId };
    }

    public Guid Id { get; set; }
    public string Nickname { get; set; } = null!;
    public HashSet<string> ConnectionIds { get; set; }
}
