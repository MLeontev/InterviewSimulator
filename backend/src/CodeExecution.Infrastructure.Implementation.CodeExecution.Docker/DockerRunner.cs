using Docker.DotNet;
using Docker.DotNet.Models;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal interface IDockerRunner
{
    Task<DockerRunResult> RunAsync(
        string image,
        string hostWorkDir,
        string command,
        int timeoutSeconds,
        int maxMemoryMb,
        int maxCpuCores,
        CancellationToken cancellationToken = default);
}

internal record DockerRunResult(int ExitCode, bool TimedOut, string ErrorMessage);

internal sealed class DockerRunner : IDockerRunner, IDisposable
{
    private const string DockerPlatformEnvVar = "CODE_EXECUTION_DOCKER_PLATFORM";
    private readonly DockerClient _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

    public async Task<DockerRunResult> RunAsync(
        string image,
        string hostWorkDir,
        string command,
        int timeoutSeconds,
        int maxMemoryMb,
        int maxCpuCores,
        CancellationToken cancellationToken = default)
    {
        string? containerId = null;

        try
        {
            await EnsureImageAsync(image, cancellationToken);

            var cpuCoresLimit = Math.Clamp(maxCpuCores, 1, Environment.ProcessorCount);

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Cmd = ["sh", "-c", command],
                WorkingDir = "/code",
                HostConfig = new HostConfig
                {
                    AutoRemove = false,
                    NetworkMode = "none",
                    PidsLimit = 64,
                    Memory = maxMemoryMb * 1024L * 1024L,
                    NanoCPUs = cpuCoresLimit * 1_000_000_000L,
                    Binds = [$"{hostWorkDir}:/code"]
                }
            };

            var createResponse = await _dockerClient.Containers.CreateContainerAsync(createParams, cancellationToken);
            containerId = createResponse.ID;

            var started = await _dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken);

            if (!started)
            {
                return new DockerRunResult(
                    ExitCode: -2,
                    TimedOut: false,
                    ErrorMessage: "Failed to start container.");
            }

            var waitTask = _dockerClient.Containers.WaitContainerAsync(containerId, cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

            if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask)
            {
                await TryKillContainerAsync(containerId);
                return new DockerRunResult(
                    ExitCode: -1,
                    TimedOut: true,
                    ErrorMessage: "Error: Execution timed out.");
            }

            var waitResponse = await waitTask;

            return new DockerRunResult(
                ExitCode: (int)waitResponse.StatusCode,
                TimedOut: false,
                ErrorMessage: string.Empty);
        }
        catch (Exception ex)
        {
            return new DockerRunResult(
                ExitCode: -2,
                TimedOut: false,
                ErrorMessage: ex.Message);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(containerId))
                await TryRemoveContainerAsync(containerId);
        }
    }

    public void Dispose()
    {
        _dockerClient.Dispose();
    }

    private async Task EnsureImageAsync(string image, CancellationToken cancellationToken)
    {
        try
        {
            await _dockerClient.Images.InspectImageAsync(image, cancellationToken);
        }
        catch (DockerImageNotFoundException)
        {
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                new AuthConfig(),
                new Progress<JSONMessage>(),
                cancellationToken);
        }
    }

    private async Task TryKillContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.KillContainerAsync(
                containerId,
                new ContainerKillParameters(),
                CancellationToken.None);
        }
        catch
        {
            // если контейнер уже удален/остановлен, игнорируем
        }
    }

    private async Task TryRemoveContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true },
                CancellationToken.None);
        }
        catch
        {
            // если контейнер уже удален/остановлен, игнорируем
        }
    }
}
