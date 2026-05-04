using Framework.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Interfaces.DataAccess;

namespace Users.UseCases.Users.Queries;

public record GetCurrentUserQuery(string IdentityId) : IRequest<Result<CurrentUser>>;

internal class GetCurrentUserQueryHandler(IDbContext dbContext) : IRequestHandler<GetCurrentUserQuery, Result<CurrentUser>>
{
    public async Task<Result<CurrentUser>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IdentityId == request.IdentityId)
            .Select(x => new CurrentUser
            {
                Id = x.Id,
                Email = x.Email,
                IdentityId = x.IdentityId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result.Failure<CurrentUser>(
                Error.NotFound("USER_NOT_FOUND", "Пользователь не найден. Сначала зарегистрируйтесь."));

        return Result.Success(user);
    }
}

/// <summary>
/// Данные текущего пользователя
/// </summary>
public record CurrentUser
{
    /// <summary>
    /// Идентификатор пользователя в базе приложения
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Адрес электронной почты пользователя
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Идентификатор пользователя в сервисе аутентификации
    /// </summary>
    public string IdentityId { get; init; } = string.Empty;
}
