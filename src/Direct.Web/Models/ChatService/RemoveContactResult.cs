namespace Direct.Web.Models.ChatService;

public record RemoveContactResult(bool ContactsMatch, Guid? UserId, IReadOnlyList<string> ContactConnectionIds);
