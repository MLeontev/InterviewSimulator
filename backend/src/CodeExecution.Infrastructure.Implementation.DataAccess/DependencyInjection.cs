using CodeExecution.Infrastructure.Interfaces.DataAccess;
using Framework.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeExecution.Infrastructure.Implementation.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeExecutionDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((_, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", Schemas.CodeExecution));
        });
        
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        
        services.AddOutboxProcessor<AppDbContext>("CodeExecution:Outbox");
        
        return services;
    }
}