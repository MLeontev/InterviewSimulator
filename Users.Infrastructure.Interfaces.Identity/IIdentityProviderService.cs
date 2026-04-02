using Framework.Domain;

namespace Users.Infrastructure.Interfaces.Identity;

public interface IIdentityProviderService
{
    Task<Result<string>> RegisterUserAsync(UserModel user, CancellationToken cancellationToken = default);
}

public record UserModel(string Email, string Password);