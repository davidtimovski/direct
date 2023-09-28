namespace Direct.Shared.Models;

public class NewMessageDto
{
    public required Guid Id { get; init; }
    public required Guid SenderId { get; init; }
    public required Guid RecipientId { get; init; }
    public required string Text { get; init; }
    public required DateTime SentAtUtc { get; init; }
}
