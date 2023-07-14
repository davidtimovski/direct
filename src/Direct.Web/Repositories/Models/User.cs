namespace Direct.Web.Repositories.Models;

public class User
{
    public Guid Id { get; set; }
    public required string PasswordHash { get; set; }
    public bool HasProfileImage { get; set; }
    public DateTime CreatedDate { get; set; }
}
