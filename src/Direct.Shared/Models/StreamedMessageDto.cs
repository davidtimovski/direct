namespace Direct.Shared.Models;

public record StreamedMessageDto(Guid Id, bool IsRecipient, string Text, string? Reaction, DateTime SentAtUtc, DateTime? EditedAtUtc);
