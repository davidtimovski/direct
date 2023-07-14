namespace Direct.Shared.Models;

public class ContactDto
{
    public required Guid Id { get; set; }
    public required string Nickname { get; set; }
    public required string ImageUri { get; set; }
    public required MessageDto[] Messages { get; set; }
}
