namespace Direct.Web.Models.ChatService;

public record AddContactResult(bool ContactsMatch, Guid? UserId, List<string> ContactConnectionIds);
