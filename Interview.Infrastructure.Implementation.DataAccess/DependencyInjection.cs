using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Interview.Infrastructure.Implementation.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddInterviewDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((_, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", Schemas.Interview));
        });
        
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        
        return services;
    }
}