using Microsoft.Extensions.DependencyInjection;

namespace Users.ModuleContract.Implementation;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersModuleApi(this IServiceCollection services)
    {
        services.AddScoped<IUsersApi, UsersApi>();
        return services;
    }
}