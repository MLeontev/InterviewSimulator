using MediatR;
using Users.UseCases.Queries;

namespace Users.ModuleContract.Implementation;

internal class UsersApi(ISender sender) : IUsersApi
{
    public async Task<Guid?> GetUserIdByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default)
    {
        return await sender.Send(new GetUserIdByIdentityIdQuery(identityId), cancellationToken);
    }
}