using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Outbox;

public static class OutboxExtensions
{
    public static IServiceCollection AddOutboxProcessor<TContext>(
        this IServiceCollection services,
        string configSectionPath)
        where TContext : ModuleDbContext
    {
        services.AddOptions<OutboxOptions>(configSectionPath).BindConfiguration(configSectionPath);
        
        services.AddSingleton<IHostedService>(sp => new OutboxProcessor<TContext>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptionsMonitor<OutboxOptions>>(),
            sp.GetRequiredService<ILogger<OutboxProcessor<TContext>>>(),
            configSectionPath));
        
        return services;
    }
}