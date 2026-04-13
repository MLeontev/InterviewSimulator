using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Verdict = CodeExecution.Domain.Entities.Verdict;

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
                await CompleteAsync(
                    submission, 
                    Verdict.FailedSystem, 
                    "Конфигурация для языка программирования не найдена", 
                    cancellationToken);
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

        await CompleteAsync(submission, overallVerdict, submission.ErrorMessage, cancellationToken);
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
    
    private async Task CompleteAsync(
        CodeSubmission submission, 
        Verdict overallVerdict, 
        string? errorMessage, 
        CancellationToken ct)
    {
        submission.OverallVerdict = overallVerdict;
        submission.CompletedAt = DateTime.UtcNow;
        submission.ErrorMessage = errorMessage;
        submission.Status = overallVerdict == Verdict.FailedSystem
            ? ExecutionStatus.Failed
            : ExecutionStatus.Completed;

        dbContext.AddOutboxMessage(ToIntegrationEvent(submission));
        await dbContext.SaveChangesAsync(ct);
    }
    
    private static CodeSubmissionCompleted ToIntegrationEvent(CodeSubmission submission)
    {
        var testCaseResults = new List<TestCaseResultDto>(submission.TestCases.Count);
        var passedCount = 0;
        
        foreach (var testCase in submission.TestCases)
        {
            var verdict = MapVerdict(testCase.Verdict);

            testCaseResults.Add(new TestCaseResultDto(
                InterviewTestCaseId: testCase.InterviewTestCaseId,
                OrderIndex: testCase.OrderIndex,
                Input: testCase.Input,
                ExpectedOutput: testCase.ExpectedOutput,
                ActualOutput: testCase.ActualOutput ?? string.Empty,
                Error: testCase.Error ?? string.Empty,
                ExitCode: testCase.ExitCode ?? 0,
                TimeElapsedMs: testCase.TimeElapsedMs ?? 0,
                MemoryUsedMb: testCase.MemoryUsedMb ?? 0,
                Verdict: verdict));

            if (testCase.Verdict == Verdict.OK)
                passedCount++;
            else
                break;
        }
        
        var overallVerdict = submission.Status == ExecutionStatus.Failed
            ? IntegrationEvents.Verdict.FailedSystem
            : MapVerdict(submission.OverallVerdict);

        return new CodeSubmissionCompleted(
            SubmissionId: submission.Id,
            InterviewQuestionId: submission.InterviewQuestionId,
            TestCaseResults: testCaseResults,
            OverallVerdict: overallVerdict,
            PassedCount: passedCount,
            TotalTests: submission.TestCases.Count,
            ErrorMessage: submission.ErrorMessage);
    }
    
    private static IntegrationEvents.Verdict MapVerdict(Verdict verdict) =>
        verdict switch
        {
            Verdict.OK => IntegrationEvents.Verdict.OK,
            Verdict.CE => IntegrationEvents.Verdict.CE,
            Verdict.RE => IntegrationEvents.Verdict.RE,
            Verdict.TLE => IntegrationEvents.Verdict.TLE,
            Verdict.MLE => IntegrationEvents.Verdict.MLE,
            Verdict.WA => IntegrationEvents.Verdict.WA,
            _ => IntegrationEvents.Verdict.FailedSystem
        };
}
