namespace Direct.Shared.Models;

public record MessageUpdateDto(Guid Id, Guid SenderId, Guid RecipientId, string Text, DateTime EditedAtUtc);
