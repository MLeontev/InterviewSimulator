using Backend.IntegrationTests.Infrastructure.Fakes;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;

namespace Backend.IntegrationTests.Infrastructure;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string RealmName = "InterviewSimulator";
    private const string PublicClientId = "interview-public-client";
    private const string ConfidentialClientId = "interview-confidential-client";
    private const string ConfidentialClientSecret = "2yOPGKPFBIFqtavNPv2l16AkelMzWkht";
    private const string KeycloakAdminUsername = "admin";
    private const string KeycloakAdminPassword = "admin";

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase($"interview_simulator_tests_{Guid.NewGuid():N}")
        .Build();

    private readonly KeycloakContainer _keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:26.6")
        .WithUsername(KeycloakAdminUsername)
        .WithPassword(KeycloakAdminPassword)
        .WithRealm(GetRealmExportPath())
        .Build();

    private string KeycloakBaseUrl => _keycloakContainer.GetBaseAddress().TrimEnd('/');
    private string RealmUrl => $"{KeycloakBaseUrl}/realms/{RealmName}";
    private string KeycloakAdminUrl => $"{KeycloakBaseUrl}/admin/realms/{RealmName}/";
    private string KeycloakTokenUrl => $"{RealmUrl}/protocol/openid-connect/token";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _keycloakContainer.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();
        await _keycloakContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        Environment.SetEnvironmentVariable(
            "Authentication:MetadataAddress", 
            $"{RealmUrl}/.well-known/openid-configuration");
        
        Environment.SetEnvironmentVariable(
            "Authentication:TokenValidationParameters:ValidIssuer", 
            RealmUrl);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["AiEvaluation:UseFake"] = "true",
                ["Interview:AiRetry:MaxRetries"] = "3",
                ["Interview:AiRetry:BaseDelaySeconds"] = "1",
                ["Interview:AiRetry:MaxDelaySeconds"] = "5",
                ["Interview:AiRetry:JitterSeconds"] = "0",
                ["Interview:SessionQuestionSet:TheoryCount"] = "3",
                ["Interview:SessionQuestionSet:CodingCount"] = "1",
                ["Interview:Outbox:BatchSize"] = "0",
                ["Interview:Outbox:PollingIntervalSeconds"] = "3600",
                ["CodeExecution:Outbox:BatchSize"] = "0",
                ["CodeExecution:Outbox:PollingIntervalSeconds"] = "3600",
                ["Keycloak:AdminUrl"] = KeycloakAdminUrl,
                ["Keycloak:TokenUrl"] = KeycloakTokenUrl,
                ["Keycloak:ConfidentialClientId"] = ConfidentialClientId,
                ["Keycloak:ConfidentialClientSecret"] = ConfidentialClientSecret,
                ["Keycloak:PublicClientId"] = PublicClientId
            });
        });

        builder.ConfigureServices(services =>
        {
            RemoveHostedServiceByImplementationName(services, "AiAnswerEvaluationWorker");
            RemoveHostedServiceByImplementationName(services, "AiSessionEvaluationWorker");
            RemoveHostedServiceByImplementationName(services, "SessionTimeoutWorker");
            RemoveHostedServiceByImplementationName(services, "CodeExecutionWorker");

            services.RemoveAll<ICodeExecutor>();
            services.AddSingleton<FakeCodeExecutor>();
            services.AddScoped<ICodeExecutor>(sp => sp.GetRequiredService<FakeCodeExecutor>());

            services.RemoveAll<IAiEvaluationService>();
            services.AddSingleton<FakeAiEvaluationService>();
            services.AddScoped<IAiEvaluationService>(sp => sp.GetRequiredService<FakeAiEvaluationService>());
        });
    }

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task<RegisterUserResponse> RegisterUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateApiClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/users/register",
            new RegisterUserRequest(email, password),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RegisterUserResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Register user response is null.");

        return payload;
    }

    public async Task<string> GetAccessTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        using var response = await client.PostAsync(
            KeycloakTokenUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = PublicClientId,
                ["scope"] = "openid profile email",
                ["username"] = email,
                ["password"] = password
            }),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Access token response is null.");

        return payload.AccessToken;
    }

    public async Task<AuthorizedUserContext> CreateAuthorizedCandidateAsync(
        CancellationToken cancellationToken = default)
    {
        var email = TestData.UniqueEmail();
        var password = TestData.DefaultPassword();

        return await CreateAuthorizedCandidateAsync(email, password, cancellationToken);
    }

    public async Task<AuthorizedUserContext> CreateAuthorizedCandidateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var registeredUser = await RegisterUserAsync(email, password, cancellationToken);
        var accessToken = await GetAccessTokenAsync(email, password, cancellationToken);

        var client = CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return new AuthorizedUserContext(
            client,
            registeredUser.UserId,
            registeredUser.IdentityId,
            email,
            password,
            accessToken);
    }

    public FakeCodeExecutor GetFakeCodeExecutor()
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FakeCodeExecutor>();
    }

    public FakeAiEvaluationService GetFakeAiEvaluationService()
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FakeAiEvaluationService>();
    }

    private static void RemoveHostedServiceByImplementationName(
        IServiceCollection services,
        string implementationName)
    {
        var descriptors = services
            .Where(x =>
                x.ServiceType == typeof(IHostedService) &&
                x.ImplementationType?.Name == implementationName)
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }

    private static string GetRealmExportPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Infrastructure", "realm-export.json");
    }

    private sealed record RegisterUserRequest(string Email, string Password);

    private sealed class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
    }

    public sealed record RegisterUserResponse(Guid UserId, string IdentityId);
}
