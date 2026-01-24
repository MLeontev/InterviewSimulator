using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Infrastructure.Interfaces.DataAccess;

namespace Users.Infrastructure.Implementation.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((_, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", Schemas.Users));
        });
        
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        
        return services;
    }
}