using CodeExecution.Infrastructure.Implementation.CodeExecution;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests.CodeExecution;

[Trait("Category", "Docker")]
public sealed class DockerCodeExecutorTests : IDisposable
{
    private const string TempRootEnvVar = "CODE_EXECUTION_TEMP_ROOT";
    private const string HostTempRootEnvVar = "CODE_EXECUTION_HOST_TEMP_ROOT";
    private readonly string _tempRoot;

    public DockerCodeExecutorTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "interview-simulator-docker-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
        Environment.SetEnvironmentVariable(TempRootEnvVar, _tempRoot);
        Environment.SetEnvironmentVariable(HostTempRootEnvVar, _tempRoot);
    }

    [Fact]
    public async Task ExecuteCode_ShouldRunPythonCodeInDocker()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var executor = scope.ServiceProvider.GetRequiredService<ICodeExecutor>();

        var result = await executor.ExecuteCode(
            """
            name = input().strip()
            print(f"Hello, {name}!")
            """,
            "Test",
            "python");

        result.Stage.Should().Be(ExecutionStage.Runtime);
        result.ExitCode.Should().Be(0);
        result.Output.Trim().Should().Be("Hello, Test!");
        result.Error.Should().BeEmpty();
        result.TimeElapsedMs.Should().BePositive();
        result.MemoryUsageMb.Should().BePositive();
    }

    [Fact]
    public async Task ExecuteCode_ShouldReturnRuntimeError_WhenPythonCodeRaisesException()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var executor = scope.ServiceProvider.GetRequiredService<ICodeExecutor>();

        var result = await executor.ExecuteCode(
            """
            raise ValueError("boom")
            """,
            string.Empty,
            "python");

        result.Stage.Should().Be(ExecutionStage.Runtime);
        result.ExitCode.Should().NotBe(0);
        result.Output.Should().BeEmpty();
        result.Error.Should().Contain("ValueError");
        result.Error.Should().Contain("boom");
    }

    [Fact]
    public async Task ExecuteCode_ShouldReturnTimeout_WhenPythonCodeExceedsTimeLimit()
    {
        await using var services = CreateServices(pythonTimeoutSeconds: 1);
        await using var scope = services.CreateAsyncScope();
        var executor = scope.ServiceProvider.GetRequiredService<ICodeExecutor>();

        var result = await executor.ExecuteCode(
            """
            import time
            time.sleep(2)
            """,
            string.Empty,
            "python");

        result.Stage.Should().Be(ExecutionStage.Runtime);
        result.ExitCode.Should().Be(-1);
        result.Output.Should().BeEmpty();
        result.Error.Should().Contain("timed out");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(TempRootEnvVar, null);
        Environment.SetEnvironmentVariable(HostTempRootEnvVar, null);

        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private static ServiceProvider CreateServices(int? pythonTimeoutSeconds = null)
    {
        var configuration = BuildConfiguration(pythonTimeoutSeconds);
        var services = new ServiceCollection();
        services.AddCodeExecutionDocker(configuration);
        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration(int? pythonTimeoutSeconds)
    {
        var runtimesPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "InterviewSimulator.API",
            "runtimes.json"));

        var overrides = pythonTimeoutSeconds is null
            ? null
            : new Dictionary<string, string?>
            {
                ["Runtimes:1:DefaultTimeoutSeconds"] = pythonTimeoutSeconds.Value.ToString()
            };

        return new ConfigurationBuilder()
            .AddJsonFile(runtimesPath, optional: false)
            .AddInMemoryCollection(overrides)
            .Build();
    }
}
