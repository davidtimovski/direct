namespace Direct.Shared.Models;

public record NewMessageDto(Guid Id, Guid SenderId, Guid RecipientId, string Text, DateTime SentAtUtc);
