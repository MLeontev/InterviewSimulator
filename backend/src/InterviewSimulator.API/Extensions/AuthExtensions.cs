namespace InterviewSimulator.API.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization();

        services.AddAuthentication().AddJwtBearer(options =>
        {
            configuration.GetSection("Authentication").Bind(options);
        });

        services.AddHttpContextAccessor();
        
        return services;
    }
}