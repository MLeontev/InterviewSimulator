namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

public record LanguageInfo(
    string Code,
    string Name,
    string Version,
    bool IsActive,
    bool IsCompiled,
    string DockerImage,
    string RunCommandTemplate,
    string CompileCommandTemplate,
    string DefaultFileName,
    int DefaultTimeoutSeconds,
    int MaxMemoryMb,
    int MaxCpuCores);