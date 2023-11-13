using System;

namespace Direct.Desktop.Storage.Entities;

public record Contact(Guid Id, string Nickname, string? ProfileImage, DateTime AddedOn);
