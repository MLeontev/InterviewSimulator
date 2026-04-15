using System.Collections.Concurrent;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;

namespace Backend.IntegrationTests.Infrastructure.Fakes;

public sealed class FakeCodeExecutor : ICodeExecutor
{
    private readonly ConcurrentQueue<Func<CodeExecutionResult>> _queuedResults = new();

    public void Reset()
    {
        while (_queuedResults.TryDequeue(out _))
        {
        }
    }

    public void EnqueueResult(CodeExecutionResult result)
    {
        _queuedResults.Enqueue(() => result);
    }

    public void EnqueueException(Exception exception)
    {
        _queuedResults.Enqueue(() => throw exception);
    }

    public Task<CodeExecutionResult> ExecuteCode(
        string code,
        string input,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (_queuedResults.TryDequeue(out var factory))
            return Task.FromResult(factory());

        return Task.FromResult(new CodeExecutionResult
        {
            Output = string.Empty,
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 0,
            MemoryUsageMb = 0,
            Stage = ExecutionStage.Runtime
        });
    }
}
