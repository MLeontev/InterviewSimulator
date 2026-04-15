namespace Backend.IntegrationTests.Infrastructure;

public sealed class AuthorizedUserContext : IDisposable
{
    public AuthorizedUserContext(
        HttpClient client,
        Guid userId,
        string identityId,
        string email,
        string password,
        string accessToken)
    {
        Client = client;
        UserId = userId;
        IdentityId = identityId;
        Email = email;
        Password = password;
        AccessToken = accessToken;
    }

    public HttpClient Client { get; }

    public Guid UserId { get; }

    public string IdentityId { get; }

    public string Email { get; }

    public string Password { get; }

    public string AccessToken { get; }

    public void Dispose()
    {
        Client.Dispose();
    }
}
