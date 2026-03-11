using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeExecutionDocker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new RuntimeConfig(configuration));
        services.AddSingleton<LanguageProvider>();
        
        services.AddSingleton<ILanguageProvider>(sp => sp.GetRequiredService<LanguageProvider>());
        services.AddSingleton<IExecutorLanguageProvider>(sp => sp.GetRequiredService<LanguageProvider>());

        services.AddScoped<IExecutionRequestStrategy, CompiledExecutionRequestStrategy>();
        services.AddScoped<IExecutionRequestStrategy, InterpretedExecutionRequestStrategy>();

        services.AddScoped<ICodeExecutor, CodeExecutor>();

        return services;
    }
}
