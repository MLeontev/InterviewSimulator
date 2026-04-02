using System.Net.Http.Json;

namespace Users.Infrastructure.Implementation.Identity.Keycloak;

internal class KeycloakClient(HttpClient httpClient)
{
    public async Task<string> RegisterUserAsync(UserRepresentation user, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("users", user, ct);
        response.EnsureSuccessStatusCode();
        return ExtractIdentityIdFromLocationHeader(response);
    }

    private string ExtractIdentityIdFromLocationHeader(HttpResponseMessage response)
    {
        const string usersSegmentName = "users/";

        var locationHeader = response.Headers.Location?.PathAndQuery;

        if (locationHeader is null)
        {
            throw new InvalidOperationException("Location header is null");
        }

        var userSegmentValueIndex = locationHeader.IndexOf(
            usersSegmentName,
            StringComparison.OrdinalIgnoreCase);

        var identityId = locationHeader.Substring(userSegmentValueIndex + usersSegmentName.Length);

        return identityId;
    }
}