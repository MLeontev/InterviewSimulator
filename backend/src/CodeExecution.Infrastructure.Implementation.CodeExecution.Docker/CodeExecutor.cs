using System.Globalization;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class CodeExecutor(IExecutorLanguageProvider languageProvider, IDockerRunner dockerRunner) : ICodeExecutor
{
    private const string TempRootEnvVar = "CODE_EXECUTION_TEMP_ROOT";
    private const string HostTempRootEnvVar = "CODE_EXECUTION_HOST_TEMP_ROOT";

    private const string StdOutFile = "stdout.txt";
    private const string StdErrFile = "stderr.txt";
    private const string CompileStdOutFile = "compile_stdout.txt";
    private const string CompileStdErrFile = "compile_stderr.txt";
    private const string TimeOutputFile = "time_output.txt";
    private const string InputFile = "input.txt";

    public async Task<CodeExecutionResult> ExecuteCode(
        string code,
        string input,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (!languageProvider.TryGetLanguage(language, out var langConfig) || langConfig is null)
            throw new KeyNotFoundException($"Unsupported language: {language}");

        string runCommandRaw;
        string compileCommand = string.Empty;

        if (langConfig.IsCompiled)
        {
            var compileCommandRaw = langConfig.CompileCommandTemplate
                .Replace("{input}", "/code/" + langConfig.DefaultFileName)
                .Replace("{output}", "/code/program.out");

            compileCommand =
                $"{compileCommandRaw} > /code/{CompileStdOutFile} 2> /code/{CompileStdErrFile}";

            runCommandRaw = "/code/program.out";
        }
        else
        {
            runCommandRaw = langConfig.RunCommandTemplate.Replace("{file_path}", "/code/" + langConfig.DefaultFileName);
        }

        var timedRunCommand =
            $"/usr/bin/time -f 'TIME_ELAPSED:%e\\nMEMORY_USAGE:%M' -o /code/{TimeOutputFile} {runCommandRaw}";

        var runCommand =
            $"{timedRunCommand} < /code/{InputFile} > /code/{StdOutFile} 2> /code/{StdErrFile}";

        var request = new ExecuteCodeRequest
        {
            Code = code,
            Input = input,
            IsCompiled = langConfig.IsCompiled,
            DockerImage = langConfig.DockerImage,
            RunCommand = runCommand,
            CompileCommand = compileCommand,
            DefaultFileName = langConfig.DefaultFileName,
            DefaultTimeoutSeconds = langConfig.DefaultTimeoutSeconds,
            MaxMemoryMb = langConfig.MaxMemoryMb,
            MaxCpuCores = langConfig.MaxCpuCores
        };

        return await ExecuteCodeInternal(request, cancellationToken);
    }

    private async Task<CodeExecutionResult> ExecuteCodeInternal(ExecuteCodeRequest request, CancellationToken cancellationToken)
    {
        var (containerTempDir, hostTempDir) = await PrepareTempFiles(request, cancellationToken);

        try
        {
            if (request.IsCompiled)
            {
                var compileRunResult = await dockerRunner.RunAsync(
                    request.DockerImage,
                    hostTempDir,
                    request.CompileCommand,
                    request.DefaultTimeoutSeconds,
                    request.MaxMemoryMb,
                    request.MaxCpuCores,
                    cancellationToken);

                var compileStdOut = await ReadTextIfExists(Path.Combine(containerTempDir, CompileStdOutFile), cancellationToken);
                var compileStdErr = await ReadTextIfExists(Path.Combine(containerTempDir, CompileStdErrFile), cancellationToken);
                compileStdErr = AppendError(compileStdErr, compileRunResult.ErrorMessage);

                if (compileRunResult.ExitCode != 0 || compileRunResult.TimedOut)
                {
                    return new CodeExecutionResult
                    {
                        Output = compileStdOut,
                        Error = compileStdErr,
                        ExitCode = compileRunResult.ExitCode,
                        TimeElapsedMs = 0,
                        MemoryUsageMb = 0,
                        Stage = ExecutionStage.Compilation
                    };
                }
            }

            var runResult = await dockerRunner.RunAsync(
                request.DockerImage,
                hostTempDir,
                request.RunCommand,
                request.DefaultTimeoutSeconds,
                request.MaxMemoryMb,
                request.MaxCpuCores,
                cancellationToken);

            var stdOut = await ReadTextIfExists(Path.Combine(containerTempDir, StdOutFile), cancellationToken);
            var stdErr = await ReadTextIfExists(Path.Combine(containerTempDir, StdErrFile), cancellationToken);
            stdErr = AppendError(stdErr, runResult.ErrorMessage);

            var (timeElapsed, memoryUsageMb) = await ParseTimeOutput(containerTempDir);

            return new CodeExecutionResult
            {
                Output = stdOut,
                Error = stdErr,
                ExitCode = runResult.ExitCode,
                TimeElapsedMs = timeElapsed,
                MemoryUsageMb = memoryUsageMb,
                Stage = ExecutionStage.Runtime
            };
        }
        catch (Exception ex)
        {
            return new CodeExecutionResult
            {
                Output = string.Empty,
                Error = "Error during code execution: " + ex.Message,
                ExitCode = -1,
                TimeElapsedMs = 0,
                MemoryUsageMb = 0,
                Stage = ExecutionStage.None
            };
        }
        finally
        {
            if (Directory.Exists(containerTempDir))
                Directory.Delete(containerTempDir, true);
        }
    }

    private async Task<(string containerTempDir, string hostTempDir)> PrepareTempFiles(
        ExecuteCodeRequest request,
        CancellationToken cancellationToken)
    {
        var containerTempRoot = ResolveTempRoot();
        var hostTempRoot = ResolveHostTempRoot();
        Directory.CreateDirectory(containerTempRoot);

        var tempFolderName = Guid.NewGuid().ToString("N");
        var containerTempDir = Path.Combine(containerTempRoot, tempFolderName);
        var hostTempDir = Path.Combine(hostTempRoot, tempFolderName);
        Directory.CreateDirectory(containerTempDir);

        var sourceFile = Path.Combine(containerTempDir, request.DefaultFileName);
        await File.WriteAllTextAsync(sourceFile, request.Code, cancellationToken);

        var inputFile = Path.Combine(containerTempDir, InputFile);
        await File.WriteAllTextAsync(inputFile, request.Input, cancellationToken);

        return (containerTempDir, hostTempDir);
    }

    private async Task<(double timeElapsedMs, double memoryUsageMb)> ParseTimeOutput(string tempDir)
    {
        var timeOutputPath = Path.Combine(tempDir, TimeOutputFile);
        if (!File.Exists(timeOutputPath))
            return (0, 0);

        double memoryUsageMb = 0;
        double timeElapsedMs = 0;

        var lines = await File.ReadAllLinesAsync(timeOutputPath);
        foreach (var line in lines)
        {
            if (line.StartsWith("TIME_ELAPSED:") &&
                double.TryParse(line.Substring("TIME_ELAPSED:".Length), NumberStyles.Any, CultureInfo.InvariantCulture, out var t))
            {
                timeElapsedMs = t * 1000;
            }
            else if (line.StartsWith("MEMORY_USAGE:") &&
                     long.TryParse(line.Substring("MEMORY_USAGE:".Length), out var m))
            {
                memoryUsageMb = m / 1024.0;
            }
        }

        return (timeElapsedMs, memoryUsageMb);
    }

    private static string ResolveTempRoot()
    {
        var configured = Environment.GetEnvironmentVariable(TempRootEnvVar);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Path.GetTempPath();
    }

    private static string ResolveHostTempRoot()
    {
        var configured = Environment.GetEnvironmentVariable(HostTempRootEnvVar);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return ResolveTempRoot();
    }

    private static async Task<string> ReadTextIfExists(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return string.Empty;

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private static string AppendError(string current, string appended)
    {
        if (string.IsNullOrWhiteSpace(appended))
            return current;

        if (string.IsNullOrWhiteSpace(current))
            return appended;

        return current + Environment.NewLine + appended;
    }
}
