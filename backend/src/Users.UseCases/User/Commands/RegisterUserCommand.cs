using FluentValidation;
using Framework.Domain;
using MediatR;
using Users.Infrastructure.Interfaces.DataAccess;
using Users.Infrastructure.Interfaces.Identity;

namespace Users.UseCases.User.Commands;

public record RegisterUserCommand(string Email, string Password) : IRequest<Result<RegisterUserDto>>;

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
        
        return new RegisterUserDto(user.Id, user.IdentityId);
    }
}

public record RegisterUserDto(Guid UserId, string IdentityId);