using Microsoft.Extensions.DependencyInjection;
using QuestionBank.InternalApi;

namespace QuestionBank.ModuleContract.Implementation;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestionBankModuleApi(this IServiceCollection services)
    {
        services.AddScoped<IQuestionBankApi, QuestionBankApi>();
        return services;
    }
}
