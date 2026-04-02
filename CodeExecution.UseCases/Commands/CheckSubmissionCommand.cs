using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeExecution.UseCases.Commands;

public record CheckSubmissionCommand(Guid SubmissionId) : IRequest;

internal class CheckSubmissionCommandHandler(
    IDbContext dbContext,
    ICodeExecutor codeExecutor) : IRequestHandler<CheckSubmissionCommand>
{
    public async Task Handle(CheckSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = await dbContext.CodeSubmissions
            .Where(s => s.Id == request.SubmissionId)
            .Include(s => s.TestCases.OrderBy(tc => tc.OrderIndex))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (submission is not { Status: ExecutionStatus.Running })
            return;

        submission.StartedAt ??= DateTime.UtcNow;

        var overallVerdict = Verdict.OK;
        
        foreach (var testCase in submission.TestCases)
        {
            CodeExecutionResult executionResult;
            try
            {
                executionResult = await codeExecutor.ExecuteCode(
                    submission.Code, testCase.Input, submission.LanguageCode, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                submission.Status = ExecutionStatus.Failed;
                submission.OverallVerdict = Verdict.FailedSystem;
                submission.ErrorMessage = "Конфигурация для языка программирования не найдена";
                submission.CompletedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var actualOutput = executionResult.Output.Trim();
            var expectedOutput = testCase.ExpectedOutput.Trim();
            
            var verdict = DetermineVerdict(
                executionResult, actualOutput, expectedOutput,
                submission.TimeLimitMs, submission.MemoryLimitMb);

            testCase.ActualOutput = actualOutput;
            testCase.Error = executionResult.Error.Trim();
            testCase.ExitCode = executionResult.ExitCode;
            testCase.TimeElapsedMs = executionResult.TimeElapsedMs;
            testCase.MemoryUsedMb = executionResult.MemoryUsageMb;
            testCase.Verdict = verdict;

            if (verdict != Verdict.OK)
            {
                overallVerdict = verdict;
                if (verdict == Verdict.FailedSystem)
                {
                    submission.ErrorMessage = string.IsNullOrWhiteSpace(executionResult.Error)
                        ? "Ошибка инфраструктуры выполнения кода"
                        : executionResult.Error;
                }
                
                break;
            }
        }

        submission.OverallVerdict = overallVerdict;
        submission.CompletedAt = DateTime.UtcNow;
        submission.Status = overallVerdict == Verdict.FailedSystem
            ? ExecutionStatus.Failed
            : ExecutionStatus.Completed;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Verdict DetermineVerdict(
        CodeExecutionResult executionResult,
        string actualOutput,
        string expectedOutput,
        int? maxTimeMs = null,
        int? maxMemoryMb = null)
    {
        if (executionResult.Stage == ExecutionStage.Compilation && executionResult.ExitCode != 0)
            return Verdict.CE;

        if (executionResult.Stage == ExecutionStage.Runtime)
        {
            if ((maxTimeMs.HasValue && executionResult.TimeElapsedMs > maxTimeMs.Value)
                || executionResult.ExitCode == -1)
                return Verdict.TLE;

            if (maxMemoryMb.HasValue && executionResult.MemoryUsageMb > maxMemoryMb.Value)
                return Verdict.MLE;

            if (executionResult.ExitCode != 0)
                return Verdict.RE;

            if (actualOutput != expectedOutput)
                return Verdict.WA;

            return Verdict.OK;
        }

        return Verdict.FailedSystem;
    }
}
