namespace Direct.Shared.Models;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public string Text { get; set; } = null!;
    public bool UserIsSender { get; set; }
    public DateTime SentAtUtc { get; set; }
    public DateTime? EditedAtUtc { get; set; }
}
