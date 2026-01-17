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
            .Include(s => s.TestCases.OrderBy(tc => tc.Order))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (submission == null)
            throw new InvalidOperationException("Submission not found");
        
        submission.StartedAt = DateTime.UtcNow;

        var overallVerdict = Verdict.OK;
        foreach (var testCase in submission.TestCases)
        {
            CodeExecutionResult executionResult;
            try
            {
                executionResult = await codeExecutor.ExecuteCode(
                    submission.Language, submission.Code, testCase.Input, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                submission.Status = ExecutionStatus.Failed;
                submission.ErrorMessage = "Конфигурация для языка программирования не найдена";
                return;
            }
            
            var actualOutput = executionResult.Output.Trim();
            var expectedOutput = testCase.ExpectedOutput.Trim();
            var verdict = DetermineVerdict(
                executionResult, actualOutput, expectedOutput, 
                submission.MaxTimeSeconds, submission.MaxMemoryMb);
            
            testCase.ActualOutput = actualOutput;
            testCase.Error = executionResult.Error.Trim();
            testCase.ExitCode = executionResult.ExitCode;
            testCase.TimeElapsed = executionResult.TimeElapsedMs;
            testCase.MemoryUsage = executionResult.MemoryUsageMb;
            
            if (verdict != Verdict.OK)
            {
                overallVerdict = verdict;
                break;
            }
        }
        
        submission.OverallVerdict = overallVerdict;
        submission.CompletedAt = DateTime.UtcNow;
        submission.Status = ExecutionStatus.Completed;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    private static Verdict DetermineVerdict(
        CodeExecutionResult executionResult, 
        string actualOutput, 
        string expectedOutput,
        int? maxTimeSeconds = null,
        int? maxMemoryMb = null)
    {
        if (executionResult.Stage == ExecutionStage.Compilation && executionResult.ExitCode != 0)
            return Verdict.CE;

        if (executionResult.Stage == ExecutionStage.Runtime)
        {
            if ((maxTimeSeconds.HasValue 
                 && executionResult.TimeElapsedMs > maxTimeSeconds.Value * 1000) 
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

        return Verdict.None;
    }
}