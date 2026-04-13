namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal record ExecuteCodeRequest
{
    public string Code { get; init; } = string.Empty;
    public string Input { get; init; } = string.Empty;
    public bool IsCompiled { get; init; }
    public string DockerImage { get; init; } = string.Empty;
    public string RunCommand { get; init; } = string.Empty;
    public string CompileCommand { get; init; } = string.Empty;
    public string DefaultFileName { get; init; } = string.Empty;
    public int DefaultTimeoutSeconds { get; init; }
    public int MaxMemoryMb { get; init; }
    public int MaxCpuCores { get; init; }
}