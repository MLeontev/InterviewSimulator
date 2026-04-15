using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Users.ModuleContract;

namespace Backend.IntegrationTests.Users;

public sealed class UsersApiTests : BaseIntegrationTest
{
    public UsersApiTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUserIdByIdentityIdAsync_ShouldReturnUserId_WhenIdentityExists()
    {
        var email = TestData.UniqueEmail();
        var password = TestData.DefaultPassword();
        var registeredUser = await RegisterUserAsync(email, password);

        using var scope = CreateScope();
        var usersApi = scope.ServiceProvider.GetRequiredService<IUsersApi>();

        var userId = await usersApi.GetUserIdByIdentityIdAsync(registeredUser.IdentityId);

        userId.Should().Be(registeredUser.UserId);
    }

    [Fact]
    public async Task GetUserIdByIdentityIdAsync_ShouldReturnNull_WhenIdentityDoesNotExist()
    {
        using var scope = CreateScope();
        var usersApi = scope.ServiceProvider.GetRequiredService<IUsersApi>();

        var userId = await usersApi.GetUserIdByIdentityIdAsync($"missing-{Guid.NewGuid():N}");

        userId.Should().BeNull();
    }
}
