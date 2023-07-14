namespace Direct.Web.Models;

public class ConnectedUser
{
    public ConnectedUser(Guid id, string nickname, string imageUri, string connectionId)
    {
        Id = id;
        Nickname = nickname;
        ImageUri = imageUri;
        ConnectionIds = new HashSet<string> { connectionId };
    }

    public Guid Id { get; set; }
    public string Nickname { get; set; } = null!;
    public string ImageUri { get; set; } = null!;
    public HashSet<string> ConnectionIds { get; set; }
}
