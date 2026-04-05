using Microsoft.Extensions.DependencyInjection;

namespace interview.Infrastructure.Workers;

public static class DependencyInjection
{
    public static IServiceCollection AddInterviewWorkers(this IServiceCollection services)
    {
        services.AddHostedService<AiAnswerEvaluationWorker>();

        services.AddHostedService<AiSessionEvaluationWorker>();

        services.AddHostedService<SessionTimeoutWorker>();

        return services;
    }
}