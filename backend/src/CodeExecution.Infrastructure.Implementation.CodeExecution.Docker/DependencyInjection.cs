using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeExecutionDocker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new RuntimeConfig(configuration));
        services.AddSingleton<LanguageProvider>();
        services.AddSingleton<IDockerRunner, DockerRunner>();
        
        services.AddSingleton<IExecutorLanguageProvider>(sp => sp.GetRequiredService<LanguageProvider>());

        services.AddScoped<ICodeExecutor, CodeExecutor>();

        return services;
    }
}
