using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Framework.UseCases.Behaviors;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddInterviewModuleUseCases(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        
        services.AddScoped<ICurrentSessionResolver, CurrentSessionResolver>();
        services.AddScoped<ICurrentQuestionResolver, CurrentQuestionResolver>();
        
        services.AddScoped<IInterviewSessionFinalizer, InterviewSessionFinalizer>();
        
        return services;
    }
}
