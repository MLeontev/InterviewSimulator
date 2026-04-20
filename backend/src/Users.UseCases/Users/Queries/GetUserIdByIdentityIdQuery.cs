using MediatR;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Interfaces.DataAccess;

namespace Users.UseCases.Users.Queries;

public record GetUserIdByIdentityIdQuery(string IdentityId) : IRequest<Guid?>;

internal class GetUserIdByIdentityIdQueryHandler(IDbContext dbContext) : IRequestHandler<GetUserIdByIdentityIdQuery, Guid?>
{
    public async Task<Guid?> Handle(GetUserIdByIdentityIdQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IdentityId == request.IdentityId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}