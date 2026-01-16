using Microsoft.EntityFrameworkCore;

namespace InterviewSimulator.API.Extensions;

internal static class MigrationExtensions
{
    internal static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        
        ApplyMigrations<CodeExecution.Infrastructure.Implementation.DataAccess.AppDbContext>(scope);
    }
    
    private static void ApplyMigrations<TDbContext>(IServiceScope scope) where TDbContext : DbContext
    {
        using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        dbContext.Database.Migrate();
    }
}