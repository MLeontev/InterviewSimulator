namespace CodeExecution.Infrastructure.Interfaces.CodeExecution;

public class CodeExecutionResult
{
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public double TimeElapsedMs { get; set; }
    public double MemoryUsageMb { get; set; }
    public ExecutionStage Stage { get; set; } = ExecutionStage.None;
}