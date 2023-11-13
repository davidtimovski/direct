namespace Direct.Web.Models.ChatService;

public class ConnectedUser
{
    public required Guid Id { get; init; }
    public required string ProfileImage { get; set; }
    public required HashSet<Guid> ContactIds { get; init; }
    public required HashSet<string> ConnectionIds { get; init; }
}
