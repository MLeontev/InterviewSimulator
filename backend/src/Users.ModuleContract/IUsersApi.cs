namespace Users.ModuleContract;

public interface IUsersApi
{
    Task<Guid?> GetUserIdByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default);
}