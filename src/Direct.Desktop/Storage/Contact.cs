using System;

namespace Direct.Desktop.Storage;

public readonly record struct Contact(Guid Id, string Nickname, DateTime AddedOn);
