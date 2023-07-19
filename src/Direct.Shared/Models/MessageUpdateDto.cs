namespace Direct.Shared.Models;

public class MessageUpdateDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public required string Text { get; set; }
    public DateTime EditedAtUtc { get; set; }
}
