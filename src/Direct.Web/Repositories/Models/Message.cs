namespace Direct.Web.Repositories.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public required string Text { get; set; }
    public DateTime SentAt { get; set; }
    public bool Read { get; set; }
    public DateTime? EditedAt { get; set; }
}
