using System;

namespace Direct.Desktop.Storage.Entities;

public readonly record struct Message(Guid Id, Guid ContactId, bool IsRecipient, string Text, string? Reaction, DateTime SentAt, DateTime? EditedAt);
