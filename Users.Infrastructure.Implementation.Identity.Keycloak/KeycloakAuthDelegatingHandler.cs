using System.Net.Http.Headers;

namespace Users.Infrastructure.Implementation.Identity.Keycloak;

internal class KeycloakAuthDelegatingHandler(IKeycloakTokenClient tokenClient) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenClient.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}