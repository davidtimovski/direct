namespace Direct.Web.Models.ChatService;

public record ConnectedUser(Guid Id, HashSet<Guid> ContactIds, HashSet<string> ConnectionIds);
