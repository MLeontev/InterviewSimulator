using Microsoft.Extensions.DependencyInjection;

namespace Interview.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddInterviewModuleUseCases(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
