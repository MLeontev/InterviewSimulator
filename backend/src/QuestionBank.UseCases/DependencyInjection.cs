using Microsoft.Extensions.DependencyInjection;

namespace QuestionBank.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestionBankModuleUseCases(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}