using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Users.Infrastructure.Interfaces.Identity;

namespace Users.Infrastructure.Implementation.Identity.Keycloak;

public static class DependencyInjection
{
    public static IServiceCollection AddKeycloakIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IIdentityProviderService, IdentityProviderService>();
        
        services.Configure<KeycloakOptions>(configuration.GetSection("Keycloak"));
        
        services.AddHttpClient<IKeycloakTokenClient, KeycloakTokenClient>();
        
        services.AddTransient<KeycloakAuthDelegatingHandler>();
        
        services
            .AddHttpClient<KeycloakClient>((serviceProvider, httpClient) =>
            {
                var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                httpClient.BaseAddress = new Uri(keycloakOptions.AdminUrl);
            })
            .AddHttpMessageHandler<KeycloakAuthDelegatingHandler>();
        
        return services;
    }
}