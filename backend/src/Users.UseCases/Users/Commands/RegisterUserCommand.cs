using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Framework.Domain;
using MediatR;
using Users.Infrastructure.Interfaces.DataAccess;
using Users.Infrastructure.Interfaces.Identity;

namespace Users.UseCases.Users.Commands;

/// <summary>
/// Запрос на регистрацию пользователя
/// </summary>
public record RegisterUserCommand : IRequest<Result<RegisterUserDto>>
{
    /// <summary>
    /// Адрес электронной почты пользователя
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public string Password { get; init; } = string.Empty;
}

internal class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Пароль обязателен");
    }
}

internal class RegisterUserCommandHandler(
    IDbContext dbContext, 
    IIdentityProviderService identityProviderService) : IRequestHandler<RegisterUserCommand, Result<RegisterUserDto>>
{
    public async Task<Result<RegisterUserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var registrationResult = await identityProviderService.RegisterUserAsync(
            new UserModel(request.Email, request.Password),
            cancellationToken);

        if (registrationResult.IsFailure)
            return Result.Failure<RegisterUserDto>(registrationResult.Error);

        var user = new Domain.User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            IdentityId = registrationResult.Value
        };
        
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return new RegisterUserDto
        {
            UserId = user.Id,
            IdentityId = user.IdentityId
        };
    }
}

/// <summary>
/// Результат регистрации пользователя
/// </summary>
public record RegisterUserDto
{
    /// <summary>
    /// Идентификатор пользователя в базе приложения
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Идентификатор пользователя в сервисе аутентификации
    /// </summary>
    public string IdentityId { get; init; } = string.Empty;
}
