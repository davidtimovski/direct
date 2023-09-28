namespace Direct.Shared.Models;

public class MessageUpdateDto
{
    public required Guid Id { get; init; }
    public required Guid SenderId { get; init; }
    public required Guid RecipientId { get; init; }
    public required string Text { get; init; }
    public required DateTime EditedAtUtc { get; init; }
}
