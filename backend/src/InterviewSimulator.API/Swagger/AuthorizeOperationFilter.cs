using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InterviewSimulator.API.Swagger;

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = endpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        var requiresAuthorization = endpointMetadata.OfType<AuthorizeAttribute>().Any();

        if (allowsAnonymous || !requiresAuthorization)
            return;

        var scheme = new OpenApiSecuritySchemeReference(
            SwaggerExtensions.OAuthSecuritySchemeName,
            context.Document);

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [scheme] = ["openid", "profile", "email"]
            }
        ];
    }
}
