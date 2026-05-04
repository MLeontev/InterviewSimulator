using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace InterviewSimulator.API.Swagger;

internal static class SwaggerExtensions
{
    internal const string OAuthSecuritySchemeName = "oauth2";

    public static IServiceCollection AddAppSwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SwaggerOptions>(configuration.GetSection(SwaggerOptions.SectionName));

        var swaggerOptions = configuration
            .GetSection(SwaggerOptions.SectionName)
            .Get<SwaggerOptions>() ?? new SwaggerOptions();

        services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Interview Simulator API",
                Version = "v1"
            });

            options.AddSecurityDefinition(OAuthSecuritySchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Authorization Code + PKCE через Keycloak",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(swaggerOptions.OAuth.AuthorizationUrl),
                        TokenUrl = new Uri(swaggerOptions.OAuth.TokenUrl),
                        Scopes = swaggerOptions.OAuth.Scopes
                    }
                }
            });

            IncludeXmlComments(options, "Interview.Presentation.xml", includeControllerXmlComments: true);
            IncludeXmlComments(options, "Interview.UseCases.xml");
            IncludeXmlComments(options, "QuestionBank.Presentation.xml", includeControllerXmlComments: true);
            IncludeXmlComments(options, "QuestionBank.UseCases.xml");
            IncludeXmlComments(options, "Users.Presentation.xml", includeControllerXmlComments: true);
            IncludeXmlComments(options, "Users.UseCases.xml");
            IncludeXmlComments(options, "Framework.Domain.xml");

            options.OperationFilter<AuthorizeOperationFilter>();
        });

        return services;
    }

    private static void IncludeXmlComments(
        Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options,
        string fileName,
        bool includeControllerXmlComments = false)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(filePath))
            options.IncludeXmlComments(filePath, includeControllerXmlComments);
    }

    public static WebApplication UseAppSwagger(this WebApplication app)
    {
        var swaggerOptions = app.Services.GetRequiredService<IOptions<SwaggerOptions>>().Value;

        if (!swaggerOptions.Enabled)
            return app;

        var routePrefix = swaggerOptions.RoutePrefix.Trim('/');

        app.UseSwagger(options =>
        {
            options.RouteTemplate = string.IsNullOrWhiteSpace(routePrefix)
                ? "{documentName}/swagger.json"
                : $"{routePrefix}/{{documentName}}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = routePrefix;
            options.SwaggerEndpoint("v1/swagger.json", "Interview Simulator API v1");
            options.OAuthClientId(swaggerOptions.OAuth.ClientId);
            options.OAuthScopes(swaggerOptions.OAuth.Scopes.Keys.ToArray());
            options.OAuthUsePkce();
        });

        return app;
    }
}
