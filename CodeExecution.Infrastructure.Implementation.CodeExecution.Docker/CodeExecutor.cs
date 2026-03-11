using System.Diagnostics;
using System.Globalization;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class CodeExecutor(IExecutorLanguageProvider languageProvider) : ICodeExecutor
{
    private const string TempRootEnvVar = "CODE_EXECUTION_TEMP_ROOT";
    private const string DockerPlatformEnvVar = "CODE_EXECUTION_DOCKER_PLATFORM";

    public async Task<CodeExecutionResult> ExecuteCode(
        string code,
        string input,
        string language,
        CancellationToken cancellationToken = default)
    {
        var langConfig = languageProvider.GetLanguage(language);

        string runCommandRaw;
        var compileCommand = "";

        if (langConfig.IsCompiled)
        {
            compileCommand = langConfig.CompileCommandTemplate
                .Replace("{input}", "/code/" + langConfig.DefaultFileName)
                .Replace("{output}", "/code/program.out");

            runCommandRaw = "/code/program.out";
        }
        else
        {
            runCommandRaw = langConfig.RunCommandTemplate.Replace("{file_path}", "/code/" + langConfig.DefaultFileName);
        }

        var runCommand = $"/usr/bin/time -f 'TIME_ELAPSED:%e\nMEMORY_USAGE:%M' " +
                         $"-o /code/time_output.txt " + runCommandRaw;

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
        var tempDir = await PrepareTempFiles(request, cancellationToken);

        try
        {
            // 1. Компиляция
            if (request.IsCompiled)
            {
                var compileCommand = request.CompileCommand;

                var (cOut, cErr, cExit) = await RunDockerProcess(
                    tempDir, compileCommand, "", request, cancellationToken);

                if (cExit != 0)
                {
                    return new CodeExecutionResult
                    {
                        Output = cOut,
                        Error = cErr,
                        ExitCode = cExit,
                        TimeElapsedMs = 0,
                        MemoryUsageMb = 0,
                        Stage = ExecutionStage.Compilation
                    };
                }
            }

            // 2. Запуск программы
            var runCommand = request.RunCommand;

            var (stdOut, stdErr, exitCode) = await RunDockerProcess(tempDir, runCommand, request.Input, request, cancellationToken);

            var (timeElapsed, memoryUsage) = await ParseTimeOutput(tempDir);

            return new CodeExecutionResult
            {
                Output = stdOut,
                Error = stdErr,
                ExitCode = exitCode,
                TimeElapsedMs = timeElapsed,
                MemoryUsageMb = memoryUsage,
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
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private async Task<string> PrepareTempFiles(ExecuteCodeRequest request, CancellationToken cancellationToken)
    {
        var tempRoot = ResolveTempRoot();
        Directory.CreateDirectory(tempRoot);

        var tempDir = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourceFile = Path.Combine(tempDir, request.DefaultFileName);
        await File.WriteAllTextAsync(sourceFile, request.Code, cancellationToken);

        return tempDir;
    }

    private async Task<(string stdOut, string stdErr, int exitCode)> RunDockerProcess(
        string tempDir,
        string runCommand,
        string input,
        ExecuteCodeRequest request,
        CancellationToken cancellationToken)
    {
        var containerName = $"code_{Guid.NewGuid():N}";
        var platformArg = ResolveDockerPlatformArgument();

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run --rm --name {containerName} -i " +
                        platformArg +
                        $"-v \"{tempDir}:/code\" " +
                        $"--memory={request.MaxMemoryMb}m --cpus={request.MaxCpuCores} " +
                        $"--pids-limit=64 --network none {request.DockerImage} " +
                        $"sh -c \"{runCommand}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = psi;
        process.Start();

        if (!string.IsNullOrEmpty(input))
        {
            await process.StandardInput.WriteAsync(input);
            await process.StandardInput.FlushAsync(cancellationToken);
        }
        process.StandardInput.Close();

        var task = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(request.DefaultTimeoutSeconds * 1000, cancellationToken);

        if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
        {
            await KillContainer(containerName, cancellationToken);
            return ("", "Error: Execution timed out.", -1);
        }

        var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);
        var exitCode = process.ExitCode;

        return (stdOut, stdErr, exitCode);
    }

    private async Task<(double timeElapsed, double memoryUsage)> ParseTimeOutput(string tempDir)
    {
        var timeOutputPath = Path.Combine(tempDir, "time_output.txt");
        if (!File.Exists(timeOutputPath))
            return (0, 0);

        double memoryUsage = 0;
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
                memoryUsage = m / 1024.0;
            }
        }

        return (timeElapsedMs, memoryUsage);
    }

    private async Task KillContainer(string containerName, CancellationToken cancellationToken)
    {
        using var killProcess = new Process();
        killProcess.StartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"kill {containerName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        killProcess.Start();
        await killProcess.WaitForExitAsync(cancellationToken);
    }

    private static string ResolveTempRoot()
    {
        var configured = Environment.GetEnvironmentVariable(TempRootEnvVar);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Path.GetTempPath();
    }

    private static string ResolveDockerPlatformArgument()
    {
        var platform = Environment.GetEnvironmentVariable(DockerPlatformEnvVar);
        if (string.IsNullOrWhiteSpace(platform))
            return string.Empty;

        return $"--platform {platform} ";
    }
}
