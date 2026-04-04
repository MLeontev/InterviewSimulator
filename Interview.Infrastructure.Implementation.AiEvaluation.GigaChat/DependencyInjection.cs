using Interview.Infrastructure.Interfaces.AiEvaluation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat;

public static class DependencyInjection
{
    public static IServiceCollection AddGigaChatAiEvaluation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GigaChatOptions>(configuration.GetSection("GigaChat"));
        services.Configure<GigaChatAuthOptions>(configuration.GetSection("GigaChat:Auth"));
        
        services.AddHttpClient<IGigaChatTokenClient, GigaChatTokenClient>();
        
        services.AddHttpClient<IAiEvaluationService, GigaChatAiEvaluationService>((_, client) =>
        {
            var options = configuration.GetSection("GigaChat").Get<GigaChatOptions>() 
                          ?? new GigaChatOptions();
            
            client.BaseAddress = new Uri(options.BaseUrl);
        });
        
        return services;
    }
}
