using Microsoft.Extensions.DependencyInjection;

namespace CodeExecution.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeExecutionModuleUseCases(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}