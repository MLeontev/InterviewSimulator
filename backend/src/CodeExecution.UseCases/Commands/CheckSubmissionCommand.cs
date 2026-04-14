using CodeExecution.Domain.Entities;
using CodeExecution.Domain.Enums;
using CodeExecution.Domain.Policies;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ExecutionStage = CodeExecution.Domain.Enums.ExecutionStage;
using InfrastructureExecutionStage = CodeExecution.Infrastructure.Interfaces.CodeExecution.ExecutionStage;
using Verdict = CodeExecution.Domain.Enums.Verdict;

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

        submission.MarkStarted(DateTime.UtcNow);

        var overallVerdict = Verdict.OK;
        string? errorMessage = null;
        
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
            
            var verdict = CodeSubmissionVerdictPolicy.Resolve(
                MapExecutionStage(executionResult.Stage),
                executionResult.ExitCode,
                executionResult.TimeElapsedMs,
                executionResult.MemoryUsageMb,
                actualOutput,
                expectedOutput,
                submission.TimeLimitMs,
                submission.MemoryLimitMb);

            testCase.ApplyExecutionResult(
                actualOutput,
                executionResult.Error.Trim(),
                executionResult.ExitCode,
                executionResult.TimeElapsedMs,
                executionResult.MemoryUsageMb,
                verdict);

            if (verdict != Verdict.OK)
            {
                overallVerdict = verdict;
                if (verdict == Verdict.FailedSystem)
                {
                    errorMessage = string.IsNullOrWhiteSpace(executionResult.Error)
                        ? "Ошибка инфраструктуры выполнения кода"
                        : executionResult.Error;
                }
                
                break;
            }
        }

        await CompleteAsync(submission, overallVerdict, errorMessage, cancellationToken);
    }

    private async Task CompleteAsync(
        CodeSubmission submission, 
        Verdict overallVerdict, 
        string? errorMessage, 
        CancellationToken ct)
    {
        submission.Complete(overallVerdict, errorMessage, DateTime.UtcNow);

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

    private static ExecutionStage MapExecutionStage(InfrastructureExecutionStage stage) =>
        stage switch
        {
            InfrastructureExecutionStage.Compilation => ExecutionStage.Compilation,
            InfrastructureExecutionStage.Runtime => ExecutionStage.Runtime,
            _ => ExecutionStage.None
        };
    
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
