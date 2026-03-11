namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal interface IExecutionRequestStrategy
{
    bool CanHandle(LanguageInfo languageInfo);

    ExecuteCodeRequest Build(string code, string input, LanguageInfo languageInfo);
}

internal abstract class ExecutionRequestStrategyBase : IExecutionRequestStrategy
{
    public abstract bool CanHandle(LanguageInfo languageInfo);

    public ExecuteCodeRequest Build(string code, string input, LanguageInfo languageInfo)
    {
        var runCommandRaw = BuildRunCommandRaw(languageInfo);
        var runCommand = BuildTimedRunCommand(runCommandRaw);
        var compileCommand = BuildCompileCommand(languageInfo);

        return new ExecuteCodeRequest
        {
            Code = code,
            Input = input,
            IsCompiled = languageInfo.IsCompiled,
            DockerImage = languageInfo.DockerImage,
            RunCommand = runCommand,
            CompileCommand = compileCommand,
            DefaultFileName = languageInfo.DefaultFileName,
            DefaultTimeoutSeconds = languageInfo.DefaultTimeoutSeconds,
            MaxMemoryMb = languageInfo.MaxMemoryMb,
            MaxCpuCores = languageInfo.MaxCpuCores
        };
    }

    protected abstract string BuildRunCommandRaw(LanguageInfo languageInfo);

    protected virtual string BuildCompileCommand(LanguageInfo languageInfo) => string.Empty;

    private static string BuildTimedRunCommand(string runCommandRaw)
        => "/usr/bin/time -f 'TIME_ELAPSED:%e\nMEMORY_USAGE:%M' -o /code/time_output.txt " + runCommandRaw;
}

internal sealed class CompiledExecutionRequestStrategy : ExecutionRequestStrategyBase
{
    public override bool CanHandle(LanguageInfo languageInfo) => languageInfo.IsCompiled;

    protected override string BuildRunCommandRaw(LanguageInfo languageInfo) => "/code/program.out";

    protected override string BuildCompileCommand(LanguageInfo languageInfo)
        => languageInfo.CompileCommandTemplate
            .Replace("{input}", "/code/" + languageInfo.DefaultFileName)
            .Replace("{output}", "/code/program.out");
}

internal sealed class InterpretedExecutionRequestStrategy : ExecutionRequestStrategyBase
{
    public override bool CanHandle(LanguageInfo languageInfo) => !languageInfo.IsCompiled;

    protected override string BuildRunCommandRaw(LanguageInfo languageInfo)
        => languageInfo.RunCommandTemplate.Replace("{file_path}", "/code/" + languageInfo.DefaultFileName);
}