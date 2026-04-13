using FluentValidation;
using Framework.UseCases.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Users.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersModuleUseCases(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });
        
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
