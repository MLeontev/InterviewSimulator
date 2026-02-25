using Microsoft.Extensions.DependencyInjection;

namespace CodeExecution.Infrastructure.Workers;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeExecutionWorkers(this IServiceCollection services)
    {
        services.AddHostedService<CodeExecutionWorker>();
        services.AddHostedService<CodeExecutionWorker>();
        services.AddHostedService<CodeExecutionWorker>();
        services.AddHostedService<CodeExecutionWorker>();

        services.AddHostedService<CodeSubmissionEventPublisher>();

        return services;
    }
}