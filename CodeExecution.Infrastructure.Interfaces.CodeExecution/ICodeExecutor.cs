namespace CodeExecution.Infrastructure.Interfaces.CodeExecution;

public interface ICodeExecutor
{
    Task<CodeExecutionResult> ExecuteCode(
        string code,
        string input,
        string language,
        CancellationToken cancellationToken = default);
}