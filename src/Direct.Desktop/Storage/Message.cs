using System;

namespace Direct.Desktop.Storage;

public readonly record struct Message(Guid Id, Guid SenderId, Guid RecipientId, string Text, string? Reaction, DateTime SentAt, DateTime? EditedAt);
