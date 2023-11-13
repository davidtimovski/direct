namespace Direct.Web.Models.ChatService;

public record AddContactResult(bool ContactsMatch, Guid? UserId, string? ProfileImage, IReadOnlyList<string> ContactConnectionIds);
