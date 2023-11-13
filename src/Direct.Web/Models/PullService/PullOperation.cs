using Direct.Shared.Models;

namespace Direct.Web.Models.PullService;

public class PullOperation
{
    public required DateTime Started { get; init; }
    public required string SenderConnectionId { get; init; }
    public required string RecipientConnectionId { get; init; }
    public List<StreamedMessageDto> Messages { get; init; } = new();

    public bool IsExpired() => DateTime.UtcNow.AddMinutes(-15) > Started;
}
