using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UsersAppDbContext = Users.Infrastructure.Implementation.DataAccess.AppDbContext;

namespace Backend.IntegrationTests.Users;

public sealed class RegisterTests : BaseIntegrationTest
{
    public RegisterTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnIdentity_WhenRequestIsValid()
    {
        var email = TestData.UniqueEmail();
        var password = TestData.DefaultPassword();

        using var client = CreateApiClient();
        using var response = await client.PostAsJsonAsync("/api/v1/users", new RegisterRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();

        payload.Should().NotBeNull();
        payload!.UserId.Should().NotBeEmpty();
        payload.IdentityId.Should().NotBeNullOrWhiteSpace();

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersAppDbContext>();

        var savedUser = await db.Users.SingleAsync(x => x.Email == email);

        savedUser.Id.Should().Be(payload.UserId);
        savedUser.IdentityId.Should().Be(payload.IdentityId);
        savedUser.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_ShouldReturnValidationError_WhenRequestIsInvalid()
    {
        using var client = CreateApiClient();
        using var response = await client.PostAsJsonAsync("/api/v1/users", new RegisterRequest("not-an-email", string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        payload.Should().NotBeNull();
        payload!.Code.Should().Be("VALIDATION_ERROR");
        payload.Errors.Should().ContainKey("Email");
        payload.Errors["Email"].Should().Contain("Некорректный формат email");
        payload.Errors.Should().ContainKey("Password");
        payload.Errors["Password"].Should().Contain("Пароль обязателен");
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailIsAlreadyRegistered()
    {
        var email = TestData.UniqueEmail();
        var password = TestData.DefaultPassword();

        await RegisterUserAsync(email, password);

        using var client = CreateApiClient();
        using var response = await client.PostAsJsonAsync("/api/v1/users", new RegisterRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        payload.Should().NotBeNull();
        payload!.Code.Should().Be("IDENTITY_EMAIL_IS_NOT_UNIQUE");
        payload.Description.Should().Be("Пользователь с указанным email уже зарегистрирован");
    }

    private sealed record RegisterRequest(string Email, string Password);

    private sealed record RegisterResponse(Guid UserId, string IdentityId);

    private sealed record ErrorResponse(string Code, string Description);

    private sealed record ValidationErrorResponse(
        string Code,
        string Description,
        Dictionary<string, string[]> Errors);
}
