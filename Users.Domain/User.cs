namespace Users.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IdentityId { get; set; } = string.Empty;
}