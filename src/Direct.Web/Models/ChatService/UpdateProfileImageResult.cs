namespace Direct.Web.Models.ChatService;

public record UpdateProfileImageResult(Guid UserId, IReadOnlyList<string> ContactConnectionIds);
