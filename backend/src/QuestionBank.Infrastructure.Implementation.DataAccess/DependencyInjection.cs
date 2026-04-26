using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestionBankDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((_, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", Schemas.QuestionBank));
        });
        
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        
        return services;
    }

    public static IServiceCollection AddQuestionBankSeeding(this IServiceCollection services)
    {
        services.AddScoped<ISeed, GradeSeed>();
        services.AddScoped<ISeed, SpecializationSeed>();
        services.AddScoped<ISeed, TechnologySeed>();
        services.AddScoped<ISeed, CompetencySeed>();

        services.AddScoped<ISeed, PythonPresetSeed>();
        services.AddScoped<ISeed, CSharpJuniorPresetSeed>();
        services.AddScoped<ISeed, CppJuniorPresetSeed>();

        services.AddScoped<ISeed, PythonTheoryQuestionsSeed>();
        services.AddScoped<ISeed, CSharpTheoryQuestionsSeed>();
        services.AddScoped<ISeed, CppTheoryQuestionsSeed>();
        services.AddScoped<ISeed, CodingQuestionsSeed>();
        services.AddScoped<ISeed, JuniorCodingQuestionsSeed>();

        services.AddScoped<QuestionBankSeedRunner>();
        
        return services;
    }
}
