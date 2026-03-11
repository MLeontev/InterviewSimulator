using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuestionBank.InternalApi;

namespace QuestionBank.ModuleContract.Implementation;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestionBankModuleApi(this IServiceCollection services)
    {
        services.AddScoped<IQuestionBankApi, QuestionBankApi>();
        
        services.AddScoped<IQuestionBankApi>(sp =>
        {
            var loggerDecorator = sp.GetRequiredService<LoggingQuestionBankApiDecorator>();
            IQuestionBankApi current = loggerDecorator;

            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            current = new CachedQuestionBankApiProxy(current, cache);

            return current;
        });
        
        return services;
    }
}
