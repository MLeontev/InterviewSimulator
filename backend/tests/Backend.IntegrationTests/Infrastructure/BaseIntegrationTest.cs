using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IDisposable
{
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        _scope = factory.Services.CreateScope();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
    }

    protected IntegrationTestWebAppFactory Factory { get; }

    protected ISender Sender { get; }

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    protected T GetRequiredService<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    protected HttpClient CreateApiClient()
    {
        return Factory.CreateApiClient();
    }

    protected Task<IntegrationTestWebAppFactory.RegisterUserResponse> RegisterUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return Factory.RegisterUserAsync(email, password, cancellationToken);
    }

    protected Task<string> GetAccessTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return Factory.GetAccessTokenAsync(email, password, cancellationToken);
    }

    protected Task<AuthorizedUserContext> CreateAuthorizedCandidateAsync(
        CancellationToken cancellationToken = default)
    {
        return Factory.CreateAuthorizedCandidateAsync(cancellationToken);
    }

    protected Task<AuthorizedUserContext> CreateAuthorizedCandidateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return Factory.CreateAuthorizedCandidateAsync(email, password, cancellationToken);
    }

    public void Dispose()
    {
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
}
