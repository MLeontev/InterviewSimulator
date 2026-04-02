using System.Net;
using Framework.Domain;
using Users.Infrastructure.Interfaces.Identity;

namespace Users.Infrastructure.Implementation.Identity.Keycloak;

internal class IdentityProviderService(KeycloakClient keycloakClient) : IIdentityProviderService
{
    public async Task<Result<string>> RegisterUserAsync(UserModel user, CancellationToken cancellationToken = default)
    {
        var userRepresentation = new UserRepresentation(
            user.Email,
            user.Email,
            true,
            true,
            [new CredentialRepresentation("Password", user.Password, false)]);

        try
        {
            return await keycloakClient.RegisterUserAsync(userRepresentation, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            return Result.Failure<string>(Error.Conflict("IDENTITY_EMAIL_IS_NOT_UNIQUE", "Пользователь с указанным email уже зарегистрирован"));
        }
    }
}