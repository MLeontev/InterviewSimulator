using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Backend.IntegrationTests.Users;

public sealed class GetMeTests : BaseIntegrationTest
{
    public GetMeTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMe_ShouldReturnCurrentUserProfile_WhenUserIsAuthorized()
    {
        using var user = await CreateAuthorizedCandidateAsync();

        using var response = await user.Client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GetMeResponse>();

        payload.Should().NotBeNull();
        payload.Id.Should().Be(user.UserId);
        payload.Email.Should().Be(user.Email);
        payload.IdentityId.Should().Be(user.IdentityId);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        using var client = CreateApiClient();

        using var response = await client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record GetMeResponse(
        Guid Id,
        string Email,
        string IdentityId);
}
