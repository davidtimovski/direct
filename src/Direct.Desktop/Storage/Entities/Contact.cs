using System;

namespace Direct.Desktop.Storage.Entities;

public readonly record struct Contact(Guid Id, string Nickname, DateTime AddedOn);
